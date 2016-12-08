using System;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.VFXEditor;
using RMGUI.GraphView;
using Object = UnityEngine.Object;

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

        protected void AddNodeChildren(VisualContainer container)
        {
            var data = (VFXNodeDrawData)dataProvider;
            foreach (var child in data.m_NodeChildren)
            {
                var drawer = VFXGraphView.GlobalDataMapper.Create(child);
                container.AddChild(drawer);
            }
        }

       /* public override void OnDataChanged()
        {
            base.OnDataChanged();

            /ClearChildren();
            AddNodeChildren(this);
        }*/
    }

    public class VFXContextDrawer : VFXNodeDrawer
    {
        private VisualElement m_Title;
        private VisualContainer m_BlockContainer;

        private VFXFlowAnchor m_InputAnchor;
        private VFXFlowAnchor m_OutputAnchor;

        private Manipulator m_Menu = null;

        public VFXContextDrawer()
        {
            m_Menu = new ContextualMenu(DoContextMenu);

            m_BlockContainer = new VisualContainer()
            {
                name = "container",
                pickingMode = PickingMode.Position
            };

            m_BlockContainer.AddManipulator(m_Menu);

            //m_BlockContainer.AddManipulator(new ContextualMenu(DoContextMenu));
        }

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

        protected void AddChildren()
        {
           // AddManipulator(m_Menu);

            m_Title = new VisualElement()
            {
                name = "title",
                content = new GUIContent(),
                pickingMode = PickingMode.Ignore
            };
            AddChild(m_Title);

           /* m_BlockContainer = new VisualContainer()
            {
                name = "container",
                pickingMode = PickingMode.Position
            };*/         

            AddChild(m_BlockContainer);

            AddNodeChildren(m_BlockContainer);

            m_InputAnchor = null;
            m_OutputAnchor = null;

            UpdateSlots((VFXContextDrawData)dataProvider);
        }

        protected EventPropagation DoContextMenu(Event evt, Object customData)
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Add Block"), false, AddBlock, null);
            gm.ShowAsContext();

            return EventPropagation.Stop;
        }

        private void AddBlock(object obj)
        {
            VFXContextDrawData data = (VFXContextDrawData)dataProvider;
            VFXContextNode node = (VFXContextNode)data.node;

            var view = this.GetFirstAncestorOfType<SerializableGraphView>();
            if (view != null)
            {
                AbstractGraphDataSource dataSource = (AbstractGraphDataSource)view.dataSource;
                var block = new VFXBlockNode();
                node.AddChild(block);
                dataSource.AddNode(block);
                //data.Initialize(node, dataSource); 
            }

            //m_BlockContainer.Touch(ChangeType.Repaint);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            ClearChildren();
           // RemoveManipulator(m_Menu);
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
