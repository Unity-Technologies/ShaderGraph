using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeCollapseButtonPresenter : AbstractNodeCollapseButtonPresenter
    {
        [SerializeField] private bool m_Expanded;

        public override bool expanded
        {
            get { return m_Expanded; }
        }

        private NodeCollapseButtonPresenter()
        {
        }

        public void Initialize(bool expanded)
        {
            m_Expanded = expanded;
        }
    }
}
