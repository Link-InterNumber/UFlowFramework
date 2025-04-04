using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public sealed class SpriteRef: AssetsRef<Sprite>
    {
        public override LoaderYieldInstruction<Sprite> Load(IAssetLoader assetLoader)
        {
            return assetLoader.LoadAsYieldInstruction<Sprite>(assetName);
        }

        public override AssetReferenceT<Sprite> GetAssetReference()
        {
            return new AssetReferenceSprite(guid);
        }

        // public static implicit operator Sprite(SpriteRef target)
        // {
        //     return target.Load();
        // }
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("Sprite") || lowerRawType.Equals("sprite");
        }

        public override string TypeName()
        {
            return "SpriteRef";
        }

        public static SpriteRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            var ret = new SpriteRef() { assetName = stringValue };
#if UNITY_EDITOR
//             var importer = AssetImporter.GetAtPath(raw);
//             if (importer == null || importer.assetBundleName == string.Empty)
//             {
//                 importer = GetUpImporter(raw);
//             }
//             if (importer == null)
//             {
//                 AssetLog.LogError($"{raw} no in any bundle!");
//                 return false;
//             }
//             ret.bundleName = importer.assetBundleName;
            ret.guid = UnityEditor.AssetDatabase.AssetPathToGUID(stringValue);
#endif
            return ret;
        }
    }
}