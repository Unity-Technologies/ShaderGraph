using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MasterNode : AbstractMaterialNode, IMasterNode
    {
        public MasterNode()
        {
            name = "MasterNode";
            UpdateNodeAfterDeserialization();
        }

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

        public virtual ShaderGraphRequirements GetNodeSpecificRequirements()
        {
            return ShaderGraphRequirements.none;
        }
        
        public abstract IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper);
    }
}
