using System;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.VFXEditor;

namespace UnityEditor.VFXEditor.Drawing
{
    [Serializable]
    public class VFXContextDrawData : AbstractNodeDrawData
    {
        private AnchorDrawData m_InputAnchor;
        private AnchorDrawData m_OutputAnchor;

        public AnchorDrawData inputAnchor
        {
            get { return m_InputAnchor; }
        }

        public AnchorDrawData outputAnchor
        {
            get { return m_OutputAnchor; }
        }

        public string title
        {
            get { return node.name; }
        }

        public Color color
        {
            get
            {
                switch (((VFXContextNode)node).ContextType)
                {
                    case VFXContextType.kInit:      return Color.yellow;
                    case VFXContextType.kUpdate:    return Color.blue;
                    case VFXContextType.kOutput:    return Color.red;
                    default:                        return Color.magenta;
                }
            }
        }

        public override void Initialize(INode node)
        {
            base.Initialize(node);

            var contextNode = (VFXContextNode)node;

            var inputFlow = contextNode.GetInputFlowSlot();
            var outputFlow = contextNode.GetOutputFlowSlot();

            m_InputAnchor = CreateInstance<AnchorDrawData>();
            m_InputAnchor.Initialize(inputFlow);
            m_InputAnchor.orientation = Orientation.Vertical;
            m_InputAnchor.capabilities |= Capabilities.Floating;
            m_Children.Add(m_InputAnchor);

            if (outputFlow != null)
            {
                m_OutputAnchor = CreateInstance<AnchorDrawData>();
                m_OutputAnchor.Initialize(outputFlow);
                m_OutputAnchor.orientation = Orientation.Vertical;
                m_OutputAnchor.capabilities |= Capabilities.Floating;
                m_Children.Add(m_OutputAnchor);
            }
        }
    }
}
