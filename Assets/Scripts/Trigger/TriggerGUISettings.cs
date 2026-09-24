using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Vectorier.Trigger
{
    public enum VarType
    {
        String,
        Int,
        Float,
        Bool,
        Color,
        Model,
        Key,
        AI,
        Template,
        None
    }

    public class TriggerGUISettings
    {
        public static Color ActionColor = Color.white;

        public static Color AttributeColor = Color.cyan;

        public static Color VariableColor = Color.goldenRod;

        public static Color ValueColor = Color.mediumSeaGreen;

        public static float IndentLevel = 20f;
    }
}
