using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class TextureMaterialSlot : MaterialSlot
    {
        public TextureMaterialSlot()
        {}

        public TextureMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStage, hidden)
        {}

        public override SlotValueType valueType { get { return SlotValueType.Texture; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}
    }
}
