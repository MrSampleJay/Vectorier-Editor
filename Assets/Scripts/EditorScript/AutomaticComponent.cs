using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Vectorier.Component;

namespace Vectorier.EditorScript
{
    [InitializeOnLoad]
    public static class AutomaticComponent
    {
        // Name to Component rule map (lowercase exact match)
        private static readonly Dictionary<string, System.Action<GameObject>> componentRules =
            new Dictionary<string, System.Action<GameObject>>
        {
            { "pill", AddItemBonus },
            { "coin", AddItemCoin },
            { "trigger", AddTrigger }
        };

        static AutomaticComponent()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
                    continue;

                // Ignore imported objects
                if (gameObject.GetComponent<AutomaticTagIgnore>() != null)
                    continue;

                // Skip if this object already has either component
                if (gameObject.GetComponent<ItemComponent>() != null ||
                    gameObject.GetComponent<TriggerComponent>() != null)
                {
                    continue;
                }

                string nameLower = gameObject.name.ToLower();

                if (componentRules.TryGetValue(nameLower, out var applyRule))
                {
                    applyRule.Invoke(gameObject);
                }
            }
        }

        // -------- METHOD --------

        private static void AddItemBonus(GameObject gameObject)
        {
            var component = gameObject.AddComponent<ItemComponent>();
            component.Type = ItemComponent.ItemType.Bonus;
            component.Score = 10;
            component.Radius = 80f;
            component.GroupId = 1;
        }

        private static void AddItemCoin(GameObject gameObject)
        {
            var component = gameObject.AddComponent<ItemComponent>();
            component.Type = ItemComponent.ItemType.Coin;
            component.Score = 10;
            component.Radius = 80f;
            component.GroupId = 1;
        }

        private static void AddTrigger(GameObject gameObject)
        {
            gameObject.AddComponent<TriggerComponent>();
        }
    }
}
