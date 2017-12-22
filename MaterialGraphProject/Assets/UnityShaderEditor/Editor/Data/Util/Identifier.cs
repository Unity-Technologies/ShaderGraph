namespace UnityEditor.ShaderGraph
{
    public struct Identifier
    {
        int m_Version;
        int m_Index;
        bool m_Valid;

        public Identifier(int index)
        {
            m_Version = 0;
            m_Index = index;
            m_Valid = true;
        }

        public void IncrementVersion()
        {
            m_Version++;
        }

        public int version
        {
            get { return m_Version; }
        }

        public int index
        {
            get { return m_Index; }
        }

        public bool valid
        {
            get { return m_Valid; }
        }
    }
}
