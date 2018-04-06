using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class TestSlot : MaterialSlot
    {
        public TestSlot(int slotId, string displayName, SlotType slotType, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, stageCapability, hidden) {}

        public TestSlot(int slotId, string displayName, SlotType slotType, int priority, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, displayName, slotType, priority, stageCapability, hidden) {}

        public override SlotValueType valueType
        {
            get { throw new System.NotImplementedException(); }
        }

        public override ConcreteSlotValueType concreteValueType
        {
            get { throw new System.NotImplementedException(); }
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            throw new System.NotImplementedException();
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            throw new System.NotImplementedException();
        }
    }
}
