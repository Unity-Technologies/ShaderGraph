using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeDrawData : AbstractNodeDrawData
    {
        protected NodeDrawData()
        {}

        protected virtual IEnumerable<GraphElementData> GetControlData()
        {
            return new ControlDrawData[0];
        }

        public override void Initialize(INode inNode)
        {
            base.Initialize(inNode);
            if (node == null)
                return;

            var m_HeaderData = CreateInstance<HeaderDrawData>();
            m_HeaderData.Initialize(inNode);
            m_Children.Add(m_HeaderData);

            foreach (var input in node.GetSlots<ISlot>())
            {
                var data = CreateInstance<AnchorDrawData>();
                data.Initialize(input); 
                m_Children.Add(data);
            }

            var controlData = GetControlData();
            m_Children.AddRange(controlData);
        }
    }
}
