using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public interface IShaderProperty
    {
        string displayName { get; set; }

        string referenceName { get; set; }
        Guid guid { get; }

        PropertyType propertyType { get; }
        bool generatePropertyBlock { get; set; }
        Vector4 defaultValue { get; }

        string GetPropertyBlockString();
        string GetPropertyDeclarationString(string delimiter = ";");

        string GetPropertyAsArgumentString();

        PreviewProperty GetPreviewMaterialProperty();
        INode ToConcreteNode();
    }
}
