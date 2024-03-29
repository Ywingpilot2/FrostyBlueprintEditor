using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BlueprintEditorPlugin.Attributes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Models.Nodes;
using FrostyEditor;
using FrostySdk;

namespace BlueprintEditorPlugin
{
    /// <summary>
    /// Central class for management of Blueprint Editor extensions.
    /// This handles everything, from GraphEditors to Node mappings.
    /// </summary>
    public static class ExtensionsManager
    {
        public static readonly Dictionary<string, Type> EntityNodeExtensions = new Dictionary<string, Type>();
        public static readonly Dictionary<string, Type> TransientNodeExtensions = new Dictionary<string, Type>();
        
        private static List<Type> _graphEditors = new List<Type>();
        public static IEnumerable<Type> GraphEditorExtensions => _graphEditors;

        private static List<Type> _graphAlgorithms = new List<Type>();

        /// <summary>
        /// Initiates the ExtensionManager
        /// </summary>
        public static void Initiate()
        {
            // Register internal Entity Nodes
            
            // TODO: Why oh why is GetCallingAssembly returning MSCoreLib?????
            // This shit fucking sucks, for some reason if a debugger isn't attatched the calling assembly changes
            // Why??? Just to fuck me and me specifically???
            // Never ONCE has it done this but now nope fuck you gonna call from a different assembly now because no debugger L Ratio fuckface have fun spending 6 hours trying to figure out why
            App.Logger.Log("Scanning of types began");
            App.Logger.Log("Currently calling assembly: {0}", Assembly.GetCallingAssembly().FullName);
            App.Logger.Log("Assembly this type belongs to: {0}", Assembly.GetAssembly(typeof(ExtensionsManager)));
            App.Logger.Log("Assembly that is executing this code: {0}", Assembly.GetExecutingAssembly());
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(EntityNode)) && !type.IsAbstract)
                {
                    App.Logger.Log($"{type.Name} is EntityNode");
                    try
                    {
                        EntityNode node = (EntityNode)Activator.CreateInstance(type);
                        if (node.IsValid() && !EntityNodeExtensions.ContainsKey(node.ObjectType))
                        {
                            EntityNodeExtensions.Add(node.ObjectType, type);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{type.Name} failed!");
                        App.Logger.LogError("Entity node {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
                if (type.GetInterface("ITransient") != null && !type.IsAbstract)
                {
                    App.Logger.Log($"{type.Name} is ITransient");
                    try
                    {
                        ITransient trans = (ITransient)Activator.CreateInstance(type);
                        if (trans.IsValid() && !TransientNodeExtensions.ContainsKey(type.Name))
                        {
                            TransientNodeExtensions.Add(type.Name, type);
                        }
                        else if (TransientNodeExtensions.ContainsKey(type.Name))
                        {
                            App.Logger.LogError("To whomever may read this: Please give type {0} a unique name. Transient nodes must have unique type names god dammit!", type.Name);
                        }
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Transient node {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
                if (type.GetInterface("IGraphEditor") != null && !type.IsAbstract)
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IGraphEditor graphEditor = (IGraphEditor)Activator.CreateInstance(type);
                            if (graphEditor.IsValid())
                            {
                                _graphEditors.Add(type);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Graph Editor {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
            }
            
            foreach (string item in Directory.EnumerateFiles("Plugins", "*.dll", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(item);
                Assembly plugin = Assembly.LoadFile(fileInfo.FullName);

                foreach (Attribute attribute in plugin.GetCustomAttributes())
                {
                    if (attribute is RegisterEntityNode entityRegister)
                    {
                        try
                        {
                            var extension = (EntityNode)Activator.CreateInstance(entityRegister.EntityNodeExtension);
                            if (extension.IsValid())
                            {
                                // Override our internal extension with external one
                                if (EntityNodeExtensions.ContainsKey(extension.ObjectType))
                                {
                                    EntityNodeExtensions.Remove(extension.ObjectType);
                                }
                                
                                EntityNodeExtensions.Add(extension.ObjectType, entityRegister.EntityNodeExtension);
                            }
                        }
                        catch (Exception e)
                        {
                            App.Logger.LogError("Could not load node extension {0}", entityRegister.EntityNodeExtension.Name);
                        }
                    }
                    else if (attribute is RegisterGraphEditor graphRegister)
                    {
                        try
                        {
                            var extension = (IGraphEditor)Activator.CreateInstance(graphRegister.GraphType);

                            if (extension.IsValid() && extension is UserControl)
                            {
                                // Override our internal extension with external one
                                _graphEditors.Add(graphRegister.GraphType);
                            }
                            else if (!(extension is Control))
                            {
                                App.Logger.LogError("Graph editor {0} must be a control", graphRegister.GraphType.Name);
                            }
                        }
                        catch (Exception e)
                        {
                            App.Logger.LogError("Could not load graph extension {0}", graphRegister.GraphType.Name);
                        }
                    }
                }
            }
        }

        public static void RegisterExtension(RegisterGraphEditor registerGraphEditor)
        {
            try
            {
                IGraphEditor graphEditor = (IGraphEditor)Activator.CreateInstance(registerGraphEditor.GraphType);
                if (graphEditor.IsValid())
                {
                    _graphEditors.Add(registerGraphEditor.GraphType);
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError("Graph Editor {0} caused an exception when processing! Exception: {1}", registerGraphEditor.GraphType.Name, e.Message);
            }
        }

        public static void RegisterExtension(RegisterEntityNode registerEntityNode)
        {
            try
            {
                EntityNode node = (EntityNode)Activator.CreateInstance(registerEntityNode.EntityNodeExtension);
                if (node.IsValid() && !EntityNodeExtensions.ContainsKey(node.ObjectType))
                {
                    EntityNodeExtensions.Add(node.ObjectType, registerEntityNode.EntityNodeExtension);
                }
            }
            catch (Exception e)
            {
                App.Logger.LogError("Entity node {0} caused an exception when processing! Exception: {1}", registerEntityNode.EntityNodeExtension.Name, e.Message);
            }
        }
    }
}