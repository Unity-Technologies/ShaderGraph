using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeCollapseButtonView : DataWatchContainer
    {
        private AbstractNodeCollapseButtonPresenter m_Presenter;
        private Button m_Button;

        public NodeCollapseButtonView(AbstractNodeCollapseButtonPresenter presenter)
        {
            this.presenter = presenter;
            AddChild(m_Button = new Button(OnClick) {content = new GUIContent("")});
        }

        public NodeCollapseButtonView() : this(null)
        {
        }

        public AbstractNodeCollapseButtonPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;
                RemoveWatch();
                m_Presenter = value;
                OnDataChanged();
                AddWatch();
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            if (m_Presenter != null)
                m_Button.content.text = m_Presenter.expanded ? "collapse" : "expand";
            else
                m_Button.content.text = "";
        }

        private void OnClick()
        {
            if (m_Presenter != null)
                m_Presenter.OnClick();
        }

        protected override object toWatch
        {
            get { return m_Presenter; }
        }
    }

    public abstract class AbstractNodeCollapseButtonPresenter : ScriptableObject
    {
        public abstract bool expanded { get; }

        public virtual void OnClick()
        {
        }
    }
}
