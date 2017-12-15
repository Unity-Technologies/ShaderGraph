using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface INodeType
    {
        void InitializeNode(INode node);
        void UpdateNodeAfterDeserialization(INode node);
    }
}
