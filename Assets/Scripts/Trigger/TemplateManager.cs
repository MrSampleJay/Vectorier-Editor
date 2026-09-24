using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Vectorier.Trigger
{
    public class TriggerTemplate
    {
        string Template;

        string Loop;

        public string GetPath() => $"{Template}.{Loop}";

        public TriggerTemplate()
        {
            Template = string.Empty;
            Loop = string.Empty;
        }

        public TriggerTemplate(string template, string loop)
        {
            Template = template;
            Loop = loop;
        }

        public string TemplateField(Rect rect, string path)
        {
            string[] temp = path.Split('.');
            Template = temp.Length > 0 ? temp[0] : string.Empty;
            Loop = temp.Length > 1 ? temp[1] : string.Empty;

            Rect templateRect = new Rect(rect.x, rect.y, rect.width * 0.75f, rect.height);
            Template = TemplateManager.TemplateEnum(templateRect, Template);
            Rect loopRect = new Rect(rect.x + rect.width * 0.75f, rect.y, rect.width * 0.25f, rect.height);
            Loop = TemplateManager.LoopEnum(loopRect, Template, Loop);
            return GetPath();
        }
    }

    public static class TemplateManager
    {
        static XmlDocument templateDoc = new XmlDocument();

        public static XmlNode templates;

        public static XmlNodeList templatesList;

        public static string templatePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Snail Runner\\Vector_Data\\StreamingAssets\\xmlroot\\TriggerTemplates.xml");

        public static void ChooseFile()
        {
            templatePath = EditorUtility.OpenFilePanel($"Select TriggerTemplate", Application.dataPath, "xml");
            Debug.Log($"New TriggerTemplate Path Selected: {templatePath}");
            OpenDoc();
        }

        public static void Init()
        {
            templatePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Snail Runner\\Vector_Data\\StreamingAssets\\xmlroot\\TriggerTemplates.xml");
        }
        public static void OpenDoc()
        {
            templates = null;
            templatesList = null;
            if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                return;

            try
            {
                templateDoc.Load(templatePath);
                templates = templateDoc.DocumentElement;
                templatesList = templates?.ChildNodes;
            }
            catch (XmlException)
            {
                Debug.LogWarning($"Unable to load trigger templates from '{templatePath}'.");
            }
        }

        public static XmlNode GetTemplate(string Name)
        {
            if (templates == null || string.IsNullOrEmpty(Name))
                return null;

            foreach (XmlNode node in templates)
            { 
                if (node?.Attributes?["Name"]?.Value == Name)
                    return node;
            }
            return null;
        }

        public static XmlNode GetLoop(string Path)
        {
            if(string.IsNullOrEmpty(Path)) 
                return null;

            string[] names = Path.Split('.');
            if (names.Length < 2)
                return null;

            string templateName = names[0];
            string loopName = names[1];

            XmlNode template = GetTemplate(templateName);
            if (template == null) 
            { 
                return null; 
            }

            foreach (XmlNode node in template.ChildNodes) 
            {
                if (node.Name != "Loop")
                    continue;

                if (node?.Attributes?["Name"]?.Value == loopName)
                    return node;
            }
            return null;
        }

        public static XmlNode GetActionBlock(string Path)
        {
            XmlNode loop = GetLoop(Path);
            return loop?["Actions"];
        }

        public static XmlNode GetEventBlock(string Path)
        {
            XmlNode loop = GetLoop(Path);
            return loop?["Events"];
        }

        public static XmlNode GetConditionBlock(string Path)
        {
            XmlNode loop = GetLoop(Path);
            return loop?["Conditions"];
        }

        public static XmlNode GetBlock(string Path, string Name)
        {
            XmlNode loop = GetLoop(Path);
            return loop?[Name];
        }

        public static string TemplateEnum(Rect rect, string selectedName)
        {
            List<string> templateNames = new List<string>();
            templateNames.Add("None");
            foreach (XmlNode node in templatesList) 
            {
                if (node?.Attributes?["Name"] != null)
                    templateNames.Add(node.Attributes["Name"].Value);
            }
            int selected = templateNames.IndexOf(selectedName);

            if(selected == -1)
                selected = 0;

            selected = EditorGUI.Popup(rect, selected, templateNames.ToArray());
            if(selected != 0)
            {
                string selectedTemplate = templateNames[selected];
                return selectedTemplate;
            }
            return string.Empty;
        }

        public static string LoopEnum(Rect rect, string targetTemplate, string loopName)
        {
            XmlNode template = GetTemplate(targetTemplate);
            if (template == null)
                return null;

            List<string> loopNames = new List<string>();
            loopNames.Add("None");

            foreach (XmlNode node in template.ChildNodes)
            {
                if (node.Name == "Loop" && node?.Attributes?["Name"] != null)
                {
                    loopNames.Add(node.Attributes["Name"].Value);
                }
            }

            int selected = loopNames.IndexOf(loopName);

            if (selected == -1)
                selected = 0;

            selected = EditorGUI.Popup(rect, selected, loopNames.ToArray());
            if (selected != 0)
            {
                string selectedLoop = loopNames[selected];
                return selectedLoop;
            }
            return string.Empty;
        }

        public static string TemplatePathField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Template Path", GUILayout.Width(100));
            templatePath = EditorGUILayout.TextField(templatePath);
            if (GUILayout.Button("Choose File", GUILayout.Width(100)))
            {
                ChooseFile();
            }
            EditorGUILayout.EndHorizontal();
            return templatePath;
        }

        public static string TemplateSearch(Rect rect, string selectedName)
        {
            List<string> templateNames = new List<string>();
            templateNames.Add("None");
            foreach (XmlNode node in templatesList)
            {
                if (node?.Attributes?["Name"] != null)
                    templateNames.Add(node.Attributes["Name"].Value);
            }

            EditorGUI.BeginChangeCheck();

            string query = EditorGUI.TextField(rect, selectedName);
            string selected = null;
            if (EditorGUI.EndChangeCheck())
            {
                selected = templateNames.Exists(name => name.Equals(query, StringComparison.OrdinalIgnoreCase)) ? query : null;
            }
           
            if (selected != null)
            {
                Debug.Log($"Selected Template: {selected}");
                return selected;
            }
            return string.Empty;
        }
    }
}
