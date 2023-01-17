using iboutsikas.CustomImporters;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

[ExecuteInEditMode]
public class CameraVisualizer : MonoBehaviour
{
    private float top;
    private float right;
    private float bottom;
    private float left;
    private float tan;

    private Vector2 pixelSize;
    
    public bool ShowGizmos = true;
    public bool ShowPlane = false;
    public bool ShowDistanceToPlane = false;
    public bool ShowHalfHeight = false;
    public bool ShowHalfWidth = false;
    public bool ShowFrustumLines = false;

    public bool ShowDebugRay = false;
    public Vector2 DebugPixel = new Vector2(0, 0);

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
        if (!ShowGizmos)
            return; 

        var viewDir = Settings.At - Settings.From;
        var d = viewDir.magnitude;

        float xOffset = ShowDebugRay ? (DebugPixel.x + 0.5f) * pixelSize.x : 0.0f;
        float yOffset = ShowDebugRay ? (DebugPixel.y + 0.5f) * pixelSize.y : 0.0f;

        var topLeft = d * transform.forward + top * transform.up + left * transform.right;
        var topRight = d * transform.forward + top * transform.up + right * transform.right;
        var bottomRight = d * transform.forward + bottom * transform.up + right * transform.right;
        var bottomLeft = d * transform.forward + bottom * transform.up + left * transform.right;
        //Debug.Log(topLeft);

        Color originalGizmoColor = Gizmos.color;
        Color originalHandlesColor = Handles.color;

        if (ShowPlane)
        {
            Gizmos.DrawLine(topLeft + Settings.From, topRight + Settings.From);
            Gizmos.DrawLine(topRight + Settings.From, bottomRight + Settings.From);
            Gizmos.DrawLine(bottomRight + Settings.From, bottomLeft + Settings.From);
            Gizmos.DrawLine(bottomLeft + Settings.From, topLeft + Settings.From);
        }

        if (ShowDistanceToPlane)
        {
            // Draw the view ray
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Settings.From, viewDir);
            // d text
            Handles.color = Color.white;
            Handles.Label(Settings.From + viewDir / 2.0f, $"d = {d}");
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
            
            Handles.Label(endPoint + dir * 0.08f , $"({endPoint.x:f2}, {endPoint.y:f2}, {endPoint.z:f2})");
            
        }

        if (ShowHalfWidth)
        {
            var dir = right * transform.right;
            var endPoint = Settings.At + dir;
            
            Gizmos.color = Color.red;   
            Gizmos.DrawRay(Settings.At, dir);
            Handles.Label(Settings.At + (dir / 2) + (0.05f * transform.up), $"r = aspect * top = {right}"); 
            Handles.Label(endPoint + dir * 0.08f , $"({endPoint.x:f2}, {endPoint.y:f2}, {endPoint.z:f2})");
        }

        if (ShowFrustumLines)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(Settings.From, topLeft);
            Gizmos.DrawRay(Settings.From, topRight);
            Gizmos.DrawRay(Settings.From, bottomRight);
            Gizmos.DrawRay(Settings.From, bottomLeft);
        }

        if (ShowDebugRay)
        {
            Gizmos.color = Color.magenta;
            var dir = d * transform.forward + (top - yOffset) * transform.up + (left + xOffset) * transform.right;
            Gizmos.DrawRay(Settings.From, dir);
        }

        Gizmos.color = originalGizmoColor;
        Handles.color = originalHandlesColor;
    }
}
