using RMGUI.GraphView;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeTitleView : DataWatchContainer
    {
        private AbstractNodeTitlePresenter m_Presenter;

        public NodeTitleView(AbstractNodeTitlePresenter presenter)
        {
            this.presenter = presenter;
            content = new GUIContent("");
        }

        public AbstractNodeTitlePresenter presenter
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
            content.text = presenter.text;
        }

        protected override object toWatch
        {
            get { return m_Presenter; }
        }
    }

    public abstract class AbstractNodeTitlePresenter : ScriptableObject
    {
        public abstract string text { get; }
    }
}
