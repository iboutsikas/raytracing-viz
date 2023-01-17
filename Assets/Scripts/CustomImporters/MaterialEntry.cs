using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using static UnityEditor.AssetImporter;

namespace iboutsikas.CustomImporters
{
    internal class MaterialEntry
    {
        public string path;
        public string materialName;

        public Material material;

        public SourceAssetIdentifier ToSourceAssetIdentifier()
        {
            return new SourceAssetIdentifier(typeof(Material), path + $":{materialName}");
        }
    }
}
