using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static Vectorier.Dynamic.DynamicTransform;
using System;
using static UnityEditor.EditorGUILayout;
using System.Collections;
using System.Reflection;

namespace Vectorier.Dynamic
{
    [CustomEditor(typeof(DynamicTransform), false)]
    public class DynamicEditor : Editor
    {
        // This is peak YandereDev Coding here
        // Lmao
        // If it works it works

        DynamicTransform menu;

        bool useMultipleTransforms = false;
        private void OnEnable()
        {
            menu = (DynamicTransform)target;
            useMultipleTransforms = EditorPrefs.GetBool("Vectorier_UseMultipleTranasformTypes", false);
        }
        public void OnSceneGUI()
        {
            switch(menu.SelectedTransform)
            {
                case TransformType.Move:
                    DrawMoveEditor();
                    break;
                case TransformType.Rotate:
                    DrawRotationEditor();
                    break;
                default:
                    DrawMoveEditor();
                    DrawRotationEditor();
                    break;

            }
        }

        private Vector3 _lastRootPosition;
        private bool _hasLastRootPosition;

        public void DrawMoveEditor()
        {
            if (!menu.showPreview)
                return;

            if (menu.moves == null)
                return;

            Transform root = menu.transform;

            //  The Starting Position
            Vector3 cumulativeOffset = Vector3.zero;

            //  Get All the Bounds and Offsets for Handles
            List<SpriteRenderer> sprites = menu.GetSpriteRenderers(menu.allowedPreviewTags);
            bool hasBounds = TryGetSpriteBounds(sprites, out Bounds spriteBounds);
            Vector3 boundsCenter = new Vector3(spriteBounds.extents.x, -spriteBounds.extents.y);

            PreviewManager.OffsetFromRoot(root, menu.moves,ref _hasLastRootPosition,ref _lastRootPosition);

            for (int i = 0; i < menu.moves.Count; i++)
            {
                // Variable Declarations
                MoveData interval = menu.moves[i];

                // Cache Finish Position and Update Interval Before Processing Stuff
                interval.UpdatePoints();
                Vector3 oldLocalFinish = interval.Finish;

                // Position Calculations (Local to World Positions)
                Vector3 startWorldPos = root.position + cumulativeOffset;
                Vector3 newFinishWorldPos = PreviewManager.CalculateFinishPos(interval, startWorldPos);
                Vector3 newSupportWorldPos = PreviewManager.CalculateSupportPos(interval, startWorldPos);
                Vector3 localHandleOffset = Vector3.zero;
                if (sprites.Count != 0)
                    localHandleOffset = PreviewManager.CalculateOffsetFromParent(sprites, root.position);

                // Render Handles
                PreviewManager.handleSize = HandleUtility.GetHandleSize(newFinishWorldPos) * 0.1f;
                PreviewManager.RenderSupportHandle(ref newSupportWorldPos, interval.UseEasing, localHandleOffset + boundsCenter);
                PreviewManager.RenderFinishHandles(ref newFinishWorldPos, localHandleOffset, spriteBounds, hasBounds);

                // Convert Back to Local Positions (And Calculate Change In Position)
                Vector3 newLocalFinish = newFinishWorldPos - startWorldPos;
                Vector3 newLocalSupport = newSupportWorldPos - startWorldPos;
                Vector3 delta = newLocalFinish - oldLocalFinish;

                interval.Finish = newLocalFinish;
                interval.Support = newLocalSupport;

                // Apply The Change In Position To The Next Interval That Is Locked 
                // This is Done to Lock it In Place While Changes From The Previous Intervals Happen.
                PreviewManager.ChangeLockedIntervals(delta, menu.moves, i);

                cumulativeOffset += (Vector3)interval.Finish;
            }
        }

