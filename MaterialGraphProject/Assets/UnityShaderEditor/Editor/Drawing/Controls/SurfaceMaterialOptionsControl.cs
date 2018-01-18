using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SurfaceMaterialOptionsControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public SurfaceMaterialOptionsControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new SurfaceMaterialOptionsControlView(m_Label, node, propertyInfo);
        }
    }

    public class SurfaceMaterialOptionsControlView : VisualElement, INodeModificationListener
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        SurfaceMaterialOptions m_SurfaceMaterialOptions;
        VisualElement m_Container;
        Button m_ExpandButton;

        EnumField m_SourceEnum;
        EnumField m_DestEnum;
        EnumField m_CullEnum;
        EnumField m_ZTestEnum;
        EnumField m_ZWriteEnum;
        EnumField m_RenderQueueEnum;
        EnumField m_RenderTypeEnum;
        UnityEngine.Experimental.UIElements.Toggle m_AlphaBlendToggle;
        UnityEngine.Experimental.UIElements.Toggle m_AlphaClipToggle;

        bool m_IsExpanded = false;

        public SurfaceMaterialOptionsControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_SurfaceMaterialOptions = (SurfaceMaterialOptions)m_PropertyInfo.GetValue(m_Node, null);

            if (propertyInfo.PropertyType != typeof(SurfaceMaterialOptions))
                throw new ArgumentException("Property must be of type SurfaceMaterialOptions.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            var titleContainer = new VisualElement { name = "Title" };
            titleContainer.Add(new Label(label));
            Action expandButtonAction = () => { OnChangeExpandButton(); };
            m_ExpandButton = new Button(expandButtonAction);
            m_ExpandButton.Add(new Label ("Expand"));
            titleContainer.Add(m_ExpandButton);
            Add(titleContainer);

            m_Container = new VisualElement { name = "Container" };
            
            var sourceEntry = new VisualElement { name = "Entry" };
            m_SourceEnum = new EnumField((Enum)m_SurfaceMaterialOptions.srcBlend);
            m_SourceEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_SrcBlend");
            sourceEntry.Add(new Label("Source"));
            sourceEntry.Add(m_SourceEnum);
            m_Container.Add(sourceEntry);

            var destEntry = new VisualElement { name = "Entry" };
            m_DestEnum = new EnumField((Enum)m_SurfaceMaterialOptions.dstBlend);
            m_DestEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_DstBlend");
            destEntry.Add(new Label("Destination"));
            destEntry.Add(m_DestEnum);
            m_Container.Add(destEntry);

            var cullEntry = new VisualElement { name = "Entry" };
            m_CullEnum = new EnumField((Enum)m_SurfaceMaterialOptions.cullMode);
            m_CullEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_CullMode");
            cullEntry.Add(new Label("Cull"));
            cullEntry.Add(m_CullEnum);
            m_Container.Add(cullEntry);

            var zTestEntry = new VisualElement { name = "Entry" };
            m_ZTestEnum = new EnumField((Enum)m_SurfaceMaterialOptions.zTest);
            m_ZTestEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_ZTest");
            zTestEntry.Add(new Label("ZTest"));
            zTestEntry.Add(m_ZTestEnum);
            m_Container.Add(zTestEntry);

            var zWriteEntry = new VisualElement { name = "Entry" };
            m_ZWriteEnum = new EnumField((Enum)m_SurfaceMaterialOptions.zWrite);
            m_ZWriteEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_ZWrite");
            zWriteEntry.Add(new Label("ZWrite"));
            zWriteEntry.Add(m_ZWriteEnum);
            m_Container.Add(zWriteEntry);

            var renderQueueEntry = new VisualElement { name = "Entry" };
            m_RenderQueueEnum = new EnumField((Enum)m_SurfaceMaterialOptions.renderQueue);
            m_RenderQueueEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_RenderQueue");
            renderQueueEntry.Add(new Label("Queue"));
            renderQueueEntry.Add(m_RenderQueueEnum);
            m_Container.Add(renderQueueEntry);

            var renderTypeEntry = new VisualElement { name = "Entry" };
            m_RenderTypeEnum = new EnumField((Enum)m_SurfaceMaterialOptions.renderType);
            m_RenderTypeEnum.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, "m_RenderType");
            renderTypeEntry.Add(new Label("Type"));
            renderTypeEntry.Add(m_RenderTypeEnum);
            m_Container.Add(renderTypeEntry);

            var alphaBlendEntry = new VisualElement { name = "Entry" };
            Action alphaBlendAction = () => { OnChangeAlphaBlendToggle(); };
            m_AlphaBlendToggle = new UnityEngine.Experimental.UIElements.Toggle(alphaBlendAction);
            m_AlphaBlendToggle.on = m_SurfaceMaterialOptions.alphaBlend;
            alphaBlendEntry.Add(new Label("Blend"));
            alphaBlendEntry.Add(m_AlphaBlendToggle);
            m_Container.Add(alphaBlendEntry);

            var alphaClipEntry = new VisualElement { name = "Entry" };
            Action alphaClipAction = () => { OnChangeAlphaClipToggle(); };
            m_AlphaClipToggle = new UnityEngine.Experimental.UIElements.Toggle(alphaClipAction);
            m_AlphaClipToggle.on = m_SurfaceMaterialOptions.alphaClip;
            alphaClipEntry.Add(new Label("Clip"));
            alphaClipEntry.Add(m_AlphaClipToggle);
            m_Container.Add(alphaClipEntry);

            //m_Container.Add(CreateEnumEntry("Source", "m_SrcBlend"));
            //m_Container.Add(CreateEnumEntry("Destination", "m_DstBlend"));
            //m_Container.Add(CreateEnumEntry("Cull", "m_CullMode"));
            //m_Container.Add(CreateEnumEntry("ZTest", "m_ZTest"));
            //m_Container.Add(CreateEnumEntry("ZWrite", "m_ZWrite"));
            //m_Container.Add(CreateEnumEntry("Queue", "m_RenderQueue"));
            //m_Container.Add(CreateEnumEntry("Type", "m_RenderType"));
            //m_Container.Add(CreateAlphaClipEntry("Alpha Clip"));

            PBRMasterNode masterNode = (PBRMasterNode)m_Node;
            bool isEnabled = masterNode.rendering == PBRMasterNode.RenderingMode.Custom;
            m_Container.SetEnabled(isEnabled);

            if(m_IsExpanded)
                Add(m_Container);
        }

        VisualElement CreateEnumEntry(string label, string fieldName)
        {
            Type fieldsType = typeof(SurfaceMaterialOptions);
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            FieldInfo[] fields = fieldsType.GetFields(bindingFlags);
            for(int i = 0; i < fields.Length; i++)
            {
                if(fields[i].Name == fieldName)
                {
                    var entry = new VisualElement { name = "Entry" };
                    var enumField = new EnumField((Enum)fields[i].GetValue(m_SurfaceMaterialOptions));
                    enumField.RegisterCallback<ChangeEvent<Enum>, string>(OnChangeEnum, fieldName);
                    entry.Add(new Label(label));
                    entry.Add(enumField);
                    return entry;
                }
            }
            return null;
        }

        void OnChangeExpandButton()
        {
            m_IsExpanded = !m_IsExpanded;
            if(m_IsExpanded)
                Add(m_Container);
            else
                Remove(m_Container);
        }

        void OnChangeAlphaClipToggle()
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            m_SurfaceMaterialOptions.alphaClip = !m_SurfaceMaterialOptions.alphaClip;
            m_PropertyInfo.SetValue(m_Node, m_SurfaceMaterialOptions, null);
            m_Node.Dirty(ModificationScope.Graph);
        }

        void OnChangeAlphaBlendToggle()
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            m_SurfaceMaterialOptions.alphaBlend = !m_SurfaceMaterialOptions.alphaBlend;
            m_PropertyInfo.SetValue(m_Node, m_SurfaceMaterialOptions, null);
            m_Node.Dirty(ModificationScope.Graph);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            PBRMasterNode masterNode = (PBRMasterNode)m_Node;
            bool isEnabled = masterNode.rendering == PBRMasterNode.RenderingMode.Custom;
            m_Container.SetEnabled(isEnabled);
            UpdateEntries();

            if (scope == ModificationScope.Node)
                m_Container.Dirty(ChangeType.Repaint);
        }

        void UpdateEntries()
        {
            m_SurfaceMaterialOptions = (SurfaceMaterialOptions)m_PropertyInfo.GetValue(m_Node, null);
            m_SourceEnum.value = m_SurfaceMaterialOptions.srcBlend;
            m_DestEnum.value = m_SurfaceMaterialOptions.dstBlend;
            m_CullEnum.value = m_SurfaceMaterialOptions.cullMode;
            m_ZTestEnum.value = m_SurfaceMaterialOptions.zTest;
            m_ZWriteEnum.value = m_SurfaceMaterialOptions.zWrite;
            m_RenderQueueEnum.value = m_SurfaceMaterialOptions.renderQueue;
            m_RenderTypeEnum.value = m_SurfaceMaterialOptions.renderType;
            m_AlphaBlendToggle.on = m_SurfaceMaterialOptions.alphaBlend;
            m_AlphaClipToggle.on = m_SurfaceMaterialOptions.alphaClip;
        }

        void OnChangeEnum(ChangeEvent<Enum> evt, string fieldName)
        {
            Type fieldsType = typeof(SurfaceMaterialOptions);
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            FieldInfo[] fields = fieldsType.GetFields(bindingFlags);
            for(int i = 0; i < fields.Length; i++)
            {
                if(fields[i].Name == fieldName)
                {
                    var value = (Enum)fields[i].GetValue(m_SurfaceMaterialOptions);
                    if (!evt.newValue.Equals(value))
                    {
                        m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                        fields[i].SetValue(m_SurfaceMaterialOptions, evt.newValue);
                        m_PropertyInfo.SetValue(m_Node, m_SurfaceMaterialOptions, null);
                        m_Node.Dirty(ModificationScope.Graph);
                    }
                }
            }
        }

        VisualElement CreateAlphaClipEntry(string label)
        {
            var entry = new VisualElement { name = "Entry" };
            Action changedToggle = () => { OnChangeAlphaClipToggle(); };
            var toggleField = new UnityEngine.Experimental.UIElements.Toggle(changedToggle);
            toggleField.on = m_SurfaceMaterialOptions.alphaClip;
            entry.Add(new Label(label));
            entry.Add(toggleField);
            return entry;
        }
    }
}
