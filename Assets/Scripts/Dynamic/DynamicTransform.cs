using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Vectorier.XML;

namespace Vectorier.Dynamic
{
    [AddComponentMenu("Vectorier/Dynamic/Transformation")]
    public class DynamicTransform : MonoBehaviour
    {
        public string transformationName;

        public List<MoveData> moves = new();
        public List<SizeData> sizes = new();
        public List<RotateData> rotations = new();
        public List<ColorData> colors = new();

        [Serializable]
        public class MoveData
        {
            public enum Easing
            {
                EaseOut,
                EaseIn,
                Linear
            }

            public float duration;
            public float delay;

            public bool useEasing = true;
            public Easing easeType;

            public Vector2 finish = new();
            public Vector2 support = new();

            // Editor Only!
            public bool isLocked;
            public bool hide;

            // Note: Use this function upon import to fix bugs from the editor if it ever happens
            public void UpdatePoints()
            {
                Easing value = easeType;
                if (!useEasing)
                    return;
                switch (value)
                {
                    case Easing.Linear:
                        support = finish / 2;
                        break;
                    case Easing.EaseIn:
                        support = Vector2.zero;
                        break;
                    case Easing.EaseOut:
                        support = finish;
                        break;
                }

            }
        }

        [Serializable]
        public class SizeData
        {
            public int duration;
            public float finalWidth;
            public float finalHeight;
        }

        [Serializable]
        public class RotateData
        {
            public float angle;
            public Vector2 anchor;
            public int duration;
        }

        [Serializable]
        public class ColorData
        {
            public Color colorStart = Color.white;
            public Color colorFinish = Color.white;
            public int duration;
        }

        private void OnDrawGizmos()
        {
            if (moves == null || moves == null || moves.Count == 0)
                return;

            // Collect sprites (children, grandchildren, etc.)
            List<SpriteRenderer> sprites = GetSpriteRenderers();

            Vector3 accumulatedPosition = transform.position;
            Gizmos.color = Color.yellow;

            foreach (var interval in moves)
            {
                Vector3 start = accumulatedPosition;
                Vector3 mid = accumulatedPosition + (Vector3)interval.support;
                Vector3 end = accumulatedPosition + (Vector3)interval.finish;

                DrawCubicBezier(start, mid, end);

                DrawSpritesAtOffset(sprites, end);

                DrawIntervalLabels(end, moves.IndexOf(interval));

                accumulatedPosition += (Vector3)interval.finish;
            }
        }

        public List<SpriteRenderer> GetSpriteRenderers()
        {
            // Tags that imply hierarchy
            if (CompareTag("Object"))
            {
                // Includes children, grandchildren, etc.
                return GetComponentsInChildren<SpriteRenderer>(true).ToList();
            }

            // Otherwise just this object
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            return sr != null ? new List<SpriteRenderer> { sr } : new List<SpriteRenderer>();
        }

