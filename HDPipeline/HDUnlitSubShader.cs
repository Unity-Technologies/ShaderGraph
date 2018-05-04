using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
//    [Serializable] ??
    public class HDUnlitSubShader : IUnlitSubShader
    {
        struct Pass
        {
            public string Name;
            public string LightMode;
            public string ShaderPassName;
            public List<string> Includes;
            public string TemplateName;
            public List<string> ExtraDefines;
            public List<int> VertexShaderSlots;         // These control what slots are used by the pass vertex shader
            public List<int> PixelShaderSlots;          // These control what slots are used by the pass pixel shader
            public List<string> RequiredFields;         // feeds into the dependency analysis
        }

        Pass m_UnlitPassForwardOnly = new Pass()
        {
            Name = "ForwardOnly",
            LightMode = "ForwardOnly",
            TemplateName = "HDUnlitPassForward.template",
            ShaderPassName = "SHADERPASS_FORWARD_UNLIT",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",	// TODO: can we drop this now?
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassForwardUnlit.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_UnlitPassForwardDepthOnly = new Pass()
        {
            Name = "DepthForwardOnly",
            LightMode = "DepthForwardOnly",
            TemplateName = "HDUnlitPassForward.template",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId
            }
        };

        private static string GetVariantDefines(UnlitMasterNode masterNode)
        {
            ShaderGenerator defines = new ShaderGenerator();

            // #pragma shader_feature _ALPHATEST_ON
            float constantAlpha = 0.0f;
            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                (float.TryParse(masterNode.GetSlotValue(PBRMasterNode.AlphaThresholdSlotId, GenerationMode.ForReals), out constantAlpha) && (constantAlpha > 0.0f)))
            {
                defines.AddShaderChunk("#define _ALPHATEST_ON 1", true);
            }

//             if (kTesselationMode != TessellationMode.None)
//             {
//                 defines.AddShaderChunk("#define _TESSELLATION_PHONG 1", true);
//             }

            // #pragma shader_feature _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
//             switch (kDisplacementMode)
//             {
//                 case DisplacementMode.None:
//                     break;
//                 case DisplacementMode.Vertex:
//                     defines.AddShaderChunk("#define _VERTEX_DISPLACEMENT 1", true);
//                     break;
//                 case DisplacementMode.Pixel:
//                     defines.AddShaderChunk("#define _PIXEL_DISPLACEMENT 1", true);
            // Depth offset is only enabled if per pixel displacement is
//                     if (kDepthOffsetEnable)
//                     {
//                         // #pragma shader_feature _DEPTHOFFSET_ON
//                         defines.AddShaderChunk("#define _DEPTHOFFSET_ON 1", true);
//                     }
//                     break;
//                 case DisplacementMode.Tessellation:
//                     if (kTessellationEnabled)
//                     {
//                         defines.AddShaderChunk("#define _TESSELLATION_DISPLACEMENT 1", true);
//                     }
//                     break;
//             }

            // #pragma shader_feature _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
            // #pragma shader_feature _DISPLACEMENT_LOCK_TILING_SCALE
            // #pragma shader_feature _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
            // #pragma shader_feature _VERTEX_WIND
            // #pragma shader_feature _ _REFRACTION_PLANE _REFRACTION_SPHERE
            //
            // #pragma shader_feature _ _MAPPING_PLANAR _MAPPING_TRIPLANAR          // MOVE to a node
            // #pragma shader_feature _NORMALMAP_TANGENT_SPACE
            // #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3

            // #pragma shader_feature _MASKMAP
            // #pragma shader_feature _BENTNORMALMAP
            // #pragma shader_feature _EMISSIVE_COLOR_MAP
            // #pragma shader_feature _ENABLESPECULAROCCLUSION
            // #pragma shader_feature _HEIGHTMAP
            // #pragma shader_feature _TANGENTMAP
            // #pragma shader_feature _ANISOTROPYMAP
            // #pragma shader_feature _DETAIL_MAP                                   // MOVE to a node
            // #pragma shader_feature _SUBSURFACE_RADIUS_MAP
            // #pragma shader_feature _THICKNESSMAP
            // #pragma shader_feature _SPECULARCOLORMAP
            // #pragma shader_feature _TRANSMITTANCECOLORMAP

            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.surfaceType != SurfaceType.Opaque)
            {
                // transparent-only defines
                defines.AddShaderChunk("#define _SURFACE_TYPE_TRANSPARENT 1", true);

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    defines.AddShaderChunk("#define _BLENDMODE_ALPHA 1", true);
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    defines.AddShaderChunk("#define _BLENDMODE_ADD 1", true);
                }
