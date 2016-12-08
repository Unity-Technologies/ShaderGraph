using System;
using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.VFXEditor;

namespace UnityEditor.VFXEditor.Drawing
{
    public class VFXNodeDrawData : AbstractNodeDrawData
    {
        public override void Initialize(INode inNode, AbstractGraphDataSource dataSource)
        {
            base.Initialize(inNode,dataSource);

            var vfxNode = (VFXNode)node;

            m_NodeChildren.Clear();
            foreach (var child in vfxNode.GetChildren())
            {
                var drawType = dataSource.MapType(child.GetType());

                var nodeData = (VFXNodeDrawData)CreateInstance(drawType);
                nodeData.Initialize(child,dataSource);

                m_NodeChildren.Add(nodeData);
            }
        }

        public List<VFXNodeDrawData> m_NodeChildren = new List<VFXNodeDrawData>();
    }
}