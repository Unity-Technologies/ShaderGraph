using System.Linq;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing.Util;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class StandardNodeEditorView : AbstractNodeEditorView
    {
        NodeEditorHeaderView m_HeaderView;
        VisualElement m_SlotsContainer;
        VisualElement m_DefaultSlotValuesSection;
        EnumField m_SrcBlendField;
        EnumField m_DstBlendField;
        EnumField m_CullMode;
        EnumField m_ZTest;
        EnumField m_ZWrite;
        EnumField m_RenderQueue;
        EnumField m_RenderType;
        AbstractMaterialNode m_Node;
        int m_SlotsHash;

        public override INode node
        {
            get { return m_Node; }
            set
            {
                if (value == m_Node)
                    return;
                if (m_Node != null)
                    m_Node.onModified -= OnModified;
                m_Node = value as AbstractMaterialNode;
                OnModified(m_Node, ModificationScope.Node);
                if (m_Node != null)
                    m_Node.onModified += OnModified;
            }
        }

        public override void Dispose()
        {
            if (m_Node != null)
                m_Node.onModified -= OnModified;
        }

        public StandardNodeEditorView()
        {
            AddToClassList("nodeEditor");

            m_HeaderView = new NodeEditorHeaderView() { type = "node" };
            Add(m_HeaderView);

            m_DefaultSlotValuesSection = new VisualElement();
            m_DefaultSlotValuesSection.AddToClassList("section");
            {
                var sectionTitle = new VisualElement { text = "Default Slot Values" };
                sectionTitle.AddToClassList("title");
                m_DefaultSlotValuesSection.Add(sectionTitle);

                m_SlotsContainer = new VisualElement { name = "slots" };
                m_DefaultSlotValuesSection.Add(m_SlotsContainer);
            }
            Add(m_DefaultSlotValuesSection);
                
            
        }

        void OnModified(INode changedNode, ModificationScope scope)
        {
            if (node == null)
                return;

            m_HeaderView.title = node.name;

            var slotsHash = UIUtilities.GetHashCode(node.GetInputSlots<MaterialSlot>().Select(s => UIUtilities.GetHashCode(s.slotReference.nodeGuid.GetHashCode(), s.slotReference.slotId)));

            if (slotsHash != m_SlotsHash)
            {
                m_SlotsHash = slotsHash;
                m_SlotsContainer.Clear();
                foreach (var slot in node.GetInputSlots<MaterialSlot>())
                    m_SlotsContainer.Add(new IMGUISlotEditorView(slot));

                if (m_SlotsContainer.Any())
                    m_DefaultSlotValuesSection.RemoveFromClassList("hidden");
                else
                    m_DefaultSlotValuesSection.AddToClassList("hidden");
            }

            var lwNode = node as AbstractLightweightMasterNode;
            if (lwNode != null)
            {
                var masterSection = new VisualElement { name = "masterSection" };
                masterSection.AddToClassList("section");
                {
                    var sectionTitle = new VisualElement { text = "Master options" };
                    sectionTitle.AddToClassList("title");
                    masterSection.Add(sectionTitle);

                    if (m_SrcBlendField == null)
                    {
                        var srcBlend = new VisualElement();
                        srcBlend.AddToClassList("row");
                        {
                            srcBlend.Add(new VisualElement { text = "Source blend mode" });
                            m_SrcBlendField = new EnumField(lwNode.materialOptions.srcBlend);
                            m_SrcBlendField.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.srcBlend = (SurfaceMaterialOptions.BlendMode)evt.newValue;
                                if (lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            srcBlend.Add(m_SrcBlendField);
                        }
                        masterSection.Add(srcBlend);
                    }
                    if (m_DstBlendField == null)
                    { 
                        var dstBlend = new VisualElement();
                        dstBlend.AddToClassList("row");
                        {
                            dstBlend.Add(new VisualElement { text = "Destination blend mode" });
                            m_DstBlendField = new EnumField(lwNode.materialOptions.dstBlend);
                            m_DstBlendField.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.dstBlend = (SurfaceMaterialOptions.BlendMode)evt.newValue;
                                if(lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            dstBlend.Add(m_DstBlendField);
                        }
                        masterSection.Add(dstBlend);
                    }
                    if (m_CullMode == null)
                    { 
                        var cullMode = new VisualElement();
                        cullMode.AddToClassList("row");
                        {
                            cullMode.Add(new VisualElement { text = "Cull Mode" });
                            m_CullMode = new EnumField(lwNode.materialOptions.cullMode);
                            m_CullMode.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.cullMode = (SurfaceMaterialOptions.CullMode)evt.newValue;
                                if(lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            cullMode.Add(m_CullMode);
                        }
                        masterSection.Add(cullMode);
                    }
                    if (m_ZTest == null)
                    { 
                        var zTest = new VisualElement();
                        zTest.AddToClassList("row");
                        {
                            zTest.Add(new VisualElement { text = "Z Test" });
                            m_ZTest = new EnumField(lwNode.materialOptions.zTest);
                            m_ZTest.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.zTest = (SurfaceMaterialOptions.ZTest)evt.newValue;
                                if(lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            zTest.Add(m_ZTest);
                        }
                        masterSection.Add(zTest);
                    }
                    if (m_ZWrite == null)
                    { 
                        var zWrite = new VisualElement();
                        zWrite.AddToClassList("row");
                        {
                            zWrite.Add(new VisualElement { text = "Z Write" });
                            m_ZWrite = new EnumField(lwNode.materialOptions.zWrite);
                            m_ZWrite.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.zWrite = (SurfaceMaterialOptions.ZWrite)evt.newValue;
                                if(lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            zWrite.Add(m_ZWrite);
                        }
                        masterSection.Add(zWrite);
                    }
                    if (m_RenderQueue == null)
                    { 
                        var renderQueue = new VisualElement();
                        renderQueue.AddToClassList("row");
                        {
                            renderQueue.Add(new VisualElement { text = "Render Queue" });
                            m_RenderQueue = new EnumField(lwNode.materialOptions.renderQueue);
                            m_RenderQueue.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.renderQueue = (SurfaceMaterialOptions.RenderQueue)evt.newValue;
                                if(lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            renderQueue.Add(m_RenderQueue);
                        }
                        masterSection.Add(renderQueue);
                    }
                    if (m_RenderType == null)
                    { 
                        var renderType = new VisualElement();
                        renderType.AddToClassList("row");
                        {
                            renderType.Add(new VisualElement { text = "Render Type" });
                            m_RenderType = new EnumField(lwNode.materialOptions.renderType);
                            m_RenderType.OnValueChanged(evt =>
                            {
                                lwNode.materialOptions.renderType = (SurfaceMaterialOptions.RenderType)evt.newValue;
                                if(lwNode.onModified != null)
                                    lwNode.onModified(lwNode, ModificationScope.Graph);
                            });
                            renderType.Add(m_RenderType);
                        }
                        masterSection.Add(renderType);
                    }
                }
               

                Add(masterSection);
            }

        }
    }
}
