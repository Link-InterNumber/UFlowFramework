using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public sealed class AudioRef: AssetsRef<AudioClip>
    {
        public override LoaderYieldInstruction<AudioClip> Load(IAssetLoader assetLoader)
        {
            return assetLoader.LoadAsYieldInstruction<AudioClip>(assetName);
        }

        public override AssetReferenceT<AudioClip> GetAssetReference()
        {
            return new AssetReferenceAudioClip(guid);
        }

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
    }
}