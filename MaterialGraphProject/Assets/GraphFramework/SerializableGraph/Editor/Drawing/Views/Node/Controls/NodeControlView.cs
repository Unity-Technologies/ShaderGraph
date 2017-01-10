using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeControlView : DataWatchContainer, IPresentableView<AbstractNodeControlPresenter>
    {
        private AbstractNodeControlPresenter m_Presenter;
        private IMGUIContainer m_ImguiContainer;

        public NodeControlView(AbstractNodeControlPresenter presenter)
        {
            AddChild(m_ImguiContainer = new IMGUIContainer
            {
                OnGUIHandler = presenter.OnGUIHandler,
                pickingMode = PickingMode.Position,
                height = presenter.height
            });
            this.presenter = presenter;
        }

        public override void OnDataChanged()
        {
            m_ImguiContainer.OnGUIHandler = presenter.OnGUIHandler;
            m_ImguiContainer.height = presenter.height;
        }

        public AbstractNodeControlPresenter presenter
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

        protected override object toWatch
        {
            get { return m_Presenter; }
        }
    }

    public abstract class AbstractNodeControlPresenter : ScriptableObject
    {
        public abstract void OnGUIHandler();
        public abstract float height { get; }
    }
}
