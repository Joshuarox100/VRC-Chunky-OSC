using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif
using System;
using UnityEngine.Animations;
#if UNITY_EDITOR
using static ChunkyOSC.ChunkyPluginUtility;
#if BMB_LIBRARIES
using BMBLibraries.Classes;
using BMBLibraries.Extensions;
#endif
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.VersionControl;
#endif

namespace ChunkyOSC
{
    [AddComponentMenu("ChunkyOSC/Setup ChunkyOSC")]
    public class ChunkyOSCSetup : MonoBehaviour
    {
#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && BMB_LIBRARIES
        public VRCAvatarDescriptor avatar;
        public VRCExpressionsMenu menu;
        public string destination = "Assets/ChunkyOSC/Temp";

        public bool writeDefaults = false;

        [HideInInspector]
        public ChunkyPlugin plugin;
        [HideInInspector]
        public object[] pluginArgs;

        [SerializeField, Header("DO NOT MODIFY")]
        private Transform prefab;
        [SerializeField]
        private AvatarMask mask;
        [SerializeField]
        private AnimationClip defaultClip;
        [SerializeField]
        private AnimatorController gestureReference;
        [SerializeField]
        private AnimatorController fxReference;
        [SerializeField]
        private VRCExpressionsMenu exprMenu;
        [SerializeField]
        private VRCExpressionParameters exprParams;
        [SerializeField]
        private Texture2D menuIcon;

        // File backup.
        private Backup backupManager;
        private AssetList generated;

        // Setup ChunkyOSC on an avatar.
        public void SetupChunkyOSC()
        {
            EditorUtility.DisplayProgressBar("ChunkyOSC", "Checking for Errors", 0f);

            // Before we do anything, validate the avatar.
            if (!ValidateDescriptor())
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            // Now we should create the systems for backing up and restoring files in case anything goes wrong.
            backupManager = new Backup();
            generated = new AssetList();

            // To make sure we catch missed exceptions, we'll use a try block.
            try
            {
                // First we need to backup and modify the animator to not have layers we may have added before.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Obtaining Gesture Animator", 0f);
                #region Create Gesture Animator
                AnimatorController animator = avatar.baseAnimationLayers[2].animatorController != null ? (AnimatorController)avatar.baseAnimationLayers[2].animatorController : null;
                AnimatorController gesture = animator;
                bool hadAnimator = animator != null;

                // If one was provided
                if (hadAnimator)
                {
                    // Backup original animator
                    backupManager.AddToBackup(new Asset(AssetDatabase.GetAssetPath(animator)));

                    // Duplicate the source controller.
                    AssetDatabase.CopyAsset(new Asset(AssetDatabase.GetAssetPath(animator)).path, destination + Path.DirectorySeparatorChar + "temp.controller");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    gesture = (AnimatorController)AssetDatabase.LoadAssetAtPath(destination + Path.DirectorySeparatorChar + "temp.controller", typeof(AnimatorController));
                    generated.Add(new Asset(AssetDatabase.GetAssetPath(gesture)));

                    // Remove exising layers.
                    for (int i = animator.layers.Length - 1; i >= 0; i--)
                    {
                        // A layer is an created layer if the State Machine has a "special transition"
                        bool hasSpecialTransition = false;
                        foreach (AnimatorStateTransition transition in animator.layers[i].stateMachine.anyStateTransitions)
                        {
                            if (transition.name == "ChunkyOSCMachineIdentifier" && transition.isExit == false && transition.mute == true && transition.destinationState == null && transition.destinationStateMachine == null)
                            {
                                hasSpecialTransition = true;
                                break;
                            }
                        }

                        if (hasSpecialTransition)
                        {
                            EditorUtility.DisplayProgressBar("ChunkyOSC", string.Format("Removing Layers: {0}", animator.layers[i].name), 0.05f + 0.025f * (float.Parse((animator.layers.Length - i).ToString()) / animator.layers.Length));

                            gesture.RemoveLayer(i);

                            EditorUtility.DisplayProgressBar("ChunkyOSC", string.Format("Removing Layers: {0}", animator.layers[i].name), 0.075f + 0.025f * ((animator.layers.Length - i + 1f) / animator.layers.Length));
                        }
                    }
                    gesture.SaveController();
                }
                else
                {
                    // Clone the template Gesture animator.
                    switch (CopyTemplate(avatar.name + "_Gesture.controller", "AV3 Demo Gesture"))
                    {
                        case 1:
                            EditorUtility.DisplayDialog("ChunkyOSC", "Cancelled.", "Close");
                            RevertChanges();
                            return;
                        case 2:
                            EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: Failed to create one or more files!", "Close");
                            RevertChanges();
                            return;
                    }

                    gesture = (AssetDatabase.FindAssets(avatar.name + "_Gesture", new string[] { destination + Path.DirectorySeparatorChar + "Animators" }).Length != 0) ? (AnimatorController)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(avatar.name + "_Gesture", new string[] { destination + Path.DirectorySeparatorChar + "Animators" })[0]), typeof(AnimatorController)) : null;

                    if (gesture == null)
                    {
                        EditorUtility.DisplayDialog("ChunkyOSC", "Failed to copy template Animator from VRCSDK.", "Close");
                        RevertChanges();
                        return;
                    }
                }

