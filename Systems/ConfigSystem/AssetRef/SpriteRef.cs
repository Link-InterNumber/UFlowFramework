using System;
using System.IO;
using System.Text;
using UnityEngine;
// using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public sealed class SpriteRef: AssetsRef<Sprite>
    {
        public override LoaderYieldInstruction<Sprite> Load(IAssetLoader assetLoader)
        {
            return assetLoader.LoadAsYieldInstruction<Sprite>(assetName);
        }

        // public override AssetReferenceT<Sprite> GetAssetReference()
        // {
        //     return new AssetReferenceSprite(guid);
        // }

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

        public static void WriteItemData(SpriteRef item, BinaryWriter writer, Encoding encoding)
        {
            item.assetName.WriteString(writer, encoding);
            item.guid.WriteString(writer, encoding);
        }

        public static SpriteRef ReadItemData(BinaryReader reader, Encoding encoding)
        {
            var assetName = StringExtension.ReadString(reader, encoding);
            var guid = StringExtension.ReadString(reader, encoding);
            return new SpriteRef() { assetName = assetName, guid = guid };
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