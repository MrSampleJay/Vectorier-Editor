using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Xml;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Vectorier.Trigger
{
    public enum LoopListQueuedAction
    {
        Delete,
        MoveUp,
        MoveDown,
        None
    }

    [Serializable]
    public class TriggerLoop
    {
        [SerializeField]
        string _Name;

        [SerializeField]
        string _TemplatePath;

        [NonSerialized]
        List<TriggerLoop> _ParentList;

        [SerializeField]
        TriggerActions _Actions = new();

        public List<TriggerNode> Actions {
            get => _Actions?.List ?? new List<TriggerNode>();
            set
            {
                _Actions ??= new TriggerActions();
                _Actions.List = value ?? new List<TriggerNode>();
            }
        }

        [SerializeReference]
        TriggerEvents _Events = new();

        public List<TriggerNode> Events
        {
            get => _Events?.List ?? new List<TriggerNode>();
            set
            {
                _Events ??= new TriggerEvents();
                _Events.List = value ?? new List<TriggerNode>();
            }
        }

        [NonSerialized]
        public LoopListQueuedAction ListAction;

        [SerializeField]
        private bool _isTemplate;

        [SerializeField]
        private bool _Collapsed;

        public TriggerLoop(List<TriggerLoop> parentList)
        {
            _ParentList = parentList;
            _Name = string.Empty;
            _Events = new TriggerEvents();
            Actions = new();
        }

        public TriggerLoop(XmlNode loop, List<TriggerLoop> parentList)
        {
            XmlAttribute name = loop?.Attributes?["Name"];
            _Name = name != null ? name.Value : string.Empty;
            _ParentList = parentList;
            _isTemplate = loop?.Attributes?["Template"] != null;
            if (_isTemplate)
            {
                _TemplatePath = loop.Attributes?["Template"]?.Value;
                loop = TemplateManager.GetLoop(_TemplatePath);
            }
            _Events = new TriggerEvents(loop);
            _Actions = new TriggerActions(loop, this);
        }

        public void Display(ReorderableList ActionList, ReorderableList EventList)
        {
            EditorGUILayout.BeginVertical("box");
            ListAction = DisplayHeader();

            TriggerNode.CheckTags(_Events?.List ?? new List<TriggerNode>());
            TriggerNode.CheckTags(Actions);

            if (_Collapsed) 
            {
                EditorGUILayout.EndVertical();
                return;
            }

            using (new EditorGUI.DisabledScope(_isTemplate))
            {
                _Events.Display(EventList);
                _Actions.Display(ActionList);
            }
            EditorGUILayout.EndVertical();
        }

        public void AppendToElement(XmlElement parentElement, XmlDocument doc)
        {
            if (parentElement == null || doc == null)
                return;

            XmlElement loop = doc.CreateElement("Loop");
            if (_Name != null && !string.IsNullOrEmpty(_Name))
                loop.SetAttribute("Name", _Name ?? string.Empty);

            if (_isTemplate && !string.IsNullOrEmpty(_TemplatePath))
                loop.SetAttribute("Template", _TemplatePath);

            if(!_Events.AppendToElement(loop, doc))
            {
                Debug.LogWarning($"Failed to append events for loop '{_Name}' to XML.");
            }
            if(!_Actions.AppendToElement(loop, doc))
            {
                Debug.LogWarning($"Failed to append actions for loop '{_Name}' to XML.");
            }

            parentElement.AppendChild(loop);
        }

        // RebuildRuntimeReferences removed: runtime references are rebuilt by TriggerComponent instead.

        private LoopListQueuedAction DisplayHeader()
        {
            GUILayout.BeginHorizontal();

            Rect headerRect = EditorGUILayout.GetControlRect();

            Rect colorRect = new Rect(headerRect.x, headerRect.y, headerRect.width, headerRect.height);
            EditorGUI.DrawRect(colorRect, new Color(0, 0, 0, 0.5f));

            Rect dropdownRect = new Rect(headerRect.x, headerRect.y, 20f, headerRect.height);
            headerRect.width -= 20f;
            headerRect.x += 20f;

            Event e = Event.current;

            if (GUI.Button(dropdownRect, _Collapsed ? "▶" : "▼"))
            {
                _Collapsed = !_Collapsed;
            }

            // Name
            if (headerRect.Contains(e.mousePosition) && !_isTemplate)
            {
                _Name = EditorGUI.TextField(headerRect, _Name);
            }
            else
            {
                string text = string.IsNullOrEmpty(_Name) ? "Loop" : _Name;
                text = _isTemplate ? $"Loop Template : {_TemplatePath}" : text;
                EditorGUI.LabelField(headerRect, text);
            }

            // List Controls
            LoopListQueuedAction result = LoopListQueuedAction.None;
            if (_ParentList != null)
            {
                int index = _ParentList.IndexOf(this);
                if (GUILayout.Button("-", GUILayout.Width(20f)))
                {
                    result = LoopListQueuedAction.Delete;
                }
                if (index > 0 && GUILayout.Button("▲", GUILayout.Width(20f)))
                {
                    result = LoopListQueuedAction.MoveUp;
                }
                if (index < _ParentList.Count - 1 && GUILayout.Button("▼", GUILayout.Width(20f)))
                {
                    result = LoopListQueuedAction.MoveDown;
                }
            }

            GUILayout.EndHorizontal();
            return result;
        }
    }
}
