using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    static readonly string xmlPath = "Assets/Scripts/Editor/PlanetGeneratorEditor.uxml";

    private Button m_RebuildButton = null;
    private Button m_ToggleGizmosButton = null;

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualElement();

        var script = serializedObject.FindProperty("m_Script");
        var scriptPropField = new PropertyField(script);
        scriptPropField.SetEnabled(false);
        inspector.Add(scriptPropField);

        inspector.Add(new PropertyField(serializedObject.FindProperty("Level")));
        inspector.Add(new PropertyField(serializedObject.FindProperty("Seed")));
        inspector.Add(new PropertyField(serializedObject.FindProperty("Size")));
        inspector.Add(new PropertyField(serializedObject.FindProperty("ContentRoot")));
        inspector.Add(new PropertyField(serializedObject.FindProperty("HandlePrefab")));

        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(xmlPath);
        visualTree.CloneTree(inspector);


        BindButton(inspector, "rebuild_planet", ref m_RebuildButton, On_RebuildPlanet);
        BindButton(inspector, "toggle_gizmos", ref m_ToggleGizmosButton, On_ToggleGizmmos);


        return inspector;
    }



    static void BindButton(VisualElement inspector, string buttonName, ref Button outButton, Action callback)
    {
        if (outButton != null)
            outButton.clicked -= callback;

        var button = inspector.Q<Button>(buttonName);

        if (button != null)
        {
            button.clicked += callback;
            outButton = button;
        }
        else
        {
            Debug.Log($"Failed to bind button with id: {buttonName}");
        }
    }

    private void OnDestroy()
    {
        if (m_RebuildButton != null)
        {
            m_RebuildButton.clicked -= On_RebuildPlanet;
        }
    }

    private void On_RebuildPlanet()
    {
        var visualizer = target as PlanetGenerator;
        if (visualizer != null)
        {
            visualizer.Rebuild();
        }
    }

    private void On_ToggleGizmmos()
    {
        var visualizer = target as PlanetGenerator;
        if (visualizer != null)
        {
            visualizer.ToggleGizmos();
        }
    }
}
