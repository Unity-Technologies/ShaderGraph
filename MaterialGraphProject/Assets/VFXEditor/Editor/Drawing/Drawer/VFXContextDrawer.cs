using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.VFXEditor.Drawing
{
    public class VFXContextDrawer : AbstractNodeDrawer
    {
        private VisualElement m_Title;
        private VisualElement m_BlockContainer;

        public VFXContextDrawer()
        {
            m_Title = new VisualElement()
            {
                name = "title",
                content = new GUIContent(),
                pickingMode = PickingMode.Ignore
            };
            AddChild(m_Title);

            m_BlockContainer = new VisualContainer()
            {
                name = "container",
                pickingMode = PickingMode.Ignore
            };
            AddChild(m_BlockContainer);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var data = dataProvider as VFXContextDrawData;

            if (data == null)
            {
                m_Title.content.text = "";
                return;
            }

            m_Title.content.text = data.title;
            borderColor = !data.selected ? data.color : Color.white;
            this.Touch(ChangeType.Repaint);
        }
    }
}
