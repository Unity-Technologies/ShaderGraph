using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class ListContainerView<TView, TPresenter> : DataWatchContainer
        where TPresenter : ScriptableObject
        where TView : VisualElement, IPresentableView<TPresenter>
    {
        private AbstractListContainerPresenter m_Presenter;
        
        protected abstract TView Instantiate(TPresenter presenter);

        public ListContainerView(AbstractListContainerPresenter presenter)
        {
            this.presenter = presenter;
        }

        public AbstractListContainerPresenter presenter
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
            if (children.OfType<IPresentableView<ScriptableObject>>().Select(v => v.presenter).SequenceEqual(presenter.presenters))
                return;

            using (var currentViewsDisposable = ListPool<IPresentableView<TPresenter>>.GetDisposable())
            using (var presentersDisposable = ListPool<ScriptableObject>.GetDisposable())
            {
                var currentViews = currentViewsDisposable.value;
                var presenters = presentersDisposable.value;

                currentViews.AddRange(children.OfType<IPresentableView<TPresenter>>());
                currentViews.RemoveAll(v => !m_Presenter.presenters.Contains(v.presenter));

                foreach (var candidatePresenter in m_Presenter.presenters.OfType<TPresenter>())
                {
                    if (currentViews.Any(v => v.presenter == candidatePresenter)) continue;
                    var view = Instantiate(candidatePresenter);
                    if (view != null)
                        currentViews.Add(view);
                }

                presenters.AddRange(presenter.presenters);
                currentViews.Sort((v1, v2) => presenters.IndexOf(v1.presenter) - presenters.IndexOf(v2.presenter));
                ClearChildren();
                foreach (var view in currentViews.OfType<VisualElement>())
                    AddChild(view);
            }
        }

        protected override object toWatch
        {
            get { return m_Presenter; }
        }
    }

    public abstract class AbstractListContainerPresenter : ScriptableObject
    {
        public abstract IEnumerable<ScriptableObject> presenters { get; }
    }
}
