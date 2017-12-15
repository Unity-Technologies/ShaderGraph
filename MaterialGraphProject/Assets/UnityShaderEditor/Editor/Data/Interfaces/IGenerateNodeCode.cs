using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IGenerateNodeCode
    {
        void GenerateNodeCode(INode node, GenerationMode generationMode, ShaderGenerator visitor);
    }
}
