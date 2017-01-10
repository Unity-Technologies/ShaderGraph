using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeTitlePresenter : AbstractNodeTitlePresenter
    {
        [SerializeField] private string m_Text;

        public override string text
        {
            get { return m_Text; }
        }

        private NodeTitlePresenter()
        {
        }

        public void Initialize(string text)
        {
            m_Text = text;
        }
    }
}
