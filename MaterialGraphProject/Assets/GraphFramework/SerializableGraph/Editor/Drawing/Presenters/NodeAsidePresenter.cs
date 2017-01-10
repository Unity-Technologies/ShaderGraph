using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeAsidePresenter : AbstractNodeAsidePresenter
    {
        [SerializeField] private bool m_Shown;

        public override bool shown
        {
            get { return m_Shown; }
        }

        private NodeAsidePresenter()
        {
        }

        public void Initialize(bool shown)
        {
            m_Shown = shown;
        }
    }
}
