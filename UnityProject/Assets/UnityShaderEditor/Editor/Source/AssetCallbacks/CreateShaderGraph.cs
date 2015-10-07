using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.Graphs.Material
{
	public class CreateShaderGraph : EndNameEditAction
	{
		[MenuItem("Assets/Create/Shader Graph", false, 208)]
		public static void CreateMaterialGraph()
		{
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateShaderGraph>(),
																	"New Shader Graph.ShaderGraph", null, null);
		}

		public override void Action(int instanceId, string pathName, string resourceFile)
		{
			var graph = CreateInstance<MaterialGraph>();
			graph.name = Path.GetFileName(pathName);
			AssetDatabase.CreateAsset(graph, pathName);
			graph.CreateSubAssets();
		}
	}
}