using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode
    {
        IEnumerable<string> GetSubshader(ShaderGraphRequirements graphRequirements, MasterRemapGraph remapper);
    }
}
