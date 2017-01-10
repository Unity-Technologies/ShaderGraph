using UnityEngine;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeControlPresenter : AbstractNodeControlPresenter
    {
        [SerializeField]
        private Vector3 m_Value;

        public override void OnGUIHandler()
        {
            EditorGUILayout.Vector3Field("", m_Value);
        }

        public override float height
        {
            get { return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.standardVerticalSpacing; }
        }

        protected NodeControlPresenter()
        {
        }

        public void Initialize(Vector3 value)
        {
            m_Value = value;
        }
    }
}
