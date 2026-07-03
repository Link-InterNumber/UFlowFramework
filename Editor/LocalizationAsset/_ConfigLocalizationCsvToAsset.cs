// using System;
// using System.Collections.Generic;
// using System.IO;
//
// namespace PowerCellStudio.Editor
// {
//     public class ConfigLocalizationCsvToAsset
//     {
//         public static void Produce()
//         {
//             var excelPath = EditorSaveUtils.GetEditorPref(ConfigSettingLogic.SaveKey.excelPath, "");
//             var directory = Path.Combine(excelPath, ConfigSettingLogic.LocalizationFolderName);
//             var csvFiles = Directory.GetFiles(directory, "*.csv");
//             var list = new List<ConfigLocalizationData>();
//             for (var i = 0; i < csvFiles.Length; i++)
//             {
//                 var csvPath = csvFiles[i];
//                 var reader = new StreamReader(csvPath);
//                 reader.ReadLine();
//                 list.Clear();
//                 while (!reader.EndOfStream)
//                 {
//                     var str = reader.ReadLine();
//                     if (string.IsNullOrEmpty(str))
//                         continue;
//                     var spanStr = str.AsSpan();
//                     var data = new ConfigLocalizationData();
//                     var leftIndex = 0;
//                     var rightIndex = 0;
//                     while (rightIndex < spanStr.Length)
//                     {
//                         if (spanStr[rightIndex] == ',')
//                         {
//                             var value = spanStr.Slice(leftIndex, rightIndex - leftIndex).ToString();
//                             data.key = value;
//                             rightIndex++;
//                             break;
//                         }
//                         rightIndex++;
//                     }
//
//                     if (rightIndex == spanStr.Length)
//                     {
//                         continue;
//                     }
//
//                     while (rightIndex < spanStr.Length)
//                     {
//                         if (spanStr[rightIndex] == ',')
//                         {
//                             leftIndex = rightIndex + 1;
//                             break;
//                         }
//                         rightIndex++;
//                     }
//                     data.value = spanStr.Slice(leftIndex, spanStr.Length - leftIndex).ToString();
//                     list.Add(data);
//                 }
//
//                 var csvName = Path.GetFileNameWithoutExtension(csvPath);
//                 var chunkPath = Path.Combine(ConfigManager.assetFolderPath, ConfigSettingLogic.LocalizationFolderName);
//                 ChunkMaker.StreamWriteSync(chunkPath, csvName, list, ConfigLocalizationData.GetKey, 512);
//             }
//                 
//
//         }
//     }
// }