using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Post Process Inputs")]
    public class PostProcessInputsNode : AbstractMaterialNode, IGenerateProperties, IGeneratesBodyCode, IMayRequireMeshUV
    {
        const string kUVSlotName = "UV";
        const string kSourceSlotName = "Source";
        const string kUserDataName = "User Data";

        public const int UVSlotId = 0;
        public const int SourceSlotId = 1;
        public const int UserDataSlotId = 2;

        private Texture2D m_TestCardTexture;

        public PostProcessInputsNode()
        {
            name = "Post Process Inputs";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector4MaterialSlot(SourceSlotId, kSourceSlotName, kSourceSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(UserDataSlotId, kUserDataName, kUserDataName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, SourceSlotId, UserDataSlotId });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            if (m_TestCardTexture == null)
                m_TestCardTexture = Resources.Load<Texture2D>("PostProcessTestCard");

            properties.Add(new PreviewProperty(PropertyType.Texture)
            {
                name = "_MainTex",
                textureValue = m_TestCardTexture
            });

            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = "_GraphUserData",
                vector4Value = new Vector4(0, 0, 0, 0)
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector4ShaderProperty
            {
                overrideReferenceName = "_GraphUserData",
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new TextureShaderProperty
            {
                overrideReferenceName = "_MainTex",
                generatePropertyBlock = false,
				modifiable = false
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {          
			visitor.AddShaderChunk(string.Format("{0}4 {1} = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, {2});"
                , precision
				, GetVariableNameForSlot(SourceSlotId)
                , GetSlotValue(UVSlotId, generationMode)), true);
                
            visitor.AddShaderChunk(string.Format("{0}4 {1} = _GraphUserData;"
                , precision
				, GetVariableNameForSlot(UserDataSlotId)), true);
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }
    }
}
