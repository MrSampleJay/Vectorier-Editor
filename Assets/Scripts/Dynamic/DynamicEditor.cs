using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static Vectorier.Dynamic.DynamicTransform;
using System;

namespace Vectorier.Dynamic
{
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

            if (_menu.moves == null)
                return;

            Vector3 cumulativeOffset = Vector3.zero;

            if (!_hasLastRootPosition)
            {
                _lastRootPosition = root.position;
                _hasLastRootPosition = true;
                return;
            }
            Vector3 rootDelta = root.position - _lastRootPosition;

            if (_menu.moves.Count > 0)
                DynamicHandles.ChangeLockedIntervals(rootDelta, _menu.moves, -1);

            _lastRootPosition = root.position;

            for (int i = 0; i < _menu.moves.Count; i++)
            {
                // Variable Declarations
                MoveData interval = _menu.moves[i];
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
                DynamicHandles.ChangeLockedIntervals(delta, _menu.moves, i);

                cumulativeOffset += (Vector3)interval.finish;
            }
        }

        private bool TryGetSpriteBounds(List<SpriteRenderer> sprites, out Bounds bounds)
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

        // Dragging variables
        int dragStartIndex = -1;
        float dragOffset = 0f;
        public override void OnInspectorGUI()
        {
            _menu.transformationName = EditorGUILayout.TextField("Tranformation Name", _menu.transformationName);

            // TODO: ADD LOOP
            // _menu.moves.loop = EditorGUILayout.IntField("Loop", _menu.moves.loop);

            int IntervalsCount = _menu.moves.Count;
            List<MoveData> Intervals = _menu.moves;
            for (int i = 0; i < IntervalsCount; i++)
            {
                MoveData currentInterval = Intervals[i];
                DisplayInterval(Intervals, currentInterval, i);
                IntervalsCount = _menu.moves.Count;
            }
            if (IntervalsCount == 0 && GUILayout.Button("Add Interval", GUILayout.Height(28)))
                Intervals.Add(new());
        }
        public void DisplayInterval(List<MoveData> intervals, MoveData currentInterval, int index)
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
            Rect titleLabel = new Rect(titleRow.x + (titleRow.width - 100) * 0.5f, titleRow.y, 100, titleRow.height);

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
            currentInterval.duration = DisplayField(ref x, y, fieldW, h, currentInterval.duration, spacing);

            // Delay
            DisplayLabel(ref x, y, labelW, h, "Delay", "The amount of time (in frames) it takes to delay this movement interval");
            currentInterval.delay = DisplayField(ref x, y, fieldW, h, currentInterval.duration);

            // Ease (conditional)
            if (currentInterval.useEasing)
            {
                Rect easeField = new Rect(x + spacing, y, fieldW + labelW - spacing, h);
                currentInterval.easeType = (MoveData.Easing)EditorGUI.EnumPopup(easeField, currentInterval.easeType);
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