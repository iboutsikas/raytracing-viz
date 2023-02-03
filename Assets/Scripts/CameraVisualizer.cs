using iboutsikas.CustomImporters;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
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
        public float xOffset;
        public float yOffset;
    }
    
    private List<Vector3> aaSamples = new List<Vector3>();
    private List<Vector3> dofOrigins = new List<Vector3>();


    private float top;
    private float right;
    private float bottom;
    private float left;
    private float tan;

    private Vector2 pixelSize;
    
    public bool ShowFrustumAndPlane = true;
    public bool ShowPlane = false;
    public bool ShowDistanceToPlane = false;
    public bool ShowHalfHeight = false;
    public bool ShowHalfWidth = false;
    public bool ShowFrustumLines = false;

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

    void Update()
    {
        if (Settings == null)
            return;

        var viewDir = Settings.At - Settings.From;
        transform.position = Settings.From;
        transform.rotation = Quaternion.LookRotation(viewDir.normalized, Settings.Up);


        var d = viewDir.magnitude;
        var theta_2 = (Settings.Angle * 0.5f) * Mathf.Deg2Rad ;
        tan = Mathf.Tan(theta_2);

        top = d * tan;
        left = -d * tan;
        bottom = -top;
        right = -left;

        pixelSize.x = (2.0f * top) / Settings.Resolution.x;
        pixelSize.y = (2.0f * right) / Settings.Resolution.y;

        if (ImagePlane == null)
            return;

        ImagePlane.transform.position = Settings.At;
        ImagePlane.transform.localScale = new Vector3(2.0f * right, 2.0f * top, 1);
        ImagePlane.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_NumPixels", new Vector4(Settings.Resolution.x, Settings.Resolution.y, 1, 1));

    }

    void OnDrawGizmos()
    {
        GizmoInfo info = new GizmoInfo();

        info.viewDir = Settings.At - Settings.From;
        info.distance = info.viewDir.magnitude;

        info.topLeft = info.distance * transform.forward + top * transform.up + left * transform.right;
        info.topRight = info.distance * transform.forward + top * transform.up + right * transform.right;
        info.bottomRight = info.distance * transform.forward + bottom * transform.up + right * transform.right;
        info.bottomLeft = info.distance * transform.forward + bottom * transform.up + left * transform.right;
        

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
        var dir = info.distance * transform.forward + (top - yOffset) * transform.up + (left + xOffset) * transform.right;
        dir *= DebugRayMultiplier;
        Gizmos.DrawRay(Settings.From, dir);
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
            float xOffset = Aperature * UnityEngine.Random.Range(-1.0f, 1.0f);
            float yOffset = Aperature * UnityEngine.Random.Range(-1.0f, 1.0f);

            var origin = transform.position + xOffset * transform.right + yOffset * transform.up;

            dofOrigins.Add(origin);
        }
    }
}
