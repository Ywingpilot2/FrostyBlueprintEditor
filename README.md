# Blueprint Editor
A plugin for [Frosty Editor](https://github.com/CadeEvs/FrostyToolsuite/tree/1.0.6.3) which allows assets such as LogicPrefabBlueprints and SpatialPrefabBlueprints to be edited in a proper graph form.

This tool is still unfinished, with many things relating to optimization and additional support needing to be done. PRs are very much so welcomed!

## What is finished:
- Core editing features, so adding and removing connections/objects
- Node extension functionality(so connections for nodes can be mapped out)
- loading blueprints into graphed form
- layout saving
- xml style configs for node mappings
- 
## What needs to be worked on:
- Optimization across the board; things are currently slow, so changes(and some refactoring) need to be done to speed things up
- Additional support for more situations(e.g external pointerrefs in say subworlds being used in connections)
- Improvements to automatic sorting
- Additional UX/UI Features

# For developers
if you wish to contribute to this project, please read this before doing so.

## Terminology 
Here is a basic overview of the Terminology used in this project:
- Object: an instance of a type or class which can be found in an asset
- Node: also known as a Verticie, is an item which contains inputs and outputs
- Node Object: An object stored inside of a Node, this represents the original object this node is based on and it's properties are what is displayed in the property grid.
- Connection: also commonly known as an edge, this connects an input and output together transferring a property or sending an event.
- Connection Object: An object stored inside the connection, this represents the original object of this connection.
- Node Mapping: a node mapping is a configuration of a type or class which details the inputs and outputs, among other details, it should have
- Node Editor: also commonly referred to as EditorViewModel or Editor, this refers to the EditorViewModel class which is used to edit and create connections and objects
- Editor Window: this refers to the window which contains the UI of the editor
- Ebx Loader: a class which is designed to load EBX, or an asset found in frosty's file browser, into a graphed representation
- Ebx Editor: a class which edits the loaded EBX or asset, converting the graphed representation into actual data

## Contribution Guidelines
There is a lot to this plugin so if you wish to make PRs these notes are important to keep in mind:
- Please do not in the core of the plugin(so anything that isn't for mapping out the connections of a specific type essentially) design things with 1 game in mind specifically. This plugin is designed to work with ALL games with full functionality, so if there is something game specific implemented, at the very least ensure that it only triggers when that games profile is loaded.
  
- It would be appreciated if you could at the very least add documentation to any methods/classes you create, though full comments explaining the process of what code is doing is the most strongly preferred. I know I am guilty of not doing this a lot in the code, though it makes managing PRs and code much easier. If I have any problems with how you are doing it, I will likely bring it up, though its not like I will strike you down like Zeus for not having enough comments keep in mind... At least hopefully for your sakes.
  
- Please try to keep things inside the EditorViewModel class(Models/Editor/NodeEditorViewModel) or adjacent, again I am guilty of not doing this a lot since I put a lot of stuff in static classes, though I want in the future for the user to be able to have more then 1 blueprint editor window open at a time, so the more stuff that is done per Editor the easier it will be for me to do.

- This requires both [Nodify](https://github.com/miroiu/nodify) and [Prism](https://www.nuget.org/packages/Prism.Wpf/), which are both avaliable as Nuget packages. In the future I would like to merge Nodify's source into this instead of having it as a independent DLL, idk what I will do about prism.
