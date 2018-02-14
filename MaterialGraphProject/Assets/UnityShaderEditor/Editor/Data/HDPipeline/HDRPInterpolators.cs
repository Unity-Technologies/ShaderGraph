using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;              // Vector3,4
using System.Reflection;


namespace UnityEditor.ShaderGraph
{
    public class HDRPInterpolators
    {
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

        [System.AttributeUsage(System.AttributeTargets.Field)]
        public class Semantic : System.Attribute
        {
            public string semantic;

            public Semantic(string semantic)
            {
                this.semantic = semantic;
            }
        }

        [System.AttributeUsage(System.AttributeTargets.Field)]
        public class Optional : System.Attribute
        {
            public Optional()
            {
            }
        }

        struct AttributesMesh
        {
            [Semantic("POSITION")]              Vector3 positionOS;
            [Semantic("NORMAL")] [Optional]     Vector3 normalOS;
            [Semantic("TANGENT")] [Optional]    Vector4 tangentOS;      // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")] [Optional]  Vector2 uv0;
            [Semantic("TEXCOORD1")] [Optional]  Vector2 uv1;
            [Semantic("TEXCOORD2")] [Optional]  Vector2 uv2;
            [Semantic("TEXCOORD3")] [Optional]  Vector2 uv3;
            [Semantic("COLOR")] [Optional]      Vector4 color;
        };

        struct VaryingsMeshToPS
        {
            [Semantic("SV_Position")]           Vector4 positionCS;
            [Optional]      Vector3 positionWS;
            [Optional]      Vector3 normalWS;
            [Optional]      Vector4 tangentWS;      // w contain mirror sign
            [Optional]      Vector2 texCoord0;
            [Optional]      Vector2 texCoord1;
            [Optional]      Vector2 texCoord2;
            [Optional]      Vector2 texCoord3;
            [Optional]      Vector4 color;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionWS",       "VaryingsMeshToDS.positionWS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "VaryingsMeshToDS.normalWS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "VaryingsMeshToDS.tangentWS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "VaryingsMeshToDS.texCoord0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "VaryingsMeshToDS.texCoord1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "VaryingsMeshToDS.texCoord2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "VaryingsMeshToDS.texCoord3"),
                new Dependency("VaryingsMeshToPS.color",            "VaryingsMeshToDS.color"),
            };

            public static Dependency[] standardDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToPS.positionWS",       "AttributesMesh.positionOS"),
                new Dependency("VaryingsMeshToPS.normalWS",         "AttributesMesh.normalOS"),
                new Dependency("VaryingsMeshToPS.tangentWS",        "AttributesMesh.tangentOS"),
                new Dependency("VaryingsMeshToPS.texCoord0",        "AttributesMesh.uv0"),
                new Dependency("VaryingsMeshToPS.texCoord1",        "AttributesMesh.uv1"),
                new Dependency("VaryingsMeshToPS.texCoord2",        "AttributesMesh.uv2"),
                new Dependency("VaryingsMeshToPS.texCoord3",        "AttributesMesh.uv3"),
                new Dependency("VaryingsMeshToPS.color",            "AttributesMesh.color"),
            };
        };

        struct VaryingsMeshToDS
        {
                            Vector3 positionWS;
                            Vector3 normalWS;
            [Optional]      Vector4 tangentWS;
            [Optional]      Vector2 texCoord0;
            [Optional]      Vector2 texCoord1;
            [Optional]      Vector2 texCoord2;
            [Optional]      Vector2 texCoord3;
            [Optional]      Vector4 color;

            public static Dependency[] tessellationDependencies = new Dependency[]
            {
                new Dependency("VaryingsMeshToDS.tangentWS",        "VaryingsMeshToPS.tangentWS"),
                new Dependency("VaryingsMeshToDS.texCoord0",        "VaryingsMeshToPS.texCoord0"),
                new Dependency("VaryingsMeshToDS.texCoord1",        "VaryingsMeshToPS.texCoord1"),
                new Dependency("VaryingsMeshToDS.texCoord2",        "VaryingsMeshToPS.texCoord2"),
                new Dependency("VaryingsMeshToDS.texCoord3",        "VaryingsMeshToPS.texCoord3"),
                new Dependency("VaryingsMeshToDS.color",            "VaryingsMeshToPS.color"),
            };
        };

        struct FragInputs
        {
            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("FragInputs.positionWS",         "VaryingsMeshToPS.positionWS"),
                new Dependency("FragInputs.worldToTangent",     "VaryingsMeshToPS.tangentWS"),
                new Dependency("FragInputs.worldToTangent",     "VaryingsMeshToPS.normalWS"),
                new Dependency("FragInputs.texCoord0",          "VaryingsMeshToPS.texCoord0"),
                new Dependency("FragInputs.texCoord1",          "VaryingsMeshToPS.texCoord1"),
                new Dependency("FragInputs.texCoord2",          "VaryingsMeshToPS.texCoord2"),
                new Dependency("FragInputs.texCoord3",          "VaryingsMeshToPS.texCoord3"),
                new Dependency("FragInputs.color",              "VaryingsMeshToPS.color"),
                new Dependency("FragInputs.isFrontFace",        "VaryingsMeshToPS.cullFace"),
            };
        };

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

        private static string BuildType(System.Type t, HashSet<string> activeFields)
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

        private static string BuildPackedType(System.Type unpacked, HashSet<string> activeFields)
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
                        semantic = (Semantic) semantics[0];
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

        /*
        private static string function_AttributesMeshToVaryingsMeshToPS =
@"            
        VaryingsMeshToPS AttributesMeshToVaryingsMeshToPS(AttributesMesh input)
        {
            VaryingsMeshToPS output;

$VaryingsMeshToPS.positionWS:       output.positionWS = GetCameraRelativePositionWS(positionWS);

            return output;
        }
";      */

        // this translates the new dependency tracker into the old preprocessor definitions for the existing shader code
        private static string defines_ActiveFields =
