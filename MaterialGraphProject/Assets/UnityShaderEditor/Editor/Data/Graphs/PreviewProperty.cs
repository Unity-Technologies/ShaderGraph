using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public struct PreviewProperty
    {
        public string name;
        public PropertyType propType;

        public Color colorValue;
        public Texture textureValue;
        public Cubemap cubemapValue;
        public Vector4 vector4Value;
        public float floatValue;
    }

    public static class PreviewPropertyExtensions
    {
        public static void SetPreviewProperty(this MaterialPropertyBlock block, PreviewProperty previewProperty)
        {
            if (previewProperty.propType == PropertyType.Texture && previewProperty.textureValue != null)
                block.SetTexture(previewProperty.name, previewProperty.textureValue);
            else if (previewProperty.propType == PropertyType.Cubemap && previewProperty.cubemapValue != null)
                block.SetTexture(previewProperty.name, previewProperty.cubemapValue);
            else if (previewProperty.propType == PropertyType.Color)
                block.SetColor(previewProperty.name, previewProperty.colorValue);
            else if (previewProperty.propType == PropertyType.Vector2 || previewProperty.propType == PropertyType.Vector3 || previewProperty.propType == PropertyType.Vector4)
                block.SetVector(previewProperty.name, previewProperty.vector4Value);
            else if (previewProperty.propType == PropertyType.Float)
                block.SetFloat(previewProperty.name, previewProperty.floatValue);
        }
    }
}
