using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.VFXEditor;

namespace UnityEditor.VFXEditor.Drawing
{
    [Serializable]
    public class VFXContextDrawData : AbstractNodeDrawData
    {
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

           // var inputFlow = contextNode.GetInputFlowSlot();
           // var outputFlow = contextNode.GetOutputFlowSlot();

          /*  var data = CreateInstance<AnchorDrawData>();
            data.Initialize(inputFlow); 
            m_Children.Add(data);

            if (outputFlow != null)
            {
                data = CreateInstance<AnchorDrawData>();
                data.Initialize(outputFlow);
                m_Children.Add(data);
            }*/
        }
    }
}
