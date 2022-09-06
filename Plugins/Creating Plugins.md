# Creating a Plugin
This guide is meant for those who wish to create their own plugins to choose from when adding ChunkyOSC to avatars. It is recommended that you are familiar with creating Editor scripts within Unity before attempting to make a plugin.

## 1. Duplicate the Template Folder
To get started, duplicate the Template folder so that you have the required files for a custom plugin. You can go ahead and rename this folder now if you wish.

This folder will house all of your plugin's assets and metadata. Within it, you'll find ChunkyPlugin.cs, the plugin's script, and package.json, the metadata for the plugin. For now, we will focus on the script.

## 2. Rename the Script's Class
Go ahead and first rename the class within the script to something unique for your plugin. If the name you chose isn't currently taken by another plugin, Unity should now properly compile scripts if you save the file and tab back in.

## 3. Configure the Plugin Metadata
Now that your class has a unique name, we need to modify the package.json so that it is seen as a valid package by ChunkyOSC. 

Go ahead and open the file in a text editor. Replace <PLUGIN_NAME> with the name you want to be displayed for your plugin, and replace <CLASS_NAME> with the name that you gave your class within the script. Afterwards, remove the line with the double slashes.

Once you save the file, your plugin should now be listed as an available option when setting up ChunkyOSC on an avatar.

## 4. Write Code!
At this point, you have a functioning plugin that you can add your own custom code to. Simply open your script and follow the commented instructions!