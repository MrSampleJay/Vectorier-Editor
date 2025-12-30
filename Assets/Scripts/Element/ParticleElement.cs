using System.Globalization;
using System.Xml;
using UnityEngine;
using UnityEngine.UIElements;
using Vectorier.Component;
using Vectorier.Handler;
using Vectorier.XML;

namespace Vectorier.Element
{
    public static class ParticleElement
    {
        public static XmlElement WriteToXML(GameObject gameObject, XmlUtility xmlUtility, XmlElement parentElement)
        {
            if (gameObject == null || xmlUtility == null || parentElement == null)
                return null;

            if (!gameObject.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                return null;

            if (!gameObject.TryGetComponent<ParticleComponent>(out var particleComponent))
                return null;

            // <Particle>
            XmlElement particleElement = xmlUtility.AddElement(parentElement, "Particle");

            // Try to find <Properties> if it already exists; otherwise create it
            XmlElement propertiesElement = xmlUtility.GetOrCreateElement(particleElement, "Properties");
            XmlElement staticElement = xmlUtility.GetOrCreateElement(propertiesElement, "Static");

            // Compute attributes
            Element.WritePosition(xmlUtility, particleElement, gameObject);
            Element.WriteSize(xmlUtility, particleElement, gameObject);

            // Write base attributes
            xmlUtility.SetAttribute(particleElement, "Frame", particleComponent.Frame.ToString(CultureInfo.InvariantCulture));
            xmlUtility.SetAttribute(particleElement, "Type", ((int)particleComponent.Type).ToString(CultureInfo.InvariantCulture));
            Element.WriteClassName(gameObject, xmlUtility, particleElement);

            // Selection Component
            Element.WriteSelectionComponent(xmlUtility, staticElement, gameObject);

            return particleElement;
        }

        public static GameObject WriteToScene(XmlElement element, Transform parent, string factor)
        {
            if (element == null)
                return null;

            // Properties
            XmlElement propertiesElement = element.SelectSingleNode("Properties") as XmlElement;
            XmlElement staticElement = propertiesElement?.SelectSingleNode("Static") as XmlElement;

            // Create object
            GameObject particleObject = Element.CreateObject("Particle", parent, element);

            // Sprite
            SpriteRenderer renderer = particleObject.AddComponent<SpriteRenderer>();

            if (!ImportHandler.SpriteCacheBuilt)
                ImportHandler.BuildSpriteCache();

            string className = element.GetAttribute("ClassName");
            ImportHandler.SpriteCache.TryGetValue(className, out Sprite loadedSprite);

            // Size
            Element.GetSizeXML(element, out float xmlWidth, out float xmlHeight);

            // Assign sprite + scale
            if (loadedSprite != null)
            {
                renderer.sprite = loadedSprite;

                Texture2D texture = loadedSprite.texture;
                if (texture != null)
                {
                    float scaleX = xmlWidth / texture.width;
                    float scaleY = xmlHeight / texture.height;
                    particleObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                }
            }
            else
            {
                Debug.LogWarning($"Sprite '{className}' not found in any texture folder.");
            }

            // Apply
            Element.ApplyPosition(particleObject, element);
            Element.ApplyLayer(particleObject, factor);
            Element.ApplySelectionComponent(staticElement, particleObject);

            // Component
            ParticleComponent particleComponent = particleObject.AddComponent<ParticleComponent>();

            if (element.HasAttribute("Frame"))
            {
                particleComponent.Frame = Element.ParseInt(element.GetAttribute("Frame"));
            }

            if (element.HasAttribute("Type"))
            {
                int typeValue = Element.ParseInt(element.GetAttribute("Type"));

                if (System.Enum.IsDefined(typeof(ParticleComponent.ParticleType), typeValue))
                    particleComponent.Type = (ParticleComponent.ParticleType)typeValue;
                else
                    particleComponent.Type = ParticleComponent.ParticleType.Type1;
            }

            // Tag
            particleObject.tag = "Particle";
            renderer.sortingOrder = 5;
            renderer.sortingLayerName = "OnTop";

            return particleObject;

        }
    }
}