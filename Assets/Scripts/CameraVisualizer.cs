using iboutsikas.CustomImporters;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[ExecuteInEditMode]
public class CameraVisualizer : MonoBehaviour
{
    private struct GizmoInfo
    {
        public Vector3 topLeft;
        public Vector3 topRight;
        public Vector3 bottomLeft;
        public Vector3 bottomRight;

        public Vector3 viewDir;
        public float distance;
    }

    const float TAU = 2.0f * Mathf.PI; 
    
    private List<Vector3> aaSamples = new List<Vector3>();
    private List<Vector3> dofOrigins = new List<Vector3>();


    private float top;
    private float right;
    private float bottom;
    private float left;
    private float tan;

    private Vector3 u, v, w;

    private Vector2 pixelSize;

    private MeshRenderer m_MeshRenderer;
    
    public bool ShowFrustumAndPlane = true;
    public bool ShowPlane = false;
    public bool ShowDistanceToPlane = false;
    public bool ShowHalfHeight = false;
    public bool ShowHalfWidth = false;
    public bool ShowFrustumLines = false;
    public bool ShowUVW = false;

    public bool ShowDebugRay = false;
    public Vector2 DebugPixel = new Vector2(0, 0);
    public float DebugRayMultiplier = 1;

    public bool ShowAntiAliasing = false;
    public int NumAASamples = 0;
    public Vector2 AAPixel = new Vector2(0, 0);
    public Color AARayColor = Color.cyan;

    public bool ShowDepthOfField = false;
    public int NumDoFSamples = 0;
    public float Aperature = 0;
    public Vector2 DoFPixel = new Vector2(0, 0);
    public Color DoFRayColor = Color.yellow;
    public float DoFRayMultiplier = 1;

    public CameraSettings Settings;
    public GameObject ImagePlane;
    public GameObject CameraAxis;

    private void Awake()
    {
        if (ImagePlane != null) 
            m_MeshRenderer = ImagePlane.GetComponent<MeshRenderer>();
    }

    private void OnValidate()
    {
        if (Settings == null)
            return;

        var viewDir = Settings.At - Settings.From;
        transform.position = Settings.From;
        transform.rotation = Quaternion.LookRotation(viewDir.normalized, Settings.Up);

        var d = viewDir.magnitude;
        var theta_2 = (Settings.Angle * 0.5f) * Mathf.Deg2Rad;
        tan = Mathf.Tan(theta_2);

        top = d * tan;
        left = -d * tan;
        bottom = -top;
        right = -left;

        w = -viewDir.normalized;
        u = Vector3.Cross(Settings.Up, w).normalized;
        v = Vector3.Cross(w, u).normalized;

        pixelSize.x = (right - left) / Settings.Resolution.x;
        pixelSize.y = (top - bottom) / Settings.Resolution.y;
    }

    void Update()
    {
        if (Settings == null)
            return;

        if (Settings.NeedsUpdate)
        {
            var viewDir = Settings.At - Settings.From;
            transform.position = Settings.From;
            transform.rotation = Quaternion.LookRotation(viewDir.normalized, Settings.Up);

            var d = viewDir.magnitude;
            var theta_2 = (Settings.Angle * 0.5f) * Mathf.Deg2Rad;
            tan = Mathf.Tan(theta_2);

            top = d * tan;
            left = -d * tan;
            bottom = -top;
            right = -left;

            w = -viewDir.normalized;
            u = Vector3.Cross(Settings.Up, w).normalized;
            v = Vector3.Cross(w, u).normalized;

            pixelSize.x = (right - left) / Settings.Resolution.x;
            pixelSize.y = (top - bottom) / Settings.Resolution.y;

            if (ImagePlane != null)
            {
                ImagePlane.transform.position = Settings.At;
                ImagePlane.transform.localScale = new Vector3(right - left, top - bottom, 1);
                ImagePlane.GetComponent<MeshRenderer>()
                    .sharedMaterial
                    .SetVector("_NumPixels", new Vector4(Settings.Resolution.x, Settings.Resolution.y, 1, 1));
            }

            Settings.NeedsUpdate = false;
        }
        
        if (CameraAxis != null)
            CameraAxis.SetActive(ShowUVW);
    }

