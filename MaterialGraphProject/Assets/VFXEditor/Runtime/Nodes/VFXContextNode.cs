using System;
using UnityEngine;

namespace UnityEngine.VFXEditor
{
    [Flags]
    public enum VFXContextType
    {
        kNone = 0,

        kInit = 1 << 0,
        kUpdate = 1 << 1,
        kOutput = 1 << 2,

        kInitAndUpdate = kInit | kUpdate,
        kAll = kInit | kUpdate | kOutput,
    }

    [Serializable]
    public class VFXContextNode : VFXNode<VFXSystemNode, VFXNode>
    {
        private VFXContextNode()
        {
            // Create slot
        }

        public VFXContextNode(VFXContextType type) : this()
        {
            m_Type = type;
            switch (m_Type)
            {
                case VFXContextType.kInit:      name = "Init"; break;
                case VFXContextType.kUpdate:    name = "Update"; break;
                case VFXContextType.kOutput:    name = "Output"; break;
            }
        }

        public VFXContextType ContextType
        {
            get { return m_Type; }
        }

       /* public VFXFlowSlot GetInputFlowSlot()
        {
            return m_InputSlot;
        }

        public VFXFlowSlot GetOutputFlowSlot();*/

        [SerializeField]
        private VFXContextType m_Type;

        // Not serialized. Reconstructed at initialization
       // private VFXFlowSlot m_InputSlot;
      //  private VFXFlowSlot m_OutputSlot; // Can be null
    }
}