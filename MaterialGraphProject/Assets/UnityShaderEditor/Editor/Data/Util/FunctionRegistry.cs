using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class ShaderChunk
    {
        public INode node { get; set; }
        public ShaderChunkType type { get; set; }
        public string name { get; set; }
        public int startLine { get; set; }
        public int lineCount { get; set; }
    }

    enum ShaderChunkType
    {
        Function,
        Body
    }

    public class FunctionRegistry
    {
        Dictionary<string, string> m_Sources = new Dictionary<string, string>();
        List<ShaderChunk> m_Chunks = new List<ShaderChunk>();
        bool m_ValidationEnabled = false;
        ShaderStringBuilder m_Builder = new ShaderStringBuilder();
        const bool k_Validate = false;

        public FunctionRegistry(int indentLevel = 0)
        {
        }

        public INode currentNode { get; set; }

        List<ShaderChunk> chunks
        {
            get { return m_Chunks; }
        }

        public void ProvideFunction(string name, Action<ShaderStringBuilder> generator)
        {
            m_Builder.AppendNewLine();
            var startIndex = m_Builder.length;
            generator(m_Builder);
            var source = m_Builder.ToString();

            string existingSource;
            if (m_Sources.TryGetValue(name, out existingSource))
            {
                if (k_Validate && source != existingSource)
                    Debug.LogErrorFormat(@"Function `{0}` has varying implementations:{1}{1}{2}{1}{1}{3}", name, Environment.NewLine, source, existingSource);
                return;
            }
            m_Sources.Add(name, source);
            m_Chunks.Add(new ShaderChunk { node = currentNode, type = ShaderChunkType.Function, name = name, source = source });
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, m_Chunks.Select(f => f.source).ToArray());
        }
    }
}
