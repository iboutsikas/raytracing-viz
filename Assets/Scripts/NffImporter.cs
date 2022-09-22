using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

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
        Mesh mesh = null;

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
                            mesh.vertices = vertices.ToArray();
                            mesh.triangles = indices.ToArray();

                            vertices = new List<Vector3>();
                            indices = new List<int>();
                        }


                        var go = new GameObject($"Mesh {meshCounter++}");
                        go.transform.SetParent(root.transform);
                        var meshFilter = go.AddComponent<MeshFilter>();
                        var meshRenderer = go.AddComponent<MeshRenderer>();

                        var latestMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        var color = new Color(
                            float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                            float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );
                        latestMaterial.color = color;
                        float diffuseComponent = float.Parse(tokens[4], CultureInfo.InvariantCulture.NumberFormat);

                        meshRenderer.sharedMaterial = latestMaterial;
                        ctx.AddObjectToAsset($"{meshName}_material{meshCounter}", latestMaterial);

                        mesh = new Mesh() {name = $"Mesh {meshCounter}"};
                        ctx.AddObjectToAsset($"{meshName}_mesh{meshCounter}", mesh);
                        meshFilter.sharedMesh = mesh;

                        break;
                    }
                case "p":
                    {
                        var numVerts = int.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);
                        for (int v = 1; v <= numVerts; v++)
                        {
                            var vertexLine = lines[lineIndex + v];
                            var vertexTokens = vertexLine.Split(" ");

                            indices.Add(vertices.Count);
                            vertices.Add(new Vector3(
                                float.Parse(vertexTokens[0], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(vertexTokens[1], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(vertexTokens[2], CultureInfo.InvariantCulture.NumberFormat)
                            ));
                        }
                        break;
                    }
            }
        }

        ctx.AddObjectToAsset($"{meshName}_cameraSettings", cameraSettings);
        if (mesh != null)
        {
            mesh.vertices = vertices.ToArray();
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

        var lines = File.ReadAllLines(ctx.assetPath);

        var root = new GameObject($"{baseName}_root");

        var reader = new NffReader(ctx.assetPath);

        reader.ParseMesh(baseName, root, ctx);

        ctx.AddObjectToAsset(ctx.assetPath, root);
        ctx.SetMainObject(root);


        //DestroyImmediate(mesh);
    }
}
