using System.IO;
using System.Linq;
using OfficeOpenXml;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio
{
    public interface IGuidanceGraphWriteHandler
    {
        public bool SetUp();

        public void Write(GuidanceNodeView node);

        public void SetDown();
    }
}