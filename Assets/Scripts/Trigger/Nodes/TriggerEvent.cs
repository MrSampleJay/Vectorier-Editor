using System;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Vectorier.Trigger
{
    [Serializable]
    public class TriggerEvent : TriggerNode
    {
        [SerializeField]
        Type _EventType = Type.Enter;

        public enum Type
        {
            Enter,
            Exit,
            OnShow,
            OnHide,
            KeyPressed,
            Activate,
            Timeout,
            Collision,
            Line,
            ValueChange,
            EventBlock
        }

        bool _UseCustomEvent = false;

        public TriggerEvent(List<TriggerNode> parentList)
        {
            _ParentList = parentList;
            _EventType = Type.Enter;
            _Name = _EventType.ToString();
            CalculateLabelSize();
            _Attributes = new();
            _Initialized = true;
        }

        public TriggerEvent(XmlNode eventNode, List<TriggerNode> parentList, bool preview = false):
            base(eventNode, new(), preview)
        {
            bool eventParsed = Enum.TryParse<Type>(Name, out Type result);
            if (!eventParsed)
            {
                _UseCustomEvent = true;
                _Name = eventNode.Name;
                Debug.LogWarning("Event Not Defined, May Not Funciton In Game");
            }
            _EventType = result;
            _ParentList = parentList;
            _Initialized = true;
        }

        public override void Display(Rect mainRect, float availableWidth)
        {
            DisplayName(mainRect, availableWidth);
            base.DisplayAttributes(mainRect, availableWidth);
        }

        protected override void DisplayName(Rect mainRect, float availableWidth)
        {
            float size = _Attributes.Count == 0 ? mainRect.width : _LabelSize;
            Rect nameRect = new Rect(mainRect.x, mainRect.y, size, mainRect.height);
            if(_UseCustomEvent)
            {
                TryDefine();
                _Name = EditorGUI.TextField(nameRect, _Name);
            }
            else
            {
                Type trigerEvent = (Type)EditorGUI.EnumPopup(nameRect, _EventType);

                if (trigerEvent != _EventType)
                {
                    _EventType = trigerEvent;
                    CheckForAttributes();
                }
                _Name = trigerEvent.ToString();
            }

            CalculateLabelSize();
        }

        private void CheckForAttributes()
        {
            _Attributes.Clear();
            if(_EventType == Type.Line)
            {
                _Attributes.Add(new BaseAttribute("Type", "Horizontal", false));
                _Attributes.Add(new BaseAttribute("Position", "0", false));
            }
            if(_EventType == Type.ValueChange)
            {
                _Attributes.Add(new BaseAttribute("Value", "0", false));
            }
            if(_EventType == Type.EventBlock)
            {
                BaseAttribute templateAttr = new BaseAttribute("Template", "FreqUsed.Enter", false, true, VarType.Template);
                _Attributes.Add(templateAttr);
            }
        }

        private void TryDefine()
        {
            if (_UseCustomEvent)
            {
                bool eventParsed = Enum.TryParse<Type>(Name, out Type result);
                if (eventParsed)
                {
                    _UseCustomEvent = false;
                    _EventType = result;
                    CheckForAttributes();
                }
            }
        }
    }
}
