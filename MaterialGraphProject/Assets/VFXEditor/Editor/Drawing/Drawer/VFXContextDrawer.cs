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

        private NodeAnchor m_InputAnchor;
        private NodeAnchor m_OutputAnchor;

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

            m_InputAnchor = null;
            m_OutputAnchor = null;
        }

        private void UpdateSlots(VFXContextDrawData data)
        {
            if (m_InputAnchor != null)
                RemoveChild(m_InputAnchor);
            if (m_OutputAnchor != null)
                RemoveChild(m_OutputAnchor);

            m_InputAnchor = new NodeAnchor(data.inputAnchor);
            m_InputAnchor.name = "input";
            AddChild(m_InputAnchor);

            if (data.outputAnchor != null)
            {
                m_OutputAnchor = new NodeAnchor(data.outputAnchor);
                m_OutputAnchor.name = "output";
                AddChild(m_OutputAnchor);
            }
            else
                m_OutputAnchor = null;
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

            UpdateSlots(data);

            this.Touch(ChangeType.Repaint);
        }
    }
}
