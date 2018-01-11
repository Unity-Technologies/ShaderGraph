using System.Collections.Generic;
using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.SurfaceShader
{
    public class CreatePBRSurfaceShader : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/PBR SurfaceShader", false, 208)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePBRSurfaceShader>(),
                "New Surface Shader.SurfaceShader", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var pbrSurface = new PBRSurfaceShader();
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(pbrSurface, true));
            AssetDatabase.Refresh();
        }
    }

    public class PBRSurfaceShader : SurfaceShader
    {
        [SerializeField]
        ShaderGraphRequirements m_Requirements = ShaderGraphRequirements.none;

        public ShaderGraphRequirements requirements
        {
            get { return m_Requirements; }
            set { m_Requirements = value; }
        }

        public enum Model
        {
            Specular,
            Metallic
        }

        public enum AlphaMode
        {
            Opaque,
            AlphaBlend,
            AdditiveBlend
        }

        [SerializeField]
        private Model m_Model = Model.Metallic;

        public Model model
        {
            get { return m_Model; }
            set
            {
                if (m_Model == value)
                    return;

                m_Model = value;
            }
        }

        [SerializeField]
        private AlphaMode m_AlphaMode;
        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
            }
        }

        [SerializeField]
        bool m_UsePerPixelNormal;

        public bool usePerPixelNormal
        {
            get { return m_UsePerPixelNormal; }
            set { m_UsePerPixelNormal = value; }
        }

        [SerializeField]
        bool m_UseAlphaClip;
        public bool useAlphaClip
        {
            get { return m_UseAlphaClip; }
            set { m_UseAlphaClip = value; }
        }

        public override string GetShader(out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            var shaderProperties = new PropertyCollector();

            foreach (var prop in properties)
                shaderProperties.AddShaderProperty(prop);

            var finalShader = new ShaderGenerator();
            finalShader.AddShaderChunk(string.Format(@"Shader ""{0}""", name), false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();

            finalShader.AddShaderChunk("Properties", false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesBlock(2), false);
            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            var lwSub = new LightWeightPBRSubShader();
            foreach (var subshader in lwSub.GetSubshaderFromSurfaceShader(this))
                finalShader.AddShaderChunk(subshader, true);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            configuredTextures = shaderProperties.GetConfiguredTexutres();
            return finalShader.GetShaderString(0);
        }
    }
}