using System;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.VFXEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.VFXEditor.Drawing
{
    // This is a hack to allow data mapper to be accessible outside graph view
    public class VFXGraphViewDataMapper : GraphViewDataMapper
    {
        public VFXGraphViewDataMapper()
        {
            this[typeof(EdgeData)] = typeof(Edge);
            this[typeof(AbstractNodeDrawData)] = typeof(AbstractNodeDrawer);

            // VFX Specific
            this[typeof(VFXNodeDrawData)] = typeof(VFXNodeDrawer);
            this[typeof(VFXContextDrawData)] = typeof(VFXContextDrawer);
            this[typeof(VFXBlockDrawData)] = typeof(VFXBlockDrawer);
            this[typeof(VFXFlowEdgeDrawData)] = typeof(VFXFlowEdgeDrawer);
        }
    }

    [StyleSheet("Assets/VFXEditor/Editor/Drawing/Styles/VFXGraph.uss")]
    public class VFXGraphView : SerializableGraphView
    {
       public static VFXGraphViewDataMapper GlobalDataMapper
       {
           get { return s_dataMapper; }
       }
       private static VFXGraphViewDataMapper s_dataMapper = null;

        public VFXGraphView()
        {
            AddManipulator(new ContextualMenu(DoContextMenu));

            // Hack!
            var vfxDataMapper = new VFXGraphViewDataMapper();
            dataMapper = s_dataMapper = vfxDataMapper;
        }

        protected EventPropagation DoContextMenu(Event evt, Object customData)
        {
            var gm = new GenericMenu();

            var canvasPos = contentViewContainer.GlobalToLocal(evt.mousePosition);

            Debug.Log("TRANSFORM: " + contentViewContainer.transform);
            Debug.Log("MOUSEPOS: " + evt.mousePosition);
            Debug.Log("CANVASPOS: " + canvasPos);

            gm.AddItem(new GUIContent("Init"),      false, AddNode, new AddNodeCreationObject(VFXContextType.kInit, canvasPos));
            gm.AddItem(new GUIContent("Update"),    false, AddNode, new AddNodeCreationObject(VFXContextType.kUpdate, canvasPos));
            gm.AddItem(new GUIContent("Output"),    false, AddNode, new AddNodeCreationObject(VFXContextType.kOutput, canvasPos));

            gm.ShowAsContext();

            return EventPropagation.Continue;
        }

        private class AddNodeCreationObject
        {
            public Vector2 m_Pos;
            public readonly VFXContextType m_Type;

            public AddNodeCreationObject(VFXContextType t, Vector2 p)
            {
                m_Type = t;
                m_Pos = p;
            }
        };

        private void AddNode(object obj)
        {
            // TODO Temp test
            AddNodeCreationObject creationData = obj as AddNodeCreationObject;

            bool valid = creationData != null && (
                creationData.m_Type == VFXContextType.kInit ||
                creationData.m_Type == VFXContextType.kUpdate ||
                creationData.m_Type == VFXContextType.kOutput);

            if (valid)
            {
                Debug.Log("CREATE NODE OF TYPE " + creationData.m_Type + " AT " + creationData.m_Pos);

                VFXContextNode node = new VFXContextNode(creationData.m_Type);

                var drawState = node.drawState;
                drawState.position = new Rect(creationData.m_Pos.x, creationData.m_Pos.y, 0, 0);
                node.drawState = drawState;

                // TODO
                /*VFXSystemNode system = new VFXSystemNode();
                system.AddChild(node);*/

                var graphDataSource = dataSource as AbstractGraphDataSource;
                graphDataSource.AddNode(node);
            }
        }
    }
}