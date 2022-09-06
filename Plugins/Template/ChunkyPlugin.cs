using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.VersionControl;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif
#if BMB_LIBRARIES
using BMBLibraries.Classes;
using BMBLibraries.Extensions;
#endif
#endif

namespace ChunkyOSC.Plugins
{
    public class ChunkyTemplate : IChunkyPlugin
    {
#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && BMB_LIBRARIES
        /*
         *  This function is called by the creation tool after it finishes all of its own operations.
         *  
         *  'avatar' contains the descriptor component on the avatar being used.
         *  'gestureController' contains the Gesture animator being used on the avatar.
         *  'fxController' contains the FX animator being using on the avatar.
         *  'backupManager' is the active file backup store being used for execution.
         *  'generated' is a companion to 'backupManager' and is used to hold generated assets.
         *  'destination' is the filepath to be used for storing generated files.
         *  'writeDefaults' states whether created animator states should have writeDefaults enabled or not.
         *  'parameters' holds any other parameters that have been given via the inspector.
         *  
         *  Any files you create and save to disk should be added to the 'generated' AssetList, and any files you plan to modify or 
         *    replace should be added to the 'backupManager' object using backupManager.AddToBackup(Asset asset).
         *  
         *  Return 0 if your code runs successfully, or 1 if you need to abort and the reversion method will be automatically invoked. 
         *    Note that you are responsible for reporting your own error messages for these cases, but any uncaught exceptions will be
         *    reported to the user and handled by the main script automatically.
         */
        public static int ExecutePlugin(VRCAvatarDescriptor avatar, AnimatorController gestureController, AnimatorController fxController, ref Backup backupManager, ref AssetList generated, string destination, bool writeDefaults, object[] parameters)
        {
            // Run whatever code you need to here.

            // Be sure to save the controllers before returning if you modified them.
            //gestureController.SaveController();
            //fxController.SaveController();

            return 0;
        }

        /*
         *  This is an optional method used for displaying any fields in the Inspector that may be needed for your plugin.
         *  Leave this function commented out if you don't plan on having any additional parameters.
         *  
         *  You are provided the 'parameters' array for storing any parameters your plugin may need.
         *  How these parameters are encoded and decoded through this array is completely your decision.
         *  
         *  You are responsible for checking if this array is null and initializing it if necessary.
         *  
         *  You do not need to mark the component as dirty when changes are made, since the calling function will do this for you.
         *  
         *  Please use IMGUI for all GUI code you write here.
         */
        /*
        public static void DisplaySettings(ref object[] parameters)
        {
            
        }
        */
#endif
    }
}