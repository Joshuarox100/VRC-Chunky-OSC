using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ChunkyOSC
{
    public class PresetUtility
    {
#if UNITY_EDITOR
        private static Preset LoadPreset(TextAsset package)
        {
            return Preset.CreateFromJson(package.text);
        }

        public static void SavePreset(string name, OscParameter[] parameters)
        {
            if (name == "")
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "Preset names cannot be blank.", "Close");
                return;
            }

            string json = JsonUtility.ToJson(new Preset { name = name, parameters = parameters, custom = true });
            
            if (File.Exists("Assets/ChunkyOSC/Presets/" + name + ".json") && !EditorUtility.DisplayDialog("ChunkyOSC", "Preset already exists. Overwrite it?", "Yes", "No"))
                return;

            File.WriteAllText("Assets/ChunkyOSC/Presets/" + name + ".json", json);
            AssetDatabase.Refresh();
        }

        public static Preset[] GetPresets()
        {
            List<Preset> presets = new List<Preset>();

            foreach (string packageGuid in AssetDatabase.FindAssets("", new string[] { "Assets/ChunkyOSC/Presets" }))
                try { presets.Add(LoadPreset(AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(packageGuid)))); } catch { }

            return presets.ToArray();
        }

        public static string[] GetPresetNames(Preset[] presets)
        {
            List<string> presetNames = new List<string>();
            foreach (Preset preset in presets)
                presetNames.Add(preset.name);
            return presetNames.ToArray();
        }
#endif
        public class Preset
        {
            public string name;
            public OscParameter[] parameters;
            public bool custom = true;

#if UNITY_EDITOR
            public static Preset CreateFromJson(string json)
            {
                return JsonUtility.FromJson<Preset>(json);
            }
#endif
        }
    }
}