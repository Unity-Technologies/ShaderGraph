using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class LightWeightPBRSubShader
    {
        Pass m_ForwardPassMetallic = new Pass()
        {
            Name = "LightweightForward",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            VertexShaderSlots = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            }
        };

        struct Pass
        {
            public string Name;
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
        }

        Pass m_ForwardPassSpecular = new Pass()
        {
            Name = "LightweightForward",
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        private static string GetShaderPassFromTemplate(string template, PBRMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
        {
            var builder = new ShaderStringBuilder(2);
            var vertexInputs = new ShaderGenerator();
            var vertexDescriptionStruct = new ShaderStringBuilder(2);
            var vertexDescriptionFunction = new ShaderStringBuilder(2);
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var functionRegistry = new FunctionRegistry(builder);
            var surfaceInputs = new ShaderGenerator();
            var vertexDescriptionInputsStruct = new ShaderStringBuilder(2);
            var vertexDescriptionInputs = new ShaderStringBuilder(3);


            var shaderProperties = new PropertyCollector();

            // Build list of nodes going into the pixel shader and their requirements
            var pixelNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);
            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes);

            // Build list of nodes going into the vertex shader and their requirements
            var vertexNodes = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, masterNode, NodeUtils.IncludeSelf.Include, pass.VertexShaderSlots);
            var vertexRequirements = ShaderGraphRequirements.FromNodes(vertexNodes);

            // Generate input structure for surface description function
            {
                surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
                surfaceInputs.Indent();

                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresNormal, InterpolatorType.Normal, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresTangent, InterpolatorType.Tangent, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(pixelRequirements.requiresPosition, InterpolatorType.Position, surfaceInputs);

                if (pixelRequirements.requiresVertexColor)
                    surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

                if (pixelRequirements.requiresScreenPosition)
                    surfaceInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

                foreach (var channel in pixelRequirements.requiresMeshUVs.Distinct())
                    surfaceInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

                surfaceInputs.Deindent();
                surfaceInputs.AddShaderChunk("};", false);
            }

            // Generate input structure for vertex description function
            vertexDescriptionInputsStruct.AppendLine("struct VertexDescriptionInputs");
            using (vertexDescriptionInputsStruct.BlockSemicolonScope())
            {
                var sg = new ShaderGenerator();
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresNormal, InterpolatorType.Normal, sg);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresTangent, InterpolatorType.Tangent, sg);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresBitangent, InterpolatorType.BiTangent, sg);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresViewDir, InterpolatorType.ViewDirection, sg);
                ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(vertexRequirements.requiresPosition, InterpolatorType.Position, sg);
                vertexDescriptionInputsStruct.AppendLines(sg.GetShaderString(0));

                if (vertexRequirements.requiresVertexColor)
                    vertexDescriptionInputsStruct.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (vertexRequirements.requiresScreenPosition)
                    vertexDescriptionInputsStruct.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                foreach (var channel in vertexRequirements.requiresMeshUVs.Distinct())
                    vertexDescriptionInputsStruct.AppendLine("half4 {0};", channel.GetUVName());
            }

            // Generate standard transforms for vertex description input
            {

            }

            var modelRequiements = ShaderGraphRequirements.none;
            modelRequiements.requiresNormal |= NeededCoordinateSpace.World;
            modelRequiements.requiresTangent |= NeededCoordinateSpace.World;
            modelRequiements.requiresBitangent |= NeededCoordinateSpace.World;
            modelRequiements.requiresPosition |= NeededCoordinateSpace.World;
            modelRequiements.requiresViewDir |= NeededCoordinateSpace.World;
            modelRequiements.requiresMeshUVs.Add(UVChannel.UV1);

            // Generate input structure for vertex shader
            GraphUtil.GenerateApplicationVertexInputs(vertexRequirements.Union(pixelRequirements.Union(modelRequiements)), vertexInputs);

            var vertexSlots = pass.VertexShaderSlots.Select(masterNode.FindSlot<MaterialSlot>).ToList();
            GraphUtil.GenerateVertexDescriptionStruct(vertexDescriptionStruct, vertexSlots);
            GraphUtil.GenerateVertexDescriptionFunction(vertexDescriptionFunction, functionRegistry, shaderProperties, mode, vertexNodes, vertexSlots);

            var slots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
                slots.Add(masterNode.FindSlot<MaterialSlot>(id));
            GraphUtil.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, true);

            var usedSlots = new List<MaterialSlot>();
            foreach (var id in pass.PixelShaderSlots)
                usedSlots.Add(masterNode.FindSlot<MaterialSlot>(id));

            GraphUtil.GenerateSurfaceDescription(
                pixelNodes,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                surfaceDescriptionFunction,
                functionRegistry,
                shaderProperties,
                pixelRequirements,
                mode,
                "PopulateSurfaceData",
                "SurfaceDescription",
                null,
                usedSlots);

            var graph = new ShaderGenerator();
            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            graph.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            graph.AddShaderChunk(vertexDescriptionInputsStruct.ToString(), false);
            graph.AddShaderChunk(builder.ToString(), false);
            graph.AddShaderChunk(vertexInputs.GetShaderString(2), false);
            graph.AddShaderChunk(vertexDescriptionStruct.ToString(), false);
            graph.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
            graph.AddShaderChunk(vertexDescriptionFunction.ToString(), false);
            graph.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);

            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            materialOptions.GetBlend(blendingVisitor);
            materialOptions.GetCull(cullingVisitor);
            materialOptions.GetDepthTest(zTestVisitor);
            materialOptions.GetDepthWrite(zWriteVisitor);

            var interpolators = new ShaderGenerator();
            var localVertexShader = new ShaderGenerator();
            var localPixelShader = new ShaderGenerator();
            var localSurfaceInputs = new ShaderGenerator();
            var surfaceOutputRemap = new ShaderGenerator();

            ShaderGenerator.GenerateStandardTransforms(
                3,
                10,
                interpolators,
                vertexDescriptionInputs,
                localVertexShader,
                localPixelShader,
                localSurfaceInputs,
                pixelRequirements,
                modelRequiements,
                vertexRequirements,
                CoordinateSpace.World);

            ShaderGenerator defines = new ShaderGenerator();

            if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
                defines.AddShaderChunk("#define _NORMALMAP 1", true);

            if (masterNode.model == PBRMasterNode.Model.Specular)
                defines.AddShaderChunk("#define _SPECULAR_SETUP 1", true);

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
                defines.AddShaderChunk("#define _AlphaClip 1", true);

            var templateLocation = ShaderGenerator.GetTemplatePath(template);

            foreach (var slot in usedSlots)
            {
                surfaceOutputRemap.AddShaderChunk(string.Format("{0} = surf.{0};", slot.shaderOutputName), true);
            }

            if (!File.Exists(templateLocation))
                return string.Empty;

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultPass = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(3));
            resultPass = resultPass.Replace("${Graph}", graph.GetShaderString(0));
            resultPass = resultPass.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultPass = resultPass.Replace("${VertexDescriptionInputs}", vertexDescriptionInputs.ToString());
            resultPass = resultPass.Replace("${VertexShader}", localVertexShader.GetShaderString(3));
            resultPass = resultPass.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceInputs}", localSurfaceInputs.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));

            resultPass = resultPass.Replace("${Tags}", string.Empty);
            resultPass = resultPass.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            return resultPass;
        }

        public IEnumerable<string> GetSubshader(PBRMasterNode masterNode, GenerationMode mode)
        {
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            subShader.AddShaderChunk("Tags{ \"RenderPipeline\" = \"LightweightPipeline\"}", true);

            var materialOptions = MasterNode.GetMaterialOptionsFromAlphaMode(masterNode.alphaMode);
            var tagsVisitor = new ShaderGenerator();
            materialOptions.GetTags(tagsVisitor);
            subShader.AddShaderChunk(tagsVisitor.GetShaderString(0), true);

            subShader.AddShaderChunk(
                GetShaderPassFromTemplate(
                    "lightweightPBRForwardPass.template",
                    masterNode,
                    masterNode.model == PBRMasterNode.Model.Metallic ? m_ForwardPassMetallic : m_ForwardPassSpecular,
                    mode,
                    materialOptions),
                true);

            var extraPassesTemplateLocation = ShaderGenerator.GetTemplatePath("lightweightPBRExtraPasses.template");
            if (File.Exists(extraPassesTemplateLocation))
                subShader.AddShaderChunk(File.ReadAllText(extraPassesTemplateLocation), true);

            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return new[] { subShader.GetShaderString(0) };
        }
    }
}