    void OnDrawGizmos()
    {
        GizmoInfo info = new GizmoInfo();

        info.viewDir = Settings.At - Settings.From;
        info.distance = info.viewDir.magnitude;

        info.topLeft = info.distance * transform.forward + top * transform.up + left * -transform.right;
        info.topRight = info.distance * transform.forward + top * transform.up + right * -transform.right;
        info.bottomRight = info.distance * transform.forward + bottom * transform.up + right * -transform.right;
        info.bottomLeft = info.distance * transform.forward + bottom * transform.up + left * -transform.right;

        if (ShowUVW)
        {
            {
                var to = Settings.From + transform.right;
                Handles.Label(to, $"u({u.x:f3}, {u.y:f3}, {u.z:f3})");
            }

            {
                var to = Settings.From + v;
                Handles.Label(to, $"v({v.x:f3}, {v.y:f3}, {v.z:f3})");
            }

            {
                var to = Settings.From + w;
                Handles.Label(to, $"w({w.x:f3}, {w.y:f3}, {w.z:f3})");
            }
        }
        

        if (ShowFrustumAndPlane)
        {
            DrawFrustumAndPlane(info);
        }

        if (ShowDebugRay)
        {
            DrawDebugRay(info);
        }

        if (ShowAntiAliasing)
        {
            DrawAASamples(info);
        }

        if (ShowDepthOfField)
        {
            OnDrawDoFRays(info);
        }

    }

    private void DrawDebugRay(GizmoInfo info)
    {
        //float xOffset = ShowDebugRay ? (DebugPixel.x + 0.5f) * pixelSize.x : 0.0f;
        //float yOffset = ShowDebugRay ? (DebugPixel.y + 0.5f) * pixelSize.y : 0.0f;

        float xOffset = (DebugPixel.x + 0.5f) * pixelSize.x;
        float yOffset = (DebugPixel.y + 0.5f) * pixelSize.y;

        var originalColor = Gizmos.color;
        Gizmos.color = Color.magenta;
        // We need two directions as Unity is left handed. dir is what students will compute for their 
        // direction while unityDir is basically flipped on the u axis so it matches what they would see
        // in a right-handed system

        var dir = -info.distance * w + (top - yOffset) * v + (left + xOffset) * u;
        var unityDir = -info.distance * w + (top - yOffset) * v + (left + xOffset) * transform.right; 

        Gizmos.DrawRay(Settings.From, DebugRayMultiplier * unityDir);
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 18;
        Handles.Label(Settings.From + (0.5f * unityDir), $"Pixel ({DebugPixel.x}, {DebugPixel.y}) Ray Direction ({dir.x}, {dir.y}, {dir.z})", labelStyle);

        if (Physics.Raycast(Settings.From, unityDir, out RaycastHit hit))
        {
            var hitPosition = Settings.From + hit.distance * unityDir.normalized;
            Gizmos.DrawWireSphere(hitPosition, 0.01f);
        }

        Gizmos.color = originalColor;
    }

    private void DrawFrustumAndPlane(GizmoInfo info)
    {
        Color originalGizmoColor = Gizmos.color;
        Color originalHandlesColor = Handles.color;

        if (ShowPlane)
        {
            Gizmos.DrawLine(info.topLeft + Settings.From, info.topRight + Settings.From);
            Gizmos.DrawLine(info.topRight + Settings.From, info.bottomRight + Settings.From);
            Gizmos.DrawLine(info.bottomRight + Settings.From, info.bottomLeft + Settings.From);
            Gizmos.DrawLine(info.bottomLeft + Settings.From, info.topLeft + Settings.From);
        }

        if (ShowDistanceToPlane)
        {
            // Draw the view ray
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Settings.From, info.viewDir);
            // d text
            Handles.color = Color.white;
            Handles.Label(Settings.From + info.viewDir / 2.0f, $"d = {info.distance}");
            // Draw end point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Settings.At, 0.05f);
            Gizmos.color = originalGizmoColor;
        }

