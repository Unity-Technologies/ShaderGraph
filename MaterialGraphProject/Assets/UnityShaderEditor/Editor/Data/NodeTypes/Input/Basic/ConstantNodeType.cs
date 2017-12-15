using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityShaderEditor.Editor.Data.NodeTypes.Input.Basic
{
    public enum ConstantType
    {
        PI,
        TAU,
        PHI,
        E,
        SQRT2
    }

    public class ConstantNodeType : INodeType, IGenerateNodeCode
    {
        static Dictionary<ConstantType, float> m_ConstantList = new Dictionary<ConstantType, float>
        {
            {ConstantType.PI, 3.1415926f },
            {ConstantType.TAU, 6.28318530f},
            {ConstantType.PHI, 1.618034f},
            {ConstantType.E, 2.718282f},
            {ConstantType.SQRT2, 1.414214f},
        };

        const int k_OutputSlotId = 0;
        const string k_OutputSlotName = "Out";

        public void InitializeNode(INode node)
        {
            node.name = "Constant";
            node.data = new ConstantNodeData { owner = node };
            UpdateNodeAfterDeserialization(node);
        }

        public void UpdateNodeAfterDeserialization(INode node)
        {
            var data = (ConstantNodeData)node.data;
            data.owner = node;
            node.AddSlot(new Vector1MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, 0));
            node.RemoveSlotsNameNotMatching(new[] { k_OutputSlotId });
        }

        public void GenerateNodeCode(INode node, GenerationMode generationMode, ShaderGenerator visitor)
        {
            var data = (ConstantNodeData)node.data;
            visitor.AddShaderChunk(string.Format("{0} {1} = {2};", node.precision, node.GetVariableNameForNode(), m_ConstantList[data.constant]), false);
        }
    }

    [Serializable]
    public class ConstantNodeData
    {
        public INode owner { get; set; }

        [SerializeField]
        ConstantType m_Constant = ConstantType.PI;

        [EnumControl("")]
        public ConstantType constant
        {
            get { return m_Constant; }
            set
            {
                if (m_Constant == value)
                    return;

                m_Constant = value;
                owner.Dirty(ModificationScope.Graph);
            }
        }
    }
}