@"
$AttributesMesh.normalOS:               #define ATTRIBUTES_NEED_NORMAL
$AttributesMesh.tangentOS:              #define ATTRIBUTES_NEED_TANGENT
$AttributesMesh.uv0:                    #define ATTRIBUTES_NEED_TEXCOORD0
$AttributesMesh.uv1:                    #define ATTRIBUTES_NEED_TEXCOORD1
$AttributesMesh.uv2:                    #define ATTRIBUTES_NEED_TEXCOORD2
$AttributesMesh.uv3:                    #define ATTRIBUTES_NEED_TEXCOORD3
$AttributesMesh.color:                  #define ATTRIBUTES_NEED_COLOR
$VaryingsMeshToPS.positionWS:           #define VARYINGS_NEED_POSITION_WS
$VaryingsMeshToPS.normalWS:             #define VARYINGS_NEED_TANGENT_TO_WORLD
$VaryingsMeshToPS.texCoord0:            #define VARYINGS_NEED_TEXCOORD0
$VaryingsMeshToPS.texCoord1:            #define VARYINGS_NEED_TEXCOORD1
$VaryingsMeshToPS.texCoord2:            #define VARYINGS_NEED_TEXCOORD2
$VaryingsMeshToPS.texCoord3:            #define VARYINGS_NEED_TEXCOORD3
$VaryingsMeshToPS.color:                #define VARYINGS_NEED_COLOR
";

        private static string function_BuildFragInputs =
@"
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);

            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.worldToTangent = k_identity3x3;
             output.positionSS = input.positionCS;       // input.positionCS is SV_Position

$FragInputs.positionWS:         output.positionWS = input.positionWS;
$FragInputs.worldToTangent:     output.worldToTangent = BuildWorldToTangent(input.tangentWS, input.normalWS);
$FragInputs.texCoord0:          output.texCoord0 = input.texCoord0;
$FragInputs.texCoord1:          output.texCoord1 = input.texCoord1;
$FragInputs.texCoord2:          output.texCoord2 = input.texCoord2;
$FragInputs.texCoord3:          output.texCoord3 = input.texCoord3;
$FragInputs.color:              output.color = input.color;
$FragInputs.isFrontFace:        output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);       // TODO: SHADER_STAGE_FRAGMENT only
$FragInputs.isFrontFace:        // Handle handness of the view matrix (In Unity view matrix default to a determinant of -1)
$FragInputs.isFrontFace:        // when we render a cubemap the view matrix handness is flipped (due to convention used for cubemap) we have a determinant of +1
$FragInputs.isFrontFace:        output.isFrontFace = _ViewParam.x <  0.0 ? output.isFrontFace : !output.isFrontFace;

            return output;
        }
