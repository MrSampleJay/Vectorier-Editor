using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using static Vectorier.Trigger.TriggerNode;

namespace Vectorier.Trigger
{
    [Serializable]
    public class TriggerActions : TriggerListBase
    {
        [NonSerialized]
        private TriggerLoop _ParentLoop;

        private int _ExportNestedLevel;

        public TriggerActions() : base()
        {
        }

        public TriggerActions(XmlNode loop, TriggerLoop parent) : base()
        {
            _ParentLoop = parent;
            XmlNode actions = loop?["Actions"];
            _IsTemplate = actions?.Attributes?["Template"] != null;
            if (actions != null)
            {
                XmlAttribute template = actions.Attributes?["Template"];
                if (template != null)
                {
                    _TemplatePath = template.Value;
                    XmlNode triggerActionBlock = TemplateManager.GetActionBlock(template.Value);
                    ParseActionBlock(triggerActionBlock);
                    return;
                }

                ParseActionBlock(actions);
            }
        }

        private void ParseActionBlock(XmlNode actions)
        {
            if (actions == null)
                return;

            if(actions.Attributes?["Template"] != null)
            { 
                ParseActionBlock(TemplateManager.GetActionBlock(actions.Attributes["Template"].Value));
                return;
            }

            foreach (XmlNode triggerAction in actions)
            {
                if (triggerAction == null)
                    continue;

                if (triggerAction.Name == "Choose")
                {
                    List.Add(new TriggerAction(triggerAction, List, _IsTemplate));
                    foreach (XmlNode action in triggerAction.ChildNodes)
                    {
                        List.Add(new TriggerAction(action, List, _IsTemplate));
                    }
                    List.Add(new TriggerNode("Choose", List, TagType.End));
                }
                else
                {
                    List.Add(new TriggerAction(triggerAction, List, _IsTemplate));
                }
            }
        }

        protected override string HeaderTitle => "Actions";

        protected override string EmptyTemplateMessage => "No actions found in the selected template.";

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

            _IsExpanded = EditorGUI.Foldout(labelField, _IsExpanded, "Actions Template : ", EditorStyles.foldout);

            // Template name and loop selector
            EditorGUI.BeginChangeCheck();
            _TemplateName = TemplateManager.TemplateEnum(nameRect, _TemplateName);
            _LoopName = TemplateManager.LoopEnum(loopRect, _TemplateName, _LoopName);

            _TemplatePath = $"{_TemplateName}.{_LoopName}";

            if (EditorGUI.EndChangeCheck())
            {
                XmlNode triggerActionBlock = TemplateManager.GetActionBlock(_TemplatePath);
                List.Clear();
                ParseActionBlock(triggerActionBlock);
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

                XmlNode triggerActionBlock = TemplateManager.GetActionBlock(_TemplatePath);
                if (triggerActionBlock == null)
                {
                    Debug.LogWarning($"Template '{_TemplatePath}' not found");
                    return;
                }

                List.Clear();
                ParseActionBlock(triggerActionBlock);
                GUI.changed = true;
            }

            // Cancel button: revert to template mode without importing (keeps previous behavior)
            if (GUI.Button(cancelButtonRect, "Cancel"))
            {
                _TemplateName = string.Empty;
                _TemplatePath = string.Empty;
                _LoopName = string.Empty;

                List.Clear();
                _IsTemplate = false;

                GUI.changed = true;
            }
        }

        public bool AppendToElement(XmlElement parentElement, XmlDocument doc)
        {
            if (parentElement == null || doc == null)
                return false;
            XmlElement actions = doc.CreateElement("Actions");
            if (_TemplatePath != null && !string.IsNullOrEmpty(_TemplatePath))
            {
                actions.SetAttribute("Template", _TemplatePath);
                return true;
            }

            AppendIterate(actions, doc);
            
            parentElement.AppendChild(actions);
            return true;
        }

        private void AppendIterate(XmlElement parent, XmlDocument doc)
        {
            bool success = true;
            List<XmlElement> listParents = new() { parent };
            foreach (TriggerNode node in List)
            {
                int lastParentIndex = listParents.Count - 1;
                if(lastParentIndex < 0)
                {
                    Debug.LogError("Error Parsing Actions");
                }
                XmlElement newParentElement = listParents[lastParentIndex];
                if (node is TriggerAction action)
                {
                    if (node.Type == TagType.Start)
                    {
                        success = action.AppendToElement(newParentElement, doc);
                        if (success)
                        {
                            listParents.Add((XmlElement)newParentElement.LastChild);
                        }
                    }
                    else if (node.Type == TagType.End)
                    {
                        listParents.RemoveAt(lastParentIndex);
                    }
                    else
                    {
                        // Regular action node (no special tag handling)
                        success = action.AppendToElement(newParentElement, doc);
                        if (!success)
                        {
                            Debug.LogWarning($"Failed to append action '{action.Name}' to XML.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Node '{node.Name}' is not a TriggerAction and will be skipped.");
                }
            }
        }
    }
}
