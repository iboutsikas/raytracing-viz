using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[CustomEditor(typeof(NffImporter))]
public class NffImporterEditor : ScriptedImporterEditor
{
    enum UITab
    {
        Model,
        Material
    }
    bool isRemapFoldoutOpen = true;
    SavedInt uiTab;

    public override void OnEnable()
    {
        base.OnEnable();
        uiTab = new SavedInt("NffImporterUITab", (int)UITab.Model);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        NffImporter importer = serializedObject.targetObject as NffImporter;

        // Draw the tab headers

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            uiTab.value = GUILayout.Toolbar(uiTab, new[] { "Model", "Materials" });
            GUILayout.FlexibleSpace();
        }

        if (uiTab == (int)UITab.Model)
        {
            EditorGUILayout.LabelField("No options implemented here yet");
        }
        else if (uiTab == (int)UITab.Material)
        {
            DrawMaterialsTab(serializedObject);
        }

        serializedObject.ApplyModifiedProperties();
        ApplyRevertGUI();
    }

    protected override void Apply()
    {
        var importer = serializedObject.targetObject as NffImporter;
        var remaps = importer.GetExternalObjectMap();
        foreach (var remap in remaps.Where(remap => remap.Value == null))
        {
            importer.RemoveRemap(remap.Key);
        }
        base.Apply();
    }

    private void DrawMaterialsTab(SerializedObject so)
    {

        var shaderContent = new GUIContent("Base Material Shader");
        var shaderProp = so.FindProperty("m_DefaultShader");
        EditorGUILayout.PropertyField(shaderProp, shaderContent);


        var importer = so.targetObject as NffImporter;
        var mainGO = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
        var materials = new List<NffMaterialEntry>();

        if (mainGO != null)
        {
            materials = NffImporter.GetMaterialSlots(importer, mainGO);

            if (materials.Count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("No facesets present in file.");
                    GUILayout.FlexibleSpace();
                }

                return;
            }
        }
        else
        {
            materials = GetPresetMaterialSlots(importer);
        }

        isRemapFoldoutOpen = EditorGUILayout.Foldout(isRemapFoldoutOpen, "Material Remap");

        if (!isRemapFoldoutOpen)
            return;

        using (new EditorGUI.IndentLevelScope())
        {
            for (int i = 0; i < materials.Count; i++)
            {
                var entry = materials[i];
                using (var c = new EditorGUI.ChangeCheckScope())
                {
                    var assign = (Material)EditorGUILayout.ObjectField(entry.materialName, entry.material, typeof(Material), false);
                
                    if (c.changed)
                    {
                        // We have a GameObject AND the user has not assigned a remap
                        if (mainGO != null && assign == null)
                        {
                            importer.RemoveRemap(entry.ToSourceAssetIdentifier());
                        }
                        else
                        {
                            Undo.RegisterCompleteObjectUndo(importer, "Nff Material");
                            importer.AddRemap(entry.ToSourceAssetIdentifier(), assign);
                        }
                    }
                }

            }
        }


    }

    private List<NffMaterialEntry> GetPresetMaterialSlots(NffImporter importer)
    {
        var ret = new List<NffMaterialEntry>();
        foreach (var r in importer.GetExternalObjectMap())
        {
            var toks = r.Key.name.Split(':');
            var entry = new NffMaterialEntry()
            {
                path = toks[0],
                material = r.Value as Material
            };
            ret.Add(entry);
        }

        return ret;
    }
}

// Taken from https://github.com/Unity-Technologies/com.unity.formats.alembic/blob/9ac750fe8575e04e298441e73c91a0cb289e519f/com.unity.formats.alembic/Editor/Importer/AlembicImporterEditor.cs#L425
class SavedInt
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