        private void DrawCubicBezier(Vector3 start, Vector3 mid, Vector3 end)
        {
            // Convert quadratic-style tangents into cubic control points
            Vector3 startTangent = 2f * (mid - start);
            Vector3 endTangent = 2f * (end - mid);

            Vector3 control1 = start + startTangent / 3f;
            Vector3 control2 = end - endTangent / 3f;

            const int resolution = 15;
            Vector3 previousPoint = start;

            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;

                Vector3 point =
                    Mathf.Pow(1 - t, 3) * start +
                    3 * Mathf.Pow(1 - t, 2) * t * control1 +
                    3 * (1 - t) * t * t * control2 +
                    Mathf.Pow(t, 3) * end;

                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
        private void DrawIntervalLabels(Vector3 position, int index)
        {
            position.y += 20;
            Handles.Label(position, $"{transform.name} (Move Interval {index + 1})");
        }
        private void DrawSpritesAtOffset(List<SpriteRenderer> sprites, Vector3 offset)
        {
            foreach (var sprite in sprites)
            {
                if (sprite == null || sprite.sprite == null)
                    continue;

                Texture2D texture = sprite.sprite.texture;

                Vector3 worldPos =
                    sprite.transform.position +
                    offset -
                    transform.position;

                Vector3 size = sprite.bounds.size;

                int flipX = sprite.flipX ? -1 : 1;
                int flipY = sprite.flipY ? -1 : 1;

                Rect rect = new Rect(worldPos.x, worldPos.y, size.x * flipX, -size.y * flipY);
                Gizmos.DrawGUITexture(rect, texture);
            }
        }

        // ================= XML Writer ================= //

        public XmlElement WriteToXML(XmlUtility xmlUtility, XmlElement parentElement)
        {
            if (xmlUtility == null || parentElement == null)
                return null;

            XmlElement dynamicElement = xmlUtility.GetOrCreateElement(parentElement, "Dynamic");
            XmlElement transformElement = xmlUtility.AddElement(dynamicElement, "Transformation");
            xmlUtility.SetAttribute(transformElement, "Name", transformationName);

            // -------- MOVE --------
            XmlElement moveElement = xmlUtility.AddElement(transformElement, "Move");

            for (int i = 0; i < moves.Count; i++)
            {
                var interval = moves[i];

                int frames = Mathf.RoundToInt(interval.duration * 60f);

                XmlElement intervalElem = xmlUtility.AddElement(moveElement, "MoveInterval");
                xmlUtility.SetAttribute(intervalElem, "Number", i + 1);
                xmlUtility.SetAttribute(intervalElem, "FramesToMove", frames);
                xmlUtility.SetAttribute(intervalElem, "Delay", interval.delay.ToString("F1", CultureInfo.InvariantCulture));

                // Start (always 0,0)
                XmlElement startElem = xmlUtility.AddElement(intervalElem, "Point");
                xmlUtility.SetAttribute(startElem, "Name", "Start");
                xmlUtility.SetAttribute(startElem, "X", "0");
                xmlUtility.SetAttribute(startElem, "Y", "0");

                // Support
                XmlElement supportElem = xmlUtility.AddElement(intervalElem, "Point");
                xmlUtility.SetAttribute(supportElem, "Name", "Support");
                xmlUtility.SetAttribute(supportElem, "Number", 1);
                xmlUtility.SetAttribute(supportElem, "X", interval.support.x.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(supportElem, "Y", interval.support.y.ToString(CultureInfo.InvariantCulture));

                // Finish
                XmlElement finishElem = xmlUtility.AddElement(intervalElem, "Point");
                xmlUtility.SetAttribute(finishElem, "Name", "Finish");
                xmlUtility.SetAttribute(finishElem, "X", interval.finish.x.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(finishElem, "Y", interval.finish.y.ToString(CultureInfo.InvariantCulture));
            }

            // -------- SIZE --------
            foreach (var size in sizes)
            {
                XmlElement sizeElem = xmlUtility.AddElement(transformElement, "Size");
                xmlUtility.SetAttribute(sizeElem, "Frames", size.duration * 60f);
                xmlUtility.SetAttribute(sizeElem, "FinalWidth", size.finalWidth.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(sizeElem, "FinalHeight", size.finalHeight.ToString(CultureInfo.InvariantCulture));
            }

            // -------- ROTATION --------
            foreach (var rotation in rotations)
            {
                XmlElement rotElem = xmlUtility.AddElement(transformElement, "Rotation");
                xmlUtility.SetAttribute(rotElem, "Angle", rotation.angle.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(rotElem, "Anchor", $"{rotation.anchor.x.ToString(CultureInfo.InvariantCulture)}|{rotation.anchor.y.ToString(CultureInfo.InvariantCulture)}");
                xmlUtility.SetAttribute(rotElem, "Frames", rotation.duration * 60f);
            }

            // -------- COLOR --------
            foreach (var color in colors)
            {
                XmlElement colorElem = xmlUtility.AddElement(transformElement, "Color");
                xmlUtility.SetAttribute(colorElem, "ColorStart", "#" + ColorUtility.ToHtmlStringRGBA(color.colorStart));
                xmlUtility.SetAttribute(colorElem, "ColorFinish", "#" + ColorUtility.ToHtmlStringRGBA(color.colorFinish));
                xmlUtility.SetAttribute(colorElem, "Frames", color.duration * 60f);
            }

            return dynamicElement;
        }

        public static DynamicTransform WriteToScene(XmlElement transformationElement, GameObject gameObject)
        {
            if (transformationElement == null || gameObject == null)
                return null;

            DynamicTransform dynamic = gameObject.AddComponent<DynamicTransform>();

            dynamic.transformationName = transformationElement.GetAttribute("Name");
            dynamic.moves.Clear();
            dynamic.sizes.Clear();
            dynamic.rotations.Clear();
            dynamic.colors.Clear();

            // -------- MOVE --------
            XmlElement moveElement = transformationElement["Move"];

            if (moveElement != null)
            {
                foreach (XmlElement intervalElement in moveElement.GetElementsByTagName("MoveInterval"))
                {
                    MoveData move = new MoveData();

                    int frames = int.Parse(intervalElement.GetAttribute("FramesToMove"));
                    move.duration = frames / 60f;

                    move.delay = float.Parse(intervalElement.GetAttribute("Delay"), CultureInfo.InvariantCulture);

                    foreach (XmlElement point in intervalElement.GetElementsByTagName("Point"))
                    {
                        string name = point.GetAttribute("Name");

                        float x = float.Parse(point.GetAttribute("X"), CultureInfo.InvariantCulture);
                        float y = float.Parse(point.GetAttribute("Y"), CultureInfo.InvariantCulture);

                        if (name == "Support")
                            move.support = new Vector2(x, y);
                        else if (name == "Finish")
                            move.finish = new Vector2(x, y);
                    }

                    dynamic.moves.Add(move);
                }
            }

            // -------- SIZE --------
            foreach (XmlElement sizeElement in transformationElement.GetElementsByTagName("Size"))
            {
                SizeData size = new SizeData();

                size.duration = Mathf.RoundToInt(float.Parse(sizeElement.GetAttribute("Frames")) / 60f);
                size.finalWidth = float.Parse(sizeElement.GetAttribute("FinalWidth"), CultureInfo.InvariantCulture);
                size.finalHeight = float.Parse(sizeElement.GetAttribute("FinalHeight"), CultureInfo.InvariantCulture);

                dynamic.sizes.Add(size);
            }

            // -------- ROTATION --------
            foreach (XmlElement rotationElement in transformationElement.GetElementsByTagName("Rotation"))
            {
                RotateData rotation = new RotateData();

                rotation.angle = float.Parse(rotationElement.GetAttribute("Angle"), CultureInfo.InvariantCulture);
                string[] anchor = rotationElement.GetAttribute("Anchor").Split('|');
                rotation.anchor = new Vector2(float.Parse(anchor[0], CultureInfo.InvariantCulture), float.Parse(anchor[1], CultureInfo.InvariantCulture));
                rotation.duration = Mathf.RoundToInt(float.Parse(rotationElement.GetAttribute("Frames")) / 60f);

                dynamic.rotations.Add(rotation);
            }

            // -------- COLOR --------
            foreach (XmlElement colorElement in transformationElement.GetElementsByTagName("Color"))
            {
                ColorData color = new ColorData();

                ColorUtility.TryParseHtmlString(colorElement.GetAttribute("ColorStart"), out color.colorStart);
                ColorUtility.TryParseHtmlString(colorElement.GetAttribute("ColorFinish"), out color.colorFinish);

                color.duration = Mathf.RoundToInt(float.Parse(colorElement.GetAttribute("Frames")) / 60f);

                dynamic.colors.Add(color);
            }

            return dynamic;
        }
    }
}
