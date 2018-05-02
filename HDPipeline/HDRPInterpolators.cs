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
            // disabling "warning CS0169: The field 'xxx' is never used" as these are optional.
            #pragma warning disable 0169
            [Semantic("POSITION")]             Vector3 positionOS;
            [Semantic("NORMAL")][Optional]     Vector3 normalOS;
            [Semantic("TANGENT")][Optional]    Vector4 tangentOS;       // Stores bi-tangent sign in w
            [Semantic("TEXCOORD0")][Optional]  Vector2 uv0;
            [Semantic("TEXCOORD1")][Optional]  Vector2 uv1;
            [Semantic("TEXCOORD2")][Optional]  Vector2 uv2;
            [Semantic("TEXCOORD3")][Optional]  Vector2 uv3;
            [Semantic("COLOR")][Optional]      Vector4 color;
            #pragma warning restore  0169
        };

        struct VaryingsMeshToPS
        {
            #pragma warning disable 0169
            [Semantic("SV_Position")]           Vector4 positionCS;
            [Optional]                          Vector3 positionWS;
            [Optional]                          Vector3 normalWS;
            [Optional]                          Vector4 tangentWS;      // w contain mirror sign
            [Optional]                          Vector2 texCoord0;
            [Optional]                          Vector2 texCoord1;
            [Optional]                          Vector2 texCoord2;
            [Optional]                          Vector2 texCoord3;
            [Optional]                          Vector4 color;
            [Optional] [Semantic("FRONT_FACE_SEMANTIC")] [OverrideType("FRONT_FACE_TYPE")] [PreprocessorIf("SHADER_STAGE_FRAGMENT")]
                                                bool cullFace;
            #pragma warning restore 0169

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
            #pragma warning disable 0169
            Vector3 positionWS;
            Vector3 normalWS;
            [Optional]      Vector4 tangentWS;
            [Optional]      Vector2 texCoord0;
            [Optional]      Vector2 texCoord1;
            [Optional]      Vector2 texCoord2;
            [Optional]      Vector2 texCoord3;
            [Optional]      Vector4 color;
            #pragma warning restore 0169

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
            #pragma warning disable 0169
            [Optional] Vector3 ObjectSpaceNormal;
            [Optional] Vector3 ViewSpaceNormal;
            [Optional] Vector3 WorldSpaceNormal;
            [Optional] Vector3 TangentSpaceNormal;

            [Optional] Vector3 ObjectSpaceTangent;
            [Optional] Vector3 ViewSpaceTangent;
            [Optional] Vector3 WorldSpaceTangent;
            [Optional] Vector3 TangentSpaceTangent;

            [Optional] Vector3 ObjectSpaceBiTangent;
            [Optional] Vector3 ViewSpaceBiTangent;
            [Optional] Vector3 WorldSpaceBiTangent;
            [Optional] Vector3 TangentSpaceBiTangent;

            [Optional] Vector3 ObjectSpaceViewDirection;
            [Optional] Vector3 ViewSpaceViewDirection;
            [Optional] Vector3 WorldSpaceViewDirection;
            [Optional] Vector3 TangentSpaceViewDirection;

            [Optional] Vector3 ObjectSpacePosition;
            [Optional] Vector3 ViewSpacePosition;
            [Optional] Vector3 WorldSpacePosition;
            [Optional] Vector3 TangentSpacePosition;

            [Optional] Vector4 screenPosition;
            [Optional] Vector4 uv0;
            [Optional] Vector4 uv1;
            [Optional] Vector4 uv2;
            [Optional] Vector4 uv3;
            [Optional] Vector4 vertexColor;
            #pragma warning restore 0169

            public static Dependency[] dependencies = new Dependency[]
            {
                new Dependency("GraphInputs.WorldSpaceNormal",          "FragInputs.worldToTangent"),
                new Dependency("GraphInputs.ObjectSpaceNormal",         "GraphInputs.WorldSpaceNormal"),
                new Dependency("GraphInputs.ViewSpaceNormal",           "GraphInputs.WorldSpaceNormal"),

                new Dependency("GraphInputs.WorldSpaceTangent",         "FragInputs.worldToTangent"),
                new Dependency("GraphInputs.ObjectSpaceTangent",        "GraphInputs.WorldSpaceTangent"),
                new Dependency("GraphInputs.ViewSpaceTangent",          "GraphInputs.WorldSpaceTangent"),

                new Dependency("GraphInputs.WorldSpaceBiTangent",       "FragInputs.worldToTangent"),
                new Dependency("GraphInputs.ObjectSpaceBiTangent",      "GraphInputs.WorldSpaceBiTangent"),
                new Dependency("GraphInputs.ViewSpaceBiTangent",        "GraphInputs.WorldSpaceBiTangent"),

                new Dependency("GraphInputs.WorldSpacePosition",        "FragInputs.positionWS"),
                new Dependency("GraphInputs.ObjectSpacePosition",       "FragInputs.positionWS"),
                new Dependency("GraphInputs.ViewSpacePosition",         "FragInputs.positionWS"),

                new Dependency("GraphInputs.ObjectSpaceViewDirection",  "GraphInputs.WorldSpaceViewDirection"),
                new Dependency("GraphInputs.ViewSpaceViewDirection",    "GraphInputs.WorldSpaceViewDirection"),
                new Dependency("GraphInputs.TangentSpaceViewDirection", "GraphInputs.WorldSpaceViewDirection"),
                new Dependency("GraphInputs.TangentSpaceViewDirection", "GraphInputs.WorldSpaceTangent"),
                new Dependency("GraphInputs.TangentSpaceViewDirection", "GraphInputs.WorldSpaceBiTangent"),
                new Dependency("GraphInputs.TangentSpaceViewDirection", "GraphInputs.WorldSpaceNormal"),

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
                if ((requirements.requiresNormal & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("GraphInputs.ObjectSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("GraphInputs.ViewSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("GraphInputs.WorldSpaceNormal");

                if ((requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("GraphInputs.TangentSpaceNormal");
            }

            if (requirements.requiresTangent != 0)
            {
                if ((requirements.requiresTangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("GraphInputs.ObjectSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("GraphInputs.ViewSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("GraphInputs.WorldSpaceTangent");

                if ((requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("GraphInputs.TangentSpaceTangent");
            }

            if (requirements.requiresBitangent != 0)
            {
                if ((requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("GraphInputs.ObjectSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("GraphInputs.ViewSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("GraphInputs.WorldSpaceBiTangent");

                if ((requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("GraphInputs.TangentSpaceBiTangent");
            }

            if (requirements.requiresViewDir != 0)
            {
                if ((requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("GraphInputs.ObjectSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("GraphInputs.ViewSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("GraphInputs.WorldSpaceViewDirection");

                if ((requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("GraphInputs.TangentSpaceViewDirection");
            }

            if (requirements.requiresPosition != 0)
            {
                if ((requirements.requiresPosition & NeededCoordinateSpace.Object) > 0)
                    activeFields.Add("GraphInputs.ObjectSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.View) > 0)
                    activeFields.Add("GraphInputs.ViewSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.World) > 0)
                    activeFields.Add("GraphInputs.WorldSpacePosition");

                if ((requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0)
                    activeFields.Add("GraphInputs.TangentSpacePosition");
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
            List<string> passRequiredFields,            // fields the pass requires
            CoordinateSpace preferedCoordinateSpace,
            HashSet<string> activeFields)
        {
            if (preferedCoordinateSpace == CoordinateSpace.Tangent)
                preferedCoordinateSpace = CoordinateSpace.World;

            // build initial requirements
//            var combinedRequirements = graphRequirements.Union(modelRequirements);
            AddActiveFieldsFromGraphRequirements(activeFields, graphRequirements);
            AddActiveFieldsFromModelRequirements(activeFields, modelRequirements);
            if (passRequiredFields != null)
            {
                foreach (var requiredField in passRequiredFields)
                {
                    activeFields.Add(requiredField);
                }
            }

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

            // generate code based on requirements
            ShaderSpliceUtil.BuildType(typeof(AttributesMesh), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(VaryingsMeshToPS), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(VaryingsMeshToDS), activeFields, codeResult);
            ShaderSpliceUtil.BuildPackedType(typeof(VaryingsMeshToPS), activeFields, codeResult);
            ShaderSpliceUtil.BuildPackedType(typeof(VaryingsMeshToDS), activeFields, codeResult);
            ShaderSpliceUtil.BuildType(typeof(GraphInputs), activeFields, codeResult);
        }
    };
}
