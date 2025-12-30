using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using UnityEngine;
using Vectorier.XML;
using Vectorier.Element;
using UnityEngine.SceneManagement;

namespace Vectorier.Handler
{
    public static class ExportHandler
    {
        public enum ExportMode
        {
            Level,
            Objects,
            Buildings
        }

        public static void Export(ExportMode exportMode, string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("[ExportHandler] Output path is empty.");
                return;
            }

            XmlUtility xmlUtility = new XmlUtility();

            if (File.Exists(outputPath)) xmlUtility.Load(outputPath);
            else xmlUtility.Create("Root");

            XmlElement rootElement = xmlUtility.RootElement;

            switch (exportMode)
            {
                case ExportMode.Level:
                    WriteLevel(xmlUtility, rootElement, exportMode);
                    break;

                case ExportMode.Objects:
                    WriteObjects(xmlUtility, rootElement, exportMode);
                    break;

                case ExportMode.Buildings:
                    WriteBuildings(xmlUtility, rootElement, exportMode);
                    break;
            }

            xmlUtility.RemoveEmptyElements(rootElement);
            xmlUtility.Save(outputPath);

            Debug.Log($"[ExportHandler] Export completed: {outputPath}");
        }

        public static void ExportToExisting(ExportMode mode, string path)
        {
            XmlUtility xml = new XmlUtility();
            xml.Load(path);

            XmlElement root = xml.RootElement;
            XmlElement objectsRoot = root["Objects"];

            if (objectsRoot == null)
                objectsRoot = xml.AddElement(root, "Objects");

            // Index existing objects by Name
            Dictionary<string, XmlElement> existing = objectsRoot.ChildNodes
                    .OfType<XmlElement>()
                    .Where(e => e.Name == "Object" && e.HasAttribute("Name"))
                    .ToDictionary(e => e.GetAttribute("Name"));

            foreach (GameObject gameObject in GetExportableObjects())
            {
                string name = gameObject.name;

                if (existing.TryGetValue(name, out XmlElement target))
                    ReplaceContent(gameObject, xml, target, mode);
                else
                    ObjectElement.WriteToXML(gameObject, xml, objectsRoot, mode);
            }

            xml.RemoveEmptyElements(root);
            xml.Save(path);

            XmlUtility.FormatXML(path, path);

            Debug.Log("[ExportHandler] Existing XML updated.");
        }

        // ================= HIERARCHY ORDER ================= //
        private static List<GameObject> GetObjectsInHierarchyOrder()
        {
            List<GameObject> ordered = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();

            foreach (var root in scene.GetRootGameObjects())
                Traverse(root, ordered);

            return ordered;
        }

        private static void Traverse(GameObject obj, List<GameObject> list)
        {
            if (IsExportable(obj))
                list.Add(obj);

            for (int i = 0; i < obj.transform.childCount; i++)
                Traverse(obj.transform.GetChild(i).gameObject, list);
        }

        // ================= LEVEL EXPORT ================= //
        private static void WriteLevel(XmlUtility xmlUtility, XmlElement rootElement, ExportMode exportMode)
        {
            XmlElement trackElement = xmlUtility.AddElement(rootElement, "Track");

            List<GameObject> allObjects = GetObjectsInHierarchyOrder();
            Dictionary<string, List<GameObject>> groupedObjects = new Dictionary<string, List<GameObject>>();

            foreach (GameObject gameObject in allObjects)
            {
                if (!IsExportable(gameObject))
                    continue;

                string layerFactor = GetLayerFactor(gameObject);
                if (!groupedObjects.ContainsKey(layerFactor))
                    groupedObjects[layerFactor] = new List<GameObject>();

                groupedObjects[layerFactor].Add(gameObject);
            }

            foreach (var layer in groupedObjects.OrderBy(l => float.Parse(l.Key, System.Globalization.CultureInfo.InvariantCulture)))
            {
                XmlElement objectLayerElement = xmlUtility.AddElement(trackElement, "Object");
                xmlUtility.SetAttribute(objectLayerElement, "Factor", layer.Key);
                XmlElement contentElement = xmlUtility.AddElement(objectLayerElement, "Content");

                var objectsInLayer = layer.Value;

                foreach (var current in objectsInLayer.Where(o => o.tag == "Object" && !IsChildOfObject(o)))
                    ObjectElement.WriteToXML(current, xmlUtility, contentElement, exportMode);

                foreach (var current in objectsInLayer.Where(o => o.tag == "Image" && !IsChildOfObject(o)).OrderBy(o => SortingOrder(o)))
                    WriteByTag(current, xmlUtility, contentElement);

                foreach (var current in objectsInLayer.Where(o => o.tag != "Object" && o.tag != "Image" && !IsChildOfObject(o)))
                    WriteByTag(current, xmlUtility, contentElement);
            }
        }

