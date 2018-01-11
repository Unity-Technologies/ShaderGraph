using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MasterNode : AbstractMaterialNode, IMasterNode
    {
        public override bool hasPreview
        {
            get { return true; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public abstract string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures);
    }
}
