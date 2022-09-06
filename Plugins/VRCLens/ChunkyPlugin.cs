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
    public class ChunkyVRCL : IChunkyPlugin
    {
#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && BMB_LIBRARIES
        private static readonly string path = "Assets/ChunkyOSC/Plugins/VRCLens";

        public static int ExecutePlugin(VRCAvatarDescriptor avatar, AnimatorController gestureController, AnimatorController fxController, ref Backup backupManager, ref AssetList generated, string destination, bool writeDefaults, object[] parameters)
        {
            // First, we configure the constraints for the camera.
            if (ConfigureConstraints(avatar) == 1) return 1;

            // Then, we need to "hack" the VRCLens drop layer to support OSC positions.
            if (UpdateDropLayer(ref fxController, writeDefaults) == 1) return 1;

            // Finally, save the controller and assets.
            fxController.SaveController();
            AssetDatabase.SaveAssets();

            return 0;
        }

        // Configures the constraints on the avatar.
        private static int ConfigureConstraints(VRCAvatarDescriptor avatar)
        {
            GameObject oscPose;
            GameObject vrcLens;

            // Get the OSC Pose.
            try
            {
                oscPose = avatar.transform.Find("ChunkyOSC").Find("Chunky Origin").Find("Chunky Pose").gameObject;
            }
            catch
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "Could not find the ChunkyOSC prefab.", "Close");
                return 1;
            }

            // Get the VRCLens camera.
            try
            {
                vrcLens = avatar.transform.Find("VRCLens").Find("WorldC").Find("CamPickup").Find("CamBase").Find("CamObject").gameObject;
            }
            catch
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "Could not find the VRCLens camera.", "Close");
                return 1;
            }

            // Get the constraints.
            ParentConstraint parConst = vrcLens.GetComponent<ParentConstraint>();

            // Assert the existance of the constraints.
            if (parConst == null)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "There is not a valid VRCLens configuration on the avatar.", "Close");
                return 1;
            }

            // Add the needed source.
            if (parConst.sourceCount != 3)
                parConst.AddSource(new ConstraintSource() { sourceTransform = oscPose.transform, weight = 0 });
            else
                parConst.SetSource(2, new ConstraintSource() { sourceTransform = oscPose.transform, weight = 0 });

            return 0;
        }

        // Updates the VRCLens Drop layer to support OSC world positions.
        private static int UpdateDropLayer(ref AnimatorController controller, bool writeDefaults)
        {
            // Get the override clip from the plugin folder.
            AnimationClip overrideClip = (AssetDatabase.FindAssets("DropHardEnable (OSC)", new string[] { path }).Length != 0) ? (AnimationClip)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("DropHardEnable (OSC)", new string[] { path })[0]), typeof(AnimationClip)) : null;

            // Get the modified source clip from the plugin folder.
            AnimationClip sourceClip = (AssetDatabase.FindAssets("DropHardEnable (Non-OSC)", new string[] { path }).Length != 0) ? (AnimationClip)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("DropHardEnable (Non-OSC)", new string[] { path })[0]), typeof(AnimationClip)) : null;

            if (overrideClip == null)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: One or more plugin files are missing.", "Close");
                return 1;
            }

            // Machine identifier.
            AnimatorStateTransition identifier = new AnimatorStateTransition
            {
                name = "ChunkyOSCOverrideIdentifier",
                hasExitTime = false,
                isExit = false,
                mute = true,
                destinationState = null,
                destinationStateMachine = null
            };

            // Hold the controller layers.
            AnimatorControllerLayer[] sourceLayers = controller.layers;

            // Find the drop layer.
            int layerIndex = -1;
            for (int i = 0; i < sourceLayers.Length; i++)
            {
                if (sourceLayers[i].name.StartsWith("vCNT_Drop"))
                {
                    layerIndex = i;
                    break;
                }
            }

            // Layer was not found.
            if (layerIndex == -1)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: The required VRCLens layer was not found on the avatar.", "Close");
                return 1;
            }

            // Hold the layer and states.
            AnimatorControllerLayer layer = sourceLayers[layerIndex];
            List<ChildAnimatorState> states = new List<ChildAnimatorState>(layer.stateMachine.states);

            // Check if we've already updated this layer.
            foreach (AnimatorStateTransition transition in layer.stateMachine.anyStateTransitions)
            {
                if (transition.name == "ChunkyOSCOverrideIdentifier" && transition.isExit == false && transition.mute == true && transition.destinationState == null && transition.destinationStateMachine == null)
                {
                    // Find our state and make sure write defaults is set correctly.
                    for (int i = 0; i < states.Count; i++)
                    {
                        if (states[i].state.name == "Dropped (OSC)")
                        {
                            states[i].state.writeDefaultValues = writeDefaults;
                            break;
                        }
                    }

                    // Apply changes and exit.
                    layer.stateMachine.states = states.ToArray();
                    sourceLayers[layerIndex] = layer;
                    controller.layers = sourceLayers;
                    return 0;
                }
            }

            // Find the state we want.
            int stateIndex = -1;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].state.name == "Dropped")
                {
                    stateIndex = i;
                    break;
                }
            }

            // State was not found.
            if (stateIndex == -1)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: The required VRCLens state was not found on the avatar.", "Close");
                return 1;
            }

            // Hold the found state.
            ChildAnimatorState state = states[stateIndex];

            // Create the OSC state.
            ChildAnimatorState oscState = new ChildAnimatorState()
            {
                position = state.position + new Vector3(250, 0),
                state = new AnimatorState { name = "Dropped (OSC)", motion = overrideClip, writeDefaultValues = writeDefaults }
            };

            // Create a template transition.
            AnimatorStateTransition templateTransition = new AnimatorStateTransition
            {
                destinationState = null,
                isExit = false,
                hasExitTime = false,
                exitTime = 0,
                duration = 0,
                canTransitionToSelf = false,
                conditions = null
            };

            // Update the motion on the source state.
            state.state.motion = sourceClip;

            // Add transitions between the states.
            templateTransition.destinationState = oscState.state;
            templateTransition.AddCondition(AnimatorConditionMode.If, 0, "ChunkyOSC");
            state.state.AddTransition((AnimatorStateTransition)templateTransition.DeepClone());

            templateTransition.destinationState = state.state;
            templateTransition.conditions = new AnimatorCondition[0];
            templateTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "ChunkyOSC");
            oscState.state.AddTransition((AnimatorStateTransition)templateTransition.DeepClone());

            // Apply the changes to the layer.
            states[stateIndex] = state;
            states.Add(oscState);
            layer.stateMachine.states = states.ToArray();
            layer.stateMachine.anyStateTransitions = new AnimatorStateTransition[1] { (AnimatorStateTransition)identifier.DeepClone() };

            // Update the layer within the controller.
            sourceLayers[layerIndex] = layer;
            controller.layers = sourceLayers;

            return 0;
        }
#endif
    }
}