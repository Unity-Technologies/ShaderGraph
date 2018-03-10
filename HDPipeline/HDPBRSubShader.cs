using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

//using UnityEngine.Experimental.Rendering.HDPipeline;      // This requires us to make ShaderGraph assembly depend on the HDPipeline assembly... probably not a good idea


namespace UnityEditor.ShaderGraph
{
    public class HDPBRSubShader : IPBRSubShader
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
            public string CullOverride;
            public string BlendOverride;
            public string BlendOpOverride;
            public string ZTestOverride;
            public string ZWriteOverride;
            public string ColorMaskOverride;
            public List<string> StencilOverride;
        }

        Pass m_PassGBuffer = new Pass()
        {
            Name = "GBuffer",
            LightMode = "GBuffer",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_GBUFFER",
            StencilOverride = new List<string>()
            {
                "// Stencil setup for gbuffer",
                "Stencil",
                "{",
                "   WriteMask 7",       // [_StencilWriteMask]    // default: StencilMask.Lighting  (fixed at compile time)
                "   Ref  2",            // [_StencilRef]          // default: StencilLightingUsage.RegularLighting  (fixed at compile time)
                "   Comp Always",
                "   Pass Replace",
                "}"
            },
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassGBuffer.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
        };

        Pass m_PassGBufferWithPrepass = new Pass()
        {
            Name = "GBufferWithPrepass",
            LightMode = "GBufferWithPrepass",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_GBUFFER",
            StencilOverride = new List<string>()
            {
                "// Stencil setup for GBufferWithPrepass",
                "Stencil",
                "{",
                "   WriteMask 7",       // _StencilWriteMask    // StencilMask.Lighting  (fixed at compile time)
                "   Ref  2",            // _StencilRef          // StencilLightingUsage.RegularLighting  (fixed at compile time)
                "   Comp Always",
                "   Pass Replace",
                "}"
            },
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
                "#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassGBuffer.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "Meta",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            CullOverride = "Cull Off",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassLightTransport.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassShadowCaster = new Pass()
        {
            Name = "ShadowCaster",
            LightMode = "ShadowCaster",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_SHADOWS",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define USE_LEGACY_UNITY_MATRIX_VARIABLES",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,          // TODO: remove all but the alpha below
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassDepthOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthOnly",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,          // TODO: remove all but the alpha below
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "Motion Vectors",
            LightMode = "MotionVectors",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_VELOCITY",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassVelocity.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            StencilOverride = new List<string>()
            {
                "// If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity",
                "Stencil",
                "{",
                "   WriteMask 128",         // [_StencilWriteMaskMV]        (int) HDRenderPipeline.StencilBitMask.ObjectVelocity   // this requires us to pull in the HD Pipeline assembly...
                "   Ref 128",               // [_StencilRefMV]
                "   Comp Always",
                "   Pass Replace",
                "}"
            }
        };

        Pass m_PassDistortion = new Pass()
        {
            Name = "Distortion",
            LightMode = "DistortionVectors",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_DISTORTION",
            BlendOverride = "Blend One One, One One",   // [_DistortionSrcBlend] [_DistortionDstBlend], [_DistortionBlurSrcBlend] [_DistortionBlurDstBlend]
            BlendOpOverride = "BlendOp Add, Add",       // Add, [_DistortionBlurBlendOp]
            ZTestOverride = "ZTest LEqual",             // [_ZTestModeDistortion]
            ZWriteOverride = "ZWrite Off",
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDistortion.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassTransparentDepthPrepass = new Pass()
        {
            Name = "TransparentDepthPrepass",
            LightMode = "TransparentDepthPrepass",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_PREPASS",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassTransparentBackface = new Pass()
        {
            Name = "TransparentBackface",
            LightMode = "TransparentBackface",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_FORWARD",
            CullOverride = "Cull Front",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
                "#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS",
                "#pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassForward.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassForward = new Pass()
        {
            Name = "Forward",
            LightMode = "Forward",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_FORWARD",
            ExtraDefines = new List<string>()
            {
                "#pragma multi_compile _ DEBUG_DISPLAY",
                "#pragma multi_compile _ LIGHTMAP_ON",
                "#pragma multi_compile _ DIRLIGHTMAP_COMBINED",
                "#pragma multi_compile _ DYNAMICLIGHTMAP_ON",
                "#pragma multi_compile _ SHADOWS_SHADOWMASK",
                "#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS",
                "#pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST"
            },
            StencilOverride = new List<string>()
            {
                "// Stencil setup for forward",
                "Stencil",
                "{",
                "   WriteMask 7",       // [_StencilWriteMask]    // default: StencilMask.Lighting  (fixed at compile time)
                "   Ref  2",            // [_StencilRef]          // default: StencilLightingUsage.RegularLighting  (fixed at compile time)
                "   Comp Always",
                "   Pass Replace",
                "}"
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassForward.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        Pass m_PassTransparentDepthPostpass = new Pass()
        {
            Name = "TransparentDepthPostpass",
            LightMode = "TransparentDepthPostpass",
            TemplateName = "HDPBRPass.template",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            ColorMaskOverride = "ColorMask 0",
            ExtraDefines = new List<string>()
            {
                "#define CUTOFF_TRANSPARENT_DEPTH_POSTPASS",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/ShaderPass/ShaderPassDepthOnly.hlsl\"",
            },
            PixelShaderSlots = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            }
        };

        private static string GetVariantDefines(PBRMasterNode masterNode)
        {
            ShaderGenerator defines = new ShaderGenerator();

            // TODO:
            // _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
            // _MATERIAL_FEATURE_TRANSMISSION
            // _MATERIAL_FEATURE_ANISOTROPY
            // _MATERIAL_FEATURE_CLEAR_COAT
            // _MATERIAL_FEATURE_IRIDESCENCE

            switch (masterNode.model)
            {
                case PBRMasterNode.Model.Metallic:
                    break;
                case PBRMasterNode.Model.Specular:
                    defines.AddShaderChunk("#define _MATERIAL_FEATURE_SPECULAR_COLOR 1", true);
                    break;
                default:
                    // TODO: error!
                    break;
            }

            // #pragma shader_feature _ALPHATEST_ON
            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
            {
                defines.AddShaderChunk("#define _ALPHATEST_ON 1", true);
            }

            // #pragma shader_feature _DOUBLESIDED_ON
            //             if (kDoubleSidedEnable)
            //             {
            //                 defines.AddShaderChunk("#define _DOUBLESIDED_ON 1", true);
            //             }

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
            //
            // #pragma shader_feature _NORMALMAP
            if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
            {
                defines.AddShaderChunk("#define _NORMALMAP 1", true);
            }

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

            // MaterialId are used as shader feature to allow compiler to optimize properly
            // Note _MATID_STANDARD is not define as there is always the default case "_". We assign default as _MATID_STANDARD, so we never test _MATID_STANDARD
            // #pragma shader_feature _ _MATID_SSS _MATID_ANISO _MATID_SPECULAR _MATID_CLEARCOAT

            // enable dithering LOD crossfade
            // #pragma multi_compile _ LOD_FADE_CROSSFADE
            // TODO: We should have this keyword only if VelocityInGBuffer is enable, how to do that ?
            //#pragma multi_compile VELOCITYOUTPUT_OFF VELOCITYOUTPUT_ON

            return defines.GetShaderString(2);
        }

        private static bool GenerateShaderPass(PBRMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions, ShaderGenerator result)
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

            if (graphRequirements.requiresVertexColor)
                graphInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.VertexColor), false);

            if (graphRequirements.requiresScreenPosition)
                graphInputs.AddShaderChunk(string.Format("float4 {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in graphRequirements.requiresMeshUVs.Distinct())
                graphInputs.AddShaderChunk(string.Format("half4 {0};", channel.GetUVName()), false);

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
            GraphUtil.GenerateSurfaceDescriptionStruct(graphOutputs, slots, true, "GraphOutputs");

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
            graph.AddShaderChunk(graphInputs.GetShaderString(2), false);
            graph.AddShaderChunk("// Graph Outputs", false);
            graph.AddShaderChunk(graphOutputs.GetShaderString(2), false);
            graph.AddShaderChunk("// ShaderGraph Properties", false);
            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);

            graph.AddShaderChunk("// Graph evaluation", false);
            graph.AddShaderChunk(graphEvalFunction.GetShaderString(2), false);

            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();
            var stencilVisitor = new ShaderGenerator();

            if (pass.BlendOverride != null)
            {
                blendingVisitor.AddShaderChunk(pass.BlendOverride, true);
            }
            else
            {
                materialOptions.GetBlend(blendingVisitor);
            }

            if (pass.BlendOpOverride != null)
            {
                blendingVisitor.AddShaderChunk(pass.BlendOpOverride, true);
            }

            if (pass.CullOverride != null)
            {
                cullingVisitor.AddShaderChunk(pass.CullOverride, true);
            }
            else
            {
                materialOptions.GetCull(cullingVisitor);
            }

            if (pass.ZTestOverride != null)
            {
                zTestVisitor.AddShaderChunk(pass.ZTestOverride, true);
            }
            else
            {
                materialOptions.GetDepthTest(zTestVisitor);
            }

            if (pass.ZWriteOverride != null)
            {
                zWriteVisitor.AddShaderChunk(pass.ZWriteOverride, true);
            }
            else
            {
                materialOptions.GetDepthWrite(zWriteVisitor);
            }

            if (pass.ColorMaskOverride != null)
            {
                // TODO!
            }

            if (pass.StencilOverride != null)
            {
                foreach (var str in pass.StencilOverride)
                {
                    stencilVisitor.AddShaderChunk(str, false);
                }
            }

            var interpolators = new ShaderGenerator();
            var localVertexShader = new ShaderGenerator();
            var localPixelShader = new ShaderGenerator();
            var localSurfaceInputs = new ShaderGenerator();
            var surfaceOutputRemap = new ShaderGenerator();
            var packedInterpolatorCode = new ShaderGenerator();
            var interpolatorDefines = new ShaderGenerator();

            // TODO: remove this call
            ShaderGenerator.GenerateStandardTransforms(
                3,
                10,
                interpolators,
                localVertexShader,
                localPixelShader,
                localSurfaceInputs,
                graphRequirements,
                modelRequirements,
                CoordinateSpace.World);

            HashSet<string> activeFields;
            HDRPShaderStructs.Generate(
                interpolatorDefines,
                packedInterpolatorCode,
                graphRequirements,
                modelRequirements,
                CoordinateSpace.World,
                out activeFields);

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
            namedFragments.Add("${Interpolators}",          interpolators.GetShaderString(3));
            namedFragments.Add("${VertexShader}",           localVertexShader.GetShaderString(3));
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
            namedFragments.Add("${Stencil}",                stencilVisitor.GetShaderString(2));
            namedFragments.Add("${LOD}",                    materialOptions.lod.ToString());
            namedFragments.Add("${VariantDefines}",         GetVariantDefines(masterNode));

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

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode)
        {
            var masterNode = iMasterNode as PBRMasterNode;

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
                    materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
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
                            materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        case AlphaMode.Additive:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        // TODO: other blend modes
                    }
                }

                // Add tags at the SubShader level
                {
                    var tagsVisitor = new ShaderGenerator();
//                    tagsVisitor.AddShaderChunk("Tags{ \"RenderPipeline\" = \"HDRP\"}", true);
                    materialOptions.GetTags(tagsVisitor);
                    subShader.AddShaderChunk(tagsVisitor.GetShaderString(0), false);
                }

                // generate the necessary shader passes
                bool opaque = (masterNode.surfaceType == SurfaceType.Opaque);
                bool transparent = (masterNode.surfaceType != SurfaceType.Opaque);
                bool distortionActive = false;
                bool transparentDepthPrepassActive = transparent && false;
                bool transparentBackfaceActive = transparent && false;
                bool transparentDepthPostpassActive = transparent && false;

                if (opaque)
                {
                    GenerateShaderPass(masterNode, m_PassGBuffer, mode, materialOptions, subShader);
                    GenerateShaderPass(masterNode, m_PassGBufferWithPrepass, mode, materialOptions, subShader);
                }

                GenerateShaderPass(masterNode, m_PassMETA, mode, materialOptions, subShader);
                GenerateShaderPass(masterNode, m_PassShadowCaster, mode, materialOptions, subShader);

                if (opaque)
                {
                    GenerateShaderPass(masterNode, m_PassDepthOnly, mode, materialOptions, subShader);
                    GenerateShaderPass(masterNode, m_PassMotionVectors, mode, materialOptions, subShader);
                }

                if (distortionActive)
                {
                    GenerateShaderPass(masterNode, m_PassDistortion, mode, materialOptions, subShader);
                }

                if (transparentDepthPrepassActive)
                {
                    GenerateShaderPass(masterNode, m_PassTransparentDepthPrepass, mode, materialOptions, subShader);
                }

                if (transparentBackfaceActive)
                {
                    GenerateShaderPass(masterNode, m_PassTransparentBackface, mode, materialOptions, subShader);
                }

                GenerateShaderPass(masterNode, m_PassForward, mode, materialOptions, subShader);

                if (transparentDepthPostpassActive)
                {
                    GenerateShaderPass(masterNode, m_PassTransparentDepthPostpass, mode, materialOptions, subShader);
                }
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }

/*
        public ShaderGenerator GetPipelineProperties(PBRMasterNode masterNode, GenerationMode mode)         // TODO: remove ?
        {
            ShaderGenerator props = new ShaderGenerator();

            props.AddShaderChunk("// Stencil state", false);
            props.AddShaderChunk("[HideInInspector] _StencilRef(\"_StencilRef\", Int) = 2 // StencilLightingUsage.RegularLighting  (fixed at compile time)", false);
            props.AddShaderChunk("[HideInInspector] _StencilWriteMask(\"_StencilWriteMask\", Int) = 7 // StencilMask.Lighting  (fixed at compile time)", false);
            props.AddShaderChunk("[HideInInspector] _StencilRefMV(\"_StencilRefMV\", Int) = 128 // StencilLightingUsage.RegularLighting  (fixed at compile time)", false);
            props.AddShaderChunk("[HideInInspector] _StencilWriteMaskMV(\"_StencilWriteMaskMV\", Int) = 128 // StencilMask.ObjectsVelocity  (fixed at compile time)", false);
            props.AddShaderChunk("", false);
            props.AddShaderChunk("// Blending state", false);
            props.AddShaderChunk("[HideInInspector] _SurfaceType(\"__surfacetype\", Float) = 0.0", false);
            props.AddShaderChunk("[HideInInspector] _BlendMode(\"__blendmode\", Float) = 0.0", false);
            props.AddShaderChunk("[HideInInspector] _SrcBlend(\"__src\", Float) = 1.0", false);             // TODO: we can hard code these now, no need to do it on the fly
            props.AddShaderChunk("[HideInInspector] _DstBlend(\"__dst\", Float) = 0.0", false);             // TODO: we can hard code these now, no need to do it on the fly
            props.AddShaderChunk("[HideInInspector] _ZWrite(\"__zw\", Float) = 1.0", false);                // TODO: we can hard code these now, no need to do it on the fly
            props.AddShaderChunk("[HideInInspector] _CullMode(\"__cullmode\", Float) = 2.0", false);
            props.AddShaderChunk("[HideInInspector] _CullModeForward(\"__cullmodeForward\", Float) = 2.0 // This mode is dedicated to Forward to correctly handle backface then front face rendering thin transparent", false);
            props.AddShaderChunk("[HideInInspector] _ZTestMode(\"_ZTestMode\", Int) = 8", false);

            return props;
        }
*/
    }
}
