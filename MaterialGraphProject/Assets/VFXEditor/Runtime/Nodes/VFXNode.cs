using System;
using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.VFXEditor
{
    [Serializable]
    public abstract class VFXNode : SerializableNode
    {
        public VFXNode GetOwner()
        {
            return m_Owner;
        }

        public IEnumerable<VFXNode> GetChildren()
        {
            return m_Children;
        }

        public VFXNode GetChild(int index)
        {
            return m_Children[index];
        }

        public void AddChild(VFXNode node)
        {
            if (CanAddChild(node,m_Children.Count))
                m_Children.Add(node);
        }

        public void RemoveChild(VFXNode node)
        {
            m_Children.Remove(node);
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableChildren = SerializationHelper.Serialize<VFXNode>(m_Children);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_Children = SerializationHelper.Deserialize<VFXNode>(m_SerializableChildren, null);
            foreach (var child in m_Children)
                child.m_Owner = this;        
        }

        public abstract bool CanAddChild(VFXNode element, int index = -1);

        protected VFXNode m_Owner;
        protected List<VFXNode> m_Children = new List<VFXNode>();

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableChildren;
    }

    [Serializable]
    public abstract class VFXNode<OwnerType,ChildrenType> : VFXNode
        where OwnerType : VFXNode
        where ChildrenType : VFXNode
    {
        public new OwnerType GetOwner()
        {
            return m_Owner as OwnerType;
        }

        public new ChildrenType GetChild(int index)
        {
            return m_Children[index] as ChildrenType;
        }

        public override bool CanAddChild(VFXNode element, int index = -1)
        {
            return index >= -1 && index <= m_Children.Count && element is ChildrenType;
        }
    }
}