using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Vectorier.XML;
using Vectorier.Handler;

namespace Vectorier.Core
{
    public class Buildmap : EditorWindow
    {
        private BuildmapConfig config;

        private Vector2 mainScrollPosition;
        private Vector2 commonModeScrollPosition;
        private Vector2 hunterModeScrollPosition;

        // -------------------------
        // Menu
        // -------------------------
        [MenuItem("Vectorier/Build")]
        public static void ShowWindow()
        {
            GetWindow<Buildmap>("Buildmap");
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
        }

        private void OnDisable()
        {
            SaveConfig();
        }

        // -------------------------
        // Config
        // -------------------------
        private void LoadOrCreateConfig()
        {
            string assetPath = "Assets/Editor/Config/BuildmapConfig.asset";
            string folder = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            config = AssetDatabase.LoadAssetAtPath<BuildmapConfig>(assetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildmapConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("[Buildmap] Created new configuration asset at " + assetPath);
            }
        }

        private void SaveConfig()
        {
            if (config != null)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        // -------------------------
        // UI
        // -------------------------
        private void OnGUI()
        {
            if (config == null)
            {
                LoadOrCreateConfig();
                return;
            }

            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition, GUILayout.ExpandHeight(true));

            EditorGUILayout.LabelField("Vectorier XML Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            config.exportType = (BuildmapConfig.ExportType)EditorGUILayout.EnumPopup("Export Type", config.exportType);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            switch (config.exportType)
            {
                case BuildmapConfig.ExportType.Level:
                    DrawLevelConfigUI();
                    break;
                case BuildmapConfig.ExportType.Objects:
                    DrawObjectsConfigUI();
                    break;
                case BuildmapConfig.ExportType.Buildings:
                    DrawBuildingsConfigUI();
                    break;
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Build and Export", GUILayout.Height(50)))
            {
                SaveConfig();
                if (config.exportType == BuildmapConfig.ExportType.Level)
                    BuildLevel();
                else if (config.exportType == BuildmapConfig.ExportType.Objects)
                    BuildObjects();
                else
                    BuildBuildings();
            }

            EditorGUILayout.EndScrollView();
        }

        // -------------------------
        // UI Drawers
        // -------------------------
        private void DrawLevelConfigUI()
        {
            config.filePathDirectory = EditorGUILayout.TextField("File Path Directory", config.filePathDirectory);
            config.fileName = EditorGUILayout.TextField("Level Name", config.fileName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<Sets>", EditorStyles.boldLabel);
            DrawSetListUI("City", config.citySets);
            DrawSetListUI("Ground", config.groundSets);
            DrawSetListUI("Library", config.librarySets);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<Music>", EditorStyles.boldLabel);
            config.musicName = EditorGUILayout.TextField("Music Name", config.musicName);
            config.musicVolume = EditorGUILayout.FloatField("Music Volume", config.musicVolume);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<Models>", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Common Mode");
            commonModeScrollPosition = EditorGUILayout.BeginScrollView(
                commonModeScrollPosition,
                true,
                true,
                GUILayout.Height(100)
            );
            config.commonModeModels = EditorGUILayout.TextArea(
                config.commonModeModels,
                new GUIStyle(EditorStyles.textArea) { wordWrap = false },
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            );
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Hunter Mode");
            hunterModeScrollPosition = EditorGUILayout.BeginScrollView(
                hunterModeScrollPosition,
                true,
                true,
                GUILayout.Height(100)
            );
            config.hunterModeModels = EditorGUILayout.TextArea(
                config.hunterModeModels,
                new GUIStyle(EditorStyles.textArea) { wordWrap = false },
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            );
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            config.coinValue = EditorGUILayout.IntField("Coins Value", config.coinValue);
            config.fastBuild = EditorGUILayout.Toggle("Fast Build", config.fastBuild);
            config.exportAsXML = EditorGUILayout.Toggle("Export as XML", config.exportAsXML);
        }

        private void DrawObjectsConfigUI()
        {
            config.filePathDirectory = EditorGUILayout.TextField("File Path Directory", config.filePathDirectory);
            config.fileName = EditorGUILayout.TextField("Objects Name", config.fileName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<Sets>", EditorStyles.boldLabel);
            DrawSetListUI("City", config.citySets);
            DrawSetListUI("Ground", config.groundSets);
            DrawSetListUI("Library", config.librarySets);

            config.fastBuild = EditorGUILayout.Toggle("Fast Build", config.fastBuild);
            config.exportAsXML = EditorGUILayout.Toggle("Export as XML", config.exportAsXML);
        }

        private void DrawBuildingsConfigUI()
        {
            config.filePathDirectory = EditorGUILayout.TextField("File Path Directory", config.filePathDirectory);
            config.fileName = EditorGUILayout.TextField("Buildings Name", config.fileName);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("<Sets>", EditorStyles.boldLabel);
            DrawSetListUI("City", config.citySets);
            DrawSetListUI("Ground", config.groundSets);
            DrawSetListUI("Library", config.librarySets);

            config.fastBuild = EditorGUILayout.Toggle("Fast Build", config.fastBuild);
            config.exportAsXML = EditorGUILayout.Toggle("Export as XML", config.exportAsXML);
        }

        private void DrawSetListUI(string setName, List<string> setList)
        {
            EditorGUILayout.LabelField(setName + " Sets", EditorStyles.boldLabel);
            int removeIndex = -1;
            for (int i = 0; i < setList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                setList[i] = EditorGUILayout.TextField(setList[i]);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0)
                setList.RemoveAt(removeIndex);

            if (GUILayout.Button($"Add {setName} Set"))
                setList.Add("");
        }

        // -------------------------
        // Build operations
        // -------------------------
        private void BuildLevel()
        {
            string xmlFolder = Path.Combine(Application.dataPath, "XML");
            string templatePath = Path.Combine(xmlFolder, "level-template.xml");
            string levelFolder = Path.Combine(xmlFolder, "level_xml");

            EnsureDirectoryExists(levelFolder);

            XmlUtility xmlUtility = new XmlUtility();
            xmlUtility.Create("Root");
            XmlElement root = xmlUtility.RootElement;

            // Add sets
            AddSetsToXml(xmlUtility, root);

            // Music
            if (!string.IsNullOrEmpty(config.musicName))
            {
                XmlElement musicElement = xmlUtility.AddElement(root, "Music");
                xmlUtility.SetAttribute(musicElement, "Name", config.musicName);
                xmlUtility.SetAttribute(musicElement, "Volume", config.musicVolume);
            }

            // Models
            if (!string.IsNullOrEmpty(config.commonModeModels))
            {
                XmlElement modelsCommon = xmlUtility.AddElement(root, "Models");
                xmlUtility.SetAttribute(modelsCommon, "Choice", "AITriggers");
                xmlUtility.SetAttribute(modelsCommon, "Variant", "CommonMode");
                modelsCommon.InnerXml = config.commonModeModels;
            }
            if (!string.IsNullOrEmpty(config.hunterModeModels))
            {
                XmlElement modelsHunter = xmlUtility.AddElement(root, "Models");
                xmlUtility.SetAttribute(modelsHunter, "Choice", "AITriggers");
                xmlUtility.SetAttribute(modelsHunter, "Variant", "HunterMode");
                modelsHunter.InnerXml = config.hunterModeModels;
            }

            // Coins
            if (config.coinValue > 0)
            {
                XmlElement coins = xmlUtility.AddElement(root, "Coins");
                xmlUtility.SetAttribute(coins, "Value", config.coinValue);
                XmlElement objects = xmlUtility.AddElement(root, "Objects");
                xmlUtility.SetAttribute(objects, "Name", "Money");
            }

            // Save template and export
            xmlUtility.Save(templatePath);

            ExportHandler.Export(ExportHandler.ExportMode.Level, templatePath);

            if (string.IsNullOrEmpty(config.fileName))
            {
                UnityEngine.Debug.LogWarning("[Buildmap] Level Name is empty. Using 'UnnamedLevel'.");
                config.fileName = "UnnamedLevel";
            }

            string destinationXml = Path.Combine(levelFolder, $"{config.fileName}.xml");

            XmlUtility.FormatXML(templatePath, templatePath);
            File.Copy(templatePath, destinationXml, true);

            CompileXML(templatePath);
        }

        private void BuildObjects()
        {
            string xmlFolder = Path.Combine(Application.dataPath, "XML");
            string templatePath = Path.Combine(xmlFolder, "objects-template.xml");
            string levelFolder = Path.Combine(xmlFolder, "level_xml");

            EnsureDirectoryExists(levelFolder);

            XmlUtility xmlUtility = new XmlUtility();
            xmlUtility.Create("Root");
            XmlElement root = xmlUtility.RootElement;

            AddSetsToXml(xmlUtility, root);

            xmlUtility.Save(templatePath);

            ExportHandler.Export(ExportHandler.ExportMode.Objects, templatePath);

            if (string.IsNullOrEmpty(config.fileName))
            {
                UnityEngine.Debug.LogWarning("[Buildmap] Name is empty. Using 'UnnamedObjectSet'.");
                config.fileName = "UnnamedObjectSet";
            }

            string destinationXml = Path.Combine(levelFolder, $"{config.fileName}.xml");

            XmlUtility.FormatXML(templatePath, templatePath);
            File.Copy(templatePath, destinationXml, true);

            CompileXML(templatePath);
        }

        private void BuildBuildings()
        {
            string xmlFolder = Path.Combine(Application.dataPath, "XML");
            string templatePath = Path.Combine(xmlFolder, "buildings-template.xml");
            string levelFolder = Path.Combine(xmlFolder, "level_xml");

            EnsureDirectoryExists(levelFolder);

            XmlUtility xmlUtility = new XmlUtility();
            xmlUtility.Create("Root");
            XmlElement root = xmlUtility.RootElement;

            AddSetsToXml(xmlUtility, root);

            xmlUtility.Save(templatePath);

            ExportHandler.Export(ExportHandler.ExportMode.Buildings, templatePath);

            if (string.IsNullOrEmpty(config.fileName))
            {
                UnityEngine.Debug.LogWarning("[Buildmap] Name is empty. Using 'UnnamedBuildingsSet'.");
                config.fileName = "UnnamedBuildingsSet";
            }

            string destinationXml = Path.Combine(levelFolder, $"{config.fileName}.xml");

            XmlUtility.FormatXML(templatePath, templatePath);
            File.Copy(templatePath, destinationXml, true);

            CompileXML(templatePath);
        }

        // -------------------------
        // Helpers
        // -------------------------
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private void AddSetsToXml(XmlUtility xmlUtility, XmlElement parentElement)
        {
            XmlElement setsElement = xmlUtility.AddElement(parentElement, "Sets");

            foreach (var citySet in config.citySets)
            {
                if (!string.IsNullOrEmpty(citySet))
                {
                    XmlElement cityElement = xmlUtility.AddElement(setsElement, "City");
                    xmlUtility.SetAttribute(cityElement, "FileName", citySet);
                }
            }

            foreach (var groundSet in config.groundSets)
            {
                if (!string.IsNullOrEmpty(groundSet))
                {
                    XmlElement groundElement = xmlUtility.AddElement(setsElement, "Ground");
                    xmlUtility.SetAttribute(groundElement, "FileName", groundSet);
                }
            }

            foreach (var librarySet in config.librarySets)
            {
                if (!string.IsNullOrEmpty(librarySet))
                {
                    XmlElement libraryElement = xmlUtility.AddElement(setsElement, "Library");
                    xmlUtility.SetAttribute(libraryElement, "FileName", librarySet);
                }
            }
        }

        private void CompileXML(string xmlPath)
        {
            // If "Export as XML" is enabled, copy directly to user-specified directory
            if (config.exportAsXML)
            {
                if (string.IsNullOrEmpty(config.filePathDirectory))
                {
                    UnityEngine.Debug.LogWarning("[Buildmap] File Path Directory is empty. Cannot export XML.");
                    return;
                }

                if (string.IsNullOrEmpty(config.fileName))
                {
                    UnityEngine.Debug.LogWarning("[Buildmap] Name is empty. Cannot export XML.");
                    return;
                }

                string destXml = Path.Combine(config.filePathDirectory, $"{config.fileName}.xml");
                try
                {
                    File.Copy(xmlPath, destXml, true);
                    UnityEngine.Debug.Log($"[Buildmap] Exported XML copied to: {destXml}");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError("[Buildmap] Failed to copy XML: " + ex.Message);
                }
                return; // Do not run batch when exporting raw XML
            }

            // Run batch builder
            string batchFile = config.fastBuild ? "compile-fast.bat" : "compile.bat";
            string batchPath = Path.Combine(Application.dataPath, "XML", batchFile);

            if (!File.Exists(batchPath))
            {
                UnityEngine.Debug.LogError("[Buildmap] Batch file not found: " + batchPath);
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Process process = new Process();
            process.StartInfo.FileName = batchPath;
            process.StartInfo.WorkingDirectory = Path.Combine(Application.dataPath, "XML");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            process.WaitForExit();

            stopwatch.Stop();

            string sourceFile = Path.Combine(Application.dataPath, "XML", "level_xml.dz");
            if (File.Exists(sourceFile) && !string.IsNullOrEmpty(config.filePathDirectory))
            {
                string dest = Path.Combine(config.filePathDirectory, "level_xml.dz");
                File.Copy(sourceFile, dest, true);
                UnityEngine.Debug.Log("[Buildmap] Copied to: " + dest);
            }

            UnityEngine.Debug.Log($"[Buildmap] Compilation finished in {stopwatch.ElapsedMilliseconds / 1000f:F2} seconds.");
        }
    }
}
