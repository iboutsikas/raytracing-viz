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
        }
    }
}
