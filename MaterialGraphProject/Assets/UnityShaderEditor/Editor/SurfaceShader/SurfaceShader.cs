using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.SurfaceShader
{
    public abstract class SurfaceShader : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_Name = "ShaderTest";

        [SerializeField]
        string m_SurfaceShader = defaultShader;

        [NonSerialized]
        List<IShaderProperty> m_Properties = new List<IShaderProperty>();

        public IEnumerable<IShaderProperty> properties
        {
            get { return m_Properties; }
        }

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        const string defaultShader = @"
SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) 
{
	SurfaceDescription surface = (SurfaceDescription)0;
	return surface;
}";

        public string surfaceShader
        {
            get { return string.IsNullOrEmpty(m_SurfaceShader) ? defaultShader : m_SurfaceShader; }
            set { m_SurfaceShader = value; }
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

        public void OnBeforeSerialize()
        {
            m_SerializedProperties = SerializationHelper.Serialize<IShaderProperty>(m_Properties);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Properties = SerializationHelper.Deserialize<IShaderProperty>(m_SerializedProperties, null);
        }

        public abstract string GetShader(out List<PropertyCollector.TextureInfo> textureInfos);
    }
}
