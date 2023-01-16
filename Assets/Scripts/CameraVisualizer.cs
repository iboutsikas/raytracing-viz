using iboutsikas.CustomImporters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraVisualizer : MonoBehaviour
{
    private float top;
    private float right;
    private float bottom;
    private float left;
    private Vector2 pixelSize;

    public bool ShowGizmos = true;
    public bool ShowDebugRay = false;
    public Vector2 DebugPixel = new Vector2(0, 0);

    public CameraSettings Settings;


    public GameObject ImagePlane;

    void OnEnable()
    {
    }
    void Start()
    {
    }

    void Update()
    {
        if (Settings == null)
            return;

        var viewDir = Settings.At - Settings.From;
        transform.position = Settings.From;
        transform.rotation = Quaternion.LookRotation(viewDir.normalized, Settings.Up);


        var d = viewDir.magnitude;
        var theta_2 = (Settings.Angle * 0.5f) * Mathf.Deg2Rad ;
        var tan = Mathf.Tan(theta_2);

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


        Gizmos.DrawRay(Settings.From, viewDir);
        {
            var dir = d * transform.forward + top * transform.up + left * transform.right;
            Gizmos.DrawRay(Settings.From, dir);
        }
        {
            var dir = d * transform.forward + top * transform.up + right * transform.right;
            Gizmos.DrawRay(Settings.From, dir);
        }
        {
            var dir = d * transform.forward + bottom * transform.up + right * transform.right;
            Gizmos.DrawRay(Settings.From, dir);
        }
        {
            var dir = d * transform.forward + bottom * transform.up + left * transform.right;
            Gizmos.DrawRay(Settings.From, dir);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Settings.At, 0.1f);

        if (ShowDebugRay)
        {
            Gizmos.color = Color.magenta;
            var dir = d * transform.forward + (top - yOffset) * transform.up + (left + xOffset) * transform.right;
            Gizmos.DrawRay(Settings.From, dir);
        }
        
    }
}
