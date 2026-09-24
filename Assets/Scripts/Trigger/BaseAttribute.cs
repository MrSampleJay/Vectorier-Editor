using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Vectorier.Trigger
{
    [Serializable]
    public class BaseAttribute
    {
        [SerializeField]
        VarType VarType = VarType.String;

        [SerializeField]
        public string Name;

        [SerializeField]
        public string Value;

        public float ValueNumber;

        public float FieldWidth;

        public bool IsActive;

        private bool _UseAlternateField;

        private bool _AlwaysShowField;

        private TriggerNode _ParentNode;

        // Only used for VarType.Template, to store the template object associated with this attribute
        private TriggerTemplate _Template;

        private static GUIStyle _AttrStyle;

        // Property to get or set the GUIStyle for attribute labels
        public GUIStyle AttrStyle
        {
            get
            {
                if (_AttrStyle == null)
                {
                    _AttrStyle = new GUIStyle(EditorStyles.label);
                    _AttrStyle.normal.textColor = TriggerGUISettings.AttributeColor;
                }
                return _AttrStyle;
            }
            set
            {
                _AttrStyle = value;
            }
        }

        private static GUIStyle _VarStyle;

        public GUIStyle VarStyle
        {
            get
            {
                if (_VarStyle == null)
                {
                    _VarStyle = new GUIStyle(EditorStyles.label);
                    _VarStyle.normal.textColor = TriggerGUISettings.VariableColor;
                }
                return _VarStyle;
            }
            set
            {
                _VarStyle = value;
            }
        }

        private static GUIStyle _ValueStyle;

        public GUIStyle ValueStyle
        {
            get
            {
                if (_ValueStyle == null)
                {
                    _ValueStyle = new GUIStyle(EditorStyles.label);
                    _ValueStyle.normal.textColor = TriggerGUISettings.ValueColor;
                }
                return _ValueStyle;
            }
            set
            {
                _ValueStyle = value;
            }
        }

        [SerializeField]
        private bool _AllowAttributeEditing;

        public bool ToBeDeleted = false;

        public BaseAttribute()
        {
            IsActive = true;
        }

        public BaseAttribute(string name, string value = "0", bool allowAttributeEditing = true, bool useAlternateField = false, VarType type = VarType.String)
        {
            Name = name;
            Value = value;
            VarType = type;
            IsActive = true;
            _AllowAttributeEditing = allowAttributeEditing;
            _UseAlternateField = useAlternateField;
            CalculateFieldWidth();

            if(type == VarType.Template)
            {
                _Template = new();
                _AlwaysShowField = true;
            }
        }

        public void Display(Rect rect, int index = 0, bool isEditingAttribute = false)
        {
            CalculateFieldWidth();

            Event e = Event.current;
            bool hovering = rect.Contains(e.mousePosition) || _AlwaysShowField;

            bool editAttributeName = isEditingAttribute;

            bool isHoveringAndEditing = editAttributeName && hovering;

            if ((isHoveringAndEditing || string.IsNullOrEmpty(Name)) && _AllowAttributeEditing)
            {
                // DisplayAlternateField(rect);
                Rect nameRect = new Rect(rect.x, rect.y, rect.width - 20f, rect.height);
                Rect delRect = new Rect(rect.x + rect.width - 20f, rect.y, 20f, rect.height);
                ToBeDeleted = GUI.Button(delRect, "-");
                Name = EditorGUI.TextField(nameRect, Name);
                return;
            }

            // Control (If user wants to drag and select text, and not lose control)
            GUI.SetNextControlName($"{Name}{index}");
            bool isFocused = GUI.GetNameOfFocusedControl() == $"{Name}{index}";

            // If Mouse Hovering Over
            if (hovering || isFocused)
            {
                if (_UseAlternateField)
                    DisplayAlternateField(rect);
                else
                    Value = EditorGUI.TextField(rect, Value);
                return;
            }

            // If Mouse Cursor Not Hovering
            if (!hovering)
            {
                string labelStr = $"{Name} : ";
                Vector2 size = AttrStyle.CalcSize(new GUIContent(labelStr));
                size.x = size.x == 0 ? 20f : size.x;

                Rect AttrRect = new Rect(rect.x, rect.y, size.x, rect.height);
                EditorGUI.LabelField(AttrRect, labelStr, AttrStyle);

                Rect varRect = new Rect(AttrRect.x + size.x, rect.y, rect.width - size.x, rect.height);
                GUIStyle fieldStyle = Value?.FirstOrDefault() == '_' ? VarStyle : ValueStyle;
                EditorGUI.LabelField(varRect, Value, fieldStyle);
                return;
            }
        }

        // Display an alternate field based on the VarType, Still needs to be implemented for other types
        public void DisplayAlternateField(Rect rect)
        {
            switch (VarType)
            {
                case VarType.String:
                    Value = EditorGUI.TextField(rect, Value);
                    break;
                case VarType.Float:
                    ValueNumber = EditorGUI.FloatField(rect, ValueNumber);
                    break;
                case VarType.Template:
                    Value = _Template.TemplateField(rect, Value);
                    break;
                default:
                    Value = EditorGUI.TextField(rect, Value);
                    break;
            }
        }

        private void CalculateFieldWidth()
        {
            FieldWidth = VarStyle.CalcSize(new GUIContent($"{Name} : {Value}")).x;
        }

        public static BaseAttribute Find(List<BaseAttribute> list, string name)
        {
            foreach (BaseAttribute attr in list)
            {
                if (attr.Name == name)
                    return attr;
            }
            return null;
        }

        string GetCapitalLetters(string input)
        {
            string result = "";

            foreach (char c in input)
            {
                if (char.IsUpper(c))
                    result += c;
            }

            return result;
        }

        // Method to set the parent node of the attribute
        // This is useful for accessing the parent node's properties or methods if needed
        public void SetParentNode(TriggerNode parentNode)
        {
            _ParentNode = parentNode;
        }
    }
}
