using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class GraphNodeView : GraphElement
    {
        private NodeTitleView m_NodeTitleView;
        private NodeCollapseButtonView m_NodeCollapseButtonView;
        private NodeControlListView m_NodeControlListView;
        private NodeAsideView m_NodeAsideView;

        public GraphNodeView()
        {
            AddChild(new HorizontalNodeView
            {
                new HorizontalNodePaneView
                {
                    new NodeMainView
                    {
                        new NodeHeaderView
                        {
                            {m_NodeTitleView = new NodeTitleView(null)},
                            {m_NodeCollapseButtonView = new NodeCollapseButtonView(null)}
                        }
                    },
                    {m_NodeControlListView = new NodeControlListView(null)},
                    {m_NodeAsideView = new NodeAsideView(null)}
                }
            });
            classList = ClassList.empty;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var graphNodePresenter = GetPresenter<GraphNodePresenter>();
            if (graphNodePresenter == null)
                return;
            m_NodeTitleView.presenter = graphNodePresenter.nodeTitlePresenter;
            m_NodeCollapseButtonView.presenter = graphNodePresenter.nodeCollapseButtonPresenter;
            m_NodeControlListView.presenter = graphNodePresenter.nodeControlListPresenter;
            m_NodeAsideView.presenter = graphNodePresenter.nodeAsidePresenter;
        }
    }
}
