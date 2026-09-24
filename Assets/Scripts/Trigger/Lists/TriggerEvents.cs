using System;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using Vectorier.Component;

namespace Vectorier.Trigger
{
    [Serializable]
    public class TriggerEvents : TriggerListBase
    {
        public TriggerEvents() : base()
        {
        }

        public TriggerEvents(XmlNode loop) : base()
        {
            XmlNode events = loop?["Events"];
            _IsTemplate = events?.Attributes?["Template"] != null;
            if (events != null)
            {
                XmlAttribute template = events.Attributes?["Template"];
                if (template != null)
                {
                    _TemplatePath = template.Value;
                    XmlNode triggerEventBlock = TemplateManager.GetEventBlock(template.Value);
                    ParseEventBlock(triggerEventBlock, true);
                    return;
                }

                ParseEventBlock(events, false);
            }
        }

        private void ParseEventBlock(XmlNode events, bool IsTemplate)
        {
            if (events == null)
                return;

            if (events.Attributes?["Template"] != null)
            {
                ParseEventBlock(TemplateManager.GetEventBlock(events.Attributes["Template"].Value), true);
                return;
            }

            foreach (XmlNode tEvent in events)
            {
                if (tEvent != null)
                    List.Add(new TriggerEvent(tEvent, List, IsTemplate));
            }
        }

        protected override string HeaderTitle => "Events";

        protected override string EmptyTemplateMessage => "No events found in the selected template.";

        protected override void DisplayTemplateHeader(Rect rect, Rect labelField)
        {
            const float outerSpacing = 5f;

            Rect fieldRect = new Rect(labelField.x + labelField.width + outerSpacing, rect.y, rect.width - labelField.width - outerSpacing, rect.height);

            const float loopWidth = 130f;
            const float importButtonWidth = 50f;
            const float cancelButtonWidth = 50f;

            float totalButtonsWidth = loopWidth + importButtonWidth + cancelButtonWidth + outerSpacing * 3f;

            float nameWidth = Mathf.Max(fieldRect.width - totalButtonsWidth, 50f);

            Rect nameRect = new Rect(fieldRect.x, fieldRect.y, nameWidth, fieldRect.height);
            Rect loopRect = new Rect(nameRect.x + nameRect.width + outerSpacing, fieldRect.y, loopWidth, fieldRect.height);
            Rect importButtonRect = new Rect(loopRect.x + loopRect.width + outerSpacing, fieldRect.y, importButtonWidth, fieldRect.height);
            Rect cancelButtonRect = new Rect(importButtonRect.x + importButtonRect.width + outerSpacing, fieldRect.y, cancelButtonWidth, fieldRect.height);

            _IsExpanded = EditorGUI.Foldout(labelField, _IsExpanded, "Events Template : ", EditorStyles.foldout);

            string TemplateName = TemplateManager.TemplateEnum(nameRect, _TemplateName);
            string LoopName = TemplateManager.LoopEnum(loopRect, _TemplateName, _LoopName);

            _TemplatePath = $"{TemplateName}.{LoopName}";

            if (TemplateName != _TemplateName || LoopName != _LoopName)
            {
                _TemplateName = TemplateName;
                _LoopName = LoopName;
                XmlNode triggerEventBlock = TemplateManager.GetEventBlock(_TemplatePath);
                List.Clear();
                ParseEventBlock(triggerEventBlock, true);
            }

            // Import button: load and parse template into the list
            if (GUI.Button(importButtonRect, "Import"))
            {
                _IsTemplate = false;

                if (string.IsNullOrEmpty(_TemplatePath))
                {
                    Debug.LogWarning("Template name is empty");
                    return;
                }

                XmlNode triggerEventBlock = TemplateManager.GetEventBlock(_TemplatePath);
                if (triggerEventBlock == null)
                {
                    Debug.LogWarning($"Template '{_TemplatePath}' not found");
                    return;
                }

                List.Clear();
                ParseEventBlock(triggerEventBlock, false);
            }

            // Cancel button: revert to template mode without importing
            if (GUI.Button(cancelButtonRect, "Cancel"))
            {
                _TemplateName = string.Empty;
                _TemplatePath = string.Empty;
                _LoopName = string.Empty;

                List.Clear();
                _IsTemplate = false;
            }
        }

        public bool AppendToElement(XmlElement parentElement, XmlDocument doc)
        {
            if (parentElement == null || doc == null)
                return false;

            XmlElement events = doc.CreateElement("Events");
            if (_IsTemplate && !string.IsNullOrEmpty(_TemplatePath))
            {
                events.SetAttribute("Template", _TemplatePath);
                return true;
            }
            
            bool success = false;
            foreach (TriggerNode node in List)
            {
                if(node == null)
                    continue;
                if(node is TriggerEvent triggerEvent)
                {
                    success = triggerEvent.AppendToElement(events, doc);
                    if (!success)
                    {
                        Debug.LogWarning($"Failed to append event '{triggerEvent?.Name}' to XML.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Node '{node?.Name}' is not a TriggerEvent and will be skipped.");
                }
            }

            parentElement.AppendChild(events);
            return true;
        }
    }
}
