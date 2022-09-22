using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NffCameraSettings", menuName = "NFF Support/Camera Settings", order = 1)]
public class CameraSettings : ScriptableObject
{
    public Vector3 From = new Vector3(0, 0, 0);
    public Vector3 At = new Vector3(0, 0, 1);
    public Vector3 Up = new Vector3(0, 1, 0);
    public Vector2Int Resolution = new Vector2Int(2, 2);
    public float Angle = 45.0f;
}
