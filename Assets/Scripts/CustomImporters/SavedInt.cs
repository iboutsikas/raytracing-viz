using UnityEditor;

namespace iboutsikas.CustomImporters
{
    // Taken from https://github.com/Unity-Technologies/com.unity.formats.alembic/blob/9ac750fe8575e04e298441e73c91a0cb289e519f/com.unity.formats.alembic/Editor/Importer/AlembicImporterEditor.cs#L425
    public class SavedInt
    {
        int m_Value;
        string m_Name;
        bool m_Loaded;
        public SavedInt(string name, int value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        void Load()
        {
            if (m_Loaded)
                return;
            m_Loaded = true;
            m_Value = EditorPrefs.GetInt(m_Name, m_Value);
        }

        public int value
        {
            get { Load(); return m_Value; }
            set
            {
                Load();
                if (m_Value == value)
                    return;
                m_Value = value;
                EditorPrefs.SetInt(m_Name, value);
            }
        }
        public static implicit operator int(SavedInt s)
        {
            return s.value;
        }
    }
}