        if (ShowHalfHeight)
        {
            var dir = top * transform.up;
            var endPoint = Settings.At + dir;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(Settings.At, dir);

            Handles.Label(Settings.At + dir / 2, $"d * tan({Settings.Angle}/2) = {top}");

            Handles.Label(endPoint + dir * 0.08f, $"({endPoint.x:f2}, {endPoint.y:f2}, {endPoint.z:f2})");

        }

        if (ShowHalfWidth)
        {
            var dir = right * transform.right;
            var endPoint = Settings.At + dir;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(Settings.At, dir);
            Handles.Label(Settings.At + (dir / 2) + (0.05f * transform.up), $"r = aspect * top = {right}");
            Handles.Label(endPoint + dir * 0.08f, $"({endPoint.x:f2}, {endPoint.y:f2}, {endPoint.z:f2})");
        }

        if (ShowFrustumLines)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(Settings.From, info.topLeft);
            Gizmos.DrawRay(Settings.From, info.topRight);
            Gizmos.DrawRay(Settings.From, info.bottomRight);
            Gizmos.DrawRay(Settings.From, info.bottomLeft);
        }

        Gizmos.color = originalGizmoColor;
        Handles.color = originalHandlesColor;
    }

    private void DrawAASamples(GizmoInfo info)
    {
        var originalColor = Gizmos.color;
        Gizmos.color = AARayColor;
        foreach(var sample in aaSamples) { 
            Gizmos.DrawRay(Settings.From, sample);    
        }
        Gizmos.color = originalColor;
    }

    private void OnDrawDoFRays(GizmoInfo info)
    {
        var originalColor = Gizmos.color;
        var originalHandlesColor = Handles.color;
        Gizmos.color = DoFRayColor;
        Handles.color = DoFRayColor;

        Handles.DrawWireDisc(transform.position, transform.forward, Aperature);
        
        float xOffset = (DoFPixel.x + 0.5f) * pixelSize.x;
        float yOffset = (DoFPixel.y + 0.5f) * pixelSize.y;

        var pixelCenter = Settings.From
            + info.distance * transform.forward 
            + (top - yOffset) * transform.up 
            + (left + xOffset) * transform.right;
        

        foreach (var origin in dofOrigins)
        {
            var dir = pixelCenter - origin;
            dir *= DoFRayMultiplier;

            Gizmos.DrawRay(origin, dir);
        }

        Gizmos.color = originalColor;
        Handles.color = originalHandlesColor;
    }

    public void RecalculateAARays()
    {
        aaSamples.Clear();

        var viewDir = Settings.At - Settings.From;
        var distance = viewDir.magnitude;


        for (int i = 0; i < NumAASamples; i++)
        {
            float xOffset = AAPixel.x * pixelSize.x + (pixelSize.x * UnityEngine.Random.Range(0.0f, 1.0f));
            float yOffset = AAPixel.y * pixelSize.y + (pixelSize.y * UnityEngine.Random.Range(0.0f, 1.0f));

            var dir = distance * transform.forward + (top - yOffset) * transform.up + (left + xOffset) * transform.right;
            aaSamples.Add(dir);
        }
    }

    public void RecalculateDoFRays()
    {
        dofOrigins.Clear();

        for (int i = 0; i < NumDoFSamples; i++)
        {

            float radius = Mathf.Sqrt(UnityEngine.Random.value);
            float theta = TAU * UnityEngine.Random.value;
            float ax = radius * Mathf.Cos(theta);
            float ay = radius * Mathf.Sin(theta);

            var origin = transform.position + Aperature * (ax * transform.right + ay * transform.up);

            dofOrigins.Add(origin);
        }
    }
}
