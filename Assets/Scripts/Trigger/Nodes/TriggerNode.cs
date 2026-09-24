using System;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Vectorier.Trigger
{
    //  The superclass for storing and displaying trigger events, actions and conditions
    [System.Serializable]
    public class TriggerNode
    {
        public enum TagType
        {
            None,
            Start,
            End,
        }
        [SerializeField]
        protected string _Name;

        [SerializeField]
        protected bool _Initialized = false;

        // For TriggerTemplates
        [SerializeField]
        protected bool _IsFromTemplate = false;

        [SerializeField]
        protected float _LabelSize = 50f;

        [SerializeField]
        private float _Height = 20f;

        [SerializeField]
        protected float _Indent;

        [SerializeField]
        protected TagType _Type = TagType.None;

        [SerializeReference]
        protected List<BaseAttribute> _Attributes;

        [NonSerialized]
        protected List<TriggerNode> _ParentList;

        [NonSerialized]
        protected TriggerNode _TagEnd;

        public static bool EditingAttributes = false;

        private static bool _ListHasError = false;

        protected GUIStyle _LabelStyle;
        protected GUIStyle LabelStyle
        {
            get
            {
                if( _LabelStyle == null)
                {
                    _LabelStyle = new GUIStyle(EditorStyles.label);
                }
                return _LabelStyle;
            }
            set => _LabelStyle = value;
        }

        public float Indent
        {
            get => _Indent;
            set => _Indent = value;
        }

        public string Name => _Name;

        public List<BaseAttribute> Attributes => _Attributes;

        public TagType Type => _Type;

        public TriggerNode()
        {
            _Type = TagType.None;
            _Attributes = new();
            _Initialized = false;
            _Name = string.Empty;
        }

        public TriggerNode(List<TriggerNode> list, TagType type = TagType.None)
        {
            _Attributes = new();
            _ParentList = list;
            _Initialized = false;
            _Type = type;
            _Name = "";
        }

        public TriggerNode(string name, List<TriggerNode> list, TagType type = TagType.None)
        {
            _Attributes = new();
            _ParentList = list;
            _Initialized = true;
            _Type = type;
            _Name = name;
            CalculateLabelSize();
        }

        // For Importing the Boolean is if it's from a template
        public TriggerNode(XmlNode node, List<TriggerNode> parentList, bool fromTemplate = false, TagType type = TagType.None)
        {
            _Attributes = new();
            _Initialized = true;
            _IsFromTemplate = fromTemplate;
            _Name = node.Name;
            _ParentList = parentList;
            _Type = type;
            CalculateLabelSize();
            ParseAttributes(node);
        }

        protected virtual void ParseAttributes(XmlNode node)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                BaseAttribute newAttr = new BaseAttribute(attr.Name, attr.Value);
                _Attributes.Add(newAttr);
            }
        }

        public virtual void Display(Rect mainRect, float availableWidth)
        {
            if (!_Initialized)
                return;

            if (_Type == TagType.End && TriggerListBase.Mode != TriggerListBase.Structure.XML)
                return;

            using (new EditorGUI.DisabledScope(_IsFromTemplate)) 
            {
                DisplayName(mainRect, availableWidth);
                DisplayAttributes(mainRect, availableWidth);
            }
        }

        protected virtual void DisplayAttributes(Rect mainRect, float availableWidth)
        {
            const float fieldHeight = 20f;
            const float spacing = 30f;
            const float labelSpacing = 20f;

            float x = mainRect.x + _Indent;
            float y = mainRect.y;

            float attributeStartX = mainRect.x + _Indent + _LabelSize + labelSpacing;
            float attributeWidth = availableWidth - _LabelSize - labelSpacing;

            x = attributeStartX;

            int index = 0;
            float height = fieldHeight;
            foreach (var attribute in _Attributes)
            {
                float width = attribute.FieldWidth;
                bool firstOnRow = Mathf.Approximately(x, attributeStartX);
                float requiredWidth = width + (firstOnRow ? 0f : spacing);

                if (!firstOnRow && x + requiredWidth > mainRect.x + availableWidth)
                {
                    x = attributeStartX;
                    y += fieldHeight;
                    height += fieldHeight;
                }

                Rect attributeRect = new Rect(x, y, width, fieldHeight);
                bool canEdit = EditingAttributes && !_IsFromTemplate;
                attribute.Display(attributeRect, index, canEdit);

                x += width + spacing;

                index++;
            }
            for (int i = 0; i < _Attributes.Count; i++)
            {
                if (_Attributes[i].ToBeDeleted)
                    _Attributes.RemoveAt(i);
            }
            _Height = height;
        }

        protected virtual void DisplayName(Rect mainRect, float availableWidth)
        {
            float x = mainRect.x + _Indent;
            float y = mainRect.y;

            Rect nameRect = new Rect(x, y, _LabelSize, mainRect.height);

            if (_Type == TagType.End)
            {
                EditorGUI.LabelField(nameRect, $"~{Name}", LabelStyle);
            }
            else if (EditingAttributes && !_IsFromTemplate)
            {
                Rect addRect = new Rect(nameRect.x, nameRect.y, nameRect.width / 2, nameRect.height);
                if (GUI.Button(addRect, "+ Attr"))
                    _Attributes.Add(new BaseAttribute("NewAttr", "0"));
            }
            else
            {
                EditorGUI.LabelField(nameRect, _Name);
            }
        }

        public float GetHeight(float availableWidth)
        {
            // lazy as fu- buggy but it works
            return _Height;
        }

        protected void CalculateLabelSize()
        {
            _LabelSize = LabelStyle.CalcSize(new GUIContent(_Name)).x + 20f;
            if(_LabelSize == 0)
            {
                _LabelSize = 20f;
            }
        }

        public void ForceInitialize()
        {
            _Initialized = true;
        }

        public static void CheckTags(List<TriggerNode> nodes)
        {
            float currIndent = 0f;
            List<TriggerNode> startTags = new();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (currIndent < 0)
                {
                    EditorGUILayout.HelpBox("Missing Or Misordered Tags!", MessageType.Error);
                    _ListHasError = true;
                }
                else
                {
                    _ListHasError = false;
                }

                if (nodes[i] == null)
                    continue;

                if (nodes[i]._Type == TagType.End)
                {
                    if (startTags.Count == 0)
                    {
                        EditorGUILayout.HelpBox($"End Tags Before Start are Not Allowed! (Index: {i})", MessageType.Error);
                        _ListHasError = true;
                        continue;
                    }
                    else
                    {
                        _ListHasError = false;
                    }
                    startTags[startTags.Count - 1]._TagEnd = nodes[i];
                    startTags.RemoveAt(startTags.Count - 1);
                    currIndent -= TriggerGUISettings.IndentLevel;
                }

                nodes[i].Indent = currIndent;

                if (nodes[i]._Type == TagType.Start)
                {
                    startTags.Add(nodes[i]);
                    currIndent += TriggerGUISettings.IndentLevel;
                }
            }
        }

        public void RemoveFromList()
        {
            if (_ParentList == null)
                return;

            if (_Type == TagType.End)
            {
                int i = _ParentList.IndexOf(this);
                while (i > 0)
                {
                    i--;
                    TriggerNode node = _ParentList[i];
                    if (node._Type == TagType.Start)
                    {
                        _ParentList.RemoveAt(i);
                        break;
                    }
                }
            }
            else if (_Type == TagType.Start)
            {
                _ParentList.Remove(_TagEnd);
            }
            _ParentList.Remove(this);
        }

        public bool AppendToElement(XmlElement parentElement, XmlDocument doc)
        {
            if (parentElement == null || doc == null || string.IsNullOrEmpty(_Name) || _IsFromTemplate || _ListHasError)
                return false;

            if (!_Initialized)
            {
                return true;
            }

            var element = doc.CreateElement(_Name);

            foreach (BaseAttribute attr in _Attributes)
            {
                element.SetAttribute(attr.Name, attr.Value);
            }

            parentElement.AppendChild(element);
            return true;
        }
    }
}
