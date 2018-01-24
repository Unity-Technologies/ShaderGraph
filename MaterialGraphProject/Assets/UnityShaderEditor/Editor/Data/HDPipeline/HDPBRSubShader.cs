using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;

//using UnityEngine.Experimental.Rendering.HDPipeline;		// This requires us to make ShaderGraph assembly depend on the HDPipeline assembly... probably not a good idea


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
            public List<int> VertexShaderSlots;
            public List<int> PixelShaderSlots;
            public List<string> StencilOverride;
        }

        Pass m_PassGBuffer = new Pass()
        {
            Name = "GBuffer",
            LightMode = "GBuffer",
            ShaderPassName = "SHADERPASS_GBUFFER",
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitSharePass.hlsl\"",
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
            }
        };

        Pass m_PassGBufferWithPrepass = new Pass()
        {
            Name = "GBufferWithPrepass",
            LightMode = "GBufferWithPrepass",
            ShaderPassName = "SHADERPASS_GBUFFER",
            ExtraDefines = new List<string>()
            {
                "#define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitSharePass.hlsl\"",
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
            }
        };

        Pass m_PassGBufferDebugDisplay = new Pass()
        {
            Name = "GBufferDebugDisplay",
            LightMode = "GBufferDebugDisplay",
            ShaderPassName = "SHADERPASS_GBUFFER",
            ExtraDefines = new List<string>()
            {
                "#define DEBUG_DISPLAY",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitSharePass.hlsl\"",
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
            }
        };

        Pass m_PassMETA = new Pass()
        {
            Name = "META",
            LightMode = "Meta",
            ShaderPassName = "SHADERPASS_LIGHT_TRANSPORT",
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitSharePass.hlsl\"",
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
            ShaderPassName = "SHADERPASS_SHADOWS",
            ExtraDefines = new List<string>()
            {
                "#define USE_LEGACY_UNITY_MATRIX_VARIABLES",
            },
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitDepthPass.hlsl\"",
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

        Pass m_PassDepthOnly = new Pass()
        {
            Name = "DepthOnly",
            LightMode = "DepthOnly",
            ShaderPassName = "SHADERPASS_DEPTH_ONLY",
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitDepthPass.hlsl\"",
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

        Pass m_PassMotionVectors = new Pass()
        {
            Name = "Motion Vectors",
            LightMode = "MotionVectors",
            ShaderPassName = "SHADERPASS_VELOCITY",
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitVelocityPass.hlsl\"",
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
                "   WriteMask 128",     	// + (int) HDRenderPipeline.StencilBitMask.ObjectVelocity,         // [_StencilWriteMaskMV]		// this requires us to pull in the HD Pipeline assembly...
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
            ShaderPassName = "SHADERPASS_DISTORTION",
            Includes = new List<string>()
            {
                "#include \"HDRP/Material/Lit/ShaderPass/LitDistortionPass.hlsl\"",
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


        private static string GetVariantDefines(PBRMasterNode masterNode)
        {
            ShaderGenerator defines = new ShaderGenerator();

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
            // #pragma shader_feature _ _MAPPING_PLANAR _MAPPING_TRIPLANAR			// MOVE to a node
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
            // #pragma shader_feature _DETAIL_MAP									// MOVE to a node
            // #pragma shader_feature _SUBSURFACE_RADIUS_MAP
            // #pragma shader_feature _THICKNESSMAP
            // #pragma shader_feature _SPECULARCOLORMAP
            // #pragma shader_feature _TRANSMITTANCECOLORMAP

            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.alphaMode != PBRMasterNode.AlphaMode.Opaque)
            {
                // transparent-only defines
                defines.AddShaderChunk("#define _SURFACE_TYPE_TRANSPARENT 1", true);

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == PBRMasterNode.AlphaMode.AlphaBlend)
                {
                    defines.AddShaderChunk("#define _BLENDMODE_ALPHA 1", true);
                }
                else if (masterNode.alphaMode == PBRMasterNode.AlphaMode.AdditiveBlend)
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


        private static string GetShaderPassFromTemplate(string template, PBRMasterNode masterNode, Pass pass, GenerationMode mode, SurfaceMaterialOptions materialOptions)
        {
            var builder = new ShaderStringBuilder();
            builder.IncreaseIndent();
            builder.IncreaseIndent();

            var vertexInputs = new ShaderGenerator();
            var surfaceDescriptionFunction = new ShaderGenerator();
            var surfaceDescriptionStruct = new ShaderGenerator();
            var surfaceVertexShader = new ShaderGenerator();
            var functionRegistry = new FunctionRegistry(builder);
            var surfaceInputs = new ShaderGenerator();

            var shaderProperties = new PropertyCollector();

            surfaceInputs.AddShaderChunk("struct SurfaceInputs{", false);
            surfaceInputs.Indent();

            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode, NodeUtils.IncludeSelf.Include, pass.PixelShaderSlots);

            var requirements = ShaderGraphRequirements.FromNodes(activeNodeList);

            var modelRequiements = ShaderGraphRequirements.none;
            modelRequiements.requiresNormal |= NeededCoordinateSpace.World;
            modelRequiements.requiresTangent |= NeededCoordinateSpace.World;
            modelRequiements.requiresBitangent |= NeededCoordinateSpace.World;
            modelRequiements.requiresPosition |= NeededCoordinateSpace.World;
            modelRequiements.requiresViewDir |= NeededCoordinateSpace.World;
            modelRequiements.requiresMeshUVs.Add(UVChannel.uv1);

            GraphUtil.GenerateApplicationVertexInputs(requirements.Union(modelRequiements), vertexInputs, 0, 8);
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
            GraphUtil.GenerateSurfaceDescriptionStruct(surfaceDescriptionStruct, slots, true);

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
            graph.AddShaderChunk("// Builder", false);
            graph.AddShaderChunk(builder.ToString(), false);
            graph.AddShaderChunk("// Vertex Inputs", false);
            graph.AddShaderChunk(vertexInputs.GetShaderString(2), false);
            graph.AddShaderChunk("// Surface Inputs", false);
            graph.AddShaderChunk(surfaceInputs.GetShaderString(2), false);
            graph.AddShaderChunk("// Surface Description", false);
            graph.AddShaderChunk(surfaceDescriptionStruct.GetShaderString(2), false);
            graph.AddShaderChunk("// Shadergraph Properties", false);
            graph.AddShaderChunk(shaderProperties.GetPropertiesDeclaration(2), false);
            graph.AddShaderChunk("// Vertex Shader", false);
            graph.AddShaderChunk(surfaceVertexShader.GetShaderString(2), false);
            graph.AddShaderChunk("// Surface Function", false);
            graph.AddShaderChunk(surfaceDescriptionFunction.GetShaderString(2), false);

            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();
            var stencilVisitor = new ShaderGenerator();

            materialOptions.GetTags(tagsVisitor);
            materialOptions.GetBlend(blendingVisitor);
            materialOptions.GetCull(cullingVisitor);
            materialOptions.GetDepthTest(zTestVisitor);
            materialOptions.GetDepthWrite(zWriteVisitor);

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

            ShaderGenerator.GenerateStandardTransforms(
                3,
                10,
                interpolators,
                localVertexShader,
                localPixelShader,
                localSurfaceInputs,
                requirements,
                modelRequiements,
                CoordinateSpace.World);

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

            var templateLocation = ShaderGenerator.GetTemplatePath(template);

            foreach (var slot in usedSlots)
            {
                surfaceOutputRemap.AddShaderChunk(string.Format("{0} = surf.{0};", slot.shaderOutputName), true);
            }

            if (!File.Exists(templateLocation))
                return string.Empty;

            var shaderPassIncludes = new ShaderGenerator();
            if (pass.Includes != null)
            {
                foreach (var include in pass.Includes)
                {
                    shaderPassIncludes.AddShaderChunk(include, true);
                }
            }

            var subShaderTemplate = File.ReadAllText(templateLocation);
            var resultPass = subShaderTemplate.Replace("${Defines}", defines.GetShaderString(2));
            resultPass = resultPass.Replace("${Graph}", graph.GetShaderString(3));
            resultPass = resultPass.Replace("${Interpolators}", interpolators.GetShaderString(3));
            resultPass = resultPass.Replace("${VertexShader}", localVertexShader.GetShaderString(3));
            resultPass = resultPass.Replace("${LocalPixelShader}", localPixelShader.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceInputs}", localSurfaceInputs.GetShaderString(3));
            resultPass = resultPass.Replace("${SurfaceOutputRemap}", surfaceOutputRemap.GetShaderString(3));
            resultPass = resultPass.Replace("${LightMode}", pass.LightMode);
            resultPass = resultPass.Replace("${PassName}", pass.Name);
            resultPass = resultPass.Replace("${Includes}", shaderPassIncludes.GetShaderString(2));

            resultPass = resultPass.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${Stencil}", stencilVisitor.GetShaderString(2));
            resultPass = resultPass.Replace("${LOD}", "" + materialOptions.lod);
            resultPass = resultPass.Replace("${VariantDefines}", GetVariantDefines(masterNode));
            return resultPass;
        }


        public string GetSubshader(PBRMasterNode masterNode, GenerationMode mode)
        {
            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
//              subShader.AddShaderChunk("Tags{ \"RenderPipeline\" = \"HDRP\"}", true);

                var materialOptions = new SurfaceMaterialOptions();
                switch (masterNode.alphaMode)
                {
                    case PBRMasterNode.AlphaMode.Opaque:
                        materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                        materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                        materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                        materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                        materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
                        materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Geometry;
                        materialOptions.renderType = SurfaceMaterialOptions.RenderType.Opaque;
                        break;
                    case PBRMasterNode.AlphaMode.AlphaBlend:
                        materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                        materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                        materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                        materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                        materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                        materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                        materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                        break;
                    case PBRMasterNode.AlphaMode.AdditiveBlend:
                        materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                        materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                        materialOptions.cullMode = SurfaceMaterialOptions.CullMode.Back;
                        materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                        materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                        materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                        materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                        break;
                }

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassGBuffer, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassDepthOnly, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassMETA, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassShadowCaster, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassGBufferWithPrepass, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassGBufferDebugDisplay, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassMotionVectors, mode, materialOptions), true);

                subShader.AddShaderChunk(
                    GetShaderPassFromTemplate("HDPBRPass.template", masterNode, m_PassDistortion, mode, materialOptions), true);

                //              var extraPassesTemplateLocation = ShaderGenerator.GetTemplatePath("lightweightPBRExtraPasses.template");
                //              if (File.Exists(extraPassesTemplateLocation))
                //                  subShader.AddShaderChunk(File.ReadAllText(extraPassesTemplateLocation), true);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }

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
    }
}
