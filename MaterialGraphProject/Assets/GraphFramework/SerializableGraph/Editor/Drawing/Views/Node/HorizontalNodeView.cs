using System.Collections;
using System.Collections.Generic;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public sealed class HorizontalNodeView : VisualContainer, IEnumerable<VisualElement>
    {
        public void Add(VisualElement child)
        {
            AddChild(child);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<VisualElement> GetEnumerator()
        {
            return GetChildren();
        }
    }
}
