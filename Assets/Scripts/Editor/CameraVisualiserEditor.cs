using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(CameraVisualizer))]
public class CameraVisualiserEditor : Editor
{
    static readonly string xmlPath = "Assets/Scripts/Editor/CameraVisualiserInspector.uxml";

    private Button m_RecalcAAButton = null;
    private Button m_RecalcDoFButton = null;

    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualElement();

        var script = serializedObject.FindProperty("m_Script");
        var scriptPropField = new PropertyField(script);
        scriptPropField.SetEnabled(false);
        inspector.Add(scriptPropField);

        inspector.Add(new PropertyField(serializedObject.FindProperty("Settings")));
        inspector.Add(new PropertyField(serializedObject.FindProperty("ImagePlane")));
        inspector.Add(new PropertyField(serializedObject.FindProperty("CameraAxis")));

        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(xmlPath);
        visualTree.CloneTree(inspector);

        if (m_RecalcAAButton != null )
        {
            m_RecalcAAButton.clicked -= On_AAButtonClicked;
        }

        m_RecalcAAButton = inspector.Q<Button>("recalculate_aa_rays");

        if (m_RecalcAAButton != null)
        {
            m_RecalcAAButton.clicked += On_AAButtonClicked;
        }

        if (m_RecalcDoFButton!= null)
        {
            m_RecalcDoFButton.clicked -= On_DoFButtonClicked;
        }

        m_RecalcDoFButton = inspector.Q<Button>("recalculate_dof_rays");

        if (m_RecalcDoFButton!= null)
        {
            m_RecalcDoFButton.clicked += On_DoFButtonClicked;
        }

        return inspector;
    }

    private void On_DoFButtonClicked()
    {
        var visualizer = target as CameraVisualizer;
        if (visualizer != null)
        {
            visualizer.RecalculateDoFRays();
        }
    }

    private void OnDestroy()
    {
        if (m_RecalcAAButton != null)
        {
            m_RecalcAAButton.clicked -= On_AAButtonClicked;
        }

        if (m_RecalcDoFButton != null)
        {
            m_RecalcDoFButton.clicked -= On_DoFButtonClicked;
        }
    }

    private void On_AAButtonClicked()
    {
        var visualizer = target as CameraVisualizer;
        if (visualizer != null)
        {
            visualizer.RecalculateAARays();
        }
    }
}
