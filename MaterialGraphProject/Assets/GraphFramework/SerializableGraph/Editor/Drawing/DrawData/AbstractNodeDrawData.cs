using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class AbstractNodeDrawData : GraphElementData
    {
        protected AbstractNodeDrawData()
        {

        }

        public INode node { get; private set; }

        public bool expanded = true;

        protected List<GraphElementData> m_Children = new List<GraphElementData>();

        public IEnumerable<GraphElementData> elements
        {
            get { return m_Children; }
        }

        public virtual void OnModified(ModificationScope scope)
        {
            expanded = node.drawState.expanded;
        }

        public void CommitChanges()
        {
            var drawData = node.drawState;
            drawData.position = position;
            node.drawState = drawData;
        }

        public virtual void Initialize(INode inNode)
        {
            node = inNode;
            capabilities |= Capabilities.Movable;

            if (node == null)
                return;

            name = inNode.name;
            expanded = node.drawState.expanded;

            position = new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0);
        }
    }
}
