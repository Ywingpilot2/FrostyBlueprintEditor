# Blueprint Editor
A plugin for [Frosty Editor](https://github.com/CadeEvs/FrostyToolsuite/tree/1.0.6.3) which allows assets such as LogicPrefabBlueprints and SpatialPrefabBlueprints to be edited in a proper graph form.

![graph](https://github.com/Ywingpilot2/FrostyBlueprintEditor/assets/136618828/6dacba23-fc95-419e-a3d0-64304305f724)

This tool is still unfinished, with many things relating to optimization and additional support needing to be done. PRs are very much so welcomed!

## What is finished:
- Core editing features, so adding and removing connections/objects
- Node extension functionality(so connections for nodes can be mapped out)
- loading blueprints into graphed form
## What needs to be worked on:
- Optimization across the board; things are currently slow, so changes(and some refactoring) need to be done to speed things up
- Additional support for more situations(e.g external pointerrefs in say subworlds being used in connections)
- Layout saving
- Improvements to automatic sorting
- Additional UX/UI Features
- Xml-style configs for node mappings(that way you can quickly assign inputs and outputs to different object types without needing to do it in C#)
