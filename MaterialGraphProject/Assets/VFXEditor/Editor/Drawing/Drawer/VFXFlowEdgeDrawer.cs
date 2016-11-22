using RMGUI.GraphView;
using UnityEngine;
using Edge = RMGUI.GraphView.Edge;

namespace UnityEditor.VFXEditor.Drawing
{
    public class VFXFlowEdgeDrawer : Edge
    {
        protected override void DrawEdge(IStylePainter painter)
        {
            var edgeData = GetData<EdgeData>();
            if (edgeData == null)
                return;

            IConnector outputData = edgeData.output;
            IConnector inputData = edgeData.input;

            if (outputData == null && inputData == null)
                return;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);

            Color edgeColor = (GetData<EdgeData>() != null && GetData<EdgeData>().selected) ? Color.white : Color.grey;

            Orientation orientation = outputData != null ? outputData.orientation : inputData.orientation;

            Vector3[] points, tangents;
            GetTangents(orientation, from, to, out points, out tangents);
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 24);
            
            // Debug control points
            //Handles.DrawLine(points[0], tangents[0]);
            //Handles.DrawLine(tangents[0], tangents[1]);
            //Handles.DrawLine(tangents[1], points[1]);
        }
    }
}