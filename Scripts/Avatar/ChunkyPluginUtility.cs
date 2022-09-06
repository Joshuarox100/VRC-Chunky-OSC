using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif
#if BMB_LIBRARIES
using BMBLibraries.Classes;
#endif
using UnityEditor.Animations;
using UnityEditor.VersionControl;
#endif

namespace ChunkyOSC
{
    // Interface used to identify valid plugins.
    public interface IChunkyPlugin { }

    public class ChunkyPluginUtility
    {
#if UNITY_EDITOR
        // Loads a plugin from its package JSON.
        private static ChunkyPlugin LoadPlugin(TextAsset package)
        {
            return ChunkyPlugin.CreateFromJson(package.text);
        }

        // Grabs all packages JSONs from the plugins folder and returns an array of plugins they're associated with.
        public static ChunkyPlugin[] GetPlugins()
        {
            List<ChunkyPlugin> plugins = new List<ChunkyPlugin>();

            foreach (string packageGuid in AssetDatabase.FindAssets("package", new string[] { "Assets/ChunkyOSC/Plugins" }))
                try { plugins.Add(LoadPlugin(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(packageGuid)))); } catch { }

            return plugins.ToArray();
        }

        // Returns the names of all plugins within the given array.
        public static string[] GetPluginNames(ChunkyPlugin[] plugins)
        {
            List<string> pluginNames = new List<string>();
            foreach (ChunkyPlugin plugin in plugins)
                pluginNames.Add(plugin.plugin);
            return pluginNames.ToArray();
        }
#endif
        public class ChunkyPlugin
        {
            // Used for deserializing plugins.
            [Serializable]
            private class ChunkyPluginDecode
            {
                public string plugin;
                public string script;
            }

            public string plugin;
            public Type script;

#if UNITY_EDITOR
            // Creates a plugin object from a given JSON string.
            public static ChunkyPlugin CreateFromJson(string json)
            {
                ChunkyPluginDecode rootObject = JsonUtility.FromJson<ChunkyPluginDecode>(json);
                return new ChunkyPlugin { plugin = rootObject.plugin, script = Type.GetType(rootObject.script)};
            }

#if BMB_LIBRARIES && VRC_SDK_VRCSDK3
            // Used to execute a plugin's script during setup.
            public int ExecutePlugin(VRCAvatarDescriptor avatar, AnimatorController gestureController, AnimatorController fxController, ref Backup backupManager, ref AssetList generated, string destination, bool writeDefaults, object[] parameters)
            {
                // Only proceed if it's a valid plugin.
                if (script.GetInterface("IChunkyPlugin") == typeof(IChunkyPlugin))
                {
                    // Grab the script's execution method and call it.
                    MethodInfo method = script.GetMethod("ExecutePlugin");
                    if (method != null)
                    {
                        object[] args = new object[] { avatar, gestureController, fxController, backupManager, generated, destination, writeDefaults, parameters };
                        int result = (int)method.Invoke(null, args);
                        backupManager = (Backup)args[3];
                        generated = (AssetList)args[4];
                        return result;
                    }
                    else
                        return EditorUtility.DisplayDialog("ChunkyOSC", "The plugin used is missing the function needed for execution.\nRefer to the template plugin for more information.", "Close") ? 1 : 1;
                }
                else
                    return EditorUtility.DisplayDialog("ChunkyOSC", "The selected plugin is invalid.", "Close") ? 1 : 1;
            }
#endif

            // Used to call the IMGUI code for this plugin's arguments.
            public void DisplaySettings(ref object[] parameters)
            {
                // Only proceed if it's a valid plugin.
                if (script.GetInterface("IChunkyPlugin") == typeof(IChunkyPlugin))
                {
                    // Grab the script's GUI method and call it.
                    MethodInfo method = script.GetMethod("DisplaySettings");
                    if (method != null)
                    {
                        object[] args = new object[] { parameters };
                        method.Invoke(null, args);
                        parameters = (object[])args[0];
                    }
                }
            }
#endif
        }
    }
}
