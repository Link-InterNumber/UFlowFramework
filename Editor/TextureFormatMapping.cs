using UnityEngine;
using System.Collections.Generic;

public class TextureFormatMapping
{
    public static Dictionary<string, HashSet<TextureImporterFormat>> PlatformFormats = new Dictionary<string, HashSet<TextureImporterFormat>>()
    {
        {
            "Standalone", new HashSet<TextureImporterFormat>
            {
                TextureImporterFormat.Alpha8,

                TextureImporterFormat.R8,
                TextureImporterFormat.R16,
                TextureImporterFormat.RG32,

                TextureImporterFormat.RGB16,
                TextureImporterFormat.RGB24,
                TextureImporterFormat.RGB48,
                TextureImporterFormat.RGBA16,
                TextureImporterFormat.RGBA32,
                TextureImporterFormat.RGBA64,

                TextureImporterFormat.BC7,
                TextureImporterFormat.BC6H,
                TextureImporterFormat.DXT1,
                TextureImporterFormat.DXT1Crunched
                TextureImporterFormat.DXT5,
                TextureImporterFormat.DXT5Crunched

            }
        },
        {
            "Android", new HashSet<TextureImporterFormat>
            {
                TextureImporterFormat.Alpha8,

                TextureImporterFormat.RGB16,
                TextureImporterFormat.RGB24,
                // TextureImporterFormat.RGB48,
                TextureImporterFormat.RGBA16,
                TextureImporterFormat.RGBA32,
                // TextureImporterFormat.RGBA64,
                TextureImporterFormat.ETC2_RGBA8,
                TextureImporterFormat.ETC2_RGBA8Crunched,

                TextureImporterFormat.ASTC_4x4,
                TextureImporterFormat.ASTC_5x5,
                TextureImporterFormat.ASTC_6x6,
                TextureImporterFormat.ASTC_8x8,
                TextureImporterFormat.ASTC_10x10,
                TextureImporterFormat.ASTC_12x12,
            }
        },
        {
            "iOS", new HashSet<TextureImporterFormat>
            {
                TextureImporterFormat.Alpha8,

                TextureImporterFormat.R8,
                TextureImporterFormat.RG32,

                TextureImporterFormat.RGB16,
                TextureImporterFormat.RGB24,
                TextureImporterFormat.RGB48,
                TextureImporterFormat.RGBA16,
                TextureImporterFormat.RGBA32,
                TextureImporterFormat.RGBA64,

                TextureImporterFormat.PVRTC_RGB4,
                TextureImporterFormat.PVRTC_RGBA4,

                TextureImporterFormat.ETC2_RGB4,
                TextureImporterFormat.ETC_RGB4Crunched,

                TextureImporterFormat.ETC2_RGBA8,
                TextureImporterFormat.ETC2_RGBA8Crunched,

                TextureImporterFormat.ASTC_4x4,
                TextureImporterFormat.ASTC_5x5,
                TextureImporterFormat.ASTC_6x6,
                TextureImporterFormat.ASTC_8x8,
                TextureImporterFormat.ASTC_10x10,
                TextureImporterFormat.ASTC_12x12,
            }
        },
        {
            "WebGL", new HashSet<TextureImporterFormat>
            {
                TextureImporterFormat.RGB16,
                TextureImporterFormat.RGB24,
                TextureImporterFormat.RGBA16,
                TextureImporterFormat.RGBA32,

                TextureImporterFormat.ETC1,
                TextureImporterFormat.DXT1,
                TextureImporterFormat.DXT5

                TextureImporterFormat.ETC2_RGB4,
                TextureImporterFormat.ETC_RGB4Crunched,

                TextureImporterFormat.ETC2_RGBA8,
                TextureImporterFormat.ETC2_RGBA8Crunched,

                // WebGL 2.0 支持
                TextureImporterFormat.ASTC_4x4,
                TextureImporterFormat.ASTC_5x5,
                TextureImporterFormat.ASTC_6x6,
                TextureImporterFormat.ASTC_8x8,
                TextureImporterFormat.ASTC_10x10,
                TextureImporterFormat.ASTC_12x12,
            }
        },
        {
            "PS4", new HashSet<TextureImporterFormat>
            {
                TextureImporterFormat.RGB16,
                TextureImporterFormat.RGB24,
                TextureImporterFormat.RGB48,
                TextureImporterFormat.RGBA16,
                TextureImporterFormat.RGBA32,
                TextureImporterFormat.RGBA64,

                TextureImporterFormat.BC7,
                TextureImporterFormat.BC6H,
                TextureImporterFormat.RGBA32
            }
        }
    };
}