using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public interface IPresentableView<out T> where T : ScriptableObject
    {
        T presenter { get; }
        VisualContainer parent { get; }
    }
}
