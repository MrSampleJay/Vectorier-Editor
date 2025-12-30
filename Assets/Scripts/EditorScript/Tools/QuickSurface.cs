using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Vectorier.EditorScript.Tools
{
    public static class QuickSurface
    {
        private enum SurfaceMode { All, Floor, LeftWall, RightWall }

        private const string CORNER_UP_L = "Images/Vector/Black/Block/Floor/v_CornerUp_L_01";
        private const string CORNER_UP_R = "Images/Vector/Black/Block/Floor/v_CornerUp_R_01";
        private const string CORNER_DOWN_L = "Images/Vector/Black/Block/Wall/v_CornerDown_L_01";
        private const string CORNER_DOWN_R = "Images/Vector/Black/Block/Wall/v_CornerDown_R_01";

        private const string FLOOR = "Images/Vector/Black/Block/Floor/v_Floor_01";
        private const string FLOOR_LONG = "Images/Vector/Black/Block/Floor/v_LongFloor_01";

        private const string WALL_L = "Images/Vector/Black/Block/Wall/v_Wall_L_01";
        private const string WALL_R = "Images/Vector/Black/Block/Wall/v_Wall_R_01";
        private const string WALL_L_LONG = "Images/Vector/Black/Block/Wall/v_LongCornerUp_L_01";
        private const string WALL_R_LONG = "Images/Vector/Black/Block/Wall/v_LongCornerUp_R_01";

        private const string BLACK_FILL = "Images/Vector/Black/Block/v_black";

        // ================= MENU =================

        [MenuItem("Vectorier/Tools/Quick Actions/Build Surface/All Surface", false, 36)]
        private static void BuildAll() => Build(SurfaceMode.All);

        [MenuItem("Vectorier/Tools/Quick Actions/Build Surface/Floor", false, 37)]
        private static void BuildFloor() => Build(SurfaceMode.Floor);

        [MenuItem("Vectorier/Tools/Quick Actions/Build Surface/Left Wall", false, 38)]
        private static void BuildLeftWall() => Build(SurfaceMode.LeftWall);

        [MenuItem("Vectorier/Tools/Quick Actions/Build Surface/Right Wall", false, 39)]
        private static void BuildRightWall() => Build(SurfaceMode.RightWall);

        // ================= CORE =================

        private static void Build(SurfaceMode mode)
        {
            List<SpriteRenderer> renderers = new();

            foreach (GameObject gameObject in Selection.gameObjects)
            {
                SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer && spriteRenderer.sprite)
                    renderers.Add(spriteRenderer);
            }

            if (renderers.Count == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++)
                bounds.Encapsulate(renderers[i].bounds);

            CreateSurface(bounds, mode);
        }

        // ================= BUILD =================

        private static void CreateSurface(Bounds bounds, SurfaceMode mode)
        {
            Sprite cornerUpLeft = LoadRandom(CORNER_UP_L);
            Sprite cornerUpRight = LoadRandom(CORNER_UP_R);
            Sprite cornerDownLeft = LoadRandom(CORNER_DOWN_L);
            Sprite cornerDownRight = LoadRandom(CORNER_DOWN_R);

            Sprite floor = LoadRandom(FLOOR);
            Sprite floorLong = LoadRandom(FLOOR_LONG);

            Sprite wallLeft = LoadRandom(WALL_L);
            Sprite wallRight = LoadRandom(WALL_R);
            Sprite wallLLong = LoadRandom(WALL_L_LONG);
            Sprite wallRLong = LoadRandom(WALL_R_LONG);

            Sprite fill = Load(BLACK_FILL);

            GameObject root = new($"QuickSurface_{mode}");
            Undo.RegisterCreatedObjectUndo(root, "Create QuickSurface");

            Vector3 topLeft = new(bounds.min.x, bounds.max.y, 0f);
            Vector3 topRight = new(bounds.max.x, bounds.max.y, 0f);
            Vector3 bottomLeft = new(bounds.min.x, bounds.min.y, 0f);
            Vector3 bottomRight = new(bounds.max.x, bounds.min.y, 0f);

            GameObject upLeft = null, upRight = null, downLeft = null, downRight = null;

            if (mode is SurfaceMode.All or SurfaceMode.Floor or SurfaceMode.LeftWall)
                upLeft = CreateSprite(cornerUpLeft, topLeft, root.transform);

            if (mode is SurfaceMode.All or SurfaceMode.Floor or SurfaceMode.RightWall)
                upRight = CreateSprite(cornerUpRight, topRight + Vector3.left * cornerUpRight.bounds.size.x, root.transform);

            if (mode is SurfaceMode.All or SurfaceMode.LeftWall)
                downLeft = CreateSprite(cornerDownLeft, bottomLeft + Vector3.up * cornerDownLeft.bounds.size.y, root.transform);

            if (mode is SurfaceMode.All or SurfaceMode.RightWall)
                downRight = CreateSprite(cornerDownRight, bottomRight + new Vector3(-cornerDownRight.bounds.size.x, cornerDownRight.bounds.size.y), root.transform);

            // -------- FLOOR (overlap-safe) --------
            if (mode is SurfaceMode.All or SurfaceMode.Floor)
            {
                float start = upLeft.transform.position.x + cornerUpLeft.bounds.size.x;
                float end = upRight.transform.position.x;

                float x = start;
                float maxTile = Mathf.Max(floor.bounds.size.x, floorLong.bounds.size.x);

                while (x + maxTile < end)
                {
                    bool useLong = x + Load(FLOOR_LONG).bounds.size.x < end;

                    Sprite chosen = useLong ? LoadRandom(FLOOR_LONG) : LoadRandom(FLOOR);

                    CreateSprite(chosen, new Vector3(x, topLeft.y, 0f), root.transform);
                    x += chosen.bounds.size.x;
                }

                // final overlapping tile
                Sprite final = floorLong && end - maxTile >= start ? floorLong : floor;
                CreateSprite(final, new Vector3(end - final.bounds.size.x, topLeft.y, 0f), root.transform);
            }

            // -------- LEFT WALL --------
            if (mode is SurfaceMode.All or SurfaceMode.LeftWall)
            {
                float topY = upLeft.transform.position.y - cornerUpLeft.bounds.size.y;
                float bottomY = downLeft.transform.position.y;

                float shortH = wallLeft.bounds.size.y;
                float longH = wallLLong.bounds.size.y;

                float totalHeight = topY - bottomY;

                // Prefer long tiles, but guarantee coverage
                int longCount = Mathf.FloorToInt(totalHeight / longH);
                float used = longCount * longH;

                int shortCount = Mathf.CeilToInt((totalHeight - used) / shortH);

                float y = topY;

                // Place long tiles first (top to bottom)
                for (int i = 0; i < longCount; i++)
                {
                    CreateSprite(LoadRandom(WALL_L_LONG), new Vector3(topLeft.x, y, 0f), root.transform);
                    y -= longH;
                }

                // Place short tiles
                for (int i = 0; i < shortCount; i++)
                {
                    CreateSprite(LoadRandom(WALL_L), new Vector3(topLeft.x, y, 0f), root.transform);
                    y -= shortH;
                }
            }

            // -------- RIGHT WALL --------
            if (mode is SurfaceMode.All or SurfaceMode.RightWall)
            {
                float topY = upRight.transform.position.y - cornerUpRight.bounds.size.y;
                float bottomY = downRight.transform.position.y;
                float x = topRight.x - wallRight.bounds.size.x;

                float shortH = wallRight.bounds.size.y;
                float longH = wallRLong.bounds.size.y;

                float totalHeight = topY - bottomY;

                int longCount = Mathf.FloorToInt(totalHeight / longH);
                float used = longCount * longH;

                int shortCount = Mathf.CeilToInt((totalHeight - used) / shortH);

                float y = topY;

                for (int i = 0; i < longCount; i++)
                {
                    CreateSprite(LoadRandom(WALL_R_LONG), new Vector3(x, y, 0f), root.transform);
                    y -= longH;
                }

                for (int i = 0; i < shortCount; i++)
                {
                    CreateSprite(LoadRandom(WALL_R), new Vector3(x, y, 0f), root.transform);
                    y -= shortH;
                }
            }

            // -------- FILL --------
            if (mode == SurfaceMode.All)
            {
                float top = upLeft.transform.position.y - cornerUpLeft.bounds.size.y;
                float bottom = downLeft.transform.position.y;
                float left = upLeft.transform.position.x + cornerUpLeft.bounds.size.x;
                float right = upRight.transform.position.x;

                GameObject mid = CreateSprite(fill, new Vector3(left, top, 1f), root.transform);
                mid.transform.localScale = new Vector3((right - left) / fill.bounds.size.x, (top - bottom) / fill.bounds.size.y, 1f);
            }

            Selection.activeGameObject = root;
        }

        // ================= HELPERS =================

        private static Sprite Load(string path)
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            if (!sprite)
                Debug.LogError($"QuickSurface missing: Resources/{path}.png");
            return sprite;
        }

        private static Sprite LoadRandom(string basePath, int maxVariants = 3)
        {
            List<Sprite> candidates = new();

            for (int i = 1; i <= maxVariants; i++)
            {
                string path = basePath.Replace("_01", $"_{i:00}");
                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite)
                    candidates.Add(sprite);
            }

            if (candidates.Count == 0)
            {
                Debug.LogError($"QuickSurface missing variants for: Resources/{basePath}.png");
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private static GameObject CreateSprite(Sprite sprite, Vector3 position, Transform parent)
        {
            GameObject gameObject = new(sprite.name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Surface Part");
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;
            gameObject.AddComponent<SpriteRenderer>().sprite = sprite;
            return gameObject;
        }
    }
}
