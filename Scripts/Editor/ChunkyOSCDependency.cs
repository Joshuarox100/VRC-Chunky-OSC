using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.Linq;

namespace ChunkyOSC
{
    [InitializeOnLoad]
    public class ChunkySymbols : Editor
    {
        // Symbols to add.
        public static readonly string[] Symbols = new string[] {
            "OSC_CORE",
        };

        // Adds the symbols to compilation if present.
        static ChunkySymbols()
        {
            // Get list of symbols.
            string symbolsStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> symbols = symbolsStr.Split(';').ToList();

            // Check if OscCore is present.
            if (Type.GetType("OscCore.OscClient, OscCore.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") != null)
            {
                // Add our symbols.
                symbols.AddRange(Symbols.Except(symbols));
            }
            else
            {
                // Remove our symbols.
                foreach (string symbol in Symbols)
                    symbols.Remove(symbol);
            }

            // Reassign symbols list to the player.
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", symbols.ToArray()));
        }
    }

    public static class Packages
    {
        static AddRequest Request;
        public static void AddPackage(string name, string url)
        {
            // Add a package to the project
            Request = Client.Add(url);
            EditorApplication.update += Progress;

            EditorUtility.DisplayProgressBar("ChunkyOSC", "Installing " + name + "...", 0f);
        }

        public static void Progress()
        {
            if (Request.IsCompleted)
            {
                if (Request.Status == StatusCode.Success)
                    Debug.Log("Installed: " + Request.Result.packageId);
                else if (Request.Status >= StatusCode.Failure)
                    Debug.Log(Request.Error.message);

                EditorUtility.ClearProgressBar();
                EditorApplication.update -= Progress;
            }
        }
    }
}


