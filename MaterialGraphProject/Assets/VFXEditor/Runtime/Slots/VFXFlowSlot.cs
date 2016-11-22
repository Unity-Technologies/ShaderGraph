using System;
using UnityEngine.Graphing;

namespace UnityEngine.VFXEditor
{
    [Serializable]
    public class VFXFlowSlot : SerializableSlot
    {
        private static int s_NextId = 0;

        public VFXFlowSlot(VFXNode ownerNode, SlotType slotType)
            : base(s_NextId++, " ", slotType, 0)
        {
            owner = ownerNode;
        } 
    }
}