";

        private static string function_InterpolateWithBaryCoordsMeshToDS =
@"
VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS ouput;

    TESSELLATION_INTERPOLATE_BARY(positionWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
$VaryingsMeshToDS.tangentWS:        // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
$VaryingsMeshToDS.tangentWS:        TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
$VaryingsMeshToDS.texCoord0:        TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
$VaryingsMeshToDS.texCoord1:        TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
$VaryingsMeshToDS.texCoord2:        TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
$VaryingsMeshToDS.texCoord3:        TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
$VaryingsMeshToDS.color:            TESSELLATION_INTERPOLATE_BARY(color, baryCoords);

    return ouput;
}
";

    struct GraphInputs
    {
        [Optional] Vector3 WorldSpaceNormal;
        [Optional] Vector3 WorldSpaceTangent;
        [Optional] Vector3 WorldSpaceBiTangent;
        [Optional] Vector3 WorldSpaceViewDirection;
        [Optional] Vector3 WorldSpacePosition;

        [Optional] Vector3 screenPosition;
        [Optional] Vector4 uv0;
        [Optional] Vector4 uv1;
        [Optional] Vector4 uv2;
        [Optional] Vector4 uv3;
        [Optional] Vector4 vertexColor;

        public static Dependency[] dependencies = new Dependency[]
        {
            new Dependency("GraphInputs.WorldSpaceNormal",          "FragInputs.worldToTangent"),
            new Dependency("GraphInputs.WorldSpaceTangent",         "FragInputs.worldToTangent"),
            new Dependency("GraphInputs.WorldSpaceBiTangent",       "FragInputs.worldToTangent"),
//            new Dependency("GraphInputs.WorldSpaceViewDirection",   "FragInputs.??"),
            new Dependency("GraphInputs.WorldSpacePosition",        "FragInputs.positionWS"),
            new Dependency("GraphInputs.screenPosition",            "FragInputs.positionSS"),
            new Dependency("GraphInputs.uv0",                       "FragInputs.texCoord0"),
            new Dependency("GraphInputs.uv1",                       "FragInputs.texCoord1"),
            new Dependency("GraphInputs.uv2",                       "FragInputs.texCoord2"),
            new Dependency("GraphInputs.uv3",                       "FragInputs.texCoord3"),
            new Dependency("GraphInputs.vertexColor",               "FragInputs.color"),
        };
    };


    private static string function_FragInputsToGraphInputs =
        @"
        GraphInputs FragInputsToGraphInputs(FragInputs input, float3 viewWS)
        {
            GraphInputs output;
            ZERO_INITIALIZE(GraphInputs, output);

$GraphInputs.WorldSpaceNormal:          output.WorldSpaceNormal =            normalize(input.worldToTangent[2].xyz);
$GraphInputs.WorldSpaceTangent:		    output.WorldSpaceTangent =           input.worldToTangent[0].xyz;
$GraphInputs.WorldSpaceBiTangent:       output.WorldSpaceBiTangent =         input.worldToTangent[1].xyz;
$GraphInputs.WorldSpaceViewDirection:   output.WorldSpaceViewDirection =     normalize(viewWS);
$GraphInputs.WorldSpacePosition:        output.WorldSpacePosition =          input.positionWS;

$GraphInputs.screenPosition:            output.screenPosition = input.positionSS;

$GraphInputs.uv0:                       output.uv0 =    float4(input.texCoord0, 0.0f, 0.0f);
$GraphInputs.uv1:                       output.uv1 =    float4(input.texCoord1, 0.0f, 0.0f);
$GraphInputs.uv2:                       output.uv2 =    float4(input.texCoord2, 0.0f, 0.0f);
$GraphInputs.uv3:                       output.uv3 =    float4(input.texCoord3, 0.0f, 0.0f);

$GraphInputs.vertexColor:               output.vertexColor =    input.color;

            return output;
        }
