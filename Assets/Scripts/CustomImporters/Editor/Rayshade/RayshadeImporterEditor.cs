using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace iboutsikas.CustomImporters
{
    [CustomEditor(typeof(RayshadeImporter))]
    public class RayshadeImporterEditor: ScriptedImporterEditor
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
            uiTab = new SavedInt("RayshadeImporterUITab", (int)UITab.Model);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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

        private void DrawMaterialsTab(SerializedObject so)
        {
            var shaderContent = new GUIContent("Base Material Shader");
            var shaderProp = so.FindProperty("m_DefaultShader");
            EditorGUILayout.PropertyField(shaderProp, shaderContent);

            var importer = so.targetObject as RayshadeImporter;
            var mainGO = AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
            var materials = new List<MaterialEntry>();

            if (mainGO != null)
            {
                materials = importer.GetMaterialSlots(mainGO);
                if (materials.Count == 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("No materials present in file.");
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
                                Undo.RegisterCompleteObjectUndo(importer, "Rayshade Material");
                                importer.AddRemap(entry.ToSourceAssetIdentifier(), assign);
                            }
                        }
                    }

                }
            }
        }

        private List<MaterialEntry> GetPresetMaterialSlots(RayshadeImporter importer)
        {
            var ret = new List<MaterialEntry>();
            foreach (var r in importer.GetExternalObjectMap())
            {
                var toks = r.Key.name.Split(':');
                var entry = new MaterialEntry()
                {
                    path = toks[0],
                    material = r.Value as Material
                };
                ret.Add(entry);
            }

            return ret;
        }
    }
}