//                else if (masterNode.alphaMode == PBRMasterNode.AlphaMode.PremultiplyAlpha)            // TODO
//                {
//                    defines.AddShaderChunk("#define _BLENDMODE_PRE_MULTIPLY 1", true);
//                }

                // #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
//                 if (kEnableBlendModePreserveSpecularLighting)
//                 {
//                     defines.AddShaderChunk("#define _BLENDMODE_PRESERVE_SPECULAR_LIGHTING 1", true);
//                 }

                // #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT
//                 if (kEnableFogOnTransparent)
//                 {
//                     defines.AddShaderChunk("#define _ENABLE_FOG_ON_TRANSPARENT 1", true);
//                 }
            }
            else
            {
                // opaque-only defines
            }

            // enable dithering LOD crossfade
            // #pragma multi_compile _ LOD_FADE_CROSSFADE
            // TODO: We should have this keyword only if VelocityInGBuffer is enable, how to do that ?
            //#pragma multi_compile VELOCITYOUTPUT_OFF VELOCITYOUTPUT_ON

            return defines.GetShaderString(2);
        }

        private static bool GenerateShaderPass(UnlitMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions, ShaderGenerator result)
        {
            var templateLocation = ShaderGenerator.GetTemplatePath(pass.TemplateName);
            if (!File.Exists(templateLocation))
            {
                // TODO: produce error here
                return false;
            }

            var nodeFunctions = new ShaderStringBuilder();
            nodeFunctions.IncreaseIndent();
            nodeFunctions.IncreaseIndent();

            var vertexInputs = new ShaderGenerator();
            var graphEvalFunction = new ShaderGenerator();
            var graphOutputs = new ShaderGenerator();
            var functionRegistry = new FunctionRegistry(nodeFunctions);
            var graphInputs = new ShaderGenerator();

            var shaderProperties = new PropertyCollector();

            graphInputs.AddShaderChunk("struct OLDSurfaceInputs {", false);
            graphInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            var graphRequirements = ShaderGraphRequirements.FromNodes(activeNodeList);

            // TODO: make this default list of requirements be per-pass defined?
            var modelRequirements = ShaderGraphRequirements.none;
            modelRequirements.requiresNormal |= NeededCoordinateSpace.World;
            modelRequirements.requiresTangent |= NeededCoordinateSpace.World;
            modelRequirements.requiresBitangent |= NeededCoordinateSpace.World;
            modelRequirements.requiresPosition |= NeededCoordinateSpace.World;
            modelRequirements.requiresViewDir |= NeededCoordinateSpace.World;

            // TODO: need to be able to handle these transformations in the dependency code...
            GraphUtil.GenerateApplicationVertexInputs(graphRequirements.Union(modelRequirements), vertexInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresNormal, InterpolatorType.Normal, graphInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresTangent, InterpolatorType.Tangent, graphInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresBitangent, InterpolatorType.BiTangent, graphInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresViewDir, InterpolatorType.ViewDirection, graphInputs);
            ShaderGenerator.GenerateSpaceTranslationSurfaceInputs(graphRequirements.requiresPosition, InterpolatorType.Position, graphInputs);

//            ShaderGenerator defines = new ShaderGenerator();
//            defines.AddShaderChunk(string.Format("#define SHADERPASS {0}", pass.ShaderPassName), true);

            if (graphRequirements.requiresVertexColor)
                graphInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (graphRequirements.requiresScreenPosition)
                graphInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in graphRequirements.requiresMeshUVs.Distinct())
            {
                graphInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);
