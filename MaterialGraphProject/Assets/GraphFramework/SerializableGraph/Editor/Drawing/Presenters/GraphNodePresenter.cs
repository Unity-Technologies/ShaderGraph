using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class GraphNodePresenter : GraphElementPresenter
    {
        [SerializeField] private NodeTitlePresenter m_NodeTitlePresenter;
        [SerializeField] private NodeCollapseButtonPresenter m_NodeCollapseButtonPresenter;
        [SerializeField] private NodeControlListPresenter m_NodeControlListPresenter;
        [SerializeField] private NodeAsidePresenter m_NodeAsidePresenter;

        public NodeTitlePresenter nodeTitlePresenter
        {
            get { return m_NodeTitlePresenter; }
        }

        public NodeCollapseButtonPresenter nodeCollapseButtonPresenter
        {
            get { return m_NodeCollapseButtonPresenter; }
        }

        public NodeAsidePresenter nodeAsidePresenter
        {
            get { return m_NodeAsidePresenter; }
        }

        public NodeControlListPresenter nodeControlListPresenter
        {
            get { return m_NodeControlListPresenter; }
        }

        private GraphNodePresenter()
        {
        }

        public void Initialize(INode node)
        {
            m_NodeTitlePresenter = m_NodeTitlePresenter ?? CreateInstance<NodeTitlePresenter>();
            m_NodeTitlePresenter.Initialize(node.name);

            m_NodeCollapseButtonPresenter = nodeCollapseButtonPresenter ?? CreateInstance<NodeCollapseButtonPresenter>();
            m_NodeCollapseButtonPresenter.Initialize(node.drawState.expanded);

            m_NodeAsidePresenter = m_NodeAsidePresenter ?? CreateInstance<NodeAsidePresenter>();
            m_NodeAsidePresenter.Initialize(node.GetOutputSlots<ISlot>().Any());

            m_NodeControlListPresenter = m_NodeControlListPresenter ?? CreateInstance<NodeControlListPresenter>();
            m_NodeControlListPresenter.Initialize();
        }
    }
}
