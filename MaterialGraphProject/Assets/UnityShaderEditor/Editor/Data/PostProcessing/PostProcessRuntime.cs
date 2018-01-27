using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
	public class PostProcessRuntime
	{
		private string GetRuntimeFromTemplate(
			string template, 
			string path, 
			PostProcessMasterNode masterNode,
			PropertyCollector properties)
		{
			var templateLocation = ShaderGenerator.GetTemplatePath(template);
			if (!File.Exists(templateLocation))
                return string.Empty;

            var runtimeTemplate = File.ReadAllText(templateLocation);

			string[] pathSplit = path.Split('/');
			var name = RemoveWhitespace(pathSplit[pathSplit.Length-1]);

			ShaderStringBuilder propertyList = new ShaderStringBuilder();
			ShaderStringBuilder propertySets = new ShaderStringBuilder();

			foreach (var prop in properties.properties.Where(x => x.generatePropertyBlock))
            {
                switch(prop.propertyType)
				{
					case PropertyType.Color:
						ColorShaderProperty colorProperty = (ColorShaderProperty)prop;
						string colorValue = "new Color ("+colorProperty.value.r+"f, "+colorProperty.value.g+"f, "+colorProperty.value.b+"f, "+colorProperty.value.a+"f)";
						propertyList.AppendLine("public ColorParameter "+colorProperty.displayName+" = new ColorParameter { value = "+colorValue+" };");
						propertySets.AppendLine("sheet.properties.SetColor(\""+colorProperty.referenceName+"\", settings."+colorProperty.displayName+".value);");
						break;
					case PropertyType.Texture:
						TextureShaderProperty textureProperty = (TextureShaderProperty)prop;
						propertyList.AppendLine("public TextureParameter "+textureProperty.displayName+" = new TextureParameter { value = "+textureProperty.value+" };");
						propertySets.AppendLine("sheet.properties.SetTexture(\""+textureProperty.referenceName+"\", settings."+textureProperty.displayName+".value);");
						break;
					case PropertyType.Cubemap:
						CubemapShaderProperty cubemapProperty = (CubemapShaderProperty)prop;
						propertyList.AppendLine("public TextureParameter "+cubemapProperty.displayName+" = new TextureParameter { value = "+cubemapProperty.value+" };");
						propertySets.AppendLine("sheet.properties.SetTexture(\""+cubemapProperty.referenceName+"\", settings."+cubemapProperty.displayName+".value);");
						break;
					case PropertyType.Boolean:
						BooleanShaderProperty booleanProperty = (BooleanShaderProperty)prop;
						propertyList.AppendLine("public BoolParameter "+booleanProperty.displayName+" = new BoolParameter { value = "+booleanProperty.value+" };");
						propertySets.AppendLine("sheet.properties.SetFloat(\""+booleanProperty.referenceName+"\", settings."+booleanProperty.displayName+".value ? 1 : 0;");
						break;
					case PropertyType.Vector1:
						Vector1ShaderProperty vector1Property = (Vector1ShaderProperty)prop;
						propertyList.AppendLine("public FloatParameter "+vector1Property.displayName+" = new FloatParameter { value = "+vector1Property.value+"f };");
						propertySets.AppendLine("sheet.properties.SetFloat(\""+vector1Property.referenceName+"\", settings."+vector1Property.displayName+".value);");
						break;
					case PropertyType.Vector2:
						Vector2ShaderProperty vector2Property = (Vector2ShaderProperty)prop;
						propertyList.AppendLine("public Vector2Parameter "+vector2Property.displayName+" = new Vector2Parameter { value = "+vector2Property.value+" };");
						propertySets.AppendLine("sheet.properties.SetVector(\""+vector2Property.referenceName+"\", settings."+vector2Property.displayName+".value);");
						break;
					case PropertyType.Vector3:
						Vector3ShaderProperty vector3Property = (Vector3ShaderProperty)prop;
						propertyList.AppendLine("public Vector3Parameter "+vector3Property.displayName+" = new Vector3Parameter { value = "+vector3Property.value+" };");
						propertySets.AppendLine("sheet.properties.SetVector(\""+vector3Property.referenceName+"\", settings."+vector3Property.displayName+".value);");
						break;
					case PropertyType.Vector4:
						Vector4ShaderProperty vector4Property = (Vector4ShaderProperty)prop;
						propertyList.AppendLine("public Vector4Parameter "+vector4Property.displayName+" = new Vector4Parameter { value = "+vector4Property.value+" };");
						propertySets.AppendLine("sheet.properties.SetVector(\""+vector4Property.referenceName+"\", settings."+vector4Property.displayName+".value);");
						break;
				}
            }

			var runtime = runtimeTemplate.Replace("${Name}", name);
			runtime = runtime.Replace("${Event}", masterNode.evt.ToString());
			runtime = runtime.Replace("${Path}", path);
			runtime = runtime.Replace("${Properties}", propertyList.ToString());
			runtime = runtime.Replace("${SetMaterialProperties}", propertySets.ToString());
			
			return runtime;
		}

		private string RemoveWhitespace(string input)
		{
			return new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());
		}

		public void BuildRuntime(
			string path, 
			PostProcessMasterNode masterNode,
			PropertyCollector properties)
		{
			var runtimeResult = GetRuntimeFromTemplate(
                    "postProcessRuntime.template",
					path,
                    masterNode,
					properties);

			string[] pathSplit = path.Split('/');
			var name = pathSplit[pathSplit.Length-1];
			/*string[] assetGuids = AssetDatabase.FindAssets (string.Format("{0} t:script", name));
			string runtime;
			if(assetGuids.Length != 0)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[0]);
				AssetDatabase.DeleteAsset(assetPath);
			}*/
			string copyPath = "Assets/" + name + ".cs";
			using (StreamWriter outfile = 
                 new StreamWriter(copyPath))
                 {
                     outfile.Write(runtimeResult);
            }
			AssetDatabase.Refresh();
		}
	}
}
