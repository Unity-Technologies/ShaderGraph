using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class SpaceMaterialSlot : Vector3MaterialSlot
    {
        [SerializeField]
        private CoordinateSpace m_Space = CoordinateSpace.World;

        public CoordinateSpace space
        {
            get { return m_Space; }
            set { m_Space = value; }
        }

        protected SpaceMaterialSlot()
        {}

        protected SpaceMaterialSlot(int slotId, string displayName, string shaderOutputName, CoordinateSpace space,
                                    ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, stageCapability, hidden)
        {
            this.space = space;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as SpaceMaterialSlot;
            if (slot != null)
                space = slot.space;
        }

        public override VisualElement InstantiateControl()
        {
            return new SpaceSlotControlView(this);
        }
    }
}
