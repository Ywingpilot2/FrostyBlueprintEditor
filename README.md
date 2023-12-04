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

## Terminology 
Here is a basic overview of the Terminology used in this project:
- Object: An instance of a type or class which can be found in an asset
- Node: Also known as a Verticie, is an item which contains inputs and outputs
- Entity Node: A node which has an Object associated with it, and can be found embedded in the EBX
- Transient Node: A node which is not stored in the EBX and is instead stored externally in layouts. This through code creates the EBX form of connections and Objects
- Node Object: An object stored inside of a Node, this represents the original object this node is based on and it's properties are what is displayed in the property grid.
- Port: Also referred to as a Connector, an input/output you can connect to another input/output to form a connection.
- Connection: Also commonly known as an edge, this connects an input and output together transferring a property or sending an event.
- Connection Object: An object stored inside the connection, this represents the original object of this connection.
- Node Mapping: A configuration of a type or class which details the inputs and outputs, among other details, it should have
- Node Extension: An extension of either EntityNode or TransientNode which acts as a C# written version of a Node Mapping, with access to a variety of events that can affect the Node Mapping.
- NodeUtils: A static class which is initialized during frosty's startup, this provides a variety of utilities to managing nodes, determing realms, and node extensions.
- Layout: A file which stores all of the information regarding the current node layout. So e.g, node positions or transient node data
- Node Editor: Also commonly referred to as EditorViewModel or Editor, this refers to the EditorViewModel class which is used to edit and create connections and nodes
- EditorUtils: A static class which gets initialized on frosty's startup, this provides a number of utilities for thr various Node Editors such as EbxLoaders, EbxEditors, or layout management.
- Editor Window: The window which contains the UI of the editor
- Ebx Loader: A class which is designed to load EBX, or an asset found in frosty's file browser, into a graphed representation
- Ebx Editor: A class which edits the loaded EBX or asset, converting the graphed representation into actual data

## Contribution Guidelines
There is a lot to this plugin so if you wish to make PRs these notes are important to keep in mind:
- Please do not in the core of the plugin(so anything that isn't for mapping out the connections of a specific type essentially) design things with 1 game in mind specifically. This plugin is designed to work with ALL games with full functionality, so if there is something game specific implemented, at the very least ensure that it only triggers when that games profile is loaded.
  
- It would be appreciated if you could at the very least add documentation to any methods/classes you create, though full comments explaining the process of what code is doing is the most strongly preferred. I know I am guilty of not doing this a lot in the code, though it makes managing PRs and code much easier. If I have any problems with how you are doing it, I will likely bring it up, though its not like I will strike you down like Zeus for not having enough comments keep in mind... At least hopefully for your sakes.

- This requires both [Nodify](https://github.com/miroiu/nodify) and [Prism](https://www.nuget.org/packages/Prism.Wpf/), which are both avaliable as Nuget packages. In the future I would like to merge Nodify's source into this instead of having it as a independent DLL, idk what I will do about prism.