";
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

        private static string PreprocessShaderCode(string code, HashSet<string> activeFields)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
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
                        result.Append("//    ");

                        // append the line
                        AppendSubstring(result, code, dollar, true, endln, false);
                        result.Append("\n");
                        cur = endln + 1;    
                    }
                    else
                    {
                        // colon found!
                        // ugh, this probably allocates memory -- wish we could do the field lookup direct from a substring
                        string predicate = code.Substring(dollar + 1, predicateLength);

                        if (activeFields.Contains(predicate))
                        {
                            // predicate is active, append the line
                            AppendSubstring(result, code, colon, false, endln, false);
                            result.Append("\n");
                            cur = endln + 1;
                        }
                        else
                        {
                            // predicate is not active -- drop line
                            cur = endln + 1;
                        }
                    }
                }
            }

            return result.ToString();
        }

        private static void ApplyDependencies(HashSet<string> activeFields, List<Dependency[]> dependsList)
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
                string field= fieldsToPropagate.Dequeue();
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

        static void AddActiveFieldsFromGraphRequirements(HashSet<string> activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add("GraphInputs.screenPosition");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add("GraphInputs.vertexColor");
            }

            if (requirements.requiresNormal != 0)
            {
                activeFields.Add("GraphInputs.WorldSpaceNormal");               // TODO: check actual space requirements:        space.ToVariableName(InterpolatorType.Normal)
            }

            if (requirements.requiresTangent != 0)
            {
                activeFields.Add("GraphInputs.WorldSpaceTangent");              // TODO: check actual space requirement
            }

            if (requirements.requiresBitangent != 0)
            {
                activeFields.Add("GraphInputs.WorldSpaceBiTangent");            // TODO: check actual space requirement
            }

            if (requirements.requiresViewDir != 0)
            {
                activeFields.Add("GraphInputs.WorldSpaceViewDirection");        // TODO: check actual space requirement
            }

            if (requirements.requiresPosition != 0)
            {
                activeFields.Add("GraphInputs.WorldSpacePosition");             // TODO: check actual space requirement
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add("GraphInputs." + channel.GetUVName());
            }
        }

        static void AddActiveFieldsFromModelRequirements(HashSet<string> activeFields, ShaderGraphRequirements requirements)
        {
            if (requirements.requiresScreenPosition)
            {
                activeFields.Add("FragInputs.positionSS");
            }

            if (requirements.requiresVertexColor)
            {
                activeFields.Add("FragInputs.color");
            }

            if (requirements.requiresNormal != 0)
            {
                activeFields.Add("FragInputs.worldToTangent");                  // TODO: check actual space requirements:        space.ToVariableName(InterpolatorType.Normal)
            }

            if (requirements.requiresTangent != 0)
            {
                activeFields.Add("FragInputs.worldToTangent");                  // TODO: check actual space requirement
            }

            if (requirements.requiresBitangent != 0)
            {
                activeFields.Add("FragInputs.worldToTangent");                  // TODO: check actual space requirement
            }

//            if (requirements.requiresViewDir != 0)
//            {
//                activeFields.Add("FragInputs.???");        // TODO: check actual space requirement
//            }

            if (requirements.requiresPosition != 0)
            {
                activeFields.Add("FragInputs.positionWS");                     // TODO: check actual space requirement
            }

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
            {
                activeFields.Add("FragInputs.texCoord" + (int) channel);
            }
        }

        public static void Generate(
            ShaderGenerator definesResult,
            ShaderGenerator codeResult,
            ShaderGraphRequirements graphRequirements,
            ShaderGraphRequirements modelRequirements,
            CoordinateSpace preferedCoordinateSpace)
        {
            if (preferedCoordinateSpace == CoordinateSpace.Tangent)
                preferedCoordinateSpace = CoordinateSpace.World;

            // build initial requirements
            var combinedRequirements = graphRequirements.Union(modelRequirements);
            HashSet<string> activeFields = new HashSet<string>();
            AddActiveFieldsFromGraphRequirements(activeFields, graphRequirements);
            AddActiveFieldsFromModelRequirements(activeFields, modelRequirements);

            // propagate requirements using dependencies
            {
                ApplyDependencies(
                    activeFields,
                    new List<Dependency[]>()
                    {
                        FragInputs.dependencies,
                        VaryingsMeshToPS.standardDependencies,
                        GraphInputs.dependencies,
                    }
                );
            }

            // generate defines based on requirements
            string defines = PreprocessShaderCode(defines_ActiveFields, activeFields);
            definesResult.AddShaderChunk(defines, false);

            definesResult.AddShaderChunk("// ACTIVE FIELDS:", false);
            foreach (string f in activeFields)
            {
                definesResult.AddShaderChunk("// " + f, false);
            }

            // generate code based on requirements
            string result;
            result = BuildType(typeof(AttributesMesh), activeFields);
            result = result + BuildType(typeof(VaryingsMeshToPS), activeFields);
            result = result + BuildType(typeof(VaryingsMeshToDS), activeFields);
            result = result + BuildPackedType(typeof(VaryingsMeshToPS), activeFields);
            result = result + BuildPackedType(typeof(VaryingsMeshToDS), activeFields);
            result = result + BuildType(typeof(GraphInputs), activeFields);

            result = result + PreprocessShaderCode(function_BuildFragInputs, activeFields);
            result = result + PreprocessShaderCode(function_FragInputsToGraphInputs, activeFields);

            Debug.Log(result);

            codeResult.AddShaderChunk(result, false);

/*
            // step 1:
            // *generate needed interpolators
            // *generate output from the vertex shader that writes into these interpolators
            // *generate the pixel shader code that declares needed variables in the local scope

            int interpolatorIndex = interpolatorStartIndex;

            // bitangent needs normal for x product
            if (combinedRequierments.requiresNormal > 0 || combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal);
                //                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                //                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.normal", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Normal)), false);
                //                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                code[Struct_AttributesMesh].AddShaderChunk("float3 normalOS : NORMAL;", false);
                code[Function_VertMeshToPS].AddShaderChunk("float3 normalOS : NORMAL;", false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresTangent > 0 || combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.tangent", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Vector)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.BiTangent);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = normalize(cross(o.{1}, o.{2}.xyz) * {3});",
                        name,
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal),
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent),
                        "v.tangent.w"), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresViewDir > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.ViewDirection);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);

                var worldSpaceViewDir = "SafeNormalize(_WorldSpaceCameraPos.xyz - mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1.0)).xyz)";
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace(worldSpaceViewDir, CoordinateSpace.World, preferedCoordinateSpace, InputType.Vector)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresPosition > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Position);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.vertex", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Position)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.NeedsTangentSpace())
            {
                pixelShader.AddShaderChunk(string.Format("float3x3 tangentSpaceTransform = float3x3({0},{1},{2});",
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent), preferedCoordinateSpace.ToVariableName(InterpolatorType.BiTangent), preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal)), false);
            }

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresNormal, InterpolatorType.Normal, preferedCoordinateSpace,
                InputType.Normal, pixelShader, Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresTangent, InterpolatorType.Tangent, preferedCoordinateSpace,
                InputType.Vector, pixelShader, Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresBitangent, InterpolatorType.BiTangent, preferedCoordinateSpace,
                InputType.Vector, pixelShader, Dimension.Three);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresViewDir, InterpolatorType.ViewDirection, preferedCoordinateSpace,
                InputType.Vector, pixelShader, Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresPosition, InterpolatorType.Position, preferedCoordinateSpace,
                InputType.Position, pixelShader, Dimension.Three);

            if (combinedRequierments.requiresVertexColor)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : COLOR;", ShaderGeneratorNames.VertexColor), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.color;", ShaderGeneratorNames.VertexColor), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);
            }

            if (combinedRequierments.requiresScreenPosition)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(UnityObjectToClipPos(v.vertex));", ShaderGeneratorNames.ScreenPosition), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            foreach (var channel in combinedRequierments.requiresMeshUVs.Distinct())
            {
                interpolators.AddShaderChunk(string.Format("half4 {0} : TEXCOORD{1};", channel.GetUVName(), interpolatorIndex == 0 ? "" : interpolatorIndex.ToString()), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.texcoord{1};", channel.GetUVName(), (int)channel), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0}  = IN.{0};", channel.GetUVName()), false);
                interpolatorIndex++;
            }

            // step 2
            // copy the locally defined values into the surface description
            // structure using the requirements for ONLY the shader graph
            // additional requirements have come from the lighting model
            // and are not needed in the shader graph
            var replaceString = "surfaceInput.{0} = {0};";
            GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresNormal, InterpolatorType.Normal, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresPosition, InterpolatorType.Position, surfaceInputs, replaceString);

            if (graphRequirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.VertexColor), false);

            if (graphRequirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in graphRequirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", channel.GetUVName()), false);
*/
        }
    };
}
