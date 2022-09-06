using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ChunkyOSC {
    [Serializable]
    public class OscParameter
    {
        public enum Types
        {
            Int = 0,
            Bool = 1,
            Float = 2
        }

        public string name = "";
        public Types type = Types.Int;
        public object Value
        {
            get
            {
                switch (type)
                {
                    case Types.Bool:
                        return boolVal;
                    case Types.Float:
                        return floatVal;
                    case Types.Int:
                        return intVal;
                    default:
                        return null;
                }
            }
            set
            {
                switch (type)
                {
                    case Types.Bool:
                        boolVal = (bool)value;
                        break;
                    case Types.Float:
                        floatVal = (float)value;
                        break;
                    case Types.Int:
                        intVal = (int)value;
                        break;
                }
            }
        }

        [SerializeField]
        private bool boolVal;
        [SerializeField]
        private float floatVal;
        [SerializeField]
        private int intVal;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(OscParameter))]
    public class OscParameterDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect leftHalf1 = new Rect(position.x, position.y, position.width / 4f, position.height);
            Rect leftHalf2 = new Rect(position.x + position.width / 4f + 2f, position.y, position.width / 4f - 4f, position.height);
            Rect rightHalf = new Rect(position.x + position.width / 2f + 2f, position.y, position.width / 2f - 2f, position.height);

            SerializedProperty propName = property.FindPropertyRelative("name");
            propName.stringValue = EditorGUI.TextField(leftHalf1, propName.stringValue);

            SerializedProperty propType = property.FindPropertyRelative("type");
            propType.enumValueIndex = (int)(OscParameter.Types)EditorGUI.EnumPopup(leftHalf2, (OscParameter.Types)propType.enumValueIndex);

            switch ((OscParameter.Types)propType.intValue)
            {
                case OscParameter.Types.Bool:
                    SerializedProperty propBool = property.FindPropertyRelative("boolVal");
                    propBool.boolValue = EditorGUI.Toggle(rightHalf, propBool.boolValue);
                    break;
                case OscParameter.Types.Float:
                    EditorGUI.Slider(rightHalf, property.FindPropertyRelative("floatVal"), -1f, 1f, "");
                    break;
                case OscParameter.Types.Int:
                    EditorGUI.IntSlider(rightHalf, property.FindPropertyRelative("intVal"), 0, 255, "");
                    break;
            }
        }
    }
#endif
}