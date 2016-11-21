using System;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.VFXEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.VFXEditor.Drawing
{
    [StyleSheet("Assets/VFXEditor/Editor/Drawing/Styles/VFXGraph.uss")]
    public class VFXGraphView : SerializableGraphView
    {
        public VFXGraphView()
        {
            AddManipulator(new ContextualMenu(DoContextMenu));

            dataMapper[typeof(VFXContextDrawData)] = typeof(VFXContextDrawer);
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

            return EventPropagation.Stop;
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

                var graphDataSource = dataSource as AbstractGraphDataSource;
                graphDataSource.AddNode(node);
            }
        }
    }
}