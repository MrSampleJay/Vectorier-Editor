using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Vectorier.Trigger
{
    [Serializable]
    public class TriggerAction : TriggerNode
    {
        [SerializeField]
        private bool _IsActionBlock = false;

        public TriggerAction(List<TriggerNode> actionList, TagType type = TagType.None)
            :base (actionList , type)
        {
        }

        public TriggerAction(string name, List<TriggerNode> actionList, TagType type = TagType.None)
            : base(name, actionList, type)
        {
        }

        public TriggerAction(XmlNode actionNode, List<TriggerNode> actionList, bool preview = false)
            : base(actionNode, actionList, preview)
        {
            if(actionNode.Name == "ActionBlock")
                _IsActionBlock = true;
            if (actionNode.Name == "Choose")
                _Type = TagType.Start;
        }

        private void TryInit()
        {
            if (_Type == TagType.End)
            {
                _Initialized = true;
                return;
            }

            switch (_Name)
            {
                case "Wait":
                    _Attributes = new()
                {
                    new ("Frames", "30")
                };
                    break;
                case "Camera":
                    _Attributes = new()
                {
                    new ("Zoom", "1"),
                    new ("Stop", "0"),
                    new ("Follow", "Player")
                };
                    break;
                case "ForceAnimation":
                    _Attributes = new()
                {
                    new("Model", "Player"),
                    new("AnimName", "RunForward"),
                    new("AnimFrame", "1"),
                    new("Reversed" , "0")
                };
                    break;
                case "Transform":
                _Attributes = new()
                {
                    new("Name", "")
                };
                    break;
                case "Choose":
                    _Type = TagType.Start;
                    _Attributes = new()
                    {
                        new ("Order", "Sync"),
                        new ("Set", "1")
                    };
                    TriggerAction endTag = new TriggerAction("Choose", _ParentList, TagType.End);
                    _TagEnd = endTag;
                    endTag.ForceInitialize();
                    _ParentList.Add(endTag);
                    break;
                default:
                    _Initialized = false;
                    // Return if Nothing
                    return;
            }
            _Initialized = true;
            GUIStyle style = new GUIStyle(EditorStyles.label);
            CalculateLabelSize();
        }

        protected override void ParseAttributes(XmlNode node)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                BaseAttribute newAttr = new BaseAttribute(attr.Name, attr.Value);
                _Attributes.Add(newAttr);
            }
        }

        public override void Display(Rect mainRect, float availableWidth)
        {
            if (!_Initialized)
            {
                _Name = EditorGUI.TextField(mainRect, "Name: ", _Name);
                TryInit();
                return;
            }

            base.Display(mainRect, availableWidth);
        }

        protected override void DisplayAttributes(Rect mainRect, float availableWidth)
        {
            base.DisplayAttributes(mainRect, availableWidth);
        }

        protected override void DisplayName(Rect mainRect, float availableWidth)
        {
            float x = mainRect.x + _Indent;
            float y = mainRect.y;

            Event e = Event.current;

            Rect nameRect = new Rect(x, y, _LabelSize, mainRect.height);
            bool nameRectHovered = nameRect.Contains(e.mousePosition);

            if (_Type == TagType.End)
            {
                EditorGUI.LabelField(nameRect, $"~{Name}");
            }
            else if (EditingAttributes && !_IsActionBlock)
            {
                Rect addRect = new Rect(nameRect.x, nameRect.y, nameRect.width / 2, nameRect.height);
                if (GUI.Button(addRect, "+ Attr"))
                    _Attributes.Add(new BaseAttribute("NewAttr", "0"));
            }
            else if (_IsActionBlock && nameRectHovered)
            {
                bool import = GUI.Button(nameRect, "Import");
                if (import) 
                {
                    ImportBlock();
                    this.RemoveFromList();
                }
            } 
            else
            {
                EditorGUI.LabelField(nameRect, _Name);
            }
        }

        public static List<TriggerNode> ConvertList(List<TriggerAction> list)
        {
            List <TriggerNode> nodes = new();
            foreach (TriggerAction action in list)
            {
                nodes.Add(action);
            }
            return nodes;
        }

        private void ImportBlock()
        {
            if (TemplateManager.templates == null)
            {
                Debug.Log("TriggerTemplates is not Opened");
            }
            else
            {
                BaseAttribute attr = BaseAttribute.Find(_Attributes, "Template");
                XmlNode block = TemplateManager.GetActionBlock(attr.Value);
                int index = _ParentList.IndexOf(this);

                for (int i = block.ChildNodes.Count - 1; i >= 0; i--)
                {
                    XmlNode action = block.ChildNodes[i];
                    _ParentList.Insert(index, new TriggerAction(action, _ParentList));
                }
            }
        }
    }
}
