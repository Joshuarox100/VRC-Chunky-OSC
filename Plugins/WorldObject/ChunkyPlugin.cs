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
    public class ChunkyWorld : IChunkyPlugin
    {
#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && BMB_LIBRARIES
        public static int ExecutePlugin(VRCAvatarDescriptor avatar, AnimatorController gestureController, AnimatorController fxController, ref Backup backupManager, ref AssetList generated, string destination, bool writeDefaults, object[] parameters)
        {
            // Extract the GameObject and make sure it isn't null.
            GameObject obj = (GameObject)parameters[0];
            if (obj == null)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "No object was provided, skipping plugin execution...", "Close");
                return 0;
            }

            // Otherwise go ahead and do the stuff.
            AddConstraints(avatar, obj);
            if (CreateClips(avatar, obj, ref generated, ref backupManager, destination, out AnimationClip[] clips) == 1) return 1;
            ConfigureAnimator(ref fxController, clips, writeDefaults);

            // Save the controller.
            fxController.SaveController();

            return 0;
        }

        // GUI code for the arguments.
        public static void DisplaySettings(ref object[] parameters)
        {
            if (parameters == null || parameters.Length != 1)
                parameters = new object[1];

            parameters[0] = EditorGUILayout.ObjectField(new GUIContent("Controlled Object", "The object to be controlled by ChunkyOSC when it's active."), (GameObject)parameters[0], typeof(GameObject), true);
        }

        // Adds the needed constraint to the target object.
        public static void AddConstraints(VRCAvatarDescriptor avatar, GameObject obj)
        {
            // Get the ChunkyOSC pose.
            Transform pose = avatar.transform.Find("ChunkyOSC").Find("Chunky Origin").Find("Chunky Pose");

            // Add the constraint to the object.
            if (obj.TryGetComponent(out ParentConstraint constraint))
            {
                if (constraint.sourceCount > 0)
                    constraint.SetSource(0, new ConstraintSource { sourceTransform = obj.transform.parent, weight = 1 });
                else
                    constraint.AddSource(new ConstraintSource { sourceTransform = obj.transform.parent, weight = 1 });

                if (constraint.sourceCount > 1)
                    constraint.SetSource(1, new ConstraintSource { sourceTransform = pose, weight = 0 });
                else
                    constraint.AddSource(new ConstraintSource { sourceTransform = pose, weight = 0 });

                constraint.constraintActive = true;
            }
            else // If the parent constraint doesn't already exist.
            {
                constraint = obj.AddComponent<ParentConstraint>();
                constraint.AddSource(new ConstraintSource { sourceTransform = obj.transform.parent, weight = 1 });
                constraint.AddSource(new ConstraintSource { sourceTransform = pose, weight = 0 });
                constraint.constraintActive = true;
            }
            
        }

        // Creates the animation clips for toggling the parent constraint source.
        public static int CreateClips(VRCAvatarDescriptor avatar, GameObject obj, ref AssetList generated, ref Backup backupManager, string destination, out AnimationClip[] clips)
        {
            // An array to hold the outted clips.
            clips = new AnimationClip[2];

            // Create the clips needed for the toggle.
            if (!AssetDatabase.IsValidFolder(destination + Path.DirectorySeparatorChar + "Clips"))
                AssetDatabase.CreateFolder(destination, "Clips");
            
            for (int i = 0; i < 2; i++)
            {
                bool state = i == 0;
                string outFile = obj.name + (state ? "_On" : "_Off");

                // Create the Animation Clip
                AnimationClip clip = new AnimationClip();
                string path = GetGameObjectPath(obj.transform);
                path = path.Substring(path.IndexOf(avatar.transform.name) + avatar.transform.name.Length + 1);
                clip.SetCurve(path, typeof(ParentConstraint), "m_Sources.Array.data[0].weight", new AnimationCurve(new Keyframe[2] { new Keyframe() { value = state ? 0 : 1, time = 0 }, new Keyframe() { value = state ? 0 : 1, time = 0.016666668f } }));
                clip.SetCurve(path, typeof(ParentConstraint), "m_Sources.Array.data[1].weight", new AnimationCurve(new Keyframe[2] { new Keyframe() { value = state ? 1 : 0, time = 0 }, new Keyframe() { value = state ? 1 : 0, time = 0.016666668f } }));

                // Save the file
                bool existed = true;
                if (File.Exists(destination + Path.DirectorySeparatorChar + "Clips" + Path.DirectorySeparatorChar + outFile + ".anim"))
                {
                    if (!EditorUtility.DisplayDialog("ChunkyOSC", outFile + ".anim" + " already exists!\nOverwrite the file?", "Overwrite", "Cancel"))
                        return 1;
                    backupManager.AddToBackup(new Asset(destination + Path.DirectorySeparatorChar + "Clips" + Path.DirectorySeparatorChar + outFile + ".anim"));
                    AssetDatabase.DeleteAsset(destination + Path.DirectorySeparatorChar + "Clips" + Path.DirectorySeparatorChar + outFile + ".anim");
                    AssetDatabase.Refresh();
                }
                else
                {
                    existed = false;
                }

                AssetDatabase.CreateAsset(clip, destination + Path.DirectorySeparatorChar + "Clips" + Path.DirectorySeparatorChar + outFile + ".anim");
                AssetDatabase.Refresh();
                if (!existed)
                    generated.Add(new Asset(AssetDatabase.GetAssetPath(clip)));

                clips[i] = clip;
            }
            
            return 0;
        }

        // Adds a new layer to the animator for toggling the constraint source.
        public static void ConfigureAnimator(ref AnimatorController fxController, AnimationClip[] clips, bool writeDefaults)
        {
            fxController.AddLayer("ChunkyOSC (World Object)");
            AnimatorControllerLayer[] layers = fxController.layers;
            AnimatorControllerLayer worldLayer = layers[layers.Length - 1];

            // Machine identifier.
            AnimatorStateTransition identifier = new AnimatorStateTransition
            {
                name = "ChunkyOSCMachineIdentifier",
                hasExitTime = false,
                isExit = false,
                mute = true,
                destinationState = null,
                destinationStateMachine = null
            };

            // Move down a row in the grid.
            Vector3 pos = worldLayer.stateMachine.entryPosition;
            pos += new Vector3(0, 125);

            // Create a template state for cloning.
            ChildAnimatorState templateState = new ChildAnimatorState
            {
                state = new AnimatorState
                {
                    name = "",
                    behaviours = new StateMachineBehaviour[0],
                    writeDefaultValues = writeDefaults
                }
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

            // Create an array states to be created.
            List<ChildAnimatorState> states = new List<ChildAnimatorState>();

            // Create the disabled state.
            templateState.position = pos - new Vector3(150, 0);
            templateState.state.name = "Disabled";
            templateState.state.motion = clips[1];
            states.Add(templateState.DeepClone());
            int offIdx = states.Count - 1;

            // Create the enabled state.
            templateState.position = pos + new Vector3(125, 0);
            templateState.state.name = "Enabled";
            templateState.state.motion = clips[0];
            states.Add(templateState.DeepClone());
            int onIdx = states.Count - 1;

            // Create Transitions.
            List<AnimatorStateTransition> anyTransitions = new List<AnimatorStateTransition>() { (AnimatorStateTransition)identifier.DeepClone() };

            // Add the transition for the off state.
            templateTransition.destinationState = states[offIdx].state;
            templateTransition.conditions = new AnimatorCondition[0];
            templateTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "ChunkyOSC");
            anyTransitions.Add((AnimatorStateTransition)templateTransition.DeepClone(states[offIdx]));

            // Add the transition for the on state.
            templateTransition.destinationState = states[onIdx].state;
            templateTransition.conditions = new AnimatorCondition[0];
            templateTransition.AddCondition(AnimatorConditionMode.If, 0, "ChunkyOSC");
            anyTransitions.Add((AnimatorStateTransition)templateTransition.DeepClone(states[onIdx]));

            // Finalize the machine settings.
            AnimatorStateMachine machine = worldLayer.stateMachine;
            machine.states = states.ToArray();
            machine.defaultState = states[offIdx].state;
            machine.anyStateTransitions = anyTransitions.ToArray();
            worldLayer.stateMachine = machine;
            worldLayer.defaultWeight = 1f;

            // Set updated layer back into list.
            layers[layers.Length - 1] = worldLayer;
            fxController.layers = layers;
        }

        // Helper method for getting the path of a game object.
        public static string GetGameObjectPath(Transform transform)
        {
            if (transform == null) 
                return "";

            // Build the path string.
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
#endif
    }
}