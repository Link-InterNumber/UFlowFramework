using System;
using System.IO;
using System.Text;
using UnityEngine;
// using UnityEngine.AddressableAssets;

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

        // public override AssetReferenceT<GameObject> GetAssetReference()
        // {
        //     return new AssetReferenceGameObject(guid);
        // }

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

        public static void WriteItemData(GameObjectRef item, BinaryWriter writer, Encoding encoding)
        {
            item.assetName.WriteString(writer, encoding);
            item.guid.WriteString(writer, encoding);
        }

        public static GameObjectRef ReadItemData(BinaryReader reader, Encoding encoding)
        {
            var assetName = StringExtension.ReadString(reader, encoding);
            var guid = StringExtension.ReadString(reader, encoding);
            return new GameObjectRef() { assetName = assetName, guid = guid };
        }

        public override void WriteData(BinaryWriter writer, Encoding encoding)
        {
            assetName.WriteString(writer, encoding);
            guid.WriteString(writer, encoding);
        }

        public override void ReadData(BinaryReader reader, Encoding encoding)
        {
            assetName = StringExtension.ReadString(reader, encoding);
            guid = StringExtension.ReadString(reader, encoding);
        }
    }
}