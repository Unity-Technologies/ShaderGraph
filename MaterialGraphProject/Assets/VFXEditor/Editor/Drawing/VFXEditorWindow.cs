using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.VFXEditor;
using Edge = UnityEngine.Graphing.Edge;

namespace UnityEditor.VFXEditor.Drawing
{
    public class VFXGraphAsset : ScriptableObject, IGraphAsset
    {
        public IGraph graph { get { return m_Graph; } }
        public bool shouldRepaint { get { return false; } }
        public ScriptableObject GetScriptableObject() { return this; }
        public void OnEnable() {}

        private IGraph m_Graph = new VFXGraph();
    }

    public class VFXGraphDataSource : SerializedGraphDataSource
    {
        protected override void AddTypeMappings()
        {
            AddTypeMapping(typeof(VFXContextNode), typeof(VFXContextDrawData));
            AddTypeMapping(typeof(VFXSystemNode), typeof(VFXNodeDrawData));
           // AddTypeMapping(typeof(Edge), typeof(EdgeDrawData));
            AddTypeMapping(typeof(VFXFlowEdge), typeof(VFXFlowEdgeDrawData));
        }
    }

    public class VFXEditorWindow : AbstractGraphEditWindow<VFXGraphAsset>
    {
        [MenuItem("Window/VFX Editor")]
        public static void OpenMenu()
        {
            GetWindow<VFXEditorWindow>();
        }

        void OnSelectionChange()
        {
            // TODO tmp
            // Just to bypass base implementation
        }

        public override AbstractGraphDataSource CreateDataSource()
        {
            var dataSource = CreateInstance<VFXGraphDataSource>();
            dataSource.Initialize(CreateInstance<VFXGraphAsset>()); 
            return dataSource;
        }

        public override GraphView CreateGraphView()
        {
            return new VFXGraphView();
        }
    }
}
