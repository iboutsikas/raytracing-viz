using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

internal class NffReader
{
    private readonly string _filepath;

    public NffReader(string filepath)
    {
        _filepath = filepath;
    }

    public void ParseMesh(string meshName, GameObject root, AssetImportContext ctx)
    {

        var lines = File.ReadAllLines(_filepath);
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        int meshCounter = 0;
        int sphereCounter = 0;
        Mesh mesh = null;
        Material latestMaterial = null;

        var cameraSettings = ScriptableObject.CreateInstance<CameraSettings>();


        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var tokens = line.Split(" ");
            switch (tokens[0])
            {
                case "from":
                    {
                        cameraSettings.From = new Vector3(
                            float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        break;
                    }
                case "at":
                    {
                        cameraSettings.At = new Vector3(
                            float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        break;
                    }
                case "up":
                    {
                        cameraSettings.Up = new Vector3(
                            float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        break;
                    }
                case "angle":
                    {
                        cameraSettings.Angle = float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    }
                case "resolution":
                    {
                        cameraSettings.Resolution = new Vector2Int(
                            int.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            int.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        break;
                    }
                case "f":
                    {
                        // First we add the vertices for the previous mesh
                        if (mesh != null)
                        {
                            if (vertices.Count != 0)
                                mesh.vertices = vertices.ToArray();
                            if (indices.Count != 0)
                                mesh.triangles = indices.ToArray();

                            vertices = new List<Vector3>();
                            indices = new List<int>();
                        }


                        var go = new GameObject($"Mesh {meshCounter++}");
                        go.transform.SetParent(root.transform);
                        var meshFilter = go.AddComponent<MeshFilter>();
                        var meshRenderer = go.AddComponent<MeshRenderer>();

                        latestMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        latestMaterial.enableInstancing = true;

                        var color = new Color(
                            float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        latestMaterial.color = color;
                        float diffuseComponent = float.Parse(tokens[4], CultureInfo.InvariantCulture.NumberFormat);

                        meshRenderer.sharedMaterial = latestMaterial;
                        ctx.AddObjectToAsset($"{meshName}_material{meshCounter}", latestMaterial);

                        mesh = new Mesh() { name = $"Mesh {meshCounter}" };
                        ctx.AddObjectToAsset($"{meshName}_mesh{meshCounter}", mesh);
                        meshFilter.sharedMesh = mesh;

                        break;
                    }
                case "p":
                    {
                        var numVerts = int.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);
                        Vector3[] polygonVertices = new Vector3[numVerts];

                        for (int v = 1; v <= numVerts; v++)
                        {
                            var vertexLine = lines[lineIndex + v];
                            var vertexTokens = vertexLine.Split(" ");

                            polygonVertices[v - 1] = new Vector3(
                                float.Parse(vertexTokens[0], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(vertexTokens[1], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(vertexTokens[2], CultureInfo.InvariantCulture.NumberFormat)
                            );
                        }

                        var v0 = polygonVertices[0];
                        var v0Index = vertices.Count;
                        vertices.Add(v0);

                        for (int v = 1; v < numVerts - 1; v++)
                        {
                            // V0 is already in, just add index
                            indices.Add(v0Index);
                            
                            // V1
                            indices.Add(vertices.Count);
                            vertices.Add(polygonVertices[v]);

                            // V2
                            indices.Add(vertices.Count);
                            vertices.Add(polygonVertices[v + 1]);
                        }

                        break;
                    }
                case "s":
                    {
                        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.name = $"Sphere {sphereCounter++}";

                        var meshRenderer = sphere.GetComponent<MeshRenderer>();
                        if (latestMaterial != null)
                            meshRenderer.sharedMaterial = latestMaterial;

                        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                        //meshRenderer.receiveShadows = false;

                        var position = new Vector3(
                            float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        var radius = float.Parse(tokens[4], CultureInfo.InvariantCulture.NumberFormat);

                        var scale = new Vector3(radius, radius, radius);

                        sphere.transform.localPosition = position;
                        sphere.transform.localScale = scale;
                        sphere.transform.SetParent(root.transform);
                        break;
                    }
            }
        }

        ctx.AddObjectToAsset($"{meshName}_cameraSettings", cameraSettings);
        if (mesh != null)
        {
            if (vertices.Count != 0)
                mesh.vertices = vertices.ToArray();
            if (indices.Count != 0)
                mesh.triangles = indices.ToArray();
        }
    }
}

[ScriptedImporter(1, "nff")]
public class NffImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var baseName = Path.GetFileName(ctx.assetPath);
        var root = new GameObject($"{baseName}_root");
        var reader = new NffReader(ctx.assetPath);

        reader.ParseMesh(baseName, root, ctx);

        ctx.AddObjectToAsset(ctx.assetPath, root);
        ctx.SetMainObject(root);
    }
}
