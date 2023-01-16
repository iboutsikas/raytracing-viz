using Codice.Client.Common.GameUI;

using iboutsikas.CustomImporters;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
        }
    }
}