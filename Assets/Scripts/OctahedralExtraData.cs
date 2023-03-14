using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class OctahedralExtraData : MonoBehaviour
{
    private bool m_OldShowState;

    public int X;
    public int Z;
    public int ArrayIndex;
    public bool ShowGizmos = false;

    private float offset = 0.0f;

    private void OnEnable()
    {
        if (transform.position == new Vector3(0, -1, 0))
        {
            offset = Random.value * 0.5f;
        }

        Selection.selectionChanged += On_SelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= On_SelectionChanged;
    }


    private void OnDrawGizmos()
    {
        if (!ShowGizmos)
            return;

        GUIStyle stl= new GUIStyle(EditorStyles.label);
        stl.fontSize = 24;
        stl.normal.textColor = Color.white;
        stl.normal.background = Texture2D.blackTexture;

        Handles.Label(transform.position + transform.position * offset, $"x: {X}, z:{Z}, idx:{ArrayIndex}", stl);
    }

    private void On_SelectionChanged()
    {
        if (Selection.activeObject == this.gameObject)
        {
            m_OldShowState = ShowGizmos;
            ShowGizmos = true;
        }
        else
        {
            ShowGizmos = m_OldShowState;
        }
    }
}
