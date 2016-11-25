using System;
using UnityEngine;

namespace UnityEngine.VFXEditor
{
    [Serializable]
    public class VFXSystemNode : VFXNode<VFXNode, VFXContextNode>
    {
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            for (int i = 1; i < m_Children.Count; ++i)
            {
                // TODO create flow edges
            }
        }
    }
}