                // Replace the Animator Controller in the descriptor if it was there.
                bool replaceAnimator = animator != null
                    && avatar.baseAnimationLayers[2].animatorController != null
                    && animator == (AnimatorController)avatar.baseAnimationLayers[2].animatorController;

                // Replace the old Animator Controller.
                if (replaceAnimator)
                {
                    string path = AssetDatabase.GetAssetPath(animator);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(gesture), path);
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(gesture), path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                EditorUtility.SetDirty(avatar);
                avatar.baseAnimationLayers[2].animatorController = gesture;
                avatar.baseAnimationLayers[2].isDefault = false;
                avatar.baseAnimationLayers[2].isEnabled = true;
                #endregion

                // Then, we must add the needed layers to the animator.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Adding Gesture Layers", 0.05f);
                if (AddControllerLayers(ref gesture, in gestureReference) == 1) return;

                // Next, we save everything and move to the FX animator.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Saving (Gesture)", 0.40f);
                gesture.SaveController();
                AssetDatabase.SaveAssets();

                // First, acquire or create the FX animator so we can work with it.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Obtaining FX Animator", 0.45f);
                #region Create FX Animator
                animator = avatar.baseAnimationLayers[4].animatorController != null ? (AnimatorController)avatar.baseAnimationLayers[4].animatorController : null;
                AnimatorController fxController = animator;
                hadAnimator = animator != null;

                // If one was provided
                if (hadAnimator)
                {
                    // Backup original animator
                    backupManager.AddToBackup(new Asset(AssetDatabase.GetAssetPath(animator)));

                    // Duplicate the source controller.
                    AssetDatabase.CopyAsset(new Asset(AssetDatabase.GetAssetPath(animator)).path, destination + Path.DirectorySeparatorChar + "temp.controller");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    fxController = (AnimatorController)AssetDatabase.LoadAssetAtPath(destination + Path.DirectorySeparatorChar + "temp.controller", typeof(AnimatorController));
                    generated.Add(new Asset(AssetDatabase.GetAssetPath(fxController)));

                    // Remove exising layers.
                    for (int i = animator.layers.Length - 1; i >= 0; i--)
                    {
                        // A layer is an created layer if the State Machine has a "special transition"
                        bool hasSpecialTransition = false;

                        foreach (AnimatorStateTransition transition in animator.layers[i].stateMachine.anyStateTransitions)
                        {
                            if (transition.name == "ChunkyOSCMachineIdentifier" && transition.isExit == false && transition.mute == true && transition.destinationState == null && transition.destinationStateMachine == null)
                            {
                                hasSpecialTransition = true;
                                break;
                            }
                        }

                        if (hasSpecialTransition)
                        {
                            EditorUtility.DisplayProgressBar("ChunkyOSC", string.Format("Removing Layers: {0}", animator.layers[i].name), 0.8625f + 0.0125f * (float.Parse((animator.layers.Length - i).ToString()) / animator.layers.Length));

                            fxController.RemoveLayer(i);

                            EditorUtility.DisplayProgressBar("ChunkyOSC", string.Format("Removing Layers: {0}", animator.layers[i].name), 0.8625f + 0.0125f * ((animator.layers.Length - i + 1f) / animator.layers.Length));
                        }
                    }
                    fxController.SaveController();
                }
                else
                {
                    // Clone the template FX animator.
                    switch (CopyTemplate(avatar.name + "_FX.controller", "AV3 Demo FX"))
                    {
                        case 1:
                            EditorUtility.DisplayDialog("ChunkyOSC", "Cancelled.", "Close");
                            RevertChanges();
                            return;
                        case 2:
                            EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: Failed to create one or more files!", "Close");
                            RevertChanges();
                            return;
                    }

                    fxController = (AssetDatabase.FindAssets(avatar.name + "_FX", new string[] { destination + Path.DirectorySeparatorChar + "Animators" }).Length != 0) ? (AnimatorController)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(avatar.name + "_FX", new string[] { destination + Path.DirectorySeparatorChar + "Animators" })[0]), typeof(AnimatorController)) : null;

