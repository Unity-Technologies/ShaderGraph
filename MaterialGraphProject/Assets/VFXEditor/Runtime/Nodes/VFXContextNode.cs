using System;
using UnityEngine;
using UnityEngine.Graphing;

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
        {}

        public VFXContextNode(VFXContextType type) : this()
        {
            m_Type = type;
            switch (m_Type)
            {
                case VFXContextType.kInit:      name = "Init"; break;
                case VFXContextType.kUpdate:    name = "Update"; break;
                case VFXContextType.kOutput:    name = "Output"; break;
            }

            m_InputSlot = new VFXFlowSlot(this,SlotType.Input);
            m_OutputSlot = m_Type != VFXContextType.kOutput ? new VFXFlowSlot(this,SlotType.Output) : null;

            AddSlot(m_InputSlot);
            if (m_OutputSlot != null)
                AddSlot(m_OutputSlot);
        }

        public VFXContextType ContextType
        {
            get { return m_Type; }
        }

        public VFXFlowSlot GetInputFlowSlot()
        {
            return m_InputSlot;
        }

        public VFXFlowSlot GetOutputFlowSlot()
        {
            return m_OutputSlot;
        }

        [SerializeField]
        private VFXContextType m_Type;

        private VFXFlowSlot m_InputSlot;
        private VFXFlowSlot m_OutputSlot; // Can be null
    }
}