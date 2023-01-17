using Codice.Client.Common.GameUI;

using iboutsikas.CustomImporters;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEditor.AssetImporters;

using UnityEngine;

namespace iboutsikas.CustomImporters
{
    [ScriptedImporter(1, "ray")]
    public class RayshadeImporter : ScriptedImporter
    {
        [SerializeField]
        private Shader m_DefaultShader = null;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var baseName = Path.GetFileName(ctx.assetPath);
            var root = new GameObject($"{baseName}_root");

            var reader = new RayshadeReader(ctx.assetPath, m_DefaultShader);

            reader.ParseMesh(baseName, root, ctx);

            ApplyMaterialOverrides(root);

            ctx.AddObjectToAsset(ctx.assetPath, root);
            ctx.SetMainObject(root);
        }

        private void ApplyMaterialOverrides(GameObject root)
        {
            var remap = GetExternalObjectMap();

            foreach (var r in remap)
            {
                if (r.Value == null)
                    continue;

                var tokens = r.Key.name.Split(':');
                var meshName = tokens[0];

                var meshGo = GetGameObjectFromPath(root, meshName);

                if (meshGo == null)
                    continue;

                var haveRenderer = meshGo.TryGetComponent<MeshRenderer>(out var renderer);

                if (haveRenderer)
                {
                    renderer.sharedMaterial = (Material)r.Value;
                }

            }
        }


        internal List<MaterialEntry> GetMaterialSlots(GameObject go)
        {
            var ret = new List<MaterialEntry>();
            var remap = GetExternalObjectMap();

            foreach (var data in go.GetComponentsInChildren<RayshadeCustomData>())
            {
                var path = GetGameObjectPath(data.gameObject);
                var entry = new MaterialEntry() { path = path, materialName = data.RayMaterialName };

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