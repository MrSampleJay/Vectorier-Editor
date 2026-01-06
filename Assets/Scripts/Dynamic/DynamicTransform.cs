using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Xml;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using NUnit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;
using Vectorier.Core;
using Vectorier.XML;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Vectorier.Dynamic.DynamicTransform;

namespace Vectorier.Dynamic
{
    public enum TransformType
    {
        Move,
        Rotate,
        Scale
    }

    [System.Serializable]
    public class MoveData
    {
        public string name;
        public int loop;
        public List<MoveInterval> intervals = new();
        public void UnlockIntervals()
        {
            foreach (var interval in intervals)
                if (interval.isLocked)
                    interval.isLocked = false;
        }
        public void Reset()
        {
            intervals.Clear();
        }

        public void CollapseAll()
        {
            foreach (var interval in intervals)
                if (!interval.hide)
                    interval.hide = true;
        }
    }

    [System.Serializable]
    public class MoveInterval
    {
        public enum Easing
        {
            EaseOut,
            EaseIn,
            Linear
        }

        public int framesToMove;
        public float delay;

        public bool useEasing = true;
        public Easing easeType;

        // The holy points, albeit we dont need start :P
        public Vector2 finish = new();
        public Vector2 start = new();
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
        private void RoundPoints()
        {
            Mathf.Round(finish.x);
            Mathf.Round(finish.y);
            Mathf.Round(support.x);
            Mathf.Round(support.y);
        }
    }

    [Serializable]
    public class SizeData
    {
        public int frames;
        public float finalWidth;
        public float finalHeight;
    }

    [Serializable]
    public class RotateData
    {
        public float angle;
        public Vector2 anchor;
        public int frames;
    }

    [Serializable]
    public class ColorData
    {
        public Color colorStart = Color.white;
        public Color colorFinish = Color.white;
        public int frames;
    }

    [AddComponentMenu("Vectorier/Dynamic/Transformation")]
    [HelpURL("https://example.com/docs")]
    public class DynamicTransform : MonoBehaviour
    {
        [SerializeField]
        public TransformType transformType = TransformType.Move;

        public MoveData move = new MoveData();

        public RotateData rotate = new RotateData();

        public SizeData size = new SizeData();
        
        public ColorData color = new ColorData();

