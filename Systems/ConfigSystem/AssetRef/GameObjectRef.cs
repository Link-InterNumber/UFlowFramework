using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public sealed class GameObjectRef: AssetsRef<GameObject>
    {
        public bool isNull => string.IsNullOrEmpty(assetName);
        
        public override LoaderYieldInstruction<GameObject> Load(IAssetLoader assetLoader)
        {
            return assetLoader.LoadAsYieldInstruction<GameObject>(assetName);
        }

        public override AssetReferenceT<GameObject> GetAssetReference()
        {
            return new AssetReferenceGameObject(guid);
        }

        // public static implicit operator GameObject(GameObjectRef target)
        // {
        //     return target.Load();
        // }
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("GameObject") || lowerRawType.Equals("gameobject") || lowerRawType.Equals("Gameobject") || lowerRawType.Equals("gameObject");
        }

        public override string TypeName()
        {
            return "GameObjectRef";
        }

        public static GameObjectRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            var ret = new GameObjectRef() { assetName = stringValue };
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