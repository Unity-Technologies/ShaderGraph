using UnityEngine.Graphing;

namespace UnityEngine.VFXEditor
{
    public class VFXFlowEdge : Edge
    {
        public VFXFlowEdge(SlotReference input, SlotReference output) : base(input,output)
        {
            Debug.Log("CREATE FLOW EDGE");
        }
    }

}