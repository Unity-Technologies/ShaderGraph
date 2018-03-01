using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    // a structure used to track active variable dependencies in the shader code
    // (i.e. the use of uv0 in the pixel shader means we need a uv0 interpolator, etc.)
    struct Dependency
    {
        public string name;             // the name of the thing
        public string dependsOn;        // the thing above depends on this -- it reads it / calls it / requires it to be defined

        public Dependency(string name, string dependsOn)
        {
            this.name = name;
            this.dependsOn = dependsOn;
        }
    };

    // attribute used to flag a field as needing an HLSL semantic applied
    // i.e.    float3 position : POSITION;
    //                           ^ semantic
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class Semantic : System.Attribute
    {
        public string semantic;

        public Semantic(string semantic)
        {
            this.semantic = semantic;
        }
    }

    // attribute used to flag a field as being optional
    // i.e. if it is not active, then we can omit it from the struct
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class Optional : System.Attribute
    {
        public Optional()
        {
        }
    }

    static class ShaderSpliceUtil
    {
        private static int GetVectorCount(System.Type type)
        {
            if (type.Name.Equals("Vector4"))
            {
                return 4;
            }
            else if (type.Name.Equals("Vector3"))
            {
                return 3;
            }
            else if (type.Name.Equals("Vector2"))
            {
                return 2;
            }
            else if (type.Name.Equals("float"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private static string[] vectorTypeNames =
        {
            "unknown",
            "float",
            "float2",
            "float3",
            "float4"
        };

        private static string ConvertFieldType(System.Type type)
        {
            int vectorCount = GetVectorCount(type);
            return vectorTypeNames[vectorCount];
        }

        private static char[] channelNames =
        { 'x', 'y', 'z', 'w' };

        private static string GetChannelSwizzle(int firstChannel, int channelCount)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            int lastChannel = System.Math.Min(firstChannel + channelCount - 1, 4);
            for (int index = firstChannel; index <= lastChannel; index++)
            {
                result.Append(channelNames[index]);
            }
            return result.ToString();
        }

        public static string BuildType(System.Type t, HashSet<string> activeFields)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();

            result.Append("struct ");
            result.Append(t.Name);
            result.Append(" {\n");

            foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.MemberType == MemberTypes.Field)
                {
                    bool isOptional = field.IsDefined(typeof(Optional), false);
                    if (isOptional)
                    {
                        string fullName = t.Name + "." + field.Name;
                        if (!activeFields.Contains(fullName))
                        {
                            // not active, skip the optional field
                            continue;
                        }
                    }
                    result.Append("\t");
                    result.Append(ConvertFieldType(field.FieldType));
                    result.Append(" ");
                    result.Append(field.Name);

                    object[] semantics = field.GetCustomAttributes(typeof(Semantic), false);
                    if (semantics.Length > 0)
                    {
                        Semantic first = (Semantic)semantics[0];
                        result.Append(" : ");
                        result.Append(first.semantic);
                    }

                    result.Append(";");

                    if (isOptional)
                    {
                        result.Append(" // optional");
                    }
                    result.Append("\n");
                }
            }
            result.Append("};\n");

            return result.ToString();
        }

        public static string BuildPackedType(System.Type unpacked, HashSet<string> activeFields)
        {
            // for each interpolator, the number of components used (up to 4 for a float4 interpolator)
            List<int> packedCounts = new List<int>();

            System.Text.StringBuilder result = new System.Text.StringBuilder();
            System.Text.StringBuilder packer = new System.Text.StringBuilder();
            System.Text.StringBuilder unpacker = new System.Text.StringBuilder();

            string unpackedName = unpacked.Name.ToString();
            string packedName = "Packed" + unpacked.Name;
            string packerName = "Pack" + unpacked.Name;
            string unpackerName = "Unpack" + unpacked.Name;

            // declare struct header
            result.Append("struct ");
            result.Append(packedName);
            result.Append(" {\n");

            // declare function headers
            //        packer.AppendFormat("{0} {1}({2} input)\n{", packedName, packerName, unpackedName);
            packer.Append(packedName);
            packer.Append(" ");
            packer.Append(packerName);
            packer.Append("(");
            packer.Append(unpackedName);
            packer.Append(" input");
            packer.Append(")\n{\n");
            packer.AppendFormat("    {0} output;\n", packedName);

            //        unpacker.AppendFormat("{0} {1}({2} input)\n{", unpackedName, unpackerName, packedName);
            unpacker.Append(unpackedName);
            unpacker.Append(" ");
            unpacker.Append(unpackerName);
            unpacker.Append("(");
            unpacker.Append(packedName);
            unpacker.Append(" input");
            unpacker.Append(")\n{\n");
            unpacker.AppendFormat("    {0} output;\n", unpackedName);

            // TODO: this could do a better job packing
            // especially if we allowed breaking up fields to pack them into remaining space...
            // would want to minimize the use of it,
            // basically limit it to a final optimization if it reduces total number of interpolators declared
            foreach (FieldInfo field in unpacked.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.MemberType == MemberTypes.Field)
                {
                    bool isOptional = field.IsDefined(typeof(Optional), false);
                    if (isOptional)
                    {
                        string fullName = unpacked.Name + "." + field.Name;
                        if (!activeFields.Contains(fullName))
                        {
                            // not active, skip the optional field
                            continue;
                        }
                    }

                    Semantic semantic = null;
                    object[] semantics = field.GetCustomAttributes(typeof(Semantic), false);
                    if (semantics.Length > 0)
                    {
                        semantic = (Semantic)semantics[0];
                    }

                    if (semantic != null)
                    {
                        // not a packed value -- has an explicit bound semantic
                        int vectorCount = GetVectorCount(field.FieldType);
                        result.AppendFormat("    {0} {1} : {2};    // unpacked (explicit semantic)\n", vectorTypeNames[vectorCount], field.Name, semantic.semantic);
                        packer.AppendFormat("    output.{0} = input.{0};\n", field.Name);
                        unpacker.AppendFormat("    output.{0} = input.{0};\n", field.Name);
                    }
                    else
                    {
                        // pack field
                        int vectorCount = GetVectorCount(field.FieldType);
                        int interpIndex = packedCounts.FindIndex(x => (x + vectorCount <= 4));      // super cheap way to pack: first slot that fits the whole value
                        int firstChannel;
                        if (interpIndex < 0)
                        {
                            // allocate a new interpolator
                            interpIndex = packedCounts.Count;
                            firstChannel = 0;
                            packedCounts.Add(vectorCount);
                        }
                        else
                        {
                            // pack into existing interpolator
                            firstChannel = packedCounts[interpIndex];
                            packedCounts[interpIndex] += vectorCount;
                        }

                        // add code to packer and unpacker
                        string channels = GetChannelSwizzle(firstChannel, vectorCount);
                        packer.AppendFormat("    output.interp{0:00}.{1} = input.{2};\n", interpIndex, channels, field.Name);
                        unpacker.AppendFormat("    output.{0} = input.interp{1:00}.{2};\n", field.Name, interpIndex, channels);
                    }
                }
            }

            // create packed structure from packedCounts
            for (int index = 0; index < packedCounts.Count; index++)
            {
                int count = packedCounts[index];
                result.AppendFormat("    {0} interp{1:00} : TEXCOORD{1};    // auto-packed\n", vectorTypeNames[count], index);
            }

            // close declarations
            result.Append("};\n\n");
            packer.Append("    return output;\n}\n\n");
            unpacker.Append("    return output;\n}\n\n");

            result.Append(packer);
            result.Append(unpacker);

            return result.ToString();
        }

        // an easier to use version of substring Append() -- explicit inclusion on each end, and checks for positive length
        private static void AppendSubstring(System.Text.StringBuilder target, string str, int start, bool includeStart, int end, bool includeEnd)
        {
            if (!includeStart)
            {
                start++;
            }
            if (!includeEnd)
            {
                end--;
            }
            int count = end - start + 1;
            if (count > 0)
            {
                target.Append(str, start, count);
            }
        }

        public static System.Text.StringBuilder PreprocessShaderCode(string code, HashSet<string> activeFields, Dictionary<string, string> namedFragments = null, System.Text.StringBuilder result = null)
        {
            if (result == null)
            {
                result = new System.Text.StringBuilder();
            }
            int cur = 0;
            int end = code.Length;

            while (cur < end)
            {
                int dollar = code.IndexOf('$', cur);
                if (dollar < 0)
                {
                    // no escape sequence found -- just append the remaining part of the code verbatim
                    AppendSubstring(result, code, cur, true, end, false);
                    cur = end;
                }
                else
                {
                    // found $ escape sequence

                    // first append everything before the beginning of the escape sequence
                    AppendSubstring(result, code, cur, true, dollar, false);

                    // next find the end of the line (or if none found, the end of the code)
                    int endln = code.IndexOf('\n', dollar + 1);
                    if (endln < 0)
                    {
                        endln = end;
                    }

                    // see if the character after '$' is '{', which would indicate a named fragment splice
                    if ((dollar + 1 < endln) && (code[dollar + 1] == '{'))
                    {
                        // named fragment splice
                        // search for the '}' within the current line
                        int curlystart = dollar + 1;
                        int curlyend = -1;
                        if (endln > curlystart + 1)
                        {
                            curlyend = code.IndexOf('}', curlystart + 1, endln - curlystart - 1);
                        }

                        int nameLength = curlyend - dollar + 1;
                        if ((curlyend < 0) || (nameLength <= 0))
                        {
                            // no } found, or zero length name
                            if (curlyend < 0)
                            {
                                result.Append("// ERROR: unterminated escape sequence ('${' and '}' must be matched)\n");
                            }
                            else
                            {
                                result.Append("// ERROR: name '${}' is empty\n");
                            }

                            // append the line (commented out) for context
                            result.Append("//    ");
                            AppendSubstring(result, code, dollar, true, endln, false);
                            result.Append("\n");
                        }
                        else
                        {
                            // } found!
                            // ugh, this probably allocates memory -- wish we could do the name lookup direct from a substring
                            string name = code.Substring(dollar, nameLength);

                            string fragment;
                            if ((namedFragments != null) && namedFragments.TryGetValue(name, out fragment))
                            {
                                // splice the fragment
                                result.Append(fragment);
                                // advance to just after the '}'
                                cur = curlyend + 1;
                            }
                            else
                            {
                                // no named fragment found
                                result.AppendFormat("/* Could not find named fragment '{0}' */", name);
                                cur = curlyend + 1;
                            }
                        }
                    }
                    else
                    {
                        // it's a predicate
                        // search for the colon within the current line
                        int colon = -1;
                        if (endln > dollar + 1)
                        {
                            colon = code.IndexOf(':', dollar + 1, endln - dollar - 1);
                        }

                        int predicateLength = colon - dollar - 1;
                        if ((colon < 0) || (predicateLength <= 0))
                        {
                            // no colon found... error!  Spit out error and context
                            if (colon < 0)
                            {
                                result.Append("// ERROR: unterminated escape sequence ('$' and ':' must be matched)\n");
                            }
                            else
                            {
                                result.Append("// ERROR: predicate is zero length\n");
                            }

                            // append the line (commented out) for context
                            result.Append("//    ");
                            AppendSubstring(result, code, dollar, true, endln, false);
                        }
                        else
                        {
                            // colon found!
                            // ugh, this probably allocates memory -- wish we could do the field lookup direct from a substring
                            string predicate = code.Substring(dollar + 1, predicateLength);

                            if (activeFields.Contains(predicate))
                            {
                                // predicate is active, append the line
                                result.Append(' ', predicateLength + 2);
                                AppendSubstring(result, code, colon, false, endln, false);
                            }
                            else
                            {
                                // predicate is not active -- comment out line
                                result.Append("//");
                                result.Append(' ', predicateLength);
                                AppendSubstring(result, code, colon, false, endln, false);
                            }
                        }
                        cur = endln + 1;
                    }
                }
            }

            return result;
        }

        public static void ApplyDependencies(HashSet<string> activeFields, List<Dependency[]> dependsList)
        {
            // add active fields to queue
            Queue<string> fieldsToPropagate = new Queue<string>();
            foreach (string f in activeFields)
            {
                fieldsToPropagate.Enqueue(f);
            }

            // foreach field in queue:
            while (fieldsToPropagate.Count > 0)
            {
                string field = fieldsToPropagate.Dequeue();
                if (activeFields.Contains(field))           // this should always be true
                {
                    // find all dependencies of field that are not already active
                    foreach (Dependency[] dependArray in dependsList)
                    {
                        foreach (Dependency d in dependArray.Where(d => (d.name == field) && !activeFields.Contains(d.dependsOn)))
                        {
                            // activate them and add them to the queue
                            activeFields.Add(d.dependsOn);
                            fieldsToPropagate.Enqueue(d.dependsOn);
                        }
                    }
                }
            }
        }
    };

    static class GraphUtil
    {
        internal static void GenerateApplicationVertexInputs(ShaderGraphRequirements graphRequiements, ShaderGenerator vertexInputs, int vertexInputStartIndex, int maxVertexInputs)
        {
            int vertexInputIndex = vertexInputStartIndex;

            vertexInputs.AddShaderChunk("struct GraphVertexInput", false);
            vertexInputs.AddShaderChunk("{", false);
            vertexInputs.Indent();
            vertexInputs.AddShaderChunk("float4 vertex : POSITION;", false);
            vertexInputs.AddShaderChunk("float3 normal : NORMAL;", false);
            vertexInputs.AddShaderChunk("float4 tangent : TANGENT;", false);

            if (graphRequiements.requiresVertexColor)
            {
                vertexInputs.AddShaderChunk("float4 color : COLOR;", false);
            }

            foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
            {
                vertexInputs.AddShaderChunk(String.Format("float4 texcoord{0} : TEXCOORD{1};", ((int)channel).ToString(), vertexInputIndex.ToString()), false);
                vertexInputIndex++;
            }

            vertexInputs.Deindent();
            vertexInputs.AddShaderChunk("};", false);
        }

        static void Visit(List<INode> outputList, Dictionary<Guid, INode> unmarkedNodes, INode node)
        {
            if (!unmarkedNodes.ContainsKey(node.guid))
                return;
            foreach (var slot in node.GetInputSlots<ISlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    Visit(outputList, unmarkedNodes, inputNode);
                }
            }
            unmarkedNodes.Remove(node.guid);
            outputList.Add(node);
        }

        public static GenerationResults GetShader(this AbstractMaterialGraph graph, AbstractMaterialNode node, GenerationMode mode, string name)
        {
            var results = new GenerationResults();
            bool isUber = node == null;

            var vertexInputs = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var functionBuilder = new ShaderStringBuilder();
            var functionRegistry = new FunctionRegistry(functionBuilder);
            var surfaceInputs = new ShaderGenerator();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            if (isUber)
            {
                var unmarkedNodes = graph.GetNodes<INode>().Where(x => !(x is IMasterNode)).ToDictionary(x => x.guid);
                while (unmarkedNodes.Any())
                {
                    var unmarkedNode = unmarkedNodes.FirstOrDefault();
                    Visit(activeNodeList, unmarkedNodes, unmarkedNode.Value);
                }
            }
            else
            {
                NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);
            }

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList);
            GenerateApplicationVertexInputs(requirements, vertexInputs, 0, 8);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

            if (requirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(String.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (requirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(String.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            results.previewMode = PreviewMode.Preview3D;
            if (!isUber)
            {
                foreach (var pNode in activeNodeList.OfType<AbstractMaterialNode>())
                {
                    if (pNode.previewMode == PreviewMode.Preview3D)
                    {
                        results.previewMode = PreviewMode.Preview3D;
                        break;
                    }
                }
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(String.Format("half4 {0};", channel.GetUVName()), false);

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            vertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
            vertexShader.Indent();
            vertexShader.AddShaderChunk("return v;", false);
            vertexShader.Deindent();
            vertexShader.AddShaderChunk("}", false);

            var slots = new List<MaterialSlot>();
            foreach (var activeNode in isUber ? activeNodeList.Where(n => ((AbstractMaterialNode)n).hasPreview) : ((INode)node).ToEnumerable())
            {
                if (activeNode is IMasterNode)
                    slots.AddRange(activeNode.GetInputSlots<MaterialSlot>());
                else
                    slots.AddRange(activeNode.GetOutputSlots<MaterialSlot>());
            }
            GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, !isUber);

            var shaderProperties = new PropertyCollector();
            results.outputIdProperty = new FloatShaderProperty
            {
                displayName = "OutputId",
                generatePropertyBlock = false,
                value = -1
            };
            if (isUber)
                shaderProperties.AddShaderProperty(results.outputIdProperty);

            GenerateSurfaceDescription(
                activeNodeList,
                node,
                graph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                requirements,
                mode,
                outputIdProperty: results.outputIdProperty);

            var finalBuilder = new ShaderStringBuilder();
            finalBuilder.AppendLine(@"Shader ""{0}""", name);
            using (finalBuilder.BlockScope())
            {
                finalBuilder.AppendLine("Properties");
                using (finalBuilder.BlockScope())
                {
                    finalBuilder.AppendLines(shaderProperties.GetPropertiesBlock(0));
                }

                finalBuilder.AppendLine(@"HLSLINCLUDE");
                finalBuilder.AppendLine("#define USE_LEGACY_UNITY_MATRIX_VARIABLES");
                finalBuilder.AppendLine(@"#include ""CoreRP/ShaderLibrary/Common.hlsl""");
                finalBuilder.AppendLine(@"#include ""CoreRP/ShaderLibrary/Packing.hlsl""");
                finalBuilder.AppendLine(@"#include ""CoreRP/ShaderLibrary/Color.hlsl""");
                finalBuilder.AppendLine(@"#include ""ShaderGraphLibrary/Functions.hlsl""");
                finalBuilder.AppendLine(@"#include ""ShaderGraphLibrary/ShaderVariables.hlsl""");
                finalBuilder.AppendLine(@"#include ""ShaderGraphLibrary/ShaderVariablesFunctions.hlsl""");

                finalBuilder.Concat(functionBuilder);
                finalBuilder.AppendLines(vertexInputs.GetShaderString(0));
                finalBuilder.AppendLines(surfaceInputs.GetShaderString(0));
                finalBuilder.AppendLines(surfaceDescriptionStruct.GetShaderString(0));
                finalBuilder.AppendLines(shaderProperties.GetPropertiesDeclaration(0));
                finalBuilder.AppendLines(vertexShader.GetShaderString(0));
                finalBuilder.AppendLines(surfaceDescriptionFunction.GetShaderString(0));
                finalBuilder.AppendLine(@"ENDHLSL");

                finalBuilder.AppendLines(ShaderGenerator.GetPreviewSubShader(node, requirements));
                ListPool<INode>.Release(activeNodeList);
            }

            results.configuredTextures = shaderProperties.GetConfiguredTexutres();
            ShaderSourceMap sourceMap;
            results.shader = finalBuilder.ToString(out sourceMap);
            results.sourceMap = sourceMap;
            return results;
        }

        internal static void GenerateSurfaceDescriptionStruct(ShaderGenerator surfaceDescriptionStruct, List<MaterialSlot> slots, bool isMaster, string structName = "SurfaceDescription")
        {
            surfaceDescriptionStruct.AddShaderChunk(String.Format("struct {0}{{", structName), false);
            surfaceDescriptionStruct.Indent();
            if (isMaster)
            {
                foreach (var slot in slots)
                {
                    if (slot != null)
                    {
                        surfaceDescriptionStruct.AddShaderChunk(String.Format("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision.@float, slot.concreteValueType), NodeUtils.GetHLSLSafeName(slot.shaderOutputName)), false);
                    }
                }
                surfaceDescriptionStruct.Deindent();
            }
            else
            {
                surfaceDescriptionStruct.AddShaderChunk("float4 PreviewOutput;", false);
            }
            surfaceDescriptionStruct.Deindent();
            surfaceDescriptionStruct.AddShaderChunk("};", false);
        }

        internal static void GenerateSurfaceDescription(
            List<INode> activeNodeList,
            AbstractMaterialNode masterNode,
            AbstractMaterialGraph graph,
            ShaderGenerator surfaceDescriptionFunction,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            ShaderGraphRequirements requirements,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            FloatShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null,
            string graphInputStructName = "SurfaceInputs")
        {
            if (graph == null)
                return;

            surfaceDescriptionFunction.AddShaderChunk(String.Format("{0} {1}({2} IN) {{", surfaceDescriptionName, functionName, graphInputStructName), false);
            surfaceDescriptionFunction.Indent();
            surfaceDescriptionFunction.AddShaderChunk(String.Format("{0} surface = ({0})0;", surfaceDescriptionName), false);

            foreach (CoordinateSpace space in Enum.GetValues(typeof(CoordinateSpace)))
            {
                var neededCoordinateSpace = space.ToNeededCoordinateSpace();
                if ((requirements.requiresNormal & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Normal)), false);
                if ((requirements.requiresTangent & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Tangent)), false);
                if ((requirements.requiresBitangent & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.BiTangent)), false);
                if ((requirements.requiresViewDir & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.ViewDirection)), false);
                if ((requirements.requiresPosition & neededCoordinateSpace) > 0)
                    surfaceDescriptionFunction.AddShaderChunk(String.Format("float3 {0} = IN.{0};", space.ToVariableName(InterpolatorType.Position)), false);
            }

            if (requirements.requiresScreenPosition)
                surfaceDescriptionFunction.AddShaderChunk(String.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
            if (requirements.requiresVertexColor)
                surfaceDescriptionFunction.AddShaderChunk(String.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceDescriptionFunction.AddShaderChunk(String.Format("half4 {0} = IN.{0};", channel.GetUVName()), false);

            graph.CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (activeNode is IGeneratesFunction)
                {
                    functionRegistry.builder.currentNode = activeNode;
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(functionRegistry, mode);
                }
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(surfaceDescriptionFunction, mode);
                if (masterNode == null && activeNode.hasPreview)
                {
                    var outputSlot = activeNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                    if (outputSlot != null)
                        surfaceDescriptionFunction.AddShaderChunk(String.Format("if ({0} == {1}) {{ surface.PreviewOutput = {2}; return surface; }}", outputIdProperty.referenceName, activeNode.tempId.index, ShaderGenerator.AdaptNodeOutputForPreview(activeNode, outputSlot.id, activeNode.GetVariableNameForSlot(outputSlot.id))), false);
                }

                activeNode.CollectShaderProperties(shaderProperties, mode);
            }
            functionRegistry.builder.currentNode = null;

            if (masterNode != null)
            {
                if (masterNode is IMasterNode)
                {
                    var usedSlots = slots ?? masterNode.GetInputSlots<MaterialSlot>();
                    foreach (var input in usedSlots)
                    {
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        if (foundEdges.Any())
                        {
                            var outputRef = foundEdges[0].outputSlot;
                            var fromNode = graph.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                            surfaceDescriptionFunction.AddShaderChunk(String.Format("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), fromNode.GetVariableNameForSlot(outputRef.slotId)), true);
                        }
                        else
                        {
                            surfaceDescriptionFunction.AddShaderChunk(String.Format("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(input.shaderOutputName), input.GetDefaultValue(mode)), true);
                        }
                    }
                }
                else if (masterNode.hasPreview)
                {
                    foreach (var slot in masterNode.GetOutputSlots<MaterialSlot>())
                        surfaceDescriptionFunction.AddShaderChunk(String.Format("surface.{0} = {1};", NodeUtils.GetHLSLSafeName(slot.shaderOutputName), masterNode.GetVariableNameForSlot(slot.id)), true);
                }
            }

            surfaceDescriptionFunction.AddShaderChunk("return surface;", false);
            surfaceDescriptionFunction.Deindent();
            surfaceDescriptionFunction.AddShaderChunk("}", false);
        }

        public static GenerationResults GetPreviewShader(this AbstractMaterialGraph graph, AbstractMaterialNode node)
        {
            return graph.GetShader(node, GenerationMode.Preview, String.Format("hidden/preview/{0}", node.GetVariableNameForNode()));
        }

        public static GenerationResults GetUberPreviewShader(this AbstractMaterialGraph graph)
        {
            return graph.GetShader(null, GenerationMode.Preview, "hidden/preview");
        }

        static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> s_LegacyTypeRemapping;

        public static Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            if (s_LegacyTypeRemapping == null)
            {
                s_LegacyTypeRemapping = new Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract)
                            continue;
                        foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                        {
                            var legacyAttribute = (FormerNameAttribute)attribute;
                            var serializationInfo = new SerializationHelper.TypeSerializationInfo { fullName = legacyAttribute.fullName };
                            s_LegacyTypeRemapping[serializationInfo] = SerializationHelper.GetTypeSerializableAsString(type);
                        }
                    }
                }
            }

            return s_LegacyTypeRemapping;
        }
    }
}
