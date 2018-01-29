using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum PostProcessEvent
    {
        BeforeTransparent = 0,
        BeforeStack = 1,
        AfterStack = 2,
    }

    [Serializable]
    [Title("Master", "Post Process")]
    public class PostProcessMasterNode : MasterNode
    {
        public const string DestinationSlotName = "Destination";
        public const string UserDataSlotName = "User Data";

        public const int DestinationSlotId = 0;
        public const int UserDataSlotId = 1;

        [SerializeField]
        private PostProcessEvent m_Event = PostProcessEvent.AfterStack;

        [EnumControl("")]
        public PostProcessEvent evt
        {
            get { return m_Event; }
            set
            {
                if (m_Event == value)
                    return;

                m_Event = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public PostProcessMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            name = "Post Process Master";
            AddSlot(new ColorRGBAMaterialSlot(DestinationSlotId, DestinationSlotName, DestinationSlotName, SlotType.Input, Color.white, ShaderStage.Fragment));
            AddSlot(new Vector4MaterialSlot(UserDataSlotId, UserDataSlotName, UserDataSlotName, SlotType.Input, Vector4.zero, ShaderStage.Fragment));
            RemoveSlotsNameNotMatching( new[] { DestinationSlotId, UserDataSlotId }, true);
        }

        public override string GetShader(GenerationMode mode, string outputName, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

            var shaderProperties = new PropertyCollector();

            var abstractMaterialGraph = owner as AbstractMaterialGraph;
            if (abstractMaterialGraph != null)
                abstractMaterialGraph.CollectShaderProperties(shaderProperties, mode);

            foreach (var activeNode in activeNodeList.OfType<AbstractMaterialNode>())
                activeNode.CollectShaderProperties(shaderProperties, mode);

            var finalShader = new ShaderGenerator();
            finalShader.AddShaderChunk(string.Format(@"Shader ""Hidden/{0}""", outputName), false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();

            finalShader.AddShaderChunk("Properties", false);
            finalShader.AddShaderChunk("{", false);
            finalShader.Indent();
            finalShader.AddShaderChunk(shaderProperties.GetPropertiesBlock(2), false);
            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            var ppSub = new PostProcessSubShader();
            finalShader.AddShaderChunk(ppSub.GetSubshader(this, mode), true);

            finalShader.Deindent();
            finalShader.AddShaderChunk("}", false);

            configuredTextures = shaderProperties.GetConfiguredTexutres();

            if(mode == GenerationMode.ForReals)
            {
                PostProcessRuntime ppRuntime = new PostProcessRuntime();
                ppRuntime.BuildRuntime(outputName, this, shaderProperties);
            }
            return finalShader.GetShaderString(0);
        }
    }
}
