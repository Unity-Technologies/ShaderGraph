using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.VFXEditor.Drawing
{
    public class VFXBlockDrawer : VFXNodeDrawer
    {
        private VisualElement m_Title;

        public VFXBlockDrawer()
        {
            //Debug.Log("CREATE VFX BLOCK DRAWER !!!!");

            m_Title = new VisualElement()
            {
                name = "title",
                content = new GUIContent(),
                pickingMode = PickingMode.Ignore
            };
            AddChild(m_Title);
        }
    }
}