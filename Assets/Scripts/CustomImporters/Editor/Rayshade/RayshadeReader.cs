using System.Collections.Generic;
using System.Globalization;
using System.IO;


using UnityEditor.AssetImporters;

using UnityEngine;

namespace iboutsikas.CustomImporters
{
    internal struct RayshadeKeywords
    {
        internal const string Surface           = "surface";
        internal const string Ambient           = "ambient";
        internal const string Diffuse           = "diffuse";
        internal const string Specular          = "specular";
        internal const string SpecularPower     = "specpow";
        internal const string Reflect           = "reflect";

        internal const string Sphere            = "sphere";
    };

    internal class RayshadeReader
    {
        private readonly string _filepath;
        private readonly Shader _defaultShader;

        public RayshadeReader(string filepath, Shader defaultShader = null)
        {
            this._filepath = filepath;
            this._defaultShader = defaultShader == null ? Shader.Find("Universal Render Pipeline/Lit") : defaultShader;
        }

        public void ParseMesh(string meshName, GameObject root, AssetImportContext ctx)
        {
            int sphereCounter = 0;

            var lines = File.ReadAllLines(_filepath);

            var cameraSettings = ScriptableObject.CreateInstance<CameraSettings>();
            cameraSettings.name = $"{meshName}:CameraSettings";
            ctx.AddObjectToAsset(cameraSettings.name, cameraSettings);

            Dictionary<string, Material> materials = new Dictionary<string, Material>();

            Material latestMaterial = null;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var tokens = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);

                if (tokens[0] == RayshadeKeywords.Surface)
                {
                    latestMaterial = new Material(_defaultShader);
                    latestMaterial.name = $"{meshName}:{tokens[1]}";
                    latestMaterial.enableInstancing = true;

                    ctx.AddObjectToAsset(latestMaterial.name, latestMaterial);
                    
                    if (materials.ContainsKey(latestMaterial.name)) {
                        Debug.Log($"Found surface {tokens[1]} on line {lineIndex + 1}, but a surface with that name already exists.");
                    }
                    materials.Add(latestMaterial.name, latestMaterial);
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
                else if (tokens[0] == RayshadeKeywords.Sphere)
                {
                    var materialName = $"{meshName}:{tokens[1]}";
                    Material theMaterial;
                    if (!materials.TryGetValue(materialName, out theMaterial))
                    {
                        Debug.Log($"Did not find surface {tokens[1]} in the already parsed surfaces.");
                        theMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
                    }

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

                    //float radius = 1.0f;
                    //Vector3 position = Vector3.one;

                    sphere.transform.localPosition = position;
                    sphere.transform.localScale = new Vector3 ( radius, radius, radius );
                    sphere.transform.SetParent(root.transform);
                }
            }
        }
    }
}
