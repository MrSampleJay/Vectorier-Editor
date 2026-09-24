using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Vectorier.Trigger
{
    [Serializable]
    public class TriggerListBase
    {
        public enum Structure
        {
            XML,
            YAML
        }

        public static Structure Mode = Structure.XML;

        [SerializeReference]
        public List<TriggerNode> List;

        [SerializeField]
        protected bool _IsTemplate;

        [SerializeField]
        protected string _TemplatePath;

        [NonSerialized]
        public ReorderableList ListDisplay;

        [SerializeField]
        protected bool _IsExpanded;

        [SerializeField]
        protected string _TemplateName;

        [SerializeField]
        protected string _LoopName;

        public TriggerListBase()
        {
            List = new();
            _IsTemplate = false;
            _TemplatePath = string.Empty;
        }

        public virtual void Display(ReorderableList list)
        {
            if (list == null)
                return;

            DisplayHeader(EditorGUILayout.GetControlRect());
            if (_IsExpanded)
            {
                if (_IsTemplate && List.Count == 0)
                {
                    string msg = EmptyTemplateMessage;
                    if (!string.IsNullOrEmpty(msg))
                        EditorGUILayout.HelpBox(msg, MessageType.Info);
                    return;
                }
                using (new EditorGUI.DisabledScope(_IsTemplate))
                {
                    list.DoLayoutList();
                }
            }
        }

        public void DisplayHeader(Rect rect)
        {
            const float labelWidth = 130f;
            const float useTemplateButtonWidth = 110f;

            Rect labelField = new Rect(rect.x, rect.y, labelWidth, rect.height);

            if (_IsTemplate)
            {
                DisplayTemplateHeader(rect, labelField);
            }
            else
            {
                _IsExpanded = EditorGUI.Foldout(labelField, _IsExpanded, HeaderTitle, EditorStyles.foldout);

                Rect useTemplateButton = new Rect(rect.x + rect.width - useTemplateButtonWidth, rect.y, useTemplateButtonWidth, rect.height);
                if (GUI.Button(useTemplateButton, "Use Template"))
                {
                    _IsTemplate = true;
                    List.Clear();
                }
            }
        }

        protected virtual void DisplayTemplateHeader(Rect rect, Rect labelField)
        {
            // Default does nothing. Derived classes should override to implement template UI.
        }

        protected virtual string HeaderTitle => "Items";

        protected virtual string EmptyTemplateMessage => null;

        // New virtual Validate method as requested
        public virtual bool Validate() => true;
    }
}
