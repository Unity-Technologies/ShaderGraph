using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeControlListView : ListContainerView<NodeControlView, AbstractNodeControlPresenter>
    {
        public NodeControlListView(AbstractNodeControlListPresenter presenter) : base(presenter)
        {
        }

        protected override NodeControlView Instantiate(AbstractNodeControlPresenter presenter)
        {
            return new NodeControlView(presenter);
        }
    }

    public abstract class AbstractNodeControlListPresenter : AbstractListContainerPresenter
    {
    }
}
