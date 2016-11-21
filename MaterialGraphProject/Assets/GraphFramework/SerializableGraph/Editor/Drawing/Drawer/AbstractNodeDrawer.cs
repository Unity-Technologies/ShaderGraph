using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEditor.Graphing.Util;

namespace UnityEditor.Graphing.Drawing
{
    public class AbstractNodeDrawer : GraphElement
    {   
        protected bool m_CurrentExpanded;

        public AbstractNodeDrawer()
        {
            //AddToClassList("AbstractNodeDrawer");
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodeData = dataProvider as AbstractNodeDrawData;

            if (nodeData == null)
            {
                ClearChildren();
                return;
            }

            if (!nodeData.expanded)
            {
                if (!classList.Contains("collapsed"))
                    AddToClassList("collapsed");
            }
            else
            {
                if (classList.Contains("collapsed"))
                    RemoveFromClassList("collapsed");
            }

            m_CurrentExpanded = nodeData.expanded;
        }
    }
}
