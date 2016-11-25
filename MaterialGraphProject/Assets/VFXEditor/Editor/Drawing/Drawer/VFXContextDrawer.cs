using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;
using RMGUI.GraphView;

namespace UnityEditor.VFXEditor.Drawing
{
    public class VFXFlowAnchor : AbstractNodeAnchor<VFXFlowEdgeDrawData>
    {
        public VFXFlowAnchor(AnchorDrawData data)
            : base(data)
        {

        }
    }

    public class VFXNodeDrawer : AbstractNodeDrawer
    {
        public VFXNodeDrawer()
        {
        }

        protected virtual void AddChildren()
        {
            var view = this.GetFirstAncestorOfType<GraphView>();
            var data = (VFXNodeDrawData)dataProvider;
            foreach (var child in data.m_NodeChildren)
            {
                var drawer = VFXGraphView.GlobalDataMapper.Create(child);
                AddChild(drawer);
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            ClearChildren();
            AddChildren();
        }
    }

    public class VFXContextDrawer : VFXNodeDrawer
    {
        private VisualElement m_Title;
        private VisualElement m_BlockContainer;

        private VFXFlowAnchor m_InputAnchor;
        private VFXFlowAnchor m_OutputAnchor;

        public VFXContextDrawer()
        {}

        private void UpdateSlots(VFXContextDrawData data)
        {
            if (m_InputAnchor != null)
                RemoveChild(m_InputAnchor);
            if (m_OutputAnchor != null)
                RemoveChild(m_OutputAnchor);

            m_InputAnchor = new VFXFlowAnchor(data.inputAnchor);
            m_InputAnchor.name = "input";
            AddChild(m_InputAnchor);

            if (data.outputAnchor != null)
            {
                m_OutputAnchor = new VFXFlowAnchor(data.outputAnchor);
                m_OutputAnchor.name = "output";
                AddChild(m_OutputAnchor);
            }
            else
                m_OutputAnchor = null;
        }

        protected override void AddChildren()
        {
            base.AddChildren();

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

            UpdateSlots((VFXContextDrawData)dataProvider);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            ClearChildren();
            AddChildren();
           // Debug.Log("RECREATE CHILDREN!");

            var data = dataProvider as VFXContextDrawData;
            if (data == null)
            {
                m_Title.content.text = "";
                return;
            }

            m_Title.content.text = data.title + " " + (m_Counter++).ToString();
            borderColor = !data.selected ? data.color : Color.white;

            this.Touch(ChangeType.Repaint);
        }

        private int m_Counter = 0;
    }
}
