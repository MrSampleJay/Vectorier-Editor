using NUnit.Framework;
using System.Collections;
using UnityEngine;
using Vectorier.Trigger;
using System.Collections.Generic;
using UnityEditor;
using System;

namespace Assets.Scripts.Trigger.Nodes
{
    public class TriggerCondition : TriggerNode
    {
        public enum InitChoice 
        {
            None,
            Comparison,
            Operator
        }

        public enum ConditionType
        {
            Equal,
            Greater,
            Less,
            GreaterEqual,
            LessEqual,
        }

        enum OperatorType 
        {
            And,
            Or
        }

        private ConditionType _ConditionType = ConditionType.Equal;

        // Only if ConditionType is Operator
        private OperatorType _OperatorType = OperatorType.And;

        private InitChoice _Initializer = InitChoice.None;

        private bool _UseCustomType;

        public static bool UseAlternateFormat;

        public TriggerCondition(List<TriggerNode> parent, ConditionType type = ConditionType.Equal) : base(parent)
        {
            _ConditionType = type;
            _Name = _ConditionType.ToString();
            _Initialized = false;
            Init();
        }

        private string ToSymbol(ConditionType type)
        {
            switch (type)
            {
                case ConditionType.Equal: return "=";
                case ConditionType.Greater: return ">";
                case ConditionType.Less: return "<";
                case ConditionType.GreaterEqual: return ">=";
                case ConditionType.LessEqual: return "<=";
                default:
                    break;
            }
            return _ConditionType.ToString();
        }

        private string OperatorToString()
        {
            switch (_OperatorType)
            {
                case OperatorType.And: return "&";
                case OperatorType.Or: return "|";
            }
            return _OperatorType.ToString();
        }

        private void Init()
        {
            if(_Initializer == InitChoice.Operator)
            {
                _Type = TagType.Start;
                _Attributes.Add(new BaseAttribute("Type", "And"));
            } 
            else if (_Initializer == InitChoice.Comparison)
            {
                switch (_ConditionType)
                {
                    case ConditionType.Greater:
                    case ConditionType.Less:
                        _Attributes.Add(new BaseAttribute("Value", "", false));
                        _Attributes.Add(new BaseAttribute("Than", "", false));
                        _Attributes.Add(new BaseAttribute("Not", "0", false));
                        break;
                    case ConditionType.Equal:
                    case ConditionType.GreaterEqual:
                    case ConditionType.LessEqual:
                    default:
                        _Attributes.Add(new BaseAttribute("Value1", "", false));
                        _Attributes.Add(new BaseAttribute("Value2", "", false));
                        _Attributes.Add(new BaseAttribute("Not", "0", false));
                        break;
                }
            }
        }

        public override void Display(Rect mainRect, float availableWidth)
        {
            if (!_Initialized)
            {
                _Initializer = (InitChoice)EditorGUI.EnumPopup(mainRect, _Initializer);
                if (_Initializer != InitChoice.None)
                    _Initialized = true;
            }

            if (UseAlternateFormat)
            {
                DisplayAlternate(mainRect, availableWidth);
            }
            else
            {
                base.Display(mainRect, availableWidth);
            }
        }

        private void DisplayAlternate(Rect rect, float availableWidth)
        {
            if(_Initializer == InitChoice.Comparison)
            {
                string value1 = _Attributes[0].Value;
                string value2 = _Attributes[1].Value;
                string not = _Attributes[2].Value;

                float availableFieldWidth = (rect.width / 2) - 15;
                Rect firstValueRect = new Rect(rect.x, rect.y, availableFieldWidth, rect.height);
                Rect operatorRect = new Rect(rect.x + availableFieldWidth, rect.y, 30, rect.height);
                Rect secondValueRect = new Rect(operatorRect.x + 30f, rect.y, availableFieldWidth, rect.height);

                value1 = EditorGUI.TextField(firstValueRect, value1);

                ConditionType[] condArray = (ConditionType[])Enum.GetValues(typeof(ConditionType));
                List<string> symbols = new();

                foreach (ConditionType type in condArray)
                {
                    symbols.Add(ToSymbol(type));
                }

                _ConditionType = (ConditionType)EditorGUI.Popup(operatorRect, (int)_ConditionType, symbols.ToArray());
                _Name = _ConditionType.ToString();

                value2 = EditorGUI.TextField(secondValueRect, value2);

                _Attributes[0].Value = value1;
                _Attributes[1].Value = value2;
                _Name = _ConditionType.ToString();
            }
            else
            {
                base.DisplayName(rect, availableWidth);
                Rect symbolRect = new Rect(rect.x + _LabelSize, rect.y, 40f, rect.height);

                List<string> symbols = new() { "&", "|" };
                _OperatorType = (OperatorType)EditorGUI.Popup(symbolRect, (int)_OperatorType, symbols.ToArray());
                _Attributes[0].Value = _OperatorType.ToString();
            }
        }
    }
}