        //  I havent figured out how to draw the preview for the thing
        //  Can't be Bothered.
        public void DrawRotationEditor()
        {
            foreach(RotateData rotation in menu.rotations)
            {
                if (rotation.useAnchorType) continue;
                Vector3 localAnchor = rotation.anchor + (Vector2)menu.transform.position;
                PreviewManager.RenderAnchorHandle(ref localAnchor);
                rotation.anchor = (Vector2)localAnchor - (Vector2)menu.transform.position;
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

        // Dragging variables
        int dragMoveStartIndex = -1;
        int dragRotateStartIndex = -1;
        float dragOffset = 0f;
        public override void OnInspectorGUI()
        {
            menu.transformationName = EditorGUILayout.TextField("Transformation Name", menu.transformationName);
            if (useMultipleTransforms)
            {
                EditorGUILayout.Space(6);
                DrawMoveSection();
                EditorGUILayout.Space(6);

                DrawRotationSection();
                EditorGUILayout.Space(6);

                //DrawSizeSection();

                DrawAddButtons();
            }
            else
            {
                menu.SelectedTransform = (TransformType)EnumPopup("Type", menu.SelectedTransform);
                menu.showPreview = Toggle("Show Preview", menu.showPreview);

                EditorGUILayout.Space(6);
                switch (menu.SelectedTransform)
                {
                    case TransformType.Move:
                        if (menu.moves.Count > 0)
                            DrawMoveSection();
                        else if (GUILayout.Button("Add Move"))
                            menu.moves.Add(new());
                    break;
                    case TransformType.Rotate:
                        if (menu.rotations.Count > 0)
                            DrawRotationSection();
                        else if (GUILayout.Button("Add Rotation"))
                            menu.rotations.Add(new());
                    break;
                    case TransformType.Size:
                        if (menu.sizes.Count > 0)
                            DrawSizeSection();
                        else if (GUILayout.Button("Add Size"))
                            menu.sizes.Add(new());
                    break;
                    case TransformType.Color:
                        if (menu.colors.Count > 0)
                            DrawSizeSection();
                        else if (GUILayout.Button("Add Color"))
                            menu.colors.Add(new());
                    break;
                }

            }
        }
        void DrawAddButtons()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            int elements = 4;
            float x = rect.x;
            float width = rect.width / elements;
            Rect moveButton = new Rect(rect.x, rect.y, width, rect.height);
            if (GUI.Button(moveButton, "Add Move"))
                menu.moves.Add(new());
            x += width;
            Rect rotateButton = new Rect(x, rect.y, width, rect.height);
            if (GUI.Button(rotateButton, "Add Rotation"))
                menu.rotations.Add(new());
            x += width;
            Rect sizeButton = new Rect(x, rect.y, width, rect.height);
            if (GUI.Button(sizeButton, "Add Size"))
                menu.sizes.Add(new());
            x += width;
            Rect colorButton = new Rect(x, rect.y, width, rect.height);
            if (GUI.Button(colorButton, "Add Color"))
                menu.colors.Add(new());
        }
        void DrawMoveSection()
        {
            var moves = menu.moves;

            if (moves.Count > 0)
            {
                EditorGUILayout.LabelField(
                    new GUIContent("Move Intervals", "Setup the Move Transform Intervals Here"),
                    "CenteredLabel");

                for (int i = 0; i < moves.Count; i++)
                    DisplayInterval(i);
            }
        }
        void DrawRotationSection()
        {
            var rotations = menu.rotations;

            if (rotations.Count > 0)
            {
                EditorGUILayout.LabelField(
                    new GUIContent("Rotations", "Setup the Rotation Transform Intervals Here"),
                    "CenteredLabel");

                for (int i = 0; i < rotations.Count; i++)
                    DisplayRotationInterval(rotations, rotations[i], i);
            }
        }
        void DrawSizeSection()
        {
            throw new NotImplementedException();
        }

        bool DrawReorderableHeader( Rect titleRow, string title, bool isHidden, ref int dragIndex, int currentIndex, int rowHeight, IList list, Action onAdd, Action onRemove)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            bool isDraggingThis = GUIUtility.hotControl != 0 && dragIndex == currentIndex;

            Rect titleLabel = new Rect( titleRow.x + (titleRow.width * 0.5f) - (title.Length * 7) / 2, titleRow.y, 100, titleRow.height);

            // Delete Button
            Rect spacer = new Rect(titleRow.xMax - 30, titleRow.y, 30, titleRow.height);
            if (GUI.Button(spacer, "X"))
            {
                onRemove?.Invoke();
                return false;
            }

            // Add Button
            spacer = new Rect(titleRow.xMax - 60, titleRow.y, 30, titleRow.height);
            if (GUI.Button(spacer, "＋"))
            {
                onAdd?.Invoke();
                return false;
            }

            // Highlighting The Bar
            if (isDraggingThis)
                EditorGUI.DrawRect(titleRow, new Color(0.3f, 0.7f, 1f, 0.35f));
            else
            {
                if (titleRow.Contains(Event.current.mousePosition) && GUIUtility.hotControl == 0)
                    EditorGUI.DrawRect(titleRow, new Color(1f, 1f, 1f, 0.04f));
                EditorGUI.DrawRect(titleRow, new Color(0.1f, 1f, 0f, 0.2f));
            }

            EditorGUI.LabelField(titleLabel, $"{title} {currentIndex + 1}", EditorStyles.boldLabel);

            // Dropdown Button
            string dropdownButton = isHidden ? "▲" : "▶";
            spacer = new Rect(titleRow.x, titleRow.y, 30, titleRow.height);
            if (GUI.Button(spacer, dropdownButton, "CenteredLabel"))
            {
                if (isHidden)
                    isHidden = false;
                else
                    isHidden = true;
            }

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (titleRow.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        dragIndex = currentIndex;
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
                            int swapIndex = dragIndex + direction;

                            if (swapIndex >= 0 && swapIndex < list.Count)
                            {
                                // swap
                                (list[dragIndex], list[swapIndex]) =
                                    (list[swapIndex], list[dragIndex]);

                                dragIndex = swapIndex;
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
                        dragRotateStartIndex = -1;
                        e.Use();
                    }
                    break;
            }

            return isHidden;
        }
        private void DisplayRotationInterval(List<RotateData> rotations, RotateData currentRotation, int index)
        {
            EditorGUILayout.BeginVertical("OL box NoExpand");
            Rect titleRow = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            float offset = 60f;
            Rect titleLabel = new Rect(titleRow.x + (titleRow.width - offset) * 0.5f, titleRow.y, 100, titleRow.height);
            int rowHeight = (int)(titleRow.height * 3);
            bool hidden = currentRotation.hide;

            if (currentRotation.hide)
                rowHeight = (int)(titleRow.height);

            hidden = DrawReorderableHeader(
                titleRow,
                "Move Interval ",
                hidden,
                ref dragMoveStartIndex,
                index,
                rowHeight,
                menu.moves, onAdd: () =>
                {
                    Undo.RecordObject(target, "Add Interval");
                    rotations.Insert(index + 1, new RotateData());
                },
                onRemove: () =>
                {
                    Undo.RecordObject(target, "Remove Interval");
                    rotations.RemoveAt(index);
                }
               );

            // Cutoff if the contents are hidden;
            if (currentRotation.hide)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            currentRotation.useAnchorType = EditorGUILayout.Toggle("Use Anchor Preset", currentRotation.useAnchorType);
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float width = rect.width / 2;
            float labelW = 75f;
            float fieldW = width - 75f;
            float padding = 5f;
            float x = rect.x;

            DisplayLabel(ref x, rect.y, labelW, rect.height, "Time");
            currentRotation.duration = DisplayField(ref x, rect.y, fieldW, rect.height, currentRotation.duration, padding);

            DisplayLabel(ref x, rect.y, labelW, rect.height, "Angle");
            currentRotation.angle = DisplayField(ref x, rect.y, fieldW, rect.height, currentRotation.angle, padding);

            Rect rect2 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            width = rect2.width / 3;
            labelW = 35f;
            fieldW = width - 35f;
            float x2 = rect2.x;

            if (!currentRotation.useAnchorType)
            {
                DisplayLabel(ref x2, rect2.y, width, rect2.height, "Anchor (Local)");

                DisplayLabel(ref x2, rect2.y, labelW, rect2.height, "X");
                currentRotation.anchor.x = DisplayField(ref x2, rect2.y, fieldW, rect.height, currentRotation.anchor.x, padding);

                DisplayLabel(ref x2, rect2.y, labelW, rect2.height, "Y");
                currentRotation.anchor.y = DisplayField(ref x2, rect2.y, fieldW, rect.height, currentRotation.anchor.y, padding);
            }
            else
            {
                DisplayLabel(ref x2, rect2.y, width, rect2.height, "Anchor Type");

                Rect enumRect = new Rect(x2, rect2.y, (width * 3) - width, rect2.height);
                currentRotation.anchorType = (RotateData.AnchorType)EditorGUI.EnumPopup(enumRect, currentRotation.anchorType);
            }
            EditorGUILayout.EndHorizontal();
        }
        private void DisplayInterval(int index)
        {
            List<MoveData> intervals = menu.moves;
            MoveData currentInterval = intervals[index];
            EditorGUILayout.BeginVertical("OL box NoExpand");
            //  ===================================
            //  TITLE
            //  ===================================
            Rect titleRow = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            int rowHeight = currentInterval.UseEasing ? (int)(titleRow.height * 4) : (int)(titleRow.height * 5);
            bool hidden = currentInterval.hide;
            hidden = DrawReorderableHeader(
                titleRow,
                "Move Interval ",
                hidden,
                ref dragMoveStartIndex,
                index,
                rowHeight,
                menu.moves, onAdd: () =>
                {
                    Undo.RecordObject(target, "Add Interval");
                    intervals.Insert(index + 1, new MoveData());
                },
                onRemove: () =>
                {
                    Undo.RecordObject(target, "Remove Interval");
                    intervals.RemoveAt(index);
                }
               );

            if (hidden)
                rowHeight = (int)titleRow.height;

            // Cutoff if the contents are hidden;
            if (hidden)
            {
                currentInterval.UpdatePoints();
                EditorGUILayout.EndVertical();
                return;
            }

            //  ============================
            //  ROW 1 (Easing and Lock)
            //  ============================

            EditorGUILayout.BeginHorizontal();
            currentInterval.UseEasing = EditorGUILayout.ToggleLeft(new GUIContent("Use Easing", "Easing enables smooth and simpler interval editing by using a preset"), currentInterval.UseEasing);
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
            int columns = currentInterval.UseEasing ? 3 : 2;

            float labelW = 35;
            float r2space = (row2.width - (labelW * columns) - spacing) / columns;
            float fieldW = r2space;

            // Time
            DisplayLabel(ref x, y, labelW, h, "Time", "The amount of time (in frames) it takes for an object to move in this interval");
            currentInterval.Duration = DisplayField(ref x, y, fieldW, h, currentInterval.Duration, spacing);

            // Delay
            DisplayLabel(ref x, y, labelW, h, "Delay", "The amount of time (in frames) it takes to delay this movement interval");
            currentInterval.Delay = DisplayField(ref x, y, fieldW, h, currentInterval.Duration);

            // Ease (conditional)
            if (currentInterval.UseEasing)
            {
                Rect easeField = new Rect(x + spacing, y, fieldW + labelW - spacing, h);
                currentInterval.EaseType = (MoveData.Easing)EditorGUI.EnumPopup(easeField, currentInterval.EaseType);
            }

            //  =============================
            //  ROW 3 (Points)
            //  =============================
            //  Support and Finish

            Rect row3 = default;

            // Support Points (Only if Easing is Off)
            if (!currentInterval.UseEasing)
            {
                row3 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                x = row3.x;
                y = row3.y;
                h = row3.height - 0.05f;

                columns = 3;
                float r3space = (row2.width - (labelW * columns) - spacing) / columns;
                fieldW = r3space;

                DisplayLabel(ref x, y, labelW + fieldW, h, "Support Point");
                x += spacing;

                DisplayLabel(ref x, y, labelW, h, "    X");
                currentInterval.Support.x = DisplayField(ref x, y, fieldW, h, currentInterval.Support.x);

                DisplayLabel(ref x, y, labelW, h, "    Y");
                currentInterval.Support.y = DisplayField(ref x, y, fieldW, h, currentInterval.Support.y);
            }

            // Finish Points
            Rect row4 = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            x = row4.x;
            y = row4.y;
            h = row4.height - 0.05f;

            string finishPointName = currentInterval.UseEasing ? "Move" : "Finish Point";

            DisplayLabel(ref x, y, labelW + fieldW, h, finishPointName);
            x += spacing;

            DisplayLabel(ref x, y, labelW, h, "    X");
            currentInterval.Finish.x = DisplayField(ref x, y, fieldW, h, currentInterval.Finish.x);

            DisplayLabel(ref x, y, labelW, h, "    Y");
            currentInterval.Finish.y = DisplayField(ref x, y, fieldW, h, currentInterval.Finish.y);

            menu.moves = intervals;

            EditorGUILayout.EndVertical();
        }

        //  Displays a label, and increments the x position for the next UI element
        public void DisplayLabel(ref float x, float y, float width, float height, string lable, string tooltip = "")
        {
            Rect lableRect = new Rect(x, y, width, height);
            GUIContent guiContent = new GUIContent(lable, tooltip);
            EditorGUI.LabelField(lableRect, guiContent);
            x += width;
        }

        //  Displays a label, and increments the x position for the next UI element
        public void DisplayLabel(ref float x, float y, float width, float height, GUIContent guiContent)
        {
            Rect lableRect = new Rect(x, y, width, height);
            EditorGUI.LabelField(lableRect, guiContent);
            x += width;
        }

        //  Displays the field for a given input, and increments the x position for the next UI element
        public float DisplayField(ref float x, float y, float width, float height, float input, float padding = 0f)
        {
            float output;
            Rect fieldRect = new Rect(x, y, width - padding, height);
            output = EditorGUI.FloatField(fieldRect, input);
            x += width + padding;
            return output;
        }

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