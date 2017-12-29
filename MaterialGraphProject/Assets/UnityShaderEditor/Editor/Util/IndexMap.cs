using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityShaderEditor.Editor.Util
{
    public class IndexMap<T> : IDictionary<int, T>
    {
        IndexSet m_Keys = new IndexSet();
        List<T> m_Values = new List<T>();

        public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            foreach (var key in m_Keys)
                yield return new KeyValuePair<int, T>(key, m_Values[key]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<int, T> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(int key, T value)
        {
            if (m_Keys.Contains(key))
                throw new ArgumentException("An element with the same key already exists", "key");
            this[key] = value;
        }

        public void Clear()
        {
            m_Keys.Clear();
            m_Values.Clear();
        }

        public bool Contains(KeyValuePair<int, T> item)
        {
            return ContainsKey(item.Key) && Equals(m_Values[item.Key], item.Value);
        }

        public bool ContainsKey(int key)
        {
            return m_Keys.Contains(key);
        }

        public void CopyTo(KeyValuePair<int, T>[] array, int arrayIndex)
        {
            var i = 0;
            foreach (var pair in this)
            {
                array[arrayIndex + i] = pair;
                i++;
            }
        }

        public bool Remove(KeyValuePair<int, T> item)
        {
            if (!ContainsKey(item.Key))
                return false;
            var value = m_Values[item.Key];
            if (!Equals(value, item.Value))
                return false;
            m_Keys.Remove(item.Key);
            m_Values[item.Key] = default(T);
            return true;
        }

        public bool Remove(int key)
        {
            if (m_Keys.Remove(key))
            {
                m_Values[key] = default(T);
                return true;
            }
            return false;
        }

        public int Count { get { return m_Keys.Count; } }
        public bool IsReadOnly { get { return false; } }

        public bool TryGetValue(int key, out T value)
        {
            if (m_Keys.Contains(key))
            {
                value = m_Values[key];
                return true;
            }
            value = default(T);
            return false;
        }

        public T GetValueOrDefault(int key)
        {
            return m_Keys.Contains(key) ? m_Values[key] : default(T);
        }

        public T this[int key]
        {
            get
            {
                if (!m_Keys.Contains(key))
                    throw new KeyNotFoundException();
                return m_Values[key];
            }
            set
            {
                for (var i = m_Values.Count; i <= key; i++)
                    m_Values.Add(default(T));
                m_Keys.Add(key);
                m_Values[key] = value;
            }
        }

        public ICollection<int> Keys { get { return new KeyCollection(this); } }
        public ICollection<T> Values { get { return new ValueCollection(this); } }

        class KeyCollection : ICollection<int>
        {
            IndexMap<T> m_IndexMap;

            internal KeyCollection(IndexMap<T> indexMap)
            {
                m_IndexMap = indexMap;
            }

            public IEnumerator<int> GetEnumerator()
            {
                return m_IndexMap.m_Keys.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(int item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(int item)
            {
                return m_IndexMap.m_Keys.Contains(item);
            }

            public void CopyTo(int[] array, int arrayIndex)
            {
                m_IndexMap.m_Keys.CopyTo(array, arrayIndex);
            }

            public bool Remove(int item)
            {
                throw new NotSupportedException();
            }

            public int Count { get { return m_IndexMap.Count; } }
            public bool IsReadOnly { get { return true; } }
        }

        class ValueCollection : ICollection<T>
        {
            IndexMap<T> m_IndexMap;

            internal ValueCollection(IndexMap<T> indexMap)
            {
                m_IndexMap = indexMap;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var key in m_IndexMap.m_Keys)
                    yield return m_IndexMap.m_Values[key];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return m_IndexMap.m_Values.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                var i = 0;
                foreach (var value in this)
                {
                    array[arrayIndex + i] = value;
                    i++;
                }
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public int Count { get { return m_IndexMap.Count; } }
            public bool IsReadOnly { get { return true; } }
        }
    }
}
