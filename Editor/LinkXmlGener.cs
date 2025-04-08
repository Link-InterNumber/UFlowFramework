using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Xml.Linq;

namespace PowerCellStudio
{
    public class StripLinkConfigEditor : EditorWindow
    {
        private const string LinkFilePath = "Assets/link.xml";
        private List<string> assemblies;
        private Dictionary<string, bool> assemblyToggles;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Strip Link Config Editor")]
        public static void ShowWindow()
        {
            GetWindow(typeof(StripLinkConfigEditor), false, "Strip LinkConfig Editor");
        }

        private void OnEnable()
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName().Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            assemblyToggles = assemblies.ToDictionary(name => name, _ => false);

            if (System.IO.File.Exists(LinkFilePath))
            {
                XElement root = XElement.Load(LinkFilePath);
                var preservedAssemblies = root.Descendants("assembly")
                    .Select(element => element.Attribute("fullname")?.Value)
                    .Where(name => !string.IsNullOrEmpty(name));

                foreach (var assembly in preservedAssemblies)
                {
                    if (assemblyToggles.ContainsKey(assembly))
                    {
                        assemblyToggles[assembly] = true;
                    }
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("勾选需要添加到link.xml的程序集，然后点击'生成'。", MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 100));

            foreach (var assembly in assemblies)
            {
                assemblyToggles[assembly] = EditorGUILayout.ToggleLeft(assembly, assemblyToggles[assembly]);
            }

            EditorGUILayout.EndScrollView();

            // 保持生成按钮在窗口的底部
            if (GUILayout.Button("生成", GUILayout.Height(30)))
            {
                GenerateLinkXml();
            }
        }

        private void GenerateLinkXml()
        {
            XElement root = new XElement("linker");
            foreach (var assembly in assemblyToggles)
            {
                if (assembly.Value)
                {
                    XElement assemblyElement = new XElement("assembly");
                    assemblyElement.SetAttributeValue("fullname", assembly.Key);
                    assemblyElement.SetAttributeValue("preserve", "all");
                    root.Add(assemblyElement);
                }
            }

            XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            doc.Save(LinkFilePath);

            Debug.Log($"link.xml has been saved at {LinkFilePath}");
        }
    }
}