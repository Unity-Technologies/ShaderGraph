using System.Collections;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeAsideView : DataWatchContainer, IPresentableView<AbstractNodeAsidePresenter>, IEnumerable<VisualElement>
    {
        private AbstractNodeAsidePresenter m_Presenter;
        private VisualContainer m_ChildContainer;

        public NodeAsideView(AbstractNodeAsidePresenter presenter)
        {
            m_ChildContainer = new VisualContainer() { name = "container" };
            this.presenter = presenter;
        }

        public AbstractNodeAsidePresenter presenter
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

        public void Add(VisualElement child)
        {
            m_ChildContainer.AddChild(child);
        }

        public override void OnDataChanged()
        {
            var shown = presenter == null || presenter.shown;
            if (shown && childrenCount == 0)
                AddChild(m_ChildContainer);
            if (!shown && childrenCount > 0)
                ClearChildren();
        }

        public IEnumerator<VisualElement> GetEnumerator()
        {
            return GetChildren();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected override object toWatch
        {
            get { return m_Presenter; }
        }
    }

    public abstract class AbstractNodeAsidePresenter : ScriptableObject
    {
        public abstract bool shown { get; }
    }
}
