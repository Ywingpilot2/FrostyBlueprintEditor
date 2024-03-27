# Blueprint Editor
A plugin for [Frosty Editor](https://github.com/CadeEvs/FrostyToolsuite/tree/1.0.6.3) which allows assets such as LogicPrefabBlueprints and SpatialPrefabBlueprints to be edited in a proper graph form.

This tool is still unfinished, with many things relating to optimization and additional support needing to be done. PRs are very much so welcomed!

## What is finished:
- Core editing features, so adding and removing connections/objects
- Node extension functionality(so connections for nodes can be mapped out)
- loading blueprints into graphed form
- layout saving
- xml style configs for node mappings
- Transient node functionality

## What needs to be worked on:
- Additional support for more situations(e.g files based around components instead of objects)
- Layered Graph Drawing algorithm for sorting nodes
- Options for customizing the look and functionality of the editor
- Comment, Redirect, and shortcut transient nodes

# For developers
if you wish to contribute to this project, please read this before doing so.

## Build Instructions
In order to build the Blueprint Editor, you first need to change the reference paths for frosty's binaries.
- Open BlueprintEditorPlugin.csproj
- Scroll to the item group containing a list of reference "tags" (should say in a comment what it is)
- Replace the occurrences of "..\1063\" with the directory to your frosty solution

Now it should build properly. But it won't copy itself over to frosty, in order to do this we need to
- In BlueprintEditorPlugin.csproj, scroll down to the bottom
- Replace "..\1063\" with the directory to your frosty solution, as before

And now for launching, you need to edit the current launch configurations and change out the path to the FrostyEditor.Exe your path.

Congrats! You should now be able to run and build Blueprint Editor.

## Contribution Guidelines
There is a lot to this plugin so if you wish to make PRs these notes are important to keep in mind:
- Please do not in the core of the plugin(so anything that isn't for mapping out the connections of a specific type essentially) design things with 1 game in mind specifically. This plugin is designed to work with ALL games with full functionality, so if there is something game specific implemented, at the very least ensure that it only triggers when that games profile is loaded.
  
- It would be appreciated if you could at the very least add documentation to any methods/classes you create, though full comments explaining the process of what code is doing is the most strongly preferred. I know I am guilty of not doing this a lot in the code, though it makes managing PRs and code much easier. If I have any problems with how you are doing it, I will likely bring it up, though its not like I will strike you down like Zeus for not having enough comments keep in mind... At least hopefully for your sakes.

- This requires both [Nodify](https://github.com/miroiu/nodify) and [Prism](https://www.nuget.org/packages/Prism.Wpf/), which are both avaliable as Nuget packages. In the future I would like to merge Nodify's source into this instead of having it as a independent DLL, idk what I will do about prism.
