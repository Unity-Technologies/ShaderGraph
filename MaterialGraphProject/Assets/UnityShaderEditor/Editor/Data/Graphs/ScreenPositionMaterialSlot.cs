using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ScreenPositionMaterialSlot : Vector4MaterialSlot, IMayRequireScreenPosition
    {
        public ScreenPositionMaterialSlot()
        {}

        public ScreenPositionMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                          ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, stageCapability, hidden)
        {}

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}", ShaderGeneratorNames.ScreenPosition);
        }

        public bool RequiresScreenPosition()
        {
            return !isConnected;
        }
    }
}
