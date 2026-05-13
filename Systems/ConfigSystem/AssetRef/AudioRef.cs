using System;
using System.IO;
using System.Text;
using UnityEngine;
// using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public sealed class AudioRef: AssetsRef<AudioClip>
    {
        public override LoaderYieldInstruction<AudioClip> Load(IAssetLoader assetLoader)
        {
            return assetLoader.LoadAsYieldInstruction<AudioClip>(assetName);
        }

        // public override AssetReferenceT<AudioClip> GetAssetReference()
        // {
        //     return new AssetReferenceAudioClip(guid);
        // }

        // public static implicit operator AudioClip(AudioRef target)
        // {
        //     return target.Load();
        // }
        public override bool isMatch(string lowerRawType)
        {
            return lowerRawType.Equals("Audio") || lowerRawType.Equals("audio");
        }

        public override string TypeName()
        {
            return "AudioRef";
        }

        public static AudioRef Parse(string stringValue, string confName, int rowIndex, int colIndex)
        {
            var ret = new AudioRef() { assetName = stringValue };
#if UNITY_EDITOR
//             var importer = AssetImporter.GetAtPath(stringValue);
//             if (importer == null || importer.assetBundleName == string.Empty)
//             {
//                 importer = GetUpImporter(stringValue);
//             }
//             if (importer == null)
//             {
//                 AssetLog.LogError($"{stringValue} no in any bundle!");
//                 return false;
//             }
//             ret.bundleName = importer.assetBundleName;
            ret.guid = UnityEditor.AssetDatabase.AssetPathToGUID(stringValue);
#endif
            return ret;
        }

        public static void WriteItemData(AudioRef item, BinaryWriter writer, Encoding encoding)
        {
            item.assetName.WriteString(writer, encoding);
            item.guid.WriteString(writer, encoding);
        }

        public static AudioRef ReadItemData(BinaryReader reader, Encoding encoding)
        {
            var assetName = StringExtension.ReadString(reader, encoding);
            var guid = StringExtension.ReadString(reader, encoding);
            return new AudioRef() { assetName = assetName, guid = guid };
        }

        public override void WriteData(BinaryWriter writer, Encoding encoding)
        {
            WriteItemData(this, writer, encoding);
        }

        public override void ReadData(BinaryReader reader, Encoding encoding)
        {
            var item = ReadItemData(reader, encoding);
            assetName = item.assetName;
            guid = item.guid;
        }
    }
}