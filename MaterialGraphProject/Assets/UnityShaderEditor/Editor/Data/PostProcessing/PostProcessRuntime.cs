using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
	public static class PostProcessRuntime
	{
		private static string GetRuntimeFromTemplate(
			string template, 
			string path, 
			PostProcessMasterNode masterNode)
		{
			var templateLocation = ShaderGenerator.GetTemplatePath(template);
			if (!File.Exists(templateLocation))
                return string.Empty;

            var runtimeTemplate = File.ReadAllText(templateLocation);

			string[] pathSplit = path.Split('/');
			var name = RemoveWhitespace(pathSplit[pathSplit.Length-1]);

			var runtime = runtimeTemplate.Replace("${Name}", name);
			runtime = runtime.Replace("${Event}", masterNode.evt.ToString());
			runtime = runtime.Replace("${Path}", path);
			runtime = runtime.Replace("${Properties}", "");
			
			return runtime;
		}

		public static string RemoveWhitespace(string input)
		{
			return new string(input.ToCharArray()
				.Where(c => !char.IsWhiteSpace(c))
				.ToArray());
		}

		public static void BuildRuntime(
			string path, 
			PostProcessMasterNode masterNode)
		{
			var runtimeResult = GetRuntimeFromTemplate(
                    "postProcessRuntime.template",
					path,
                    masterNode);

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
