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

        if (ImagePlane == null)
            return;

        ImagePlane.transform.position = Settings.At;
        ImagePlane.transform.localScale = new Vector3(2.0f * right, 2.0f * top, 1);
        ImagePlane.GetComponent<MeshRenderer>().sharedMaterial.SetVector("_NumPixels", new Vector4(Settings.Resolution.x, Settings.Resolution.y, 1, 1));

    }

    void OnDrawGizmos()
    {
        var viewDir = Settings.At - Settings.From;
        var d = viewDir.magnitude;


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
        
    }
}
