using System.IO;
using System.Linq;
using OfficeOpenXml;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio
{
    public class GuidanceGraphExcelHandler : IGuidanceGraphWriteHandler
    {
        // 使用EPPlus写入Excel中
        private string _filePath;
        private OfficeOpenXml.ExcelPackage _package;
        private OfficeOpenXml.ExcelWorksheet _sheet;

        public bool SetUp()
        {
            var lastSelectedDir = EditorPrefs.GetString("GuidanceGraphLastExcelDir", Application.dataPath);
            _filePath = EditorUtility.OpenFilePanel("Select Excel File to Save Guidance Data", lastSelectedDir, "xlsx");
            if (string.IsNullOrEmpty(_filePath))
            {
                Debug.LogError("No file selected for saving guidance data.");
                return false;
            }
            var fileInfo = new FileInfo(_filePath);
            if (string.IsNullOrEmpty(_filePath) || !fileInfo.Directory.Exists)
            {
                Debug.LogError("Invalid file path selected for saving guidance data.");
                return false;
            }
            if (!string.IsNullOrEmpty(_filePath))
            {
                EditorPrefs.SetString("GuidanceGraphLastExcelDir", _filePath);
            }
            _package = new OfficeOpenXml.ExcelPackage(fileInfo);
            _sheet = _package.Workbook.Worksheets[0];
            return true;
        }

        public void Write(GuidanceNodeView node)
        {
            if (_package == null) return;
            var targetId = node.GetGuidanceId();
            // 查找第一列中是否存在该ID，否则在末尾添加新行
            int targetRow = -1;
            for (int row = 2; row <= _sheet.Dimension.End.Row; row++)
            {
                var cellValue = _sheet.Cells[row, 1].GetValue<int>();
                if (cellValue == targetId)
                {
                    targetRow = row;
                    break;
                }
            }
            if (targetRow == -1)
            {
                targetRow = _sheet.Dimension.End.Row + 1;
            }
            var nextGuidance = 0;
            if (node.outputContainer.Query<Port>().AtIndex(0).connected)
            {
                var nextPort = node.outputContainer.Query<Port>().AtIndex(0);
                var nextNode = nextPort.connections.First().input.node;
                nextGuidance = (nextNode as GuidanceNodeView).GetGuidanceId();
            }
            // ID
            // NextId
            // public LocalizationStringRef decs
            // public bool touchScreenToSkip
            // public bool blockInteraction
            // public GameObjectRef uiPrefab

            _sheet.Cells[targetRow, 1].Value = targetId;
            if (nextGuidance > 0) _sheet.Cells[targetRow, 2].Value = nextGuidance;
            _sheet.Cells[targetRow, 3].Value = node.GetDecs();
            _sheet.Cells[targetRow, 4].Value = node.GetTouchScreenToSkip() ? 1 : 0;
            _sheet.Cells[targetRow, 5].Value = node.GetBlockInteraction() ? 1 : 0;
            var uiPrefabPath = AssetDatabase.GetAssetPath(node.GetUiPrefab());
            if (!string.IsNullOrEmpty(uiPrefabPath))
                _sheet.Cells[targetRow, 6].Value = uiPrefabPath;
        }

        public void SetDown()
        {
            if (_package == null) return;
            // _sheet从第5行开始，按照第一列ID排序
            var totalRows = _sheet.Dimension.End.Row;
            var totalCols = _sheet.Dimension.End.Column;
            using (ExcelRange range = _sheet.Cells[4, 1, totalRows, totalCols])
            {
                range.Sort(1);
            }
            _package.Save();
            _package.Dispose();
            _package = null;
            _sheet = null;
        }
    }
}