        private void OnDrawGizmos()
        {
            if (move == null || move.intervals == null || move.intervals.Count == 0)
                return;

            // Collect sprites (children, grandchildren, etc.)
            List<SpriteRenderer> sprites = GetSpriteRenderers();

            Vector3 accumulatedPosition = transform.position;
            Gizmos.color = Color.yellow;

            foreach (var interval in move.intervals)
            {
                Vector3 start = accumulatedPosition + (Vector3)interval.start;
                Vector3 mid = accumulatedPosition + (Vector3)interval.support;
                Vector3 end = accumulatedPosition + (Vector3)interval.finish;

                DrawCubicBezier(start, mid, end);

                DrawSpritesAtOffset(sprites, end);

                DrawIntervalLabels(end, move.intervals.IndexOf(interval));

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
        public static bool TryGetSpriteBounds(List<SpriteRenderer> sprites, out Bounds bounds)
        {
            if (sprites.Count == 0)
            {
                bounds = default;
                return false;
            }

            bounds = sprites[0].bounds;

            for (int i = 1; i < sprites.Count; i++)
                bounds.Encapsulate(sprites[i].bounds);

            return true;
        }

        // -------------------------------------------------------------------
        // XML Writer
        // -------------------------------------------------------------------
        public XmlElement WriteToXML(XmlUtility xmlUtility, XmlElement parentElement)
        {
            if (xmlUtility == null || parentElement == null)
                return null;

            XmlElement dynamicElement = xmlUtility.GetOrCreateElement(parentElement, "Dynamic");

            MoveData t = move;

            XmlElement transformElem = xmlUtility.AddElement(dynamicElement, "Transformation");
            xmlUtility.SetAttribute(transformElem, "Name", t.name);

            XmlElement moveElem = xmlUtility.AddElement(transformElem, "Move");

            for (int i = 0; i < move.intervals.Count; i++)
            {
                var interval = move.intervals[i];
                XmlElement intervalElem = xmlUtility.AddElement(moveElem, "MoveInterval");
                xmlUtility.SetAttribute(intervalElem, "Number", i + 1);
                xmlUtility.SetAttribute(intervalElem, "FramesToMove", interval.framesToMove);
                xmlUtility.SetAttribute(intervalElem, "Delay", interval.delay.ToString("F1", CultureInfo.InvariantCulture));

                // Start
                XmlElement startElem = xmlUtility.AddElement(intervalElem, "Point");
                xmlUtility.SetAttribute(startElem, "Name", "Start");
                xmlUtility.SetAttribute(startElem, "X", 0.0f.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(startElem, "Y", 0.0f.ToString(CultureInfo.InvariantCulture));

                // Finish
                XmlElement supportElem = xmlUtility.AddElement(intervalElem, "Point");
                xmlUtility.SetAttribute(supportElem, "Name", "Finish");
                xmlUtility.SetAttribute(supportElem, "Number", 1);
                xmlUtility.SetAttribute(supportElem, "X", interval.support.x.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(supportElem, "Y", interval.support.y.ToString(CultureInfo.InvariantCulture));

                // Finish
                XmlElement finishElem = xmlUtility.AddElement(intervalElem, "Point");
                xmlUtility.SetAttribute(finishElem, "Name", "Finish");
                xmlUtility.SetAttribute(finishElem, "X", interval.finish.x.ToString(CultureInfo.InvariantCulture));
                xmlUtility.SetAttribute(finishElem, "Y", interval.finish.y.ToString(CultureInfo.InvariantCulture));
            }

            //// --- Size ---
            //foreach (var s in t.sizes)
            //{
            //    XmlElement sizeElem = xmlUtility.AddElement(transformElem, "Size");
            //    xmlUtility.SetAttribute(sizeElem, "Frames", s.frames);
            //    xmlUtility.SetAttribute(sizeElem, "FinalWidth", s.finalWidth.ToString(CultureInfo.InvariantCulture));
            //    xmlUtility.SetAttribute(sizeElem, "FinalHeight", s.finalHeight.ToString(CultureInfo.InvariantCulture));
            //}

            //// --- Rotation ---
            //foreach (var r in t.rotations)
            //{
            //    XmlElement rotElem = xmlUtility.AddElement(transformElem, "Rotation");
            //    xmlUtility.SetAttribute(rotElem, "Angle", r.angle.ToString(CultureInfo.InvariantCulture));
            //    xmlUtility.SetAttribute(rotElem, "Anchor", $"{r.anchor.x.ToString(CultureInfo.InvariantCulture)}|{r.anchor.y.ToString(CultureInfo.InvariantCulture)}");
            //    xmlUtility.SetAttribute(rotElem, "Frames", r.frames);
            //}

            //// --- Color ---
            //foreach (var c in t.colors)
            //{
            //    XmlElement colorElem = xmlUtility.AddElement(transformElem, "Color");
            //    xmlUtility.SetAttribute(colorElem, "ColorStart", "#" + ColorUtility.ToHtmlStringRGBA(c.colorStart));
            //    xmlUtility.SetAttribute(colorElem, "ColorFinish", "#" + ColorUtility.ToHtmlStringRGBA(c.colorFinish));
            //    xmlUtility.SetAttribute(colorElem, "Frames", c.frames);
            //}

            return dynamicElement;
        }
    }

    [CustomEditor(typeof(DynamicTransform), true)]
    public class DynamicEditor : Editor
    {
        DynamicTransform _menu;
        private void OnEnable()
        {
            _menu = (DynamicTransform)target;
        }

        private Vector3 _lastRootPosition;
        private bool _hasLastRootPosition;
        public void OnSceneGUI()
        {
            Transform root = _menu.transform;

            if (_menu.move.intervals == null)
                return;

            Vector3 cumulativeOffset = Vector3.zero;

            if (!_hasLastRootPosition)
            {
                _lastRootPosition = root.position;
                _hasLastRootPosition = true;
                return;
            }
            Vector3 rootDelta = root.position - _lastRootPosition;

            if (_menu.move.intervals.Count > 0)
                DynamicHandles.ChangeLockedIntervals(rootDelta, _menu.move.intervals, -1);

            _lastRootPosition = root.position;

            for (int i = 0; i < _menu.move.intervals.Count; i++)
            {
                // Variable Declarations
                MoveInterval interval = _menu.move.intervals[i];
                List<SpriteRenderer> sprites = _menu.GetSpriteRenderers();
                bool hasBounds = TryGetSpriteBounds(sprites, out Bounds spriteBounds);

                // Cache Finish Position and Update Interval Before Processing Stuff
                interval.UpdatePoints();
                Vector3 oldLocalFinish = interval.finish;

                // Position Calculations (Local to World Positions)
                Vector3 startWorldPos = root.position + cumulativeOffset;
                Vector3 newFinishWorldPos = DynamicHandles.CalculateFinishPos(interval, startWorldPos);
                Vector3 newSupportWorldPos = DynamicHandles.CalculateSupportPos(interval, startWorldPos);
                Vector3 localHandleOffset = DynamicHandles.CalculateHandleOffset(sprites, root.position);

                // Render Handles
                DynamicHandles.handleSize = HandleUtility.GetHandleSize(newFinishWorldPos) * 0.1f;
                DynamicHandles.RenderSupportHandle(ref newSupportWorldPos, interval.useEasing);
                DynamicHandles.RenderFinishHandles(ref newFinishWorldPos, localHandleOffset, spriteBounds, hasBounds);

                // Convert Back to Local Positions (And Calculate Change In Position)
                Vector3 newLocalFinish = newFinishWorldPos - startWorldPos;
                Vector3 newLocalSupport = newSupportWorldPos - startWorldPos;
                Vector3 delta = newLocalFinish - oldLocalFinish;

                interval.finish = newLocalFinish;
                interval.support = newLocalSupport;

                // Apply The Change In Position To The Next Interval That Is Locked 
                // This is Done to Lock it In Place While Movement From The Previous Intervals Happen.
                DynamicHandles.ChangeLockedIntervals(delta, _menu.move.intervals, i);

                cumulativeOffset += (Vector3)interval.finish;
            }
        }

        // Dragging variables
        int dragStartIndex = -1;
        float dragOffset = 0f;
        public override void OnInspectorGUI()
        {
            TransformType transform = _menu.transformType;
            _menu.move.name = EditorGUILayout.TextField("Tranformation Name", _menu.move.name);
            transform = (TransformType)EditorGUILayout.EnumPopup("Type: ", transform);
            switch (transform)
            {
                case TransformType.Move:
                    _menu.move.loop = EditorGUILayout.IntField("Loop", _menu.move.loop);
                    int IntervalsCount = _menu.move.intervals.Count;
                    List<MoveInterval> Intervals = _menu.move.intervals;
                    for (int i = 0; i < IntervalsCount; i++)
                    {
                        MoveInterval currentInterval = Intervals[i];
                        DisplayInterval(Intervals ,currentInterval, i);
                        IntervalsCount = _menu.move.intervals.Count;
                    }
                    if (IntervalsCount == 0 && GUILayout.Button("Add Interval", GUILayout.Height(28)))
                        Intervals.Add(new());
                    break;
            }
            _menu.transformType = transform;
        }
        public void DisplayInterval(List<MoveInterval> intervals, MoveInterval currentInterval, int index)
        {
            EditorGUILayout.BeginVertical("OL box NoExpand");
            //  ===================================
            //  TITLE AND DRAGGING FUNCTIONS
            //  ===================================
            // Get the Entire Row's Rectangle
            Rect titleRow = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            int rowHeight = currentInterval.useEasing ? (int)(titleRow.height * 4) : (int)(titleRow.height * 5);

            if (currentInterval.hide)
                rowHeight = (int)(titleRow.height);

            // Initialize the Rect for the Title
            Rect titleLabel = new Rect(titleRow.x + (titleRow.width - 100) * 0.5f,titleRow.y, 100, titleRow.height);

            int id = GUIUtility.GetControlID(FocusType.Passive);
            bool isDraggingThis = GUIUtility.hotControl != 0 && dragStartIndex == index;

            // Delete Button
            Rect spacer = new Rect(titleRow.xMax - 30, titleRow.y, 30, titleRow.height);
            if (GUI.Button(spacer, "X"))
            {
                intervals.RemoveAt(index);
                return;
            }

            // Add Button
            spacer = new Rect(titleRow.xMax - 60, titleRow.y, 30, titleRow.height);
            if (GUI.Button(spacer, "＋"))
            {
                intervals.Insert(index + 1, new());
                return;
            }

            // Dropdown Button
            string dropdownButton = currentInterval.hide ? "▲" : "▶";
            spacer = new Rect(titleRow.x, titleRow.y, 30, titleRow.height);
            if (GUI.Button(spacer, dropdownButton, "CenteredLabel"))
            {
                if (currentInterval.hide)
                    currentInterval.hide = false;
                else
                    currentInterval.hide = true;
            }

            // Highlighting The Bar (this is all it does lmao)
            if (isDraggingThis)
                EditorGUI.DrawRect(titleRow, new Color(0.3f, 0.7f, 1f, 0.35f));
            else
            {
                if (titleRow.Contains(Event.current.mousePosition) && GUIUtility.hotControl == 0)
                    EditorGUI.DrawRect(titleRow, new Color(1f, 1f, 1f, 0.04f));
                EditorGUI.DrawRect(titleRow, new Color(0.1f, 1f, 0f, 0.2f));
            }

            EditorGUI.LabelField(titleLabel, $"Move Interval {index + 1}", EditorStyles.boldLabel);

            // I dont even know how the control ids and stuff works, it just does, its black magic
            // ask chatgpt instead lmao.
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (titleRow.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        dragStartIndex = index;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Repaint();
                        dragOffset += e.delta.y;

                        if (Mathf.Abs(dragOffset) >= rowHeight)
                        {
                            int direction = dragOffset > 0 ? 1 : -1;
                            int swapIndex = dragStartIndex + direction;

                            if (swapIndex >= 0 && swapIndex < intervals.Count)
                            {
                                // swap
                                (intervals[dragStartIndex], intervals[swapIndex]) =
                                    (intervals[swapIndex], intervals[dragStartIndex]);

                                dragStartIndex = swapIndex;
                                dragOffset -= rowHeight * direction;

                                GUI.changed = true;
                            }
                        }
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        dragOffset = 0;
                        dragStartIndex = -1;
                        e.Use();
                    }
                    break;
            }

            // Cutoff if the contents are hidden;
            if (currentInterval.hide)
            {
                currentInterval.UpdatePoints();
                EditorGUILayout.EndVertical();
                return;
            }
            //  ============================
            //  ROW 1 (Easing and Lock)
            //  ============================

            EditorGUILayout.BeginHorizontal();
            currentInterval.useEasing = EditorGUILayout.ToggleLeft(new GUIContent("Use Easing", "Easing enables smooth and simpler interval editing by using a preset"), currentInterval.useEasing);
            currentInterval.isLocked = EditorGUILayout.ToggleLeft("Lock", currentInterval.isLocked);
            EditorGUILayout.EndHorizontal();

            //  ==================================
            //  ROW 2 (Time and Easing Options)
            //  ==================================
            Rect row2 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            // Dimensions
            float spacing = 5f;
            float x = row2.x;
            float y = row2.y;
            float h = row2.height - 0.05f;

            // This is the amount of UI elements (Label and Field) and will decide the width of the fields.
            int elements = currentInterval.useEasing ? 3 : 2;

            float labelW = 35;
            float r2space = (row2.width - (labelW * elements) - spacing) / elements;
            float fieldW = r2space;

            // Time
            DisplayLabel(ref x, y, labelW, h, "Time", "The amount of time (in frames) it takes for an object to move in this interval");
            currentInterval.framesToMove = DisplayField(ref x, y, fieldW, h, currentInterval.framesToMove, spacing);

            // Delay
            DisplayLabel(ref x, y, labelW, h, "Delay", "The amount of time (in frames) it takes to delay this movement interval");
            currentInterval.delay = DisplayField(ref x, y, fieldW, h, currentInterval.framesToMove);

            // Ease (conditional)
            if (currentInterval.useEasing)
            {
                Rect easeField = new Rect(x + spacing, y, fieldW + labelW - spacing, h);
                currentInterval.easeType = (MoveInterval.Easing)EditorGUI.EnumPopup(easeField, currentInterval.easeType);
            }

            //  =============================
            //  ROW 3 (Points)
            //  =============================
            //  Support and Finish

            Rect row3 = default;

            // Support Points (Only if Easing is Off)
            if (!currentInterval.useEasing)
            {
                row3 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                x = row3.x;
                y = row3.y;
                h = row3.height - 0.05f;

                elements = 3;
                float r3space = (row2.width - (labelW * elements) - spacing) / elements;
                fieldW = r3space;

                DisplayLabel(ref x, y, labelW + fieldW, h, "Support Point");
                x += spacing;

                DisplayLabel(ref x, y, labelW, h, "    X");
                currentInterval.support.x = DisplayField(ref x, y, fieldW, h, currentInterval.support.x);

                DisplayLabel(ref x, y, labelW, h, "    Y");
                currentInterval.support.y = DisplayField(ref x, y, fieldW, h, currentInterval.support.y);
            }

            // Finish Points
            Rect row4 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            x = row4.x;
            y = row4.y;
            h = row4.height - 0.05f;

            string finishPointName = currentInterval.useEasing ? "Move" : "Finish Point";

            DisplayLabel(ref x, y, labelW + fieldW, h, finishPointName);
            x += spacing;

            DisplayLabel(ref x, y, labelW, h, "    X");
            currentInterval.finish.x = DisplayField(ref x, y, fieldW, h, currentInterval.finish.x);

            DisplayLabel(ref x, y, labelW, h, "    Y");
            currentInterval.finish.y = DisplayField(ref x, y, fieldW, h, currentInterval.finish.y);

            EditorGUILayout.EndVertical();
        }

        //  Summary:
        //  Displays a label, and increments the x position for the next UI element
        public void DisplayLabel(ref float x, float y, float width, float height, string lable, string tooltip = "")
        {
            Rect lableRect = new Rect(x, y, width, height);
            GUIContent guiContent = new GUIContent(lable, tooltip);
            EditorGUI.LabelField(lableRect, guiContent);
            x += width;
        }

        //  Summary:
        //  Displays a label, and increments the x position for the next UI element
        public void DisplayLabel(ref float x, float y, float width, float height, GUIContent guiContent)
        {
            Rect lableRect = new Rect(x, y, width, height);
            EditorGUI.LabelField(lableRect, guiContent);
            x += width;
        }

        //  Summary:
        //  Displays the field for a given input, and increments the x position for the next UI element
        public float DisplayField(ref float x, float y, float width, float height, float input, float padding = 0f)
        {
            float output;
            Rect fieldRect = new Rect(x, y, width - padding, height);
            output = EditorGUI.FloatField(fieldRect, input);
            x += width + padding;
            return output;
        }

        //  Summary:
        //  Displays the field for a given input, and increments the x position for the next UI element
        public int DisplayField(ref float x, float y, float width, float height, int input, float padding = 0f)
        {
            int output;
            Rect fieldRect = new Rect(x, y, width - padding, height);
            output = EditorGUI.IntField(fieldRect, input);
            x += width + padding;
            return output;
        }
    }
}
