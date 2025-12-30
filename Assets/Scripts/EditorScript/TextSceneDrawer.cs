using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Vectorier.Component;

namespace Vectorier.EditorScript
{
    [InitializeOnLoad]
    public static class TextSceneDrawer
    {
        // ================= CACHED DATA ================= //
        private class CachedObject
        {
            public GameObject GameObject;
            public SpriteRenderer SpriteRenderer;
            public string CleanName;
        }

        private static readonly List<CachedObject> CachedObjects = new();
        private static readonly GUIStyle SharedTextStyle = new();
        private static bool NeedsCacheRefresh = true;

        private static bool showOutline;
        private static bool showPlatformOutline;
        private static bool showTriggerText;
        private static bool showAreaText;

        private static readonly Color PlatformOutlineColor = new Color(0f, 0f, 1f, 1f);
        private static readonly Color TriggerOutlineColor = new Color(1f, 0.647f, 0f, 1f);

        // -------- OUTLINE BUFFER --------
        private static readonly Vector3[] OutlinePoints = new Vector3[5];

        // ================= INIT ================= //

        static TextSceneDrawer()
        {
            EditorApplication.hierarchyChanged += MarkCacheDirty;

            SharedTextStyle.fontSize = 10;
            SharedTextStyle.wordWrap = true;
            SharedTextStyle.alignment = TextAnchor.UpperLeft;
            SharedTextStyle.normal = new GUIStyleState();

            SceneView.duringSceneGui += DrawInSceneView;
        }

        private static void MarkCacheDirty() => NeedsCacheRefresh = true;

        // ================= CACHE SYSTEM ================= //
        private static void RefreshCacheIfNeeded()
        {
            if (!NeedsCacheRefresh) return;

            CachedObjects.Clear();

            foreach (var trigger in Object.FindObjectsByType<TriggerComponent>(FindObjectsSortMode.None))
                CacheSceneObject(trigger.gameObject);

            foreach (var area in GameObject.FindGameObjectsWithTag("Area"))
                CacheSceneObject(area);

            foreach (var platform in GameObject.FindGameObjectsWithTag("Platform"))
                CacheSceneObject(platform);

            foreach (var comment in GameObject.FindGameObjectsWithTag("Comment"))
                CacheSceneObject(comment);

            NeedsCacheRefresh = false;
        }

        private static void CacheSceneObject(GameObject obj)
        {
            var sr = obj.GetComponent<SpriteRenderer>();

            CachedObjects.Add(new CachedObject
            {
                GameObject = obj,
                SpriteRenderer = sr,
                CleanName = CleanObjectName(obj.name)
            });
        }

        private static string CleanObjectName(string name)
        {
            name = name.Replace("(Clone)", "");
            return Regex.Replace(name, @" \(\d+\)$", "");
        }

        // ================= FADE ================= //
        private static float ComputeFade(SceneView sceneView, Vector3 position)
        {
            var camera = sceneView.camera;
            if (camera == null) return 1f;

            const float baseScale = 100f;

            if (camera.orthographic)
            {
                float size = camera.orthographicSize;
                return Mathf.Clamp01(1f - (size - 5f * baseScale) / (10f * baseScale));
            }

            float distance = Vector3.Distance(camera.transform.position, position);
            return Mathf.Clamp01(1f - (distance - 10f * baseScale) / (30f * baseScale));
        }

        // ================= SCENEVIEW RENDERING ================= //
        private static void DrawInSceneView(SceneView sceneView)
        {
            LoadPrefs();
            RefreshCacheIfNeeded();

            Camera camera = sceneView.camera;
            if (camera == null) return;

            Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(camera);

            foreach (var entry in CachedObjects)
            {
                if (entry.GameObject == null)
                    continue;

                // disabled objects
                if (!entry.GameObject.activeInHierarchy)
                    continue;

                // objects hidden hideFlags
                if ((entry.GameObject.hideFlags & HideFlags.HideInHierarchy) != 0)
                    continue;

                // Scene hidden objects (eye icon)
                if (SceneVisibilityManager.instance.IsHidden(entry.GameObject))
                    continue;

                Vector3 worldPos = entry.GameObject.transform.position;

                if (entry.SpriteRenderer != null &&
                    !GeometryUtility.TestPlanesAABB(frustum, entry.SpriteRenderer.bounds))
                    continue;

                float fade = ComputeFade(sceneView, worldPos);
                if (fade < 0.01f) continue;

                bool isPlatform = entry.GameObject.CompareTag("Platform");
                bool isTrigger = entry.GameObject.GetComponent<TriggerComponent>() != null;

                if (showOutline && entry.SpriteRenderer != null)
                    DrawSpriteOutline(entry.SpriteRenderer, fade, isPlatform, isTrigger);

                DrawLabel(entry, worldPos, fade);
            }
        }
        
