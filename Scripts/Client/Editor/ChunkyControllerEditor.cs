using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ChunkyOSC
{
    [CustomEditor(typeof(ChunkyController))]
    public class ChunkyControllerEditor : Editor
    {
        public ChunkyController model;
        
        public ReorderableList list;
        public SerializedProperty extras;

        private PresetUtility.Preset customPreset = new PresetUtility.Preset { name = "Custom", parameters = new OscParameter[0] };

        private PresetUtility.Preset[] presets;
        private string[] presetNames;

        private void OnEnable()
        {
            if (Selection.activeGameObject == null)
                return;

            model = Selection.activeGameObject.GetComponent<ChunkyController>();
            extras = serializedObject.FindProperty("ExtraParameters");

            ReloadPresets();

            list = new ReorderableList(serializedObject, extras, false, true, true, true);
            list.drawHeaderCallback += DrawHeader;
            list.drawElementCallback += DrawElement;
            list.elementHeightCallback += ElementHeight;
        }

        private void OnDisable()
        {
            if (list != null)
            {
                list.drawHeaderCallback -= DrawHeader;
                list.drawElementCallback -= DrawElement;
                list.elementHeightCallback -= ElementHeight;
            }
        }

        public override void OnInspectorGUI()
        {
#if OSC_CORE
            serializedObject.Update();

            // Wait until the object is actually found
            if (model == null)
                return;

            EditorGUI.BeginChangeCheck();

            // Chunk Size
            EditorGUILayout.Space();
            model.ChunkSize = EditorGUILayout.IntSlider(new GUIContent("Chunk Size", "The cubic size of each chunk."), model.ChunkSize, 1, 8);
            GUILayout.Space(2f);
            
            // Boundaries
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Boundaries"));
            model.AlwaysShowBounds = !Convert.ToBoolean(GUILayout.Toolbar(Convert.ToInt32(!model.AlwaysShowBounds), new string[] { "Always Shown", "When Selected" })); ;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6f);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(model);

            DrawLine();

            // Extra Param Presets
            int presetIdx;
            if (model.currentPreset != null && model.currentPreset != customPreset)
            {
                presetIdx = Array.IndexOf(presetNames, model.currentPreset.name);
                if (presetIdx == -1)
                {
                    model.currentPreset = customPreset;
                    presetIdx = 0;
                }
            }
            else
                presetIdx = 0;
            EditorGUI.BeginChangeCheck();
            presetIdx = EditorGUILayout.IntPopup("Preset", presetIdx, presetNames, Enumerable.Range(0, presetNames.Length).ToArray());
            model.currentPreset = presets[presetIdx];
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck())
            {
                if (model.currentPreset == customPreset)
                    model.customName = "";
                else
                    model.customName = model.currentPreset.name;
                model.ExtraParameters = model.currentPreset.parameters;
                EditorUtility.SetDirty(model);
            }

            // Preset Settings
            if (model.currentPreset.custom)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                if (model.currentPreset.name != model.customName)
                    model.currentPreset.name = model.customName;
                EditorGUI.BeginChangeCheck();
                model.currentPreset.name = EditorGUILayout.TextField("Name", model.currentPreset.name);
                if (EditorGUI.EndChangeCheck())
                {
                    model.customName = model.currentPreset.name;
                    EditorUtility.SetDirty(model);
                }
                if (GUILayout.Button("Save Preset"))
                {
                    PresetUtility.SavePreset(model.customName, model.ExtraParameters);
                    ReloadPresets();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(6f);
            }

            // Extra Parameters
            list.DoLayoutList();
            GUILayout.Space(4f);

            serializedObject.ApplyModifiedProperties();
#else
            // Show alert if OscCore is missing.
            EditorGUILayout.HelpBox("OscCore could not be found. It must be installed to use this tool.\nUse the button below to install it automatically.", MessageType.Error);
            if (GUILayout.Button("Clone OscCore from Github"))
                Packages.AddPackage("OscCore", "https://github.com/stella3d/OscCore.git");
#endif
        }

        // Obtains the most recent list of presets.
        private void ReloadPresets()
        {
            customPreset.name = "Custom";
            presets = new PresetUtility.Preset[] { customPreset }.Concat(PresetUtility.GetPresets()).ToArray();
            presetNames = PresetUtility.GetPresetNames(presets);
        }

        // Draws a line across the GUI.
        private void DrawLine(bool addSpace = true)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
            EditorGUILayout.EndHorizontal();
            if (addSpace)
            {
                EditorGUILayout.Space();
            }
        }

#region List Functions

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Extra Parameters");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty item = list.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, rect.height - 2f), item);
        }

        private float ElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 2f;
        }

#endregion
    }
}

