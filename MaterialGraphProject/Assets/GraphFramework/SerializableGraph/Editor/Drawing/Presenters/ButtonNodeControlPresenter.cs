using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Graphing.Drawing;
using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public class ButtonNodeControlPresenter : AbstractNodeControlPresenter
    {
        private Action m_ClickAction;

        public override void OnGUIHandler()
        {
            if (GUILayout.Button("Click me!") && m_ClickAction != null)
            {
                m_ClickAction();
            }
        }

        public override float height
        {
            get { return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.standardVerticalSpacing; }
        }

        protected ButtonNodeControlPresenter()
        {
        }

        public void Initialize(Action clickAction)
        {
            m_ClickAction = clickAction;
        }
    }
}
