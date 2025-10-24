using System.Globalization;
using System.Xml;
using UnityEngine;
using Vectorier.XML;
using Vectorier.Handler;
using System.Collections.Generic;

namespace Vectorier.Element
{
    public static class ObjectElement
    {
        public static XmlElement WriteToXML(GameObject gameObject, XmlUtility xmlUtility, XmlElement parentElement, ExportHandler.ExportMode exportMode)
        {
            if (gameObject == null || xmlUtility == null || parentElement == null)
                return null;

            XmlElement objectElement = xmlUtility.AddElement(parentElement, "Object");
            string cleanName = RemoveUnityCloneSuffix(gameObject.name);
            if (!string.IsNullOrEmpty(cleanName))
                xmlUtility.SetAttribute(objectElement, "Name", cleanName);

            bool hasChildren = gameObject.transform.childCount > 0;

            Vector3 localPos = gameObject.transform.localPosition;

            switch (exportMode)
            {
                case ExportHandler.ExportMode.Level:
                    xmlUtility.SetAttribute(objectElement, "X", (localPos.x * 100f).ToString(CultureInfo.InvariantCulture));
                    xmlUtility.SetAttribute(objectElement, "Y", (localPos.y * -100f).ToString(CultureInfo.InvariantCulture));

                    if (hasChildren)
                    {
                        XmlElement contentElement = xmlUtility.AddElement(objectElement, "Content");
                        WriteChildren(gameObject, xmlUtility, contentElement, exportMode);
                    }
                    break;

                case ExportHandler.ExportMode.Objects:
                    XmlElement objectContent = xmlUtility.AddElement(objectElement, "Content");
                    WriteChildren(gameObject, xmlUtility, objectContent, exportMode);
                    break;

                case ExportHandler.ExportMode.Buildings:
                    AddInOutAttributes(gameObject, xmlUtility, objectElement);
                    AddBoundingBoxAttributes(gameObject, xmlUtility, objectElement);

                    XmlElement buildingContent = xmlUtility.AddElement(objectElement, "Content");
                    WriteChildren(gameObject, xmlUtility, buildingContent, exportMode);
                    break;
            }

            return objectElement;
        }

        private static void WriteChildren(GameObject parentObject, XmlUtility xmlUtility, XmlElement contentElement, ExportHandler.ExportMode exportMode)
        {
            int childCount = parentObject.transform.childCount;

            // Separate image and non-image children
            List<GameObject> imageChildren = new List<GameObject>();
            List<GameObject> otherChildren = new List<GameObject>();

            for (int i = 0; i < childCount; i++)
            {
                GameObject child = parentObject.transform.GetChild(i).gameObject;

                if (!child.activeInHierarchy)
                    continue;

                if (string.IsNullOrEmpty(child.tag) || child.CompareTag("Untagged"))
                    continue;

                if (child.CompareTag("Image"))
                    imageChildren.Add(child);
                else
                    otherChildren.Add(child);
            }

            // Sort image children by sortingOrder (ascending)
            imageChildren.Sort((a, b) =>
            {
                SpriteRenderer sa = a.GetComponent<SpriteRenderer>();
                SpriteRenderer sb = b.GetComponent<SpriteRenderer>();
                int orderA = sa ? sa.sortingOrder : 0;
                int orderB = sb ? sb.sortingOrder : 0;
                return orderA.CompareTo(orderB);
            });

            // Write image children first (sorted)
            foreach (GameObject imageChild in imageChildren)
            {
                ExportHandler.WriteByTag(imageChild, xmlUtility, contentElement);
            }

            foreach (GameObject child in otherChildren)
            {
                if (child.CompareTag("Object"))
                    WriteToXML(child, xmlUtility, contentElement, exportMode);
                else
                    ExportHandler.WriteByTag(child, xmlUtility, contentElement);
            }
        }

        private static void AddInOutAttributes(GameObject gameObject, XmlUtility xmlUtility, XmlElement element)
        {
            Transform inObj = gameObject.transform.Find("In");
            Transform outObj = gameObject.transform.Find("Out");

            float inX = inObj ? inObj.position.x * 100f : 0f;
            float inY = inObj ? inObj.position.y * -100f : 0f;
            float outX = outObj ? outObj.position.x * 100f : 0f;
            float outY = outObj ? outObj.position.y * -100f : 0f;

            xmlUtility.SetAttribute(element, "InX", inX.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(element, "InY", inY.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(element, "OutX", outX.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(element, "OutY", outY.ToString(CultureInfo.InvariantCulture));
        }

        private static void AddBoundingBoxAttributes(GameObject parentObject, XmlUtility xmlUtility, XmlElement element)
        {
            if (parentObject.transform.childCount == 0) return;

            Bounds? combinedBounds = null;
            foreach (Transform child in parentObject.transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer == null) continue;
                if (combinedBounds == null) combinedBounds = renderer.bounds;
                else combinedBounds.Value.Encapsulate(renderer.bounds);
            }

            if (combinedBounds == null) return;

            Bounds bounds = combinedBounds.Value;
            Vector3 topLeft = new Vector3(bounds.min.x, bounds.max.y);
            Vector3 topRight = new Vector3(bounds.max.x, bounds.max.y);
            Vector3 bottomLeft = new Vector3(bounds.min.x, bounds.min.y);

            float boxX = topLeft.x * 100f;
            float boxY = topLeft.y * -100f;
            float width = (topRight.x - topLeft.x) * 100f;
            float height = (topLeft.y - bottomLeft.y) * 100f;

            xmlUtility.SetAttribute(element, "BoxX", boxX.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(element, "BoxY", boxY.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(element, "BoxWidth", width.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(element, "BoxHeight", height.ToString(CultureInfo.InvariantCulture));
        }

        private static string RemoveUnityCloneSuffix(string originalName)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(originalName, @"\s*\(\d+\)$", "");
        }
    }
}
