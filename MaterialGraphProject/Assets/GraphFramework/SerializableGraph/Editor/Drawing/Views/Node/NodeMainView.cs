using System.Collections;
using System.Collections.Generic;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class NodeMainView : VisualContainer, IEnumerable<VisualElement>
    {
        public void Add(VisualElement child)
        {
            AddChild(child);
        }

        public IEnumerator<VisualElement> GetEnumerator()
        {
            return GetChildren();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
