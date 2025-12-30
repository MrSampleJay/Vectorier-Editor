using UnityEngine;

namespace Vectorier.Matrix
{
    // ================= DATA ================= //

    public struct AffineMatrixData
    {
        public float A, B, C, D, Tx, Ty;
        public float TopLeftX, TopLeftY;
        public float BoundingWidth, BoundingHeight;
        public int NativeWidth, NativeHeight;
    }

    // ================= MAIN ================= //

    public static class AffineTransformation
    {
        /* Computes the affine matrix for a GameObject with SpriteRenderer.
           Returns false if no rotation or flipping is applied. */
        public static bool Compute(GameObject obj, SpriteRenderer spriteRenderer, out AffineMatrixData matrix)
        {
            matrix = new AffineMatrixData();

            if (obj == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return false;

            Texture2D texture = spriteRenderer.sprite.texture;
            if (texture == null)
                return false;

            Transform transform = obj.transform;
            Vector3 worldEuler = transform.rotation.eulerAngles;

            bool noRotation =
                Mathf.Approximately(worldEuler.x, 0f) &&
                Mathf.Approximately(worldEuler.y, 0f) &&
                Mathf.Approximately(worldEuler.z, 0f);

            bool noFlip = !spriteRenderer.flipX && !spriteRenderer.flipY;

            if (noRotation && noFlip)
                return false;

            int nativeWidth = texture.width;
            int nativeHeight = texture.height;

            float signX = spriteRenderer.flipX ? -1f : 1f;
            float signY = spriteRenderer.flipY ? -1f : 1f;

            // Build local corner offsets in WORLD UNITS
            Vector3 localTopLeft = new Vector3(0f, 0f, 0f);
            Vector3 localTopRight = new Vector3(signX * nativeWidth, 0f, 0f);
            Vector3 localBottomLeft = new Vector3(0f, -signY * nativeHeight, 0f);
            Vector3 localBottomRight = new Vector3(signX * nativeWidth, -signY * nativeHeight, 0f);

            // Transform local corners to world space (world units)
            Vector3 worldTopLeft = transform.TransformPoint(localTopLeft);
            Vector3 worldTopRight = transform.TransformPoint(localTopRight);
            Vector3 worldBottomLeft = transform.TransformPoint(localBottomLeft);
            Vector3 worldBottomRight = transform.TransformPoint(localBottomRight);

            // Project to 2D pixel-space
            Vector2 pointTopLeft = new Vector2(worldTopLeft.x, -worldTopLeft.y);
            Vector2 pointTopRight = new Vector2(worldTopRight.x, -worldTopRight.y);
            Vector2 pointBottomLeft = new Vector2(worldBottomLeft.x, -worldBottomLeft.y);
            Vector2 pointBottomRight = new Vector2(worldBottomRight.x, -worldBottomRight.y);

            Vector2 vectorWidth = pointTopRight - pointTopLeft;
            Vector2 vectorHeight = pointBottomLeft - pointTopLeft;

            float A = vectorWidth.x;
            float B = vectorWidth.y;
            float C = vectorHeight.x;
            float D = vectorHeight.y;

            float imagePosX = transform.position.x;
            float imagePosY = -transform.position.y;

            // Compute bounding-box top-left
            float topLeftX = imagePosX + Mathf.Min(0f, A) + Mathf.Min(0f, C);
            float topLeftY = imagePosY + Mathf.Min(0f, B) + Mathf.Min(0f, D);

            float Tx = imagePosX - topLeftX;
            float Ty = imagePosY - topLeftY;

            // Bounding box in pixel units
            float minX = Mathf.Min(pointTopLeft.x, Mathf.Min(pointTopRight.x, Mathf.Min(pointBottomLeft.x, pointBottomRight.x)));
            float minY = Mathf.Min(pointTopLeft.y, Mathf.Min(pointTopRight.y, Mathf.Min(pointBottomLeft.y, pointBottomRight.y)));
            float maxX = Mathf.Max(pointTopLeft.x, Mathf.Max(pointTopRight.x, Mathf.Max(pointBottomLeft.x, pointBottomRight.x)));
            float maxY = Mathf.Max(pointTopLeft.y, Mathf.Max(pointTopRight.y, Mathf.Max(pointBottomLeft.y, pointBottomRight.y)));

            matrix.A = A;
            matrix.B = B;
            matrix.C = C;
            matrix.D = D;
            matrix.Tx = Tx;
            matrix.Ty = Ty;
            matrix.TopLeftX = topLeftX;
            matrix.TopLeftY = topLeftY;
            matrix.BoundingWidth = maxX - minX;
            matrix.BoundingHeight = maxY - minY;

            matrix.NativeWidth = nativeWidth;
            matrix.NativeHeight = nativeHeight;

            return true;
        }

        public static void ApplyToObject(GameObject obj, SpriteRenderer renderer, AffineMatrixData matrix)
        {
            /* NOTE: Unity transform can't represent general 2D affine transforms,
                     it will always destroy the shear part when decomposing. */

            if (obj == null || renderer == null || renderer.sprite == null)
                return;

            float roundScale = 1000000f;

            Transform transform = obj.transform;
            Transform parent = transform.parent;

            float nativeWidth = renderer.sprite.texture.width;
            float nativeHeight = renderer.sprite.texture.height;

            // XML to Unity top-left
            Vector2 topLeftWorld = new Vector2(matrix.TopLeftX, -matrix.TopLeftY);

            // Offset from top-left to pivot
            float minX = Mathf.Min(0f, matrix.A) + Mathf.Min(0f, matrix.C);
            float minY = Mathf.Min(0f, matrix.B) + Mathf.Min(0f, matrix.D);

            Vector3 pivotWorld2D = new Vector3(topLeftWorld.x - minX, topLeftWorld.y + minY);
            Vector3 pivotWorld = new Vector3(pivotWorld2D.x, pivotWorld2D.y, 0f);

            // Rounding position
            pivotWorld.x = Mathf.Round(pivotWorld.x * roundScale) / roundScale;
            pivotWorld.y = Mathf.Round(pivotWorld.y * roundScale) / roundScale;
            pivotWorld.z = Mathf.Round(pivotWorld.z * roundScale) / roundScale;

            if (parent != null)
                transform.localPosition = parent.InverseTransformPoint(pivotWorld);
            else
                transform.position = pivotWorld;

            // Basis vectors (XML to Unity)
            Vector2 widthUnity = new Vector2(matrix.A, -matrix.B);
            Vector2 heightUnity = new Vector2(matrix.C, -matrix.D);

            float scaleX = widthUnity.magnitude / nativeWidth;
            float scaleY = heightUnity.magnitude / nativeHeight;

            // Rounding scales
            scaleX = Mathf.Round(scaleX * roundScale) / roundScale;
            scaleY = Mathf.Round(scaleY * roundScale) / roundScale;

            if (parent != null)
            {
                Vector3 parentScale = parent.lossyScale;
                scaleX /= Mathf.Abs(parentScale.x);
                scaleY /= Mathf.Abs(parentScale.y);

                // Rounding again after parent adjustment
                scaleX = Mathf.Round(scaleX * roundScale) / roundScale;
                scaleY = Mathf.Round(scaleY * roundScale) / roundScale;
            }

            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Rotation
            Vector3 right = new Vector3(widthUnity.x, widthUnity.y, 0f).normalized;
            Vector3 up = new Vector3(-heightUnity.x, -heightUnity.y, 0f).normalized;
            Vector3 forward = Vector3.Cross(right, up);

            // Rounding basis vectors
            right.x = Mathf.Round(right.x * roundScale) / roundScale;
            right.y = Mathf.Round(right.y * roundScale) / roundScale;
            up.x = Mathf.Round(up.x * roundScale) / roundScale;
            up.y = Mathf.Round(up.y * roundScale) / roundScale;
            forward.x = Mathf.Round(forward.x * roundScale) / roundScale;
            forward.y = Mathf.Round(forward.y * roundScale) / roundScale;

            Quaternion worldRot = Quaternion.LookRotation(forward, up);

            if (parent != null)
                transform.localRotation = Quaternion.Inverse(parent.rotation) * worldRot;
            else
                transform.rotation = worldRot;
        }
    }
}
