# ChunkyOSC
Author: Joshuarox100  
Version: 1.0.0

<p align="center"><iframe width="560" height="315" src="https://www.youtube.com/embed/57fyZ9Lw4Io" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe></p>

Description: Take control of objects with the power of OSC! With ChunkyOSC, it's easy to create and control objects in world space, with any position and rotation synced remotely for several thousand meters (even for late-joiners) with amazing precision!

> If you would rather watch a video tutorial for getting set up, you can watch the one I have posted on my YouTube channel [here](https://youtu.be/02ItIClVjCw).

## Installation Guide
If you haven't already, download and import the VRCSDK3 for Avatars either from the official VRChat website or from their Creator Companion app.

You can then simply import the ChunkyOSC package into the project. You will be prompted to install other required packages you may be missing when you first attempt to use both the avatar setup and the client.

## Setting up Avatars
To begin, drag the Avatar Setup prefab from the ChunkyOSC folder into your Scene. This has the setup script you will need to use already on it.

1. Select your avatar.
2. Choose a menu for controls to be added to. (Optional)
3. Select a [plugin](#using-plugins) to use.\* (Optional)
4. Choose a path for created files to be placed in.
5. Click the 'Add to Avatar' button.

\* - If you choose to use a plugin, you must also fill in any additional fields required by it.

After you have uploaded your model with ChunkyOSC on it, you must enable OSC in the game and configure your avatar's OSC settings as described in [this section](#setting-up-osc). You should then be able to use the ChunkyOSC client to control it.

### Using Plugins
When setting up ChunkyOSC for an avatar, you can also select a plugin to use. These plugins are used as shortcuts for setting up ChunkyOSC to work with other systems that may already be present on your avatar. 

Included with ChunkyOSC are two plugins, World Object and VRCLens, and a template for creating more.

- World Object is used for quickly attaching an object on your avatar to ChunkyOSC. When the client is connected, it will force the object to use the pose given by the client, and the object will return to its previous pose when the client disconnects.

- VRCLens is used to configure and hijack the VRCL camera's Drop functionality to hand off control to ChunkyOSC while the client is connected. It can only be used if VRCLens has already been added on the avatar and will be skipped if the camera isn't found.

If you choose to not use a plugin, only the ChunkyOSC system will be added to the avatar. In this case, you will need to manually setup your avatar to use it.

If you have any third-party plugins you would like to install, you can copy the folder holding them into the Plugins folder. If you're interested in making your own plugins, refer to the guide within the Plugins folder to get started.

### Setting up OSC
For ChunkyOSC to work properly, you'll need to confirm that the parameters used by it are listed as parameters using OSC on your avatar.

1. Load the avatar in VRChat at least once with OSC turned on in the settings.
2. Go to "%APPDATA%/../LocalLow/VRChat/VRChat/OSC/<USER-ID>\/Avatars" in File Explorer.
3. Open the JSON file with your avatar's ID in a text editor of your choice.
4. Make sure that the parameters listed in the "OSC Settings.txt" file are all present in the JSON. If any are missing, copy them over.\*
5. Save the file and reload your avatar if you were still using it.

\* - If you feel the need, you may also remove the output fields of any parameters that lack them in the "OSC Settings.txt" file.

After you've performed these steps, the ChunkyOSC client should work correctly with the avatar.

## Using the Client
Start by opening the scene named "OSC Client" in the same folder as the setup.

Expand the ChunkyOSC object in the hierarchy and select the Controller within. This is the object you will be moving around to control the object on your avatar.

If OscCore is not present in your project, you will then be prompted to clone it from GitHub before you can continue.

In the inspector, you will see the settings you can configure.
- Chunk size changes the size of each chunk, with greater numbers having less precision remotely.
- Boundaries show the edge bounds of the controllable space depending on the currently selected chunk size.

You can also define other parameters you wish to control with ChunkyOSC, or load a premade list of them using a [preset](#using-presets). Any extra parameters you list here need must be configured for OSC on your avatar to work.

Once you have your avatar loaded in VRChat, you can start the client by entering Play Mode in the editor. Your starting point will always be the world origin when ChunkyOSC is synced or at the location where syncing was last disabled.

If you want to change more advanced settings such as the used ports for OSC or parameter names, you can modify the settings for the "Object Pose (OSC)" object in the client scene that you can find using this path: ChunkyOSC -> Tracking System -> Object Pose (OSC).

### Using Presets
If you have different setups for avatars that require extra parameters, it may be useful to save them as presets so you can swap between them easily.

After you've created your list of parameters in the inspector, you can give the configuration a name and save or update it. To add pre-existing presets, you can simply place the file for them within the presets folder.

## Contact & Support
If you wish to contact me or are having issues, you can contact me on Discord using my tag (Joshuarox100#5024). Note that you'll also need to be in the official VRChat server in order to send me messages.