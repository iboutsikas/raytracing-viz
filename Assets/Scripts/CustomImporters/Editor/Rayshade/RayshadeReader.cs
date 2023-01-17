using Codice.Client.Common;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

using UnityEditor.AssetImporters;

using UnityEngine;

namespace iboutsikas.CustomImporters
{
    internal class Triangle
    {
        private Vector3[] vertices;
        private Vector3[] normals;

        public Triangle(ref List<string> tokens)
        {
            vertices = new Vector3[3];
            normals = new Vector3[3];

            // 9 floats == 3 positions
            if (tokens.Count == 9)
            {
                for (int i = 0; i < 3; ++i)
                {
                    vertices[i] = new Vector3(
                        float.Parse(tokens[(i * 3) + 0], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[(i * 3) + 1], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[(i * 3) + 2], CultureInfo.InvariantCulture.NumberFormat)
                    );
                }

                var ab = vertices[1] - vertices[0];
                var ac = vertices[2] - vertices[0];

                var normal = Vector3.Cross(ab, ac);
                normal.Normalize();

                normals[0] = normal;
                normals[1] = normal;
                normals[2] = normal;
            }
            // 18 floats = 3 positions + 3 normals
            else if (tokens.Count == 9 + 9)
            {
                Vector3[] vertices = new Vector3[3];
                Vector3[] normals = new Vector3[3];


                for (int i = 0; i < 3; ++i)
                {
                    vertices[i] = new Vector3(
                        float.Parse(tokens[(i * 6) + 0], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[(i * 6) + 1], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[(i * 6) + 2], CultureInfo.InvariantCulture.NumberFormat)
                    );

                    normals[i] = new Vector3(
                        float.Parse(tokens[(i * 6) + 3], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[(i * 6) + 4], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[(i * 6) + 5], CultureInfo.InvariantCulture.NumberFormat)
                    );
                }
            }

        }

        public Mesh ToMesh()
        {
            Mesh m = new Mesh();

            m.vertices = vertices;
            m.normals = normals;

            m.triangles = new int[] {0, 1, 2 };

            return m;
        }
    }

    internal struct RayshadeKeywords
    {
        // Surface related keywords
        internal const string Surface           = "surface";
        internal const string Ambient           = "ambient";
        internal const string Diffuse           = "diffuse";
        internal const string Specular          = "specular";
        internal const string SpecularPower     = "specpow";
        internal const string Reflect           = "reflect";

        // View related
        internal const string EyePosition       = "eyep";
        internal const string LookAt            = "lookp";
        internal const string Up                = "up";
        internal const string FOV               = "fov";
        internal const string Resolution        = "screen";

        // Shapes
        internal const string Sphere            = "sphere";
        internal const string Triangle          = "triangle";
        internal const string Polygon           = "polygon";

        // Misc
        internal const string Comment            = "#";
    };

    internal class RayshadeReader
    {
        private readonly string _filepath;
        private readonly Shader _defaultShader;
        private Dictionary<string, Material> _materials;

        public RayshadeReader(string filepath, Shader defaultShader = null)
        {
            this._filepath = filepath;
            this._defaultShader = defaultShader == null ? Shader.Find("Universal Render Pipeline/Lit") : defaultShader;
            this._materials = new Dictionary<string, Material>();
        }

        public void ParseMesh(string meshName, GameObject root, AssetImportContext ctx)
        {
            int sphereCounter = 0;
            int triangleCounter = 0;
            int polygonCounter = 0;

            var lines = File.ReadAllLines(_filepath);

            var cameraSettings = ScriptableObject.CreateInstance<CameraSettings>();
            cameraSettings.name = $"{meshName}:CameraSettings";
            ctx.AddObjectToAsset(cameraSettings.name, cameraSettings);

            

            Material latestMaterial = null;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var tokens = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries).ToList();                

                if (tokens[0] == RayshadeKeywords.Comment)
                    continue;

