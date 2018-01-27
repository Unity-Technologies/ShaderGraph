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
                PostProcessMasterNode.ColorSlotId
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

			surfaceInputs.AddShaderChunk("float2 texcoord;", true);

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

            ShaderGenerator.GenerateStandardTransforms(
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
                surfaceOutputRemap.AddShaderChunk(slot.shaderOutputName
                    + " = surf."
                    + slot.shaderOutputName + ";", true);
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

        public string GetSubshader(PostProcessMasterNode masterNode, GenerationMode mode)
        {
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
			subShader.AddShaderChunk("Cull Off ZWrite Off ZTest Always", true);
            //subShader.AddShaderChunk("Tags{ \"RenderType\" = \"Opaque\" \"RenderPipeline\" = \"LightweightPipeline\"}", true);

            //var materialOptions = MasterNode.GetMaterialOptionsFromAlphaMode(masterNode.alphaMode);
            //var tagsVisitor = new ShaderGenerator();
            //materialOptions.GetTags(tagsVisitor);
            //subShader.AddShaderChunk(tagsVisitor.GetShaderString(0), true);

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
