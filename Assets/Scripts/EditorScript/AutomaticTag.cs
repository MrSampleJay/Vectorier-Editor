using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Vectorier.EditorScript
{
    [InitializeOnLoad]
    public static class AutomaticTag
    {
        // Dictionary where each key is an exact match (lowercase) for GameObject name
        // Values are the tags to apply.
        private static readonly Dictionary<string, string> directNameTags = new Dictionary<string, string>
        {
            { "trigger", "Trigger" },
            { "platform", "Platform" },
            { "trapezoid_type1", "Trapezoid" },
            { "trapezoid_type2", "Trapezoid" },
            { "pill", "Item" },
            { "coin", "Item" }
        };

        static AutomaticTag()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            foreach (GameObject gameObject in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
                    continue;

                // ignore imported objects
                if (gameObject.GetComponent<AutomaticTagIgnore>() != null)
                    continue;

                // Tag only if untagged (means newly placed or not processed yet)
                if (!gameObject.CompareTag("Untagged"))
                    continue;

                string nameLower = gameObject.name.ToLower();

                if (directNameTags.TryGetValue(nameLower, out string matchTag))
                {
                    TryAssignTag(gameObject, matchTag);
                    continue;
                }

                SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    TryAssignTag(gameObject, "Image");
                }
            }
        }

        private static void TryAssignTag(GameObject gameObject, string tag)
        {
            if (!TagExists(tag))
                return;

            gameObject.tag = tag;
        }

        private static bool TagExists(string tag)
        {
            foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
            {
                if (t == tag)
                    return true;
            }
            return false;
        }
    }
}
