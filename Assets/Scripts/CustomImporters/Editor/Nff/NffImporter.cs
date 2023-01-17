using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.AssetImporter;

namespace iboutsikas.CustomImporters
{
    internal class NffReader
    {
        private readonly string _filepath;
        private readonly Shader _defaultShader;

        public NffReader(string filepath, Shader defaultShader = null)
        {
            _filepath = filepath;
            _defaultShader = defaultShader == null ? Shader.Find("Universal Render Pipeline/Lit") : defaultShader;
        }

        public void ParseMesh(string meshName, GameObject root, AssetImportContext ctx)
        {

            var lines = File.ReadAllLines(_filepath);
            var vertices = new List<Vector3>();
            var lights = new List<GameObject>();
            var indices = new List<int>();
            int meshCounter = 0;
            int sphereCounter = 0;
            int materialCounter = 0;
            Mesh mesh = null;
            Material latestMaterial = null;

            var cameraSettings = ScriptableObject.CreateInstance<CameraSettings>();
            cameraSettings.name = $"{meshName}:CameraSettings";


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
                            var data = go.AddComponent<NffCustomData>();

                            latestMaterial = new Material(_defaultShader);
                            latestMaterial.enableInstancing = true;
                            latestMaterial.name = $"{meshName}:material{materialCounter++}";
                            data.NffMaterialName = latestMaterial.name;

                            var color = new Color(
                                float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                            );
                            float diffuseComponent = float.Parse(tokens[4], CultureInfo.InvariantCulture.NumberFormat);

                            latestMaterial.color = color * diffuseComponent;

                            meshRenderer.sharedMaterial = latestMaterial;
                            ctx.AddObjectToAsset(latestMaterial.name, latestMaterial);

                            mesh = new Mesh() { name = $"Mesh {meshCounter}" };
                            ctx.AddObjectToAsset($"{meshName}:mesh{meshCounter}", mesh);
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
                    case "l":
                        {
                            var go = new GameObject($"Light #{lights.Count}");
                            var light = go.AddComponent<Light>();
                            light.type = LightType.Directional;
                            light.shadows = LightShadows.Hard;

                            var position = new Vector3(
                                float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat),
                                float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat)
                            );

                            light.transform.position = position;
                            var lightDir = -position;
                            light.transform.rotation = Quaternion.LookRotation(lightDir);
                            if (tokens.Length == 7)
                            {
                                light.color = new Color(
                                        float.Parse(tokens[4], CultureInfo.InvariantCulture.NumberFormat),
                                        float.Parse(tokens[5], CultureInfo.InvariantCulture.NumberFormat),
                                        float.Parse(tokens[6], CultureInfo.InvariantCulture.NumberFormat)
                                    );
                            }
                            light.transform.SetParent(root.transform);
                            lights.Add(go);
                            break;
                        }
                }
            }

            ctx.AddObjectToAsset(cameraSettings.name, cameraSettings);
            if (mesh != null)
            {
                if (vertices.Count != 0)
                    mesh.vertices = vertices.ToArray();
                if (indices.Count != 0)
                    mesh.triangles = indices.ToArray();
            }

            var intensity = 1.0f / Mathf.Sqrt(lights.Count);
            foreach (var go in lights)
            {
                var light = go.GetComponent<Light>();
                light.intensity = intensity;
            }
        }
    }

    internal struct NffMaterialEntry
    {
        public string path;
        public string materialName;

        public Material material;

        public SourceAssetIdentifier ToSourceAssetIdentifier()
        {
            return new SourceAssetIdentifier(typeof(Material), path + $":{materialName}");
        }
    }

    [ScriptedImporter(1, "nff")]
    public class NffImporter : ScriptedImporter
    {
        [SerializeField]
        private Shader m_DefaultShader = null;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var baseName = Path.GetFileName(ctx.assetPath);
            var root = new GameObject($"{baseName}_root");
            var reader = new NffReader(ctx.assetPath, m_DefaultShader);

            reader.ParseMesh(baseName, root, ctx);

            ApplyMaterialOverrides(root);

            ctx.AddObjectToAsset(ctx.assetPath, root);
            ctx.SetMainObject(root);

        }

        void ApplyMaterialOverrides(GameObject go)
        {
            var remap = GetExternalObjectMap();

            foreach (var r in remap)
            {
                if (r.Value == null)
                    continue;

                var tokens = r.Key.name.Split(':');
                var meshName = tokens[0];

                var meshGo = GetGameObjectFromPath(go, meshName);

                if (meshGo == null)
                    continue;

                var haveRenderer = meshGo.TryGetComponent<MeshRenderer>(out var renderer);

                if (haveRenderer)
                {
                    renderer.sharedMaterial = (Material)r.Value;
                }

            }

        }


        internal static List<NffMaterialEntry> GetMaterialSlots(NffImporter importer, GameObject go)
        {
            var ret = new List<NffMaterialEntry>();
            var remap = importer.GetExternalObjectMap();

            foreach (var data in go.GetComponentsInChildren<NffCustomData>())
            {
                var path = GetGameObjectPath(data.gameObject);
                var entry = new NffMaterialEntry() { path = path, materialName = data.NffMaterialName };

                if (remap.TryGetValue(entry.ToSourceAssetIdentifier(), out var overrideMaterial))
                {
                    entry.material = (Material)overrideMaterial;
                }
                ret.Add(entry);
            }


            return ret;
        }

        private static string GetGameObjectPath(GameObject go)
        {
            var reversePath = new List<string>();
            var parent = go.transform;
            while (parent != null)
            {
                reversePath.Add(parent.name);
                parent = parent.parent;
            }

            var sb = new StringBuilder();
            for (var i = reversePath.Count - 2; i >= 0; --i) // We don't want the root
            {
                sb.Append(reversePath[i]);
                sb.Append('/');
            }
            return sb.ToString().TrimEnd('/');
        }

        private static GameObject GetGameObjectFromPath(GameObject root, string path)
        {
            var go = root;
            foreach (var name in path.Split('/'))
            {
                var found = false;
                for (var i = 0; i < go.transform.childCount; ++i)
                {
                    var ch = go.transform.GetChild(i);
                    if (ch.name == name)
                    {
                        go = ch.gameObject;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return null;
                }
            }

            return go;
        }
    }
}