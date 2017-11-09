using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal interface ICustomNodeUI
    {
        ModificationScope DrawCustomNodeUI();
    }
}