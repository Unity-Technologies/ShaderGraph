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

			string[] propertyChunks = GetProperties(properties);

            var runtimeTemplate = File.ReadAllText(templateLocation);
			var runtime = runtimeTemplate.Replace("${Name}", NameFromFilepath(path, true));
			runtime = runtime.Replace("${Event}", masterNode.evt.ToString());
			runtime = runtime.Replace("${Path}", path);
			runtime = runtime.Replace("${Properties}", propertyChunks[0].ToString());
			runtime = runtime.Replace("${GetUserData}", GetUserData());
			runtime = runtime.Replace("${SetMaterialProperties}", propertyChunks[1].ToString());
			runtime = runtime.Replace("${SetUserData}", SetUserData());
			return runtime;
		}

		private string[] GetProperties(PropertyCollector properties)
		{
			ShaderStringBuilder propertyList = new ShaderStringBuilder();
			ShaderStringBuilder propertySets = new ShaderStringBuilder();

			foreach (var prop in properties.properties.Where(x => x.generatePropertyBlock))
            {
                switch(prop.propertyType)
				{
					case PropertyType.Color:
						ColorShaderProperty colorProperty = (ColorShaderProperty)prop;
						string colorValue = "new Color ("+colorProperty.value.r+"f, "+colorProperty.value.g+"f, "+colorProperty.value.b+"f, "+colorProperty.value.a+"f)";
						propertyList.AppendLine("public ColorParameter "+RemoveWhitespace(colorProperty.displayName)+" = new ColorParameter { value = "+colorValue+" };");
						propertySets.AppendLine("sheet.properties.SetColor(\""+colorProperty.referenceName+"\", settings."+RemoveWhitespace(colorProperty.displayName)+".value);");
						break;
					case PropertyType.Texture:
						TextureShaderProperty textureProperty = (TextureShaderProperty)prop;
						propertyList.AppendLine("public TextureParameter "+RemoveWhitespace(textureProperty.displayName)+" = new TextureParameter ();");
						propertySets.AppendLine("if(settings."+RemoveWhitespace(textureProperty.displayName)+".value != null)");
						using (propertySets.BlockScope())
							propertySets.AppendLine("sheet.properties.SetTexture(\""+textureProperty.referenceName+"\", settings."+RemoveWhitespace(textureProperty.displayName)+".value);");
						break;
					case PropertyType.Cubemap:
						CubemapShaderProperty cubemapProperty = (CubemapShaderProperty)prop;
						propertyList.AppendLine("public TextureParameter "+RemoveWhitespace(cubemapProperty.displayName)+" = new TextureParameter ();");
						propertySets.AppendLine("if(settings."+RemoveWhitespace(cubemapProperty.displayName)+".value != null)");
						using (propertySets.BlockScope())
							propertySets.AppendLine("sheet.properties.SetTexture(\""+cubemapProperty.referenceName+"\", settings."+RemoveWhitespace(cubemapProperty.displayName)+".value);");
						//propertyList.AppendLine("public TextureParameter "+RemoveWhitespace(cubemapProperty.displayName)+" = new TextureParameter { value = "+cubemapProperty.value+" };");
						//propertySets.AppendLine("sheet.properties.SetTexture(\""+cubemapProperty.referenceName+"\", settings."+RemoveWhitespace(cubemapProperty.displayName)+".value);");
						break;
					case PropertyType.Boolean:
						BooleanShaderProperty booleanProperty = (BooleanShaderProperty)prop;
						string booleanValue = booleanProperty.value == true ? "true" : "false";
						propertyList.AppendLine("public BoolParameter "+RemoveWhitespace(booleanProperty.displayName)+" = new BoolParameter { value = "+booleanValue+" };");
						propertySets.AppendLine("sheet.properties.SetFloat(\""+booleanProperty.referenceName+"\", settings."+RemoveWhitespace(booleanProperty.displayName)+".value ? 1 : 0);");
						break;
					case PropertyType.Vector1:
						Vector1ShaderProperty vector1Property = (Vector1ShaderProperty)prop;
						string vector1Value = vector1Property.value + "f";
						switch(vector1Property.floatType)
						{
							case FloatType.Slider:
								propertyList.AppendLine("[Range("+vector1Property.rangeValues.x+"f, "+vector1Property.rangeValues.y+"f)]public FloatParameter "+RemoveWhitespace(vector1Property.displayName)+" = new FloatParameter { value = "+vector1Value+" };");
								break;
							case FloatType.Integer:
								propertyList.AppendLine("public IntParameter "+RemoveWhitespace(vector1Property.displayName)+" = new IntParameter { value = "+vector1Property.value+" };");
								break;
							default:
								propertyList.AppendLine("public FloatParameter "+RemoveWhitespace(vector1Property.displayName)+" = new FloatParameter { value = "+vector1Value+" };");
								break;
						}
						propertySets.AppendLine("sheet.properties.SetFloat(\""+vector1Property.referenceName+"\", settings."+RemoveWhitespace(vector1Property.displayName)+".value);");
						break;
					case PropertyType.Vector2:
						Vector2ShaderProperty vector2Property = (Vector2ShaderProperty)prop;
						string vector2Value = "new Vector2 ("+vector2Property.value.x+"f, "+vector2Property.value.y+"f)";
						propertyList.AppendLine("public Vector2Parameter "+RemoveWhitespace(vector2Property.displayName)+" = new Vector2Parameter { value = "+vector2Value+" };");
						propertySets.AppendLine("sheet.properties.SetVector(\""+vector2Property.referenceName+"\", settings."+RemoveWhitespace(vector2Property.displayName)+".value);");
						break;
					case PropertyType.Vector3:
						Vector3ShaderProperty vector3Property = (Vector3ShaderProperty)prop;
						string vector3Value = "new Vector3 ("+vector3Property.value.x+"f, "+vector3Property.value.y+"f, "+vector3Property.value.z+"f)";
						propertyList.AppendLine("public Vector3Parameter "+RemoveWhitespace(vector3Property.displayName)+" = new Vector3Parameter { value = "+vector3Value+" };");
						propertySets.AppendLine("sheet.properties.SetVector(\""+vector3Property.referenceName+"\", settings."+RemoveWhitespace(vector3Property.displayName)+".value);");
						break;
					case PropertyType.Vector4:
						Vector4ShaderProperty vector4Property = (Vector4ShaderProperty)prop;
						string vector4Value = "new Vector4 ("+vector4Property.value.x+"f, "+vector4Property.value.y+"f, "+vector4Property.value.z+"f, "+vector4Property.value.w+"f)";
						propertyList.AppendLine("public Vector4Parameter "+RemoveWhitespace(vector4Property.displayName)+" = new Vector4Parameter { value = "+vector4Value+" };");
						propertySets.AppendLine("sheet.properties.SetVector(\""+vector4Property.referenceName+"\", settings."+RemoveWhitespace(vector4Property.displayName)+".value);");
						break;
				}
			}

			return new string[2] { propertyList.ToString(), propertySets.ToString() };
		}

		private string SetUserData()
		{
			ShaderStringBuilder s = new ShaderStringBuilder();
			s.AppendLine("Vector4 userDataOut = sheet.properties.GetVector(\"_GraphUserData\");");
			s.AppendLine("context.userData.Add(\"_GraphUserData\", userDataOut);");
			return s.ToString();
		}

		private string GetUserData()
		{
			ShaderStringBuilder s = new ShaderStringBuilder();
			s.AppendLine("Vector4 userDataIn = Vector4.zero;");
            s.AppendLine("if(context.userData != null)");
			using (s.BlockScope())
            {
                s.AppendLine("object o = 0;");
                s.AppendLine("if(context.userData.TryGetValue(\"_GraphUserData\", out o))");
				s.IncreaseIndent();
                s.AppendLine("userDataIn = (Vector4)o;");
				s.DecreaseIndent();
                s.AppendLine("context.userData.Remove(\"_GraphUserData\");");
            }
            s.AppendLine("sheet.properties.SetVector(\"_GraphUserData\", userDataIn);");
			return s.ToString();
		}

		private string RemoveWhitespace(string input)
		{
			return new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());
		}

		private string NameFromFilepath(string input, bool removeWhitespace)
		{
			string[] pathSplit = input.Split('/');
			var name = pathSplit[pathSplit.Length-1];
			if(removeWhitespace)
				name = RemoveWhitespace(name);
			return name;
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
			
			string copyPath = "Assets/" + NameFromFilepath(path, false) + ".cs";
			using (StreamWriter outfile = 
                 new StreamWriter(copyPath))
                 {
                     outfile.Write(runtimeResult);
            }
			AssetDatabase.Refresh();
		}
	}
}
