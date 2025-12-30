using System;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Vectorier.Component;
using Vectorier.XML;

namespace Vectorier.Element
{
    public static class AreaElement
    {
        public static XmlElement WriteToXML(GameObject gameObject, XmlUtility xmlUtility, XmlElement parentElement)
        {
            if (gameObject == null || xmlUtility == null || parentElement == null)
                return null;

            if (!gameObject.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                return null;

            // <Area>
            XmlElement areaElement = xmlUtility.AddElement(parentElement, "Area");

            // Try to find <Properties> if it already exists; otherwise create it
            XmlElement propertiesElement = xmlUtility.GetOrCreateElement(areaElement, "Properties");
            XmlElement staticElement = xmlUtility.GetOrCreateElement(propertiesElement, "Static");

            // Write
            Element.WriteName(gameObject, xmlUtility, areaElement);
            Element.WritePosition(xmlUtility, areaElement, gameObject);
            Element.WriteSize(xmlUtility, areaElement, gameObject);

            // AreaComponent
            gameObject.TryGetComponent<AreaComponent>(out var areaComponent);

            // Default type = Animation (if no component)
            AreaComponent.AreaType type = AreaComponent.AreaType.Animation;
            if (areaComponent != null)
                type = areaComponent.Type;

            xmlUtility.SetAttribute(areaElement, "Type", type.ToString());

            // Write type-specific attributes
            if (areaComponent != null)
            {
                switch (type)
                {
                    case AreaComponent.AreaType.Catch:
                        xmlUtility.SetAttribute(areaElement, "Distance",
                            areaComponent.Distance.ToString(CultureInfo.InvariantCulture));
                        break;

                    case AreaComponent.AreaType.Trick:
                        xmlUtility.SetAttribute(areaElement, "ItemName", areaComponent.ItemName);
                        xmlUtility.SetAttribute(areaElement, "Score",
                            areaComponent.Score.ToString(CultureInfo.InvariantCulture));
                        break;

                    case AreaComponent.AreaType.Help:
                        xmlUtility.SetAttribute(areaElement, "Key", areaComponent.Key);
                        xmlUtility.SetAttribute(areaElement, "Description", areaComponent.Description ?? string.Empty);
                        break;
                }
            }

            // Selection
            Element.WriteSelectionComponent(xmlUtility, staticElement, gameObject);

            return areaElement;
        }

        public static GameObject WriteToScene(XmlElement element, Transform parent, string factor)
        {
            if (element == null)
                return null;

            // Properties
            XmlElement propertiesElement = element.SelectSingleNode("Properties") as XmlElement;
            XmlElement staticElement = propertiesElement?.SelectSingleNode("Static") as XmlElement;

            // Create object
            GameObject areaObject = Element.CreateObject("Area", parent, element);

            // Sprite
            SpriteRenderer spriteRenderer = areaObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("Images/Editor/Trigger/trigger");
            spriteRenderer.color = new Color(1f, 0f, 0f, 1f);

            // Apply
            Element.ApplyPosition(areaObject, element);
            Element.ApplySize(areaObject, spriteRenderer.sprite, element);
            Element.ApplyLayer(areaObject, factor);
            Element.ApplySelectionComponent(staticElement, areaObject);
            Element.ApplyDynamic(propertiesElement, areaObject);

            // Read Type
            string typeStr = element.GetAttribute("Type");

            if (!Enum.TryParse(typeStr, out AreaComponent.AreaType typeEnum))
                typeEnum = AreaComponent.AreaType.Animation;

            // Only add AreaComponent for non-Animation types
            AreaComponent areaComponent;

            if (typeEnum != AreaComponent.AreaType.Animation)
            {
                areaComponent = areaObject.AddComponent<AreaComponent>();
                areaComponent.Type = typeEnum;

                switch (typeEnum)
                {
                    case AreaComponent.AreaType.Catch:
                        int.TryParse(element.GetAttribute("Distance"), out areaComponent.Distance);
                        break;

                    case AreaComponent.AreaType.Trick:
                        areaComponent.ItemName = element.GetAttribute("ItemName");
                        int.TryParse(element.GetAttribute("Score"), out areaComponent.Score);
                        break;

                    case AreaComponent.AreaType.Help:
                        areaComponent.Key = element.GetAttribute("Key");
                        areaComponent.Description = element.GetAttribute("Description");
                        break;
                }
            }

            // Tag
            areaObject.tag = "Area";
            spriteRenderer.sortingLayerName = "OnTop";
            spriteRenderer.sortingOrder = 1;

            return areaObject;
        }
    }
}