                    if (fxController == null)
                    {
                        EditorUtility.DisplayDialog("ChunkyOSC", "Failed to copy template Animator from VRCSDK.", "Close");
                        RevertChanges();
                        return;
                    }
                }

                // Replace the Animator Controller in the descriptor if it was there.
                replaceAnimator = animator != null
                    && avatar.baseAnimationLayers[4].animatorController != null
                    && animator == (AnimatorController)avatar.baseAnimationLayers[4].animatorController;

                // Replace the old Animator Controller.
                if (replaceAnimator)
                {
                    string path = AssetDatabase.GetAssetPath(animator);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(fxController), path);
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(fxController), path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                EditorUtility.SetDirty(avatar);
                avatar.baseAnimationLayers[4].animatorController = fxController;
                avatar.baseAnimationLayers[4].isDefault = false;
                avatar.baseAnimationLayers[4].isEnabled = true;
                #endregion

                // We need to add the necessary layers to it too.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Adding FX Layers", 0.50f);
                if (AddControllerLayers(ref fxController, in fxReference) == 1) return;

                // Next we'll add the prefab object to the root of the avatar.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Adding Prefab to Avatar", 0.75f);
                AddWorldPrefab();

                // Time to finalize some settings on the avatar.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Adding Expression Parameters", 0.80f);
                if (AddExpressionParameters() == 1) return;

                EditorUtility.DisplayProgressBar("ChunkyOSC", "Adding Expressions Menu", 0.85f);
                AddExpressionsMenu();

                // Finally, we save everything.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Saving (FX & Expressions)", 0.875f);
                fxController.SaveController();
                AssetDatabase.SaveAssets();

                // Run any extra plugin code since we're done with our part now.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Running Plugin Scripts", 0.9f);
                if (plugin != null && plugin.script != null &&
                    plugin.ExecutePlugin(avatar, gesture, fxController, ref backupManager, ref generated, destination, writeDefaults,  pluginArgs) == 1)
                {
                    RevertChanges();
                    return;
                }

                // Run a save after the plugin is done with its work.
                EditorUtility.DisplayProgressBar("ChunkyOSC", "Saving", 1f);
                AssetDatabase.SaveAssets();

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("ChunkyOSC", "Done.", "Close");
            }
            catch (Exception err)
            {
                // If we missed anything, assume it's unrecoverable and abort the operation.
                Debug.LogError(err);
                EditorUtility.DisplayDialog("ChunkyOSC", "Uncaught exception occurred. Look at the console for details.", "Close");
                RevertChanges();
                return;
            }
        }

        // Validates the given avatar before attempting anything.
        private bool ValidateDescriptor()
        {
            // Check required objects aren't null.
            if (mask == null || defaultClip == null || gestureReference == null || fxReference == null || exprMenu == null || exprParams == null)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "Please use the prefab to do the setup.", "Close");
                return false;
            }

            if (avatar == null)
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "No avatar was provided.", "Close");
                return false;
            }

            // Animation Layers
            if (avatar.baseAnimationLayers != null && avatar.baseAnimationLayers.Length == 5)
            {
                // Gesture
                if (avatar.baseAnimationLayers[2].isDefault == false && avatar.baseAnimationLayers[2].animatorController == null)
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "No Gesture animator exists on the avatar.", "Close");
                    return false;
                }

                // FX
                if (avatar.baseAnimationLayers[4].isDefault == false && avatar.baseAnimationLayers[4].animatorController == null)
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "No FX animator exists on the avatar.", "Close");
                    return false;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "The avatar must be Humanoid.", "Close");
                return false;
            }

            // Expressions
            if (avatar.customExpressions)
            {
                // Check that the menu has space.
                VRCExpressionsMenu mainMenu = menu ?? avatar.expressionsMenu;
                if (mainMenu != null && mainMenu.controls.Count >= 8)
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "There must be at least one unused control on the Expressions Menu.", "Close");
                    return false;
                }

                // Check that a parameters object exists.
                if (avatar.expressionParameters == null)
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "No Expression Parameters are set for the avatar.", "Close");
                    return false;
                }

                // Exclude existing parameters, count non-existant ones.
                int neededBits = 0;
                {
                    List<VRCExpressionParameters.Parameter> requiredParameters = new List<VRCExpressionParameters.Parameter>(exprParams.parameters);
                    List<string> parameters = new List<string>();
                    foreach (VRCExpressionParameters.Parameter param in requiredParameters)
                        parameters.Add(param.name);

                    // Get the bits of parameters not present.
                    foreach (string param in parameters)
                    {
                        if (avatar.expressionParameters.FindParameter(param) != null)
                        {
                            if (avatar.expressionParameters.FindParameter(param).valueType != exprParams.FindParameter(param).valueType)
                            {
                                EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: Expression Parameter \"" + param + "\" is present with the wrong type.", "Close");
                                Selection.activeObject = avatar.expressionParameters;
                                return false;
                            }
                        }
                        else
                        {
                            switch (exprParams.FindParameter(param).valueType)
                            {
                                case VRCExpressionParameters.ValueType.Bool:
                                    neededBits += 1;
                                    break;
                                default:
                                    neededBits += 8;
                                    break;
                            }
                        }
                    }
                }

                if (avatar.expressionParameters.CalcTotalCost() + neededBits > VRCExpressionParameters.MAX_PARAMETER_COST)
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "There is not enough space for all the required Expression Parameters.\n(Need " + (avatar.expressionParameters.CalcTotalCost() + neededBits - VRCExpressionParameters.MAX_PARAMETER_COST) + " more bits.)", "Close");
                    return false;
                }
            }

            // Destination
            AssetDatabase.Refresh(); // Catch newly made folders in Explorer.
            if (!AssetDatabase.IsValidFolder(destination))
            {
                // Check if the parent folder exists.
                string parent = destination.Substring(0, destination.LastIndexOf('/'));
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "The chosen file destination does not exist.", "Close");
                    return false;
                }

                // Attempt creating the folder.
                if (AssetDatabase.CreateFolder(parent, destination.Substring(destination.LastIndexOf('/') + 1)) == "")
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "The chosen file destination does not exist and could not be created.", "Close");
                    return false;
                }
            }

            return true;
        }

        // Adds the required parameters to the Gesture animator controller on the avatar.
        private int AddAnimatorParameters(ref AnimatorController controller, in AnimatorController reference)
        {
            // Get the required parameters from the other controller.
            List<AnimatorControllerParameter> requiredParameters = new List<AnimatorControllerParameter>(reference.parameters);
            List<string> reqNames = new List<string>();
            foreach (AnimatorControllerParameter param in requiredParameters)
                reqNames.Add(param.name);

            // Check if the parameters already exist. If one does as the correct type, use it. If one already exists as the wrong type, abort.
            AnimatorControllerParameter[] existingParameters = controller.parameters;
            List<AnimatorControllerParameter> missingParameters = new List<AnimatorControllerParameter>(requiredParameters);

            foreach (AnimatorControllerParameter param in existingParameters)
            {
                if (reqNames.Contains(param.name))
                {
                    int paramIdx = reqNames.IndexOf(param.name);
                    if (param.type != requiredParameters[paramIdx].type)
                    {
                        EditorUtility.DisplayDialog("ChunkyOSC", "ERROR: Animator Parameter \"" + param.name + "\" already exists as the incorrect type.", "Close");
                        RevertChanges();
                        Selection.activeObject = controller;
                        return 1;
                    }
                    missingParameters.Remove(requiredParameters[paramIdx]);
                }
            }

            // Add any missing parameters.
            foreach (AnimatorControllerParameter param in missingParameters)
                controller.AddParameter(param.name, param.type);

            return 0;
        }

        // Copies the layers from one animator to another.
        private void CopyAnimatorLayers(ref AnimatorController controller, in AnimatorController reference)
        {
            // Hold the reference layers.
            AnimatorControllerLayer[] sourceLayers = reference.layers;

            // Add each layer to the controller.
            int offset = controller.layers.Length;
            foreach (AnimatorControllerLayer layer in sourceLayers)
            {
                if (layer.syncedLayerIndex != -1)
                {
                    // Get all overridden motions.
                    List<KeyValuePair<int, Motion>> overridden = new List<KeyValuePair<int, Motion>>();
                    ChildAnimatorState[] sourceStates = sourceLayers[layer.syncedLayerIndex].stateMachine.states;
                    for (int i = 0; i < sourceStates.Length; i++)
                    {
                        Motion replacedMotion = layer.GetOverrideMotion(sourceStates[i].state);
                        if (replacedMotion != null)
                            overridden.Add(new KeyValuePair<int, Motion>(i, replacedMotion));
                    }

                    // Create clone of layer.
                    layer.syncedLayerIndex += offset;
                    AnimatorControllerLayer copy = layer.DeepClone();

                    // Reapply all overridden motions.
                    foreach (KeyValuePair<int, Motion> replacement in overridden)
                        copy.SetOverrideMotion(controller.layers[copy.syncedLayerIndex].stateMachine.states[replacement.Key].state, replacement.Value);

                    controller.AddLayer(copy);
                }
                else
                {
                    AnimatorControllerLayer copy = layer.DeepClone();

                    // Set write defaults as specified.
                    AnimatorStateMachine machine = copy.stateMachine;
                    ChildAnimatorState[] states = machine.states;
                    for (int i = 0; i < states.Length; i++)
                        states[i].state.writeDefaultValues = writeDefaults;
                    machine.states = states;
                    copy.stateMachine = machine;

                    controller.AddLayer(copy);
                }
            }
        }

        // Adds the parameters and layers from an animator controller.
        private int AddControllerLayers(ref AnimatorController controller, in AnimatorController reference)
        {
            // Add the parameters.
            if (AddAnimatorParameters(ref controller, in reference) == 1) return 1;

            // Add the layers.
            CopyAnimatorLayers(ref controller, in reference);

            return 0;
        }

        // Adds the required parameters to the Expression Parameters attached to the avatar.
        private int AddExpressionParameters()
        {
            List<VRCExpressionParameters.Parameter> requiredParameters = new List<VRCExpressionParameters.Parameter>(exprParams.parameters);
            List<string> requiredNames = new List<string>();
            foreach (VRCExpressionParameters.Parameter param in requiredParameters)
                requiredNames.Add(param.name);

            bool expOverwrite = false;

            List<VRCExpressionParameters.Parameter> parameters = new List<VRCExpressionParameters.Parameter>(avatar.expressionParameters.parameters);

            // Add or replace Expression Parameters as needed.
            foreach (string name in requiredNames)
            {
                if (avatar.expressionParameters.FindParameter(name) != null)
                {
                    if (!expOverwrite)
                    {
                        if (!EditorUtility.DisplayDialog("ChunkyOSC", "One or more Expression Parameters already exist!\nOverwrite them all?", "Overwrite", "Cancel"))
                        {
                            EditorUtility.DisplayDialog("ChunkyOSC", "Cancelled.", "Close");
                            Selection.activeObject = avatar.expressionParameters;
                            RevertChanges();
                            return 1;
                        }
                        else
                            expOverwrite = true;
                    }
                    parameters.Remove(avatar.expressionParameters.FindParameter(name));
                }
                VRCExpressionParameters.Parameter item = exprParams.FindParameter(name);
                parameters.Add(new VRCExpressionParameters.Parameter()
                {
                    name = item.name,
                    saved = item.saved,
                    valueType = item.valueType,
                    defaultValue = item.defaultValue
                });
            }

            // Update the parameters on the avatar.
            EditorUtility.SetDirty(avatar.expressionParameters);
            avatar.expressionParameters.parameters = parameters.ToArray();

            return 0;
        }

        // Adds the control menu to the avatar's Expressions Menu.
        private void AddExpressionsMenu()
        {
            VRCExpressionsMenu mainMenu = menu ?? avatar.expressionsMenu;

            // Use as first menu if none present.
            if (mainMenu == null)
            {
                EditorUtility.SetDirty(avatar.expressionsMenu);
                avatar.expressionsMenu = exprMenu;
                return;
            }

            // Check if the control already exists.
            foreach (VRCExpressionsMenu.Control control in mainMenu.controls)
            {
                if (control.name == "ChunkyOSC" && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    return;
            }

            // Otherwise append it to the root or given menu.
            EditorUtility.SetDirty(avatar.expressionsMenu);
            avatar.expressionsMenu.controls.Add(new VRCExpressionsMenu.Control()
            {
                name = "ChunkyOSC",
                icon = menuIcon,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = exprMenu
            });
        }

        // Adds the main prefab to the avatar.
        private void AddWorldPrefab()
        {
            // Check if it has already been added. If so, replace.
            Transform instance = avatar.transform.Find(prefab.name);
            if (instance != null)
            {
                if (EditorUtility.DisplayDialog("ChunkyOSC", "The OSC prefab is already on the avatar.\nReplace it?", "Replace", "Skip"))
                    DestroyImmediate(instance.gameObject);
                else
                    return;
            }

            // Add the prefab to the avatar.
            Transform newInstance = (Transform)PrefabUtility.InstantiatePrefab(prefab, avatar.transform);
            newInstance.localScale =
                new Vector3(1f / avatar.transform.localScale.x, 1f / avatar.transform.localScale.y, 1f / avatar.transform.localScale.z);
        }

        // Copies a template file from the package to the given location.
        private int CopyTemplate(string outFile, string templateFile)
        {
            if (!AssetDatabase.IsValidFolder(destination + Path.DirectorySeparatorChar + "Animators"))
                AssetDatabase.CreateFolder(destination, "Animators");
            bool existed = true;
            if (File.Exists(destination + Path.DirectorySeparatorChar + "Animators" + Path.DirectorySeparatorChar + outFile))
            {
                if (!EditorUtility.DisplayDialog("ChunkyOSC", outFile + " already exists!\nOverwrite the file?", "Overwrite", "Cancel"))
                {
                    EditorUtility.DisplayDialog("ChunkyOSC", "Cancelled.", "Close");
                    RevertChanges();
                    return 2;
                }
                backupManager.AddToBackup(new Asset(destination + Path.DirectorySeparatorChar + "Animators" + Path.DirectorySeparatorChar + outFile));
            }
            else
            {
                existed = false;
            }
            if (!AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(templateFile, new string[] { "Assets" + Path.DirectorySeparatorChar + "ChunkyOSC" + Path.DirectorySeparatorChar + "Assets" + Path.DirectorySeparatorChar + "Templates" })[0]), destination + Path.DirectorySeparatorChar + "Animators" + Path.DirectorySeparatorChar + outFile))
            {
                EditorUtility.DisplayDialog("ChunkyOSC", "Failed to create one or more files. Aborting...", "Close");
                RevertChanges();
                return 1;
            }
            else
            {
                AssetDatabase.Refresh();
                if (!existed)
                    generated.Add(new Asset(destination + Path.DirectorySeparatorChar + "Animators" + Path.DirectorySeparatorChar + outFile));
            }
            return 0;
        }

        // Reverts any changes made during the process in case of an error or exception.
        public void RevertChanges()
        {
            // Save Assets.
            AssetDatabase.SaveAssets();

            // Restore original data to pre-existing files.
            EditorUtility.DisplayProgressBar("ChunkyOSC", "Reverting Changes", 0f);
            if (backupManager != null && !backupManager.RestoreAssets())
                Debug.LogError("[ChunkyOSC] Failed to revert all changes.");

            // Delete any generated assets that didn't overwrite files.
            EditorUtility.DisplayProgressBar("ChunkyOSC", "Reverting Changes", 0.25f);
            for (int i = 0; generated != null && i < generated.ToArray().Length; i++)
                if (File.Exists(generated[i].path) && !AssetDatabase.DeleteAsset(generated[i].path))
                    Debug.LogError("[ChunkyOSC] Failed to revert all changes.");

            // Save assets so folders will be seen as empty.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Delete created folders if now empty.
            if (AssetDatabase.IsValidFolder(destination + Path.DirectorySeparatorChar + "Animators") && AssetDatabase.FindAssets("", new string[] { destination + Path.DirectorySeparatorChar + "Animators" }).Length == 0)
                if (!AssetDatabase.DeleteAsset(destination + Path.DirectorySeparatorChar + "Animators"))
                    Debug.LogError("[ChunkyOSC] Failed to revert all changes.");

            // Final asset save.
            EditorUtility.DisplayProgressBar("ChunkyOSC", "Saving", 0.99f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }
#endif
    }
}