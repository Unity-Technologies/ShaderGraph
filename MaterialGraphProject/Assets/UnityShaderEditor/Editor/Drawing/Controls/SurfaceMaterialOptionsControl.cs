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

        public SurfaceMaterialOptionsControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_SurfaceMaterialOptions = (SurfaceMaterialOptions)m_PropertyInfo.GetValue(m_Node, null);

            if (propertyInfo.PropertyType != typeof(SurfaceMaterialOptions))
                throw new ArgumentException("Property must be of type SurfaceMaterialOptions.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            Add(new Label(label));

            m_Container = new VisualElement { name = "Container" };
            m_Container.Add(CreateEnumEntry("Source", "m_SrcBlend"));
            m_Container.Add(CreateEnumEntry("Destination", "m_DstBlend"));
            m_Container.Add(CreateEnumEntry("Cull", "m_CullMode"));
            m_Container.Add(CreateEnumEntry("ZTest", "m_ZTest"));
            m_Container.Add(CreateEnumEntry("ZWrite", "m_ZWrite"));
            m_Container.Add(CreateEnumEntry("Queue", "m_RenderQueue"));
            m_Container.Add(CreateEnumEntry("Type", "m_RenderType"));
            m_Container.Add(CreateAlphaClipEntry("Alpha Clip"));

            PBRMasterNode masterNode = (PBRMasterNode)m_Node;
            bool isEnabled = masterNode.rendering == PBRMasterNode.RenderingMode.Custom;
            m_Container.SetEnabled(isEnabled);

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

        void OnChangeEnum(ChangeEvent<Enum> evt, string fieldName)
        {
            //SurfaceMaterialOptions instance = new SurfaceMaterialOptions();
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
            Action changedToggle = () => { OnChangeToggle(); };
            var toggleField = new UnityEngine.Experimental.UIElements.Toggle(changedToggle);
            toggleField.on = m_SurfaceMaterialOptions.alphaClip;
            entry.Add(new Label(label));
            entry.Add(toggleField);
            return entry;
        }

        void OnChangeToggle()
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            m_SurfaceMaterialOptions.alphaClip = !m_SurfaceMaterialOptions.alphaClip;
            m_PropertyInfo.SetValue(m_Node, m_SurfaceMaterialOptions, null);
            m_Node.Dirty(ModificationScope.Graph);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            PBRMasterNode masterNode = (PBRMasterNode)m_Node;
            bool isEnabled = masterNode.rendering == PBRMasterNode.RenderingMode.Custom;
            m_Container.SetEnabled(isEnabled);

            if (scope == ModificationScope.Graph)
                m_Container.Dirty(ChangeType.Repaint);
        }
    }
}