        // ================= LABEL ================= //
        private static void DrawLabel(CachedObject entry, Vector3 worldPos, float fade)
        {
            Handles.BeginGUI();

            SharedTextStyle.normal.textColor = new Color(0, 0, 0, fade);

            bool isTrigger = entry.GameObject.GetComponent<TriggerComponent>() != null;
            bool isArea = entry.GameObject.CompareTag("Area");
            bool isPlatform = entry.GameObject.CompareTag("Platform");

            if ((isTrigger && !showTriggerText) || (isArea && !showAreaText) || isPlatform)
            {
                Handles.EndGUI();
                return;
            }

            if (entry.SpriteRenderer != null)
                DrawLabelWithinSprite(entry);
            else
                DrawLabelForWorldPosition(entry, worldPos);

            Handles.EndGUI();
        }

        private static void DrawLabelWithinSprite(CachedObject entry)
        {
            Bounds bound = entry.SpriteRenderer.bounds;

            Vector3 center = bound.center;
            Vector3 extent = bound.extents;

            Vector2 topLeft = HandleUtility.WorldToGUIPoint(new Vector3(center.x - extent.x, center.y + extent.y));
            Vector2 bottomRight = HandleUtility.WorldToGUIPoint(new Vector3(center.x + extent.x, center.y - extent.y));

            float x = topLeft.x;
            float y = topLeft.y;
            float width = bottomRight.x - topLeft.x;
            float height = Mathf.Abs(bottomRight.y - topLeft.y);

            GUI.BeginGroup(new Rect(x, y, width, height));
            GUI.Label(new Rect(0, 0, width, height), entry.CleanName, SharedTextStyle);
            GUI.EndGroup();
        }

        private static void DrawLabelForWorldPosition(CachedObject entry, Vector3 pos)
        {
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(pos + Vector3.up * 0.5f);
            GUI.Label(new Rect(guiPos.x, guiPos.y, 200f, 40f), entry.CleanName, SharedTextStyle);
        }

        // ================= OUTLINE ================= //
        private static void DrawSpriteOutline(SpriteRenderer sr, float fade, bool isPlatform, bool isTrigger)
        {
            if (isPlatform && !showPlatformOutline)
                return;

            Bounds bound = sr.bounds;
            Vector3 center = bound.center;
            Vector3 extent = bound.extents;

            OutlinePoints[0] = new Vector3(center.x - extent.x, center.y + extent.y);
            OutlinePoints[1] = new Vector3(center.x + extent.x, center.y + extent.y);
            OutlinePoints[2] = new Vector3(center.x + extent.x, center.y - extent.y);
            OutlinePoints[3] = new Vector3(center.x - extent.x, center.y - extent.y);
            OutlinePoints[4] = OutlinePoints[0];

            Color outlineColor;

            if (isTrigger && sr.color == new Color(1f, 1f, 0f, 1f))
                outlineColor = TriggerOutlineColor;
            else
                outlineColor = isPlatform ? PlatformOutlineColor : sr.color;

            outlineColor.a = fade;

            Handles.color = outlineColor;
            Handles.DrawAAPolyLine(4f, OutlinePoints);
        }

        private static void LoadPrefs()
        {
            showOutline = EditorPrefs.GetBool("Vectorier_ShowOutline", true);
            showPlatformOutline = EditorPrefs.GetBool("Vectorier_ShowPlatformOutline", false);
            showTriggerText = EditorPrefs.GetBool("Vectorier_ShowTriggerText", true);
            showAreaText = EditorPrefs.GetBool("Vectorier_ShowAreaText", false);
        }
    }
}