                if (tokens[0] == RayshadeKeywords.Surface)
                {
                    latestMaterial = new Material(_defaultShader);
                    latestMaterial.name = $"{meshName}:{tokens[1]}";
                    latestMaterial.enableInstancing = true;

                    ctx.AddObjectToAsset(latestMaterial.name, latestMaterial);
                    
                    if (_materials.ContainsKey(latestMaterial.name)) {
                        Debug.Log($"Found surface {tokens[1]} on line {lineIndex + 1}, but a surface with that name already exists.");
                    }
                    _materials.Add(latestMaterial.name, latestMaterial);
                }
                else if (tokens[0] == RayshadeKeywords.Diffuse)
                {
                    Debug.Assert(latestMaterial != null);

                    var color = new Color(
                         float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                         float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                         float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                        );

                    latestMaterial.color = color;
                }
                else if (tokens[0] == RayshadeKeywords.EyePosition)
                {
                    var position = new Vector3(
                        float.Parse(tokens[1],  CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[2],  CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[3],  CultureInfo.InvariantCulture.NumberFormat)
                    );
                    cameraSettings.From = position;
                }
                else if (tokens[0] == RayshadeKeywords.LookAt)
                {
                    var position = new Vector3(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                    );
                    cameraSettings.At = position;
                }
                else if (tokens[0] == RayshadeKeywords.Up)
                {
                    var up = new Vector3(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                    );
                    cameraSettings.Up = up;
                }
                else if (tokens[0] == RayshadeKeywords.FOV)
                {
                    var angle = float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);

                    cameraSettings.Angle = angle;
                }
                else if (tokens[0] == RayshadeKeywords.Resolution)
                {
                    var resolution = new Vector2Int(
                        int.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                        int.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat)
                    );
                    cameraSettings.Resolution = resolution;
                }
                else if (tokens[0] == RayshadeKeywords.Sphere)
                {
                    var materialName = $"{meshName}:{tokens[1]}";

                    Material theMaterial = TryGetMaterial(materialName);

                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = $"Sphere {sphereCounter++}";

                    var meshRenderer = sphere.GetComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = theMaterial;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;

                    float radius = float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat);

                    Vector3 position = new Vector3(
                        float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[4], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(tokens[5], CultureInfo.InvariantCulture.NumberFormat)
                    );

                    sphere.transform.localPosition = position;
                    sphere.transform.localScale = new Vector3 ( radius, radius, radius );
                    sphere.transform.SetParent(root.transform);
                }
                else if (tokens[0] == RayshadeKeywords.Triangle)
                {
                    var materialName = $"{meshName}:{tokens[1]}";
                    Material theMaterial = TryGetMaterial(materialName);

                    var go = new GameObject($"Triangle {triangleCounter++}");
                    var meshFilter = go.AddComponent<MeshFilter>();
                    var meshRenderer = go.AddComponent<MeshRenderer>();
                    

                    var linesConsumed = GatherAdditionalTokens(lineIndex, ref lines, ref tokens);

                    // Remove the original 2 tokens (keyword + surface name)
                    tokens.RemoveRange(0, 2);
                    
                    if (tokens.Count != 9 && tokens.Count != 18)
                    {
                        Debug.Log($"Got invalid triangle on line {lineIndex + 1}");
                    }
                    else
                    {
                        var triangle = new Triangle(ref tokens);
                        var mesh = triangle.ToMesh();
                        mesh.name = "potato";
                        meshFilter.sharedMesh = mesh;
                        meshRenderer.sharedMaterial = theMaterial;
                        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        meshRenderer.receiveShadows = false;
                        ctx.AddObjectToAsset($"{meshName}:{go.name}", mesh);
                    }

                    go.transform.SetParent(root.transform);

                    // Advance index by however many lines we just consumed
                    lineIndex += linesConsumed;
                }
            }
        }
    
        
        private Material TryGetMaterial(string materialName)
        {
            Material theMaterial;
            if (!_materials.TryGetValue(materialName, out theMaterial))
            {
                Debug.Log($"Did not find surface {materialName} in the already parsed surfaces.");
                theMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            return theMaterial;
        }

        private int GatherAdditionalTokens(int currentIndex, ref string[] lines, ref List<string> tokens)
        {
            int offset = 1;

            if (currentIndex + offset >= lines.Length)
            {
                // We are already at the end of the file
                return 0;
            }

            var line = lines[currentIndex + offset];
            var newTokens = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries).ToArray();
            bool finished = false;

            while (newTokens.Length != 0 && !finished)
            {
                // We only check the line if it is not a comment
                if (newTokens[0] != RayshadeKeywords.Comment) {
                    foreach (var token in newTokens)
                    {
                        // If the token contains anything but numbers, ., -, it is not a valid token
                        // and we need to stop collecting more tokens
                        if (token.Any(c => !Char.IsDigit(c) && c != '.' && c != '-'))
                        {
                            finished = true;
                            break;
                        }
                        else
                        {
                            tokens.Add(token);
                        }
                    }
                }

                // Move on to the next line
                offset++;

                if (currentIndex + offset >= lines.Length)
                {
                    break;
                }

                line = lines[currentIndex + offset];
                newTokens = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries).ToArray();
            }

            return offset - 1;
        }
    }
}
