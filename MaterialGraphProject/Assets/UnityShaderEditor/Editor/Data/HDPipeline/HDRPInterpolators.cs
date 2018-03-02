using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;              // Vector3,4
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    // TODO: rename this file to something along the lines of this class:
    public class HDRPShaderStructs
    {
        struct AttributesMesh
        {
            [Semantic("POSITION")]              Vector3 positionOS;
            [Semantic("NORMAL")][Optional]     Vector3 normalOS;
            [Semantic("TANGENT")][Optional]    Vector4 tangentOS;       // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")][Optional]  Vector2 uv0;
            [Semantic("TEXCOORD1")][Optional]  Vector2 uv1;
            [Semantic("TEXCOORD2")][Optional]  Vector2 uv2;
            [Semantic("TEXCOORD3")][Optional]  Vector2 uv3;
            [Semantic("COLOR")][Optional]      Vector4 color;
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
                activeFields.Add("FragInputs.texCoord" + (int)channel);
            }
        }

        public static void Generate(
            ShaderGenerator definesResult,
            ShaderGenerator codeResult,
            ShaderGraphRequirements graphRequirements,
            ShaderGraphRequirements modelRequirements,
            CoordinateSpace preferedCoordinateSpace,
            out HashSet<string> activeFields)
        {
            if (preferedCoordinateSpace == CoordinateSpace.Tangent)
                preferedCoordinateSpace = CoordinateSpace.World;

            // build initial requirements
//            var combinedRequirements = graphRequirements.Union(modelRequirements);
            activeFields = new HashSet<string>();
            AddActiveFieldsFromGraphRequirements(activeFields, graphRequirements);
            AddActiveFieldsFromModelRequirements(activeFields, modelRequirements);

            // propagate requirements using dependencies
            {
                ShaderSpliceUtil.ApplyDependencies(
                    activeFields,
                    new List<Dependency[]>()
                    {
                        FragInputs.dependencies,
                        VaryingsMeshToPS.standardDependencies,
                        GraphInputs.dependencies,
                    });
            }

            definesResult.AddShaderChunk("// ACTIVE FIELDS:", false);
            foreach (string f in activeFields)
            {
                definesResult.AddShaderChunk("// " + f, false);
            }

            // generate code based on requirements
            ShaderSpliceUtil.BuildType(typeof(AttributesMesh), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(VaryingsMeshToPS), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(VaryingsMeshToDS), activeFields, codeResult);
            ShaderSpliceUtil.BuildPackedType(typeof(VaryingsMeshToPS), activeFields, codeResult);
            ShaderSpliceUtil.BuildPackedType(typeof(VaryingsMeshToDS), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(GraphInputs), activeFields, codeResult);

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
