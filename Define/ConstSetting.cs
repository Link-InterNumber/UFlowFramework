using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace PowerCellStudio
{
	public class ConstSetting
	{
		/// <summary>
		/// 路径分隔符
		/// </summary>
		public static readonly char[] PathSeparator = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		/// <summary>
		/// 分包配置文件夹
		/// </summary>
		public static readonly string BundleAssetConfigFolder = "AssetBundleData";
		/// <summary>
		/// 分包配置文件名
		/// </summary>
		public static readonly string BundleAssetConfigName = "AssetBundleData.asset";
		/// <summary>
		/// 设计分辨率范围
		/// </summary>
		public static readonly int[] Resolution = new[] { 360, 720, 1080, 1440, 2160 };
		/// <summary>
		/// 默认分辨率
		/// </summary>
		public static readonly ResolutionLv DefaultResolutionLv = ResolutionLv.High;
		/// <summary>
		/// 设计UI画布尺寸
		/// </summary>
		public static readonly Vector2Int DefaultUISize = new Vector2Int(1080, 1920);
		public static readonly string FileEncryptionKey = "Catcatlittlecat";
		/// <summary>
		/// 默认语言
		/// </summary>
		public static readonly Language DefaultLanguage = Language.ChineseSimplified;
		/// <summary>
		/// 本地化字符串表
		/// </summary>
		public static readonly string LocalizationStringTable = "ThiefHero";
		/// <summary>
		/// 本地化资源表
		/// </summary>
		public static readonly string LocalizationAssetTable = "ThiefHeroAsset";
		/// <summary>
		/// 本地化语言对应字体
		/// </summary>
		public static readonly Dictionary<Language, string> LanguageFont = new Dictionary<Language, string>()
		{
			{ Language.ChineseSimplified, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
			{ Language.English, "Assets/UFlowFramework/Fonts/ZiHunBianTaoTi.ttf" },
			{ Language.ChineseTraditional, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi-2-85-Bold.ttf" },
			{ Language.Japanese, "Assets/UFlowFramework/Fonts/AlibabaPuHuiTi-2-85-Bold.ttf" },
		}
		;
		public enum ConfigSaveMode { ScriptableObject, Json, Binary }
		public static readonly ConfigSaveMode ConfigConfigSaveMode = ConfigSaveMode.Binary;
	}
}
