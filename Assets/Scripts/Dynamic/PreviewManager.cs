using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Vectorier.Dynamic.DynamicTransform;
using System.Linq;

namespace Vectorier.Dynamic
{
    public static class PreviewManager
    {
        public static float handleSize = 0;
        static Vector3 snap = new Vector3(1f, 1f, 1f);

        //  Calculate the position from the previous finish point (startingPos), and return it
        public static Vector3 CalculateFinishPos(MoveData interval, Vector3 startingPos )
        {
            return startingPos + (Vector3)interval.Finish;
        }

        public static Vector3 CalculateSupportPos(MoveData interval, Vector3 startingPos)
        {
            return startingPos + (Vector3)interval.Support;
        }

        public static Vector3 CalculateOffsetFromParent(List<SpriteRenderer> sprites, Vector3 WorldPos)
        {
            if (sprites == null)
                return Vector3.zero;

            List<float> PositionsX = new List<float>();
            List<float> PositionsY = new List<float>();

            foreach (SpriteRenderer sprite in sprites) 
            {
                if (sprite == null)
                    continue;

                PositionsX.Add(sprite.transform.position.x - WorldPos.x);
                PositionsY.Add(sprite.transform.position.y - WorldPos.y);
            }
            float lowestXOffset = PositionsX.Min(posX => posX);
            float lowestYOffset = PositionsY.Max(posY => posY);
            return new Vector3 (lowestXOffset, lowestYOffset);
        }

        //  Takes in a current finishWorldPos, and returns the new one as bew, Requires spriteBounds for the 3 other corners, and a bool to render them
        //  if not the handle appears on the finish point itself.
        public static void RenderFinishHandles(ref Vector3 newFinishWorldPos, Vector3 localOffset, Bounds spriteBounds, bool hasBounds)
        {
            Vector3 finishWorldPos = newFinishWorldPos;
            if (hasBounds)
            {
                Vector3 ext = (spriteBounds.extents * 2) + new Vector3(localOffset.x, -localOffset.y);

                // Top-right
                Vector3 trOffset = new Vector3(spriteBounds.max.x, spriteBounds.max.y, 0) - spriteBounds.center + new Vector3(localOffset.x, localOffset.y);
                EditorGUI.BeginChangeCheck();
                Vector3 tr = Handles.FreeMoveHandle(finishWorldPos + trOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += tr - (finishWorldPos + trOffset);

                // Top-Left
                Vector3 tlOffset = new Vector3(spriteBounds.min.x, spriteBounds.max.y, 0) - spriteBounds.center + new Vector3(localOffset.x, localOffset.y);
                EditorGUI.BeginChangeCheck();
                Vector3 tl = Handles.FreeMoveHandle(finishWorldPos + tlOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += tl - (finishWorldPos + tlOffset);

                // Bottom-right
                Vector3 brOffset = new Vector3(spriteBounds.max.x, spriteBounds.min.y, 0) - spriteBounds.center + new Vector3(localOffset.x, localOffset.y);
                EditorGUI.BeginChangeCheck();
                Vector3 br = Handles.FreeMoveHandle(finishWorldPos + brOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += br - (finishWorldPos + brOffset);

                // Bottom-left
                Vector3 blOffset = new Vector3(spriteBounds.min.x, spriteBounds.min.y, 0) - spriteBounds.center + new Vector3(localOffset.x, localOffset.y);
                EditorGUI.BeginChangeCheck();
                Vector3 bl = Handles.FreeMoveHandle(finishWorldPos + blOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += bl - (finishWorldPos + blOffset);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Vector3 pivot = Handles.FreeMoveHandle(newFinishWorldPos, handleSize, snap, Handles.CircleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos = pivot;
            }
        }

        public static void RenderSupportHandle(ref Vector3 support, bool isEasing, Vector3 localOffset)
        {
            if (!isEasing)
            {
                Vector3 newsupport = Handles.FreeMoveHandle(support + localOffset, handleSize, snap, Handles.CircleHandleCap);
                support = newsupport - localOffset;
            }
        }

        public static void OffsetFromRoot(Transform root, List<MoveData> moves, ref bool hasLastRootPosition, ref Vector3 lastRootPosition)
        {
            if (!hasLastRootPosition)
            {
                lastRootPosition = root.position;
                hasLastRootPosition = true;
                return;
            }

            Vector3 rootDelta = root.position - lastRootPosition;

            //  Change Locked Intervals Based off 
            if (moves.Count > 0)
                PreviewManager.ChangeLockedIntervals(rootDelta, moves, -1);

            lastRootPosition = root.position;
        }
        public static void ChangeLockedIntervals(Vector3 delta, List<MoveData> subsequentIntervals, int currentIndex)
        {
            for (int ii = currentIndex; ii < subsequentIntervals.Count; ii++)
            {
                if (ii + 1 < subsequentIntervals.Count && subsequentIntervals[ii + 1].isLocked)
                {
                    subsequentIntervals[ii + 1].Finish -= (Vector2)delta;
                    return;
                }
            }
        }

        public static void RenderAnchorHandle(ref Vector3 localPos)
        {
            localPos = Handles.FreeMoveHandle(localPos, handleSize, snap, Handles.CircleHandleCap);
        }
    }
}