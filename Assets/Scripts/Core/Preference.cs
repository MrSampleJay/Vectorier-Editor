using UnityEngine;
using UnityEditor;

namespace Vectorier.Core.Preferences
{
    public class Preference : EditorWindow
    {
        // ================= EDITOR_PREFS KEY ================= //

        private const string KEY_SHOW_OUTLINE = "Vectorier_ShowOutline";
        private const string KEY_SHOW_PLATFORM_OUTLINE = "Vectorier_ShowPlatformOutline";
        private const string KEY_SHOW_TRIGGER_TEXT = "Vectorier_ShowTriggerText";
        private const string KEY_SHOW_AREA_TEXT = "Vectorier_ShowAreaText";

        // ================= CACHED VALUES ================= //

        private bool showOutline;
        private bool showPlatformOutline;
        private bool showTriggerText;
        private bool showAreaText;


        // ================= MAIN ================= //

        [MenuItem("Vectorier/Preferences...", false, 31)]
        private static void OpenWindow()
        {
            var window = GetWindow<Preference>("Preferences");
            window.minSize = new Vector2(360, 240);
            window.LoadPrefs();
        }

        private void OnEnable()
        {
            LoadPrefs();
        }

        private void OnGUI()
        {
            var subHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            EditorGUILayout.LabelField("Scene", subHeaderStyle);
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("Trigger & Area", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            showOutline = EditorGUILayout.Toggle("Show Outline", showOutline);
            showPlatformOutline = EditorGUILayout.Toggle("Show Platform Outline", showPlatformOutline);
            showTriggerText = EditorGUILayout.Toggle("Show Trigger Text", showTriggerText);
            showAreaText = EditorGUILayout.Toggle("Show Area Text", showAreaText);

            if (EditorGUI.EndChangeCheck())
            {
                SavePrefs();
            }
        }

        private void LoadPrefs()
        {
            showOutline = EditorPrefs.GetBool(KEY_SHOW_OUTLINE, true);
            showPlatformOutline = EditorPrefs.GetBool(KEY_SHOW_PLATFORM_OUTLINE, false);
            showTriggerText = EditorPrefs.GetBool(KEY_SHOW_TRIGGER_TEXT, true);
            showAreaText = EditorPrefs.GetBool(KEY_SHOW_AREA_TEXT, false);
        }

        private void SavePrefs()
        {
            EditorPrefs.SetBool(KEY_SHOW_OUTLINE, showOutline);
            EditorPrefs.SetBool(KEY_SHOW_PLATFORM_OUTLINE, showPlatformOutline);
            EditorPrefs.SetBool(KEY_SHOW_TRIGGER_TEXT, showTriggerText);
            EditorPrefs.SetBool(KEY_SHOW_AREA_TEXT, showAreaText);
        }
    }
}