        // ================= OBJECT EXPORT ================= //
        private static void WriteObjects(XmlUtility xmlUtility, XmlElement rootElement, ExportMode exportMode)
        {
            XmlElement objectsRootElement = xmlUtility.AddElement(rootElement, "Objects");

            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject gameObject in allObjects)
            {
                if (!IsExportable(gameObject))
                    continue;

                if (gameObject.CompareTag("Object") && !IsChildOfObject(gameObject))
                    ObjectElement.WriteToXML(gameObject, xmlUtility, objectsRootElement, exportMode);
            }
        }

        // ================= BUILDINGS EXPORT ================= //
        private static void WriteBuildings(XmlUtility xmlUtility, XmlElement rootElement, ExportMode exportMode)
        {
            XmlElement objectsRootElement = xmlUtility.AddElement(rootElement, "Objects");

            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject gameObject in allObjects)
            {
                if (!IsExportable(gameObject))
                    continue;

                if (gameObject.CompareTag("Object") && !IsChildOfObject(gameObject))
                    ObjectElement.WriteToXML(gameObject, xmlUtility, objectsRootElement, exportMode);
            }
        }

        // ================= TAG WRITER ================= //
        public static void WriteByTag(GameObject gameObject, XmlUtility xmlUtility, XmlElement parentElement)
        {
            switch (gameObject.tag)
            {
                case "Image": ImageElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Trigger": TriggerElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Area": AreaElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Platform": PlatformElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Trapezoid": TrapezoidElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Camera": CameraElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Spawn": SpawnElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Item": ItemElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Model": ModelElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Particle": ParticleElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
                case "Animation": AnimationElement.WriteToXML(gameObject, xmlUtility, parentElement); break;
            }
        }

        // ================= HELPERS ================= //
        private static bool IsExportable(GameObject gameObject)
        {
            if (!gameObject.activeInHierarchy)
                return false;

            if (gameObject.tag == null || gameObject.tag == "Untagged")
                return false;

            return true;
        }

        private static bool IsChildOfObject(GameObject gameObject)
        {
            Transform parentTransform = gameObject.transform.parent;
            while (parentTransform != null)
            {
                if (parentTransform.CompareTag("Object"))
                    return true;

                parentTransform = parentTransform.parent;
            }

            return false;
        }

        private static int SortingOrder(GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<SpriteRenderer>();
            return renderer ? renderer.sortingOrder : 0;
        }

        private static string GetLayerFactor(GameObject gameObject)
        {
            string layerName = LayerMask.LayerToName(gameObject.layer);
            if (!float.TryParse(layerName, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float factorValue))
                factorValue = 1f;

            return factorValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ReplaceContent(GameObject source, XmlUtility xml, XmlElement objectElement, ExportMode mode)
        {
            XmlElement oldContent = objectElement["Content"];

            if (oldContent != null)
                objectElement.RemoveChild(oldContent);

            XmlElement newContent = xml.AddElement(objectElement, "Content");

            foreach (Transform child in source.transform)
                ExportHandler.WriteByTag(child.gameObject, xml, newContent);
        }

        private static IEnumerable<GameObject> GetExportableObjects()
        {
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject gameObject in allObjects)
            {
                if (!IsExportable(gameObject))
                    continue;

                if (!gameObject.CompareTag("Object"))
                    continue;

                if (IsChildOfObject(gameObject))
                    continue;

                yield return gameObject;
            }
        }
    }
}
