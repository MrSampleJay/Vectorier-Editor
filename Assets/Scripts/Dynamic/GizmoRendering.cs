using NUnit.Framework;
using TMPro;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Linq;

namespace Vectorier.Dynamic
{
    public static class DynamicHandles
    {
        public static float handleSize = 0;
        static Vector3 snap = new Vector3(1f, 1f, 1f);

        //  Calculate the position from the previous finish point (startingPos), and return it
        public static Vector3 CalculateFinishPos(MoveInterval interval, Vector3 startingPos )
        {
            return startingPos + (Vector3)interval.finish;
        }

        public static Vector3 CalculateSupportPos(MoveInterval interval, Vector3 startingPos)
        {
            return startingPos + (Vector3)interval.support;
        }

        public static Vector3 CalculateHandleOffset(System.Collections.Generic.List<SpriteRenderer> sprites, Vector3 WorldPos)
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
        public static void RenderFinishHandles(ref Vector3 newFinishWorldPos, Vector3 localOffset, Bounds spriteBounds, bool hasBounds)
        {
            Vector3 finishWorldPos = newFinishWorldPos;
            if (hasBounds)
            {
                Vector3 ext = (spriteBounds.extents * 2) + new Vector3(localOffset.x, -localOffset.y);

                // Top-right
                Vector3 trOffset = new Vector3(ext.x, localOffset.y, 0) ;
                EditorGUI.BeginChangeCheck();
                Vector3 tr = Handles.FreeMoveHandle(finishWorldPos + trOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += tr - (finishWorldPos + trOffset);

                // Top-Left
                Vector3 tlOffset = new Vector3(localOffset.x, localOffset.y, 0);
                EditorGUI.BeginChangeCheck();
                Vector3 tl = Handles.FreeMoveHandle(finishWorldPos + tlOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += tl - (finishWorldPos + tlOffset);

                // Bottom-right
                Vector3 brOffset = new Vector3(ext.x, -ext.y, 0);
                EditorGUI.BeginChangeCheck();
                Vector3 br = Handles.FreeMoveHandle(finishWorldPos + brOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += br - (finishWorldPos + brOffset);

                // Bottom-left
                Vector3 blOffset = new Vector3(localOffset.x, -ext.y, 0);
                EditorGUI.BeginChangeCheck();
                Vector3 bl = Handles.FreeMoveHandle(finishWorldPos + blOffset, handleSize, snap, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                    newFinishWorldPos += bl - (finishWorldPos + blOffset);
            }

            //EditorGUI.BeginChangeCheck();
            //Vector3 pivot = Handles.FreeMoveHandle(newFinishWorldPos, handleSize, snap, Handles.CircleHandleCap);
            //if (EditorGUI.EndChangeCheck())
            //    newFinishWorldPos = pivot;
        }

        public static void RenderSupportHandle(ref Vector3 support, bool isEasing)
        {
            if (!isEasing)
            {
                support = Handles.FreeMoveHandle(support, handleSize, snap, Handles.CircleHandleCap);
            }
        }

        public static void ChangeLockedIntervals(Vector3 delta, System.Collections.Generic.List<MoveInterval> subsequentIntervals, int currentIndex)
        {
            for (int ii = currentIndex; ii < subsequentIntervals.Count; ii++)
            {
                if (ii + 1 < subsequentIntervals.Count && subsequentIntervals[ii + 1].isLocked)
                {
                    subsequentIntervals[ii + 1].finish -= (Vector2)delta;
                    return;
                }
            }
        }
    }
}