//                defines.AddShaderChunk(string.Format("#define ATTRIBUTES_NEED_TEXCOORD{0}", (int)channel), true);
//                defines.AddShaderChunk(string.Format("#define VARYINGS_NEED_TEXCOORD{0}", (int)channel), true);
            }

            graphInputs.Deindent();
            graphInputs.AddShaderChunk("};", false);

            var slots = new List<MaterialSlot>();
            var usedSlots = new List<MaterialSlot>();           // TODO can we use the same list of slots?
            foreach (var id in pass.PixelShaderSlots)
            {
                MaterialSlot slot = masterNode.FindSlot<MaterialSlot>(id);
                if (slot != null)
                {
                    slots.Add(slot);
                    usedSlots.Add(slot);
                }
            }

            HashSet<string> activeFields = new HashSet<string>();
            GraphUtil.GenerateSurfaceDescriptionStruct(graphOutputs, slots, true, "GraphOutputs", activeFields);

            GraphUtil.GenerateSurfaceDescription(
                activeNodeList,
                masterNode,
                masterNode.owner as AbstractMaterialGraph,
                graphEvalFunction,
                functionRegistry,
                shaderProperties,
                graphRequirements,
                mode,
                "EvaluateGraph",
                "GraphOutputs",
                null,
                usedSlots,
                "GraphInputs");

            var graph = new ShaderGenerator();
            graph.AddShaderChunk("// Node function definitions", false);
            graph.AddShaderChunk(nodeFunctions.ToString(), false);

            graph.AddShaderChunk("// Graph Inputs", false);
            graph.AddGenerator(graphInputs);
            graph.AddShaderChunk("// Graph Outputs", false);
            graph.AddGenerator(graphOutputs);
            graph.AddShaderChunk("// ShaderGraph Properties", false);
            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);

            graph.AddShaderChunk("// Graph evaluation", false);
            graph.AddGenerator(graphEvalFunction);

//            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

//            var materialOptions = new SurfaceMaterialOptions();
  //          materialOptions.GetTags(tagsVisitor);
            materialOptions.GetBlend(blendingVisitor);
            materialOptions.GetCull(cullingVisitor);
            materialOptions.GetDepthTest(zTestVisitor);
            materialOptions.GetDepthWrite(zWriteVisitor);

            var localPixelShader = new ShaderGenerator();
            var localSurfaceInputs = new ShaderGenerator();
            var surfaceOutputRemap = new ShaderGenerator();
            var packedInterpolatorCode = new ShaderGenerator();
            var interpolatorDefines = new ShaderGenerator();


            if (masterNode.twoSided.isOn)
            {
                activeFields.Add("DoubleSided");
                if (pass.ShaderPassName != "SHADERPASS_VELOCITY")   // HACK to get around lack of a good interpolator dependency system
                {                                                   // we need to be able to build interpolators using multiple input structs
                                                                    // also: should only require isFrontFace if Normals are required...
                    activeFields.Add("DoubleSided.Mirror");         // TODO: change this depending on what kind of normal flip you want..
                    activeFields.Add("FragInputs.isFrontFace");     // will need this for determining normal flip mode
                }
            }

            HDRPShaderStructs.Generate(
                interpolatorDefines,
                packedInterpolatorCode,
                graphRequirements,
                modelRequirements,
                pass.RequiredFields,
                CoordinateSpace.World,
                activeFields);

            // debug output all active fields
            {
                interpolatorDefines.AddShaderChunk("// ACTIVE FIELDS:", false);
                foreach (string f in activeFields)
                {
                    interpolatorDefines.AddShaderChunk("// " + f, false);
                }
            }

            ShaderGenerator defines = new ShaderGenerator();
            {
                defines.AddShaderChunk(string.Format("#define SHADERPASS {0}", pass.ShaderPassName), true);
                if (pass.ExtraDefines != null)
                {
                    foreach (var define in pass.ExtraDefines)
                    {
                        defines.AddShaderChunk(define, true);
                    }
                }
            }

            foreach (var slot in usedSlots)
            {
                surfaceOutputRemap.AddShaderChunk(string.Format("{0} = surf.{0};", slot.shaderOutputName), true);
            }

            var shaderPassIncludes = new ShaderGenerator();
            if (pass.Includes != null)
            {
                foreach (var include in pass.Includes)
                {
                    shaderPassIncludes.AddShaderChunk(include, true);
                }
            }

            string definesString = defines.GetShaderString(2);
            definesString = definesString + "\n\n\t\t// Interpolator defines\n";
            definesString = definesString + interpolatorDefines.GetShaderString(2);

            // build the hash table of all named fragments
            Dictionary<string, string> namedFragments = new Dictionary<string, string>();
            namedFragments.Add("${Defines}",                definesString);

            namedFragments.Add("${Graph}",                  graph.GetShaderString(3));
