using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif
using static ChunkyOSC.ChunkyPluginUtility;

namespace ChunkyOSC
{
    [CustomEditor(typeof(ChunkyOSCSetup))]
    public class ChunkyOSCSetupEditor : Editor
    {
        public ChunkyOSCSetup model;

        private ChunkyPlugin[] plugins;
        private string[] pluginNames;

        private void OnEnable()
        {
            if (Selection.activeGameObject == null)
                return;

            // Grab all valid plugins.
            plugins = new ChunkyPlugin[] { new ChunkyPlugin { plugin = "None", script = null } }.Concat(GetPlugins()).ToArray();
            pluginNames = GetPluginNames(plugins);

            model = Selection.activeGameObject.GetComponent<ChunkyOSCSetup>();
        }

        public override void OnInspectorGUI()
        {
#if VRC_SDK_VRCSDK3 && BMB_LIBRARIES
            // Wait until the object is actually found
            if (model == null)
                return;

            // The Avatar
            EditorGUILayout.Space();
            AvatarPicker();
            if (model.avatar == null)
                EditorGUILayout.HelpBox("No Avatars found in the current Scene!", MessageType.Warning);
            GUILayout.Space(2f);

            // The Menu
            EditorGUI.BeginChangeCheck();
            model.menu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(new GUIContent("Expressions Menu", "The Expressions Menu you want the OSC controls added to. Leave this empty if you want to use the menu currently set on the avatar.\n(Controls will be added as a submenu.)"), model.menu, typeof(VRCExpressionsMenu), true);
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(model);

            DrawLine();

            // Plugin Selector
            int pluginIdx;
            if (model.plugin != null)
            {
                pluginIdx = Array.IndexOf(pluginNames, model.plugin.plugin);
                if (pluginIdx == -1)
                {
                    model.plugin = null;
                    pluginIdx = 0;
                }
            }
            else
                pluginIdx = 0;
            EditorGUI.BeginChangeCheck();
            pluginIdx = EditorGUILayout.IntPopup("Selected Plugin", pluginIdx, pluginNames, Enumerable.Range(0, pluginNames.Length).ToArray());
            model.plugin = plugins[pluginIdx];
            if (EditorGUI.EndChangeCheck())
            {
                model.pluginArgs = null;
                EditorUtility.SetDirty(model);
            }

            // Plugin Settings
            if (model.plugin != null && model.plugin.script != null)
            {
                EditorGUI.BeginChangeCheck();

                // Call the plugin's inspector code, if it has any.
                model.plugin.DisplaySettings(ref model.pluginArgs);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(model);
            }

            EditorGUILayout.Space();
            GUILayout.Space(4f);

            DrawLine(false);

            // The File Destination
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Destination", "The folder where any created files will be saved to."));
            // Format file path to fit in a button.
            string displayPath = (model.destination != null) ? model.destination.Replace('\\', '/') : "";
            while (new GUIStyle(GUI.skin.GetStyle("Box")) { richText = true, font = GUI.skin.label.font }.CalcSize(new GUIContent("<i>" + displayPath + "</i>")).x > EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 48f)
                displayPath = "..." + displayPath.Substring(4);
            if (displayPath.IndexOf("...") == 0 && displayPath.IndexOf('/') != -1)
                displayPath = "..." + displayPath.Substring(displayPath.IndexOf('/'));
            // Destination.
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("<i>" + displayPath + "</i>", (model.destination != null ? model.destination.Replace('\\', '/') : "") + "\nClick to Change"), new GUIStyle(GUI.skin.GetStyle("Box")) { richText = true, active = GUI.skin.GetStyle("Button").active, normal = GUI.skin.box.hover, font = GUI.skin.label.font }, new GUILayoutOption[] { GUILayout.ExpandWidth(true) }))
            {
                EditorUtility.SetDirty(model);
                string absPath = EditorUtility.OpenFolderPanel("Destination Folder", "", "");
                if (absPath.StartsWith(Application.dataPath))
                    model.destination = "Assets" + absPath.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUILayout.Space(4f);
            DrawLine();

            // Write Defaults
            EditorGUI.BeginChangeCheck();
            model.writeDefaults = EditorGUILayout.Toggle(new GUIContent("Write Defaults", "Forces animator states to write default values."), model.writeDefaults);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(model);

            EditorGUILayout.Space();
            DrawLine();

            // Start Button
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUILayout.HelpBox("You cannot add the OSC camera while in Play mode.", MessageType.Info);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            if (GUILayout.Button("Add to Avatar"))
                model.SetupChunkyOSC();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(4f);
#else
#if !VRC_SDK_VRCSDK3
            // Show alert if the VRCSDK is missing.
            EditorGUILayout.HelpBox("The VRCSDK3 could not be found. It must be installed to use this tool.\nYou can get it from the official VRChat website.", MessageType.Error);
            EditorGUILayout.Space();
#endif
#if !BMB_LIBRARIES
            // Show alert if BMB Libraries is missing.
            EditorGUILayout.HelpBox("BMB Libraries could not be found. It must be installed to use this tool.\nUse the button below to install it automatically.", MessageType.Error);
            if (GUILayout.Button("Clone BMB Libraries from Github"))
                Packages.AddPackage("BMB Libraries", "https://github.com/Joshuarox100/BMB-Libraries.git");
#endif
#endif
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

#if VRC_SDK_VRCSDK3 && BMB_LIBRARIES
        private void AvatarPicker()
        {
            VRCAvatarDescriptor selected = null;
            VRCAvatarDescriptor[] descriptors = FindObjectsOfType<VRCAvatarDescriptor>();
            if (descriptors.Length > 0)
            {
                // Create list of avatar names in the scene.
                string[] avatarNames = new string[descriptors.Length];
                for (int i = 0; i < descriptors.Length; i++)
                    avatarNames[i] = descriptors[i].gameObject.name;

                // Create a popup menu using them to choose from.
                int currentIndex = Array.IndexOf(descriptors, model.avatar);
                int nextIndex = EditorGUILayout.Popup(new GUIContent("Active Avatar", "The Avatar you want to add ChunkyOSC to."), currentIndex, avatarNames);
                if ((nextIndex = Math.Max(0, nextIndex)) != currentIndex)
                    selected = descriptors[nextIndex];
                else return;
            }

            // Update the selection if it was modified.
            if (selected != model.avatar)
                model.avatar = selected;
        }
#endif
    }
}