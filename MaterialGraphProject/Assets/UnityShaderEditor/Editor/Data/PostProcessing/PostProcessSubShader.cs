using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class PostProcessSubShader
    {
        Pass m_PostProcessPass = new Pass()
        {
            Name = "PostProcess",
            PixelShaderSlots = new List<int>()
            {
                PostProcessMasterNode.DestinationSlotId,
                PostProcessMasterNode.UserDataSlotId
            }
        };

        struct Pass
        {
            public string Name;
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
        }

        private static string GetShaderPassFromTemplate(
            string template, 
            PostProcessMasterNode masterNode, 
            Pass pass, 
            GenerationMode mode)
        {
            var builder = new ShaderStringBuilder();
            builder.IncreaseIndent();
            builder.IncreaseIndent();
            var vertexInputs = new ShaderGenerator();
            var surfaceVertexShader = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var functionRegistry = new FunctionRegistry(builder);
            var surfaceInputs = new ShaderGenerator();

            var shaderProperties = new PropertyCollector();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList);
            GraphUtil.GenerateApplicationVertexInputs(requirements, vertexInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

            if (requirements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (requirements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

            surfaceInputs.Deindent();
            surfaceInputs.AddShaderChunk("};", false);

            surfaceVertexShader.AddShaderChunk("GraphVertexInput PopulateVertexData(GraphVertexInput v){", false);
            surfaceVertexShader.Indent();
            surfaceVertexShader.AddShaderChunk("return v;", false);
            surfaceVertexShader.Deindent();
            surfaceVertexShader.AddShaderChunk("}", false);

            var slots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
            {
                var slot = masterNode.FindSlot<MaterialSlot>(id);
                if (slot != null)
                    slots.Add(slot);
            }
            GraphUtil.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, true);

            var usedSlots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
                usedSlots.Add(masterNode.FindSlot<MaterialSlot>(id));

            GraphUtil.GenerateSurfaceDescription(
                activeNodeList,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                requirements,
                mode,
                "PopulateSurfaceData",
                "SurfaceDescription",
                null,
                usedSlots);

            var graph = new ShaderGenerator();

            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            graph.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            graph.AddShaderChunk(builder.ToString(), false);
            graph.AddShaderChunk(vertexInputs.GetShaderString(2), false);
            graph.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
            graph.AddShaderChunk(surfaceVertexShader.GetShaderString(2), false);
            graph.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);

            var interpolators = new ShaderGenerator();
            var localVertexShader = new ShaderGenerator();
            var localPixelShader = new ShaderGenerator();
            var localSurfaceInputs = new ShaderGenerator();
            var surfaceOutputRemap = new ShaderGenerator();

            var reqs = ShaderGraphRequirements.none;

            GenerateStandardTransforms(
                3,
                10,
                interpolators,
                localVertexShader,
                localPixelShader,
                localSurfaceInputs,
                requirements,
                reqs,
                CoordinateSpace.World);

            ShaderGenerator defines = new ShaderGenerator();

            var templateLocation = ShaderGenerator.GetTemplatePath(template);

            foreach (var slot in usedSlots)
            {
                if(slot.id == PostProcessMasterNode.UserDataSlotId)
                {
                    //surfaceOutputRemap.AddShaderChunk("_GraphUserData = surf."
                        //+ NodeUtils.GetHLSLSafeName(slot.shaderOutputName) + ";", true);
                }
                else
                {
                    surfaceOutputRemap.AddShaderChunk(slot.shaderOutputName
                        + " = surf."
                        + slot.shaderOutputName + ";", true);
                }
            }

            if (!File.Exists(templateLocation))
                return string.Empty;

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultPass = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
            resultPass = resultPass.Replace("${Graph}", graph.GetShaderString(3));
            resultPass = resultPass.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultPass = resultPass.Replace("${VertexShader}", localVertexShader.GetShaderString(3));
            resultPass = resultPass.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceInputs}", localSurfaceInputs.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));

            return resultPass;
        }

        static void GenerateStandardTransforms(
            int interpolatorStartIndex,
            int maxInterpolators,
            ShaderGenerator interpolators,
            ShaderGenerator vertexShader,
            ShaderGenerator pixelShader,
            ShaderGenerator surfaceInputs,
            ShaderGraphRequirements graphRequiements,
            ShaderGraphRequirements modelRequiements,
            CoordinateSpace preferedCoordinateSpace)
        {
            if (preferedCoordinateSpace == CoordinateSpace.Tangent)
                preferedCoordinateSpace = CoordinateSpace.World;

            // step 1:
            // *generate needed interpolators
            // *generate output from the vertex shader that writes into these interpolators
            // *generate the pixel shader code that declares needed variables in the local scope
            var combinedRequierments = graphRequiements.Union(modelRequiements);

            int interpolatorIndex = interpolatorStartIndex;

            // bitangent needs normal for x product
            if (combinedRequierments.requiresNormal > 0 || combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ShaderGenerator.ConvertBetweenSpace("v.normal", CoordinateSpace.Object, preferedCoordinateSpace, ShaderGenerator.InputType.Normal)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresTangent > 0 || combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ShaderGenerator.ConvertBetweenSpace("v.tangent", CoordinateSpace.Object, preferedCoordinateSpace, ShaderGenerator.InputType.Vector)), false);
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
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ShaderGenerator.ConvertBetweenSpace(worldSpaceViewDir, CoordinateSpace.World, preferedCoordinateSpace, ShaderGenerator.InputType.Vector)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresPosition > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Position);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ShaderGenerator.ConvertBetweenSpace("v.vertex", CoordinateSpace.Object, preferedCoordinateSpace, ShaderGenerator.InputType.Position)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.NeedsTangentSpace())
            {
                pixelShader.AddShaderChunk(string.Format("float3x3 tangentSpaceTransform = float3x3({0},{1},{2});",
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent), preferedCoordinateSpace.ToVariableName(InterpolatorType.BiTangent), preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal)), false);
            }

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresNormal, InterpolatorType.Normal, preferedCoordinateSpace,
                ShaderGenerator.InputType.Normal, pixelShader, ShaderGenerator.Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresTangent, InterpolatorType.Tangent, preferedCoordinateSpace,
                ShaderGenerator.InputType.Vector, pixelShader, ShaderGenerator.Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresBitangent, InterpolatorType.BiTangent, preferedCoordinateSpace,
                ShaderGenerator.InputType.Vector, pixelShader, ShaderGenerator.Dimension.Three);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresViewDir, InterpolatorType.ViewDirection, preferedCoordinateSpace,
                ShaderGenerator.InputType.Vector, pixelShader, ShaderGenerator.Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresPosition, InterpolatorType.Position, preferedCoordinateSpace,
                ShaderGenerator.InputType.Position, pixelShader, ShaderGenerator.Dimension.Three);

            if (combinedRequierments.requiresVertexColor)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : COLOR;", ShaderGeneratorNames.VertexColor), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.color;", ShaderGeneratorNames.VertexColor), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);
            }

            if (combinedRequierments.requiresScreenPosition)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), v.vertex)), _ProjectionParams.x);", ShaderGeneratorNames.ScreenPosition), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            foreach (var channel in combinedRequierments.requiresMeshUVs.Distinct())
            {
                interpolators.AddShaderChunk(string.Format("half4 {0} : TEXCOORD{1};", channel.GetUVName(), interpolatorIndex == 0 ? "" : interpolatorIndex.ToString()), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = texcoord;", channel.GetUVName()), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", channel.GetUVName()), false);
                interpolatorIndex++;
            }

            // step 2
            // copy the locally defined values into the surface description
            // structure using the requirements for ONLY the shader graph
            // additional requirements have come from the lighting model
            // and are not needed in the shader graph
            var replaceString = "surfaceInput.{0} = {0};";
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresNormal, InterpolatorType.Normal, surfaceInputs, replaceString);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresTangent, InterpolatorType.Tangent, surfaceInputs, replaceString);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs, replaceString);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs, replaceString);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresPosition, InterpolatorType.Position, surfaceInputs, replaceString);

            if (graphRequiements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.VertexColor), false);

            if (graphRequiements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", channel.GetUVName()), false);
        }

        public string GetSubshader(PostProcessMasterNode masterNode, GenerationMode mode)
        {
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
			subShader.AddShaderChunk("Cull Off ZWrite Off ZTest Always", true);

            subShader.AddShaderChunk(
                GetShaderPassFromTemplate(
                    "postProcessPass.template",
                    masterNode,
                    m_PostProcessPass,
                    mode),
                true);

            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