//             namedFragments.Add("${Interpolators}", interpolators.GetShaderString(3));
//             namedFragments.Add("${VertexShader}", localVertexShader.GetShaderString(3));
            namedFragments.Add("${LocalPixelShader}",       localPixelShader.GetShaderString(3));
            namedFragments.Add("${SurfaceInputs}",          localSurfaceInputs.GetShaderString(3));
            namedFragments.Add("${SurfaceOutputRemap}",     surfaceOutputRemap.GetShaderString(3));
            namedFragments.Add("${LightMode}",              pass.LightMode);
            namedFragments.Add("${PassName}",               pass.Name);
            namedFragments.Add("${Includes}",               shaderPassIncludes.GetShaderString(2));
            namedFragments.Add("${InterpolatorPacking}",    packedInterpolatorCode.GetShaderString(2));
            namedFragments.Add("${Blending}",               blendingVisitor.GetShaderString(2));
            namedFragments.Add("${Culling}",                cullingVisitor.GetShaderString(2));
            namedFragments.Add("${ZTest}",                  zTestVisitor.GetShaderString(2));
            namedFragments.Add("${ZWrite}",                 zWriteVisitor.GetShaderString(2));
//             namedFragments.Add("${Stencil}", stencilVisitor.GetShaderString(2));
            namedFragments.Add("${LOD}",                    materialOptions.lod.ToString());
            namedFragments.Add("${VariantDefines}",         GetVariantDefines(masterNode));

//            namedFragments.Add("${Tags}", tagsVisitor.GetShaderString(2));

            // process the template to generate the shader code for this pass
            string[] templateLines = File.ReadAllLines(templateLocation);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (string line in templateLines)
            {
                ShaderSpliceUtil.PreprocessShaderCode(line, activeFields, namedFragments, builder);
                builder.AppendLine();
            }

            result.AddShaderChunk(builder.ToString(), false);

            return true;
        }

        public string GetSubshader(IMasterNode inMasterNode, GenerationMode mode)
        {
            var masterNode = inMasterNode as UnlitMasterNode;
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var materialOptions = new SurfaceMaterialOptions();
                if (masterNode.surfaceType == SurfaceType.Opaque)
                {
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Geometry;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Opaque;
                }
                else
                {
                    switch (masterNode.alphaMode)
                    {
                        case AlphaMode.Alpha:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        case AlphaMode.Additive:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        // TODO: other blend modes
                    }
                }

                materialOptions.cullMode = masterNode.twoSided.isOn ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;

                // Add tags at the SubShader level
                {
                    var tagsVisitor = new ShaderGenerator();
                    //                    tagsVisitor.AddShaderChunk("Tags{ \"RenderPipeline\" = \"HDRP\"}", true);
                    //  subShader.AddShaderChunk("Tags{ \"RenderType\" = \"Opaque\" }", true);
                    materialOptions.GetTags(tagsVisitor);
                    subShader.AddShaderChunk(tagsVisitor.GetShaderString(0), false);
                }

                // generate the necessary shader passes
//                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
//                bool transparent = (masterNode.surfaceType != SurfaceType.Opaque);

                GenerateShaderPass(masterNode, m_UnlitPassForwardDepthOnly, mode, materialOptions, subShader);
                GenerateShaderPass(masterNode, m_UnlitPassForwardOnly, mode, materialOptions, subShader);

            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }
    }
}
