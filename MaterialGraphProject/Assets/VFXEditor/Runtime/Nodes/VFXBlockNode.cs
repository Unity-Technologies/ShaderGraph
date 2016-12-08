using System;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEngine.VFXEditor
{
    [Serializable]
    public class VFXBlockNode : VFXNode<VFXContextNode, VFXNode>
    {
        public VFXBlockNode()
        {}

        public override bool CanAddChild(VFXNode element, int index = -1)
        {
            return false;
        }
    }
}