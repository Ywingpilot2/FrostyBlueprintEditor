using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using BlueprintEditorPlugin.Attributes;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Types.EbxEditorTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Transient;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Interfaces;

namespace BlueprintEditorPlugin.Utils
{
    /// <summary>
    /// A variety of utilities for nodes
    /// </summary>
    public static class NodeUtils
    {
        /// <summary>
        /// A list of extensions for nodes
        /// </summary>
        public static Dictionary<string, EntityNode> EntityNodeExtensions = new Dictionary<string, EntityNode>();
        
        public static Dictionary<string, TransientNode> TransNodeExtensions = new Dictionary<string, TransientNode>();

        /// <summary>
        /// A list of node mapping configs for nodes
        /// </summary>
        public static Dictionary<string, List<string>> NmcExtensions = new Dictionary<string, List<string>>();

        #region Cleanup

        private static string[] DataTypes =
        {
            "EntityData",
            "ObjectData",
            "ComponentData",
            "DescriptorData",
            "Data"
        };

        /// <summary>
        /// Cleans the name of a type so its easier to read
        /// E.g BoolEntityData will be changed to Bool
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns>A name without Data type inside it</returns>
        private static string CleanNodeName(string typeName)
        {
            foreach (string dataType in DataTypes)
            {
                if (typeName.Contains(dataType) && typeName != dataType)
                {
                    return typeName.Replace(dataType, "");
                }
            }

            return typeName;
        }

        #endregion

        #region Node Mapping Configs

        private static string NodeMappingConfigsPath => $@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\NodeMappings\";
        
        public static void GenerateNodeMapping(NodeBaseModel node)
        {
            //First check if filepath exists, if it doesn't we create it
            FileInfo fi = new FileInfo($"{NodeMappingConfigsPath}{node.Object.GetType().Name}.nmc");
            if (fi.Directory != null && !fi.Directory.Exists) 
            { 
                Directory.CreateDirectory(fi.DirectoryName); 
            }

            if (fi.Exists)
            {
                MessageBoxResult result = FrostyMessageBox.Show(
                    "A Node Mapping Config for this type already exists, are you sure you want to overwrite this?",
                    "Blueprint Editor", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            //Now we can create the file with stream writer
            StreamWriter sw = new StreamWriter($"{NodeMappingConfigsPath}{node.Object.GetType().Name}.nmc");
            
            sw.WriteLine($"Type = {node.Object.GetType().Name}");
            sw.WriteLine($"DisplayName = {CleanNodeName(node.Object.GetType().Name)}");

            //Now we create the inputs
            for (var index = 0; index < node.Inputs.Count; index++)
            {
                InputViewModel input = node.Inputs[index];
                if (!input.IsConnected)
                {
                    node.Inputs.RemoveAt(index);
                    index -= 1;
                    continue;
                }
                switch (input.Type)
                {
                    case ConnectionType.Event:
                    {
                        sw.WriteLine(node.Object.GetType().GetProperty("Realm") == null
                            ? $"InputEvent = {input.Title}, {input.Realm}"
                            : $"InputEvent = {input.Title}, ObjectRealm");
                    } break;
                    case ConnectionType.Property:
                    {
                        sw.WriteLine(node.Object.GetType().GetProperty("Realm") == null
                            ? $"InputProperty = {input.Title}, {input.Realm}"
                            : $"InputProperty = {input.Title}, ObjectRealm");
                    } break;
                    case ConnectionType.Link:
                    {
                        sw.WriteLine(node.Object.GetType().GetProperty("Realm") == null
                            ? $"InputLink = {input.Title}, {input.Realm}"
                            : $"InputLink = {input.Title}, ObjectRealm");
                    } break;
                }
            }

            //Create the outputs
            for (var index = 0; index < node.Outputs.Count; index++)
            {
                OutputViewModel output = node.Outputs[index];
                if (!output.IsConnected)
                {
                    node.Inputs.RemoveAt(index);
                    index -= 1;
                    continue;
                }
                switch (output.Type)
                {
                    case ConnectionType.Event:
                    {
                        sw.WriteLine(node.Object.GetType().GetProperty("Realm") == null
                            ? $"OutputEvent = {output.Title}, {output.Realm}"
                            : $"OutputEvent = {output.Title}, ObjectRealm");
                    } break;
                    case ConnectionType.Property:
                    {
                        sw.WriteLine(node.Object.GetType().GetProperty("Realm") == null
                            ? $"OutputProperty = {output.Title}, {output.Realm}"
                            : $"OutputProperty = {output.Title}, ObjectRealm");
                    } break;
                    case ConnectionType.Link:
                    {
                        sw.WriteLine(node.Object.GetType().GetProperty("Realm") == null
                            ? $"OutputLink = {output.Title}, {output.Realm}"
                            : $"OutputLink = {output.Title}, ObjectRealm");
                    } break;
                }
            }

            sw.Close();
        }

        /// <summary>
        /// This will apply a Node Mapping Config(.nmc) to a NodeBaseModel
        /// </summary>
        /// <param name="newNode">The node to apply it to</param>
        /// <returns>A bool indicating whether or not the operation was a success</returns>
        public static void ApplyNodeMapping(NodeBaseModel newNode)
        {
            List<string> extension = NmcExtensions[newNode.ObjectType];
            
            foreach (string arg in extension)
            {
                try
                {
                    switch (arg.Split('=')[0])
                    {
                        case "DisplayName":
                        {
                            newNode.Name = arg.Replace("DisplayName=", "");
                        } break;
                        case "InputEvent":
                        {
                            string cleanArg = arg.Replace("InputEvent=", "");
                            string title = cleanArg.Split(',')[0];
                            string realmArg = cleanArg.Split(',')[1];
                            ConnectionRealm realm = ConnectionRealm.Invalid;
                            if (realmArg == "ObjectRealm")
                            {
                                if (((Type)newNode.Object.GetType()).GetProperty("Realm") != null)
                                {
                                    realm = ParseRealmFromString(newNode.Object.Realm.ToString());
                                }
                                else
                                {
                                    App.Logger.LogError("NMC had an ports realm set to ObjectRealm, yet the type {0} has no Realm property", newNode.Object.GetType().Name);
                                }
                            }
                            else
                            {
                                realm = ParseRealmFromString(realmArg);
                            }
                            newNode.Inputs.Add(new InputViewModel() { Title = title.Split(',')[0], Type = ConnectionType.Event, Realm = realm});
                        } break;
                        case "InputProperty":
                        {
                            string cleanArg = arg.Replace("InputProperty=", "");
                            string title = cleanArg.Split(',')[0];
                            string realmArg = cleanArg.Split(',')[1];
                            ConnectionRealm realm = ConnectionRealm.Invalid;
                            if (realmArg == "ObjectRealm")
                            {
                                if (((Type)newNode.Object.GetType()).GetProperty("Realm") != null)
                                {
                                    realm = ParseRealmFromString(newNode.Object.Realm.ToString());
                                }
                                else
                                {
                                    App.Logger.LogError("NMC had an ports realm set to ObjectRealm, yet the type {0} has no Realm property", newNode.Object.GetType().Name);
                                }
                            }
                            else
                            {
                                realm = ParseRealmFromString(realmArg);
                            }
                            newNode.Inputs.Add(new InputViewModel() { Title = title, Type = ConnectionType.Property, Realm = realm });
                        } break;
                        case "InputLink":
                        {
                            string cleanArg = arg.Replace("InputLink=", "");
                            string title = cleanArg.Split(',')[0];
                            string realmArg = cleanArg.Split(',')[1];
                            ConnectionRealm realm = ConnectionRealm.Invalid;
                            if (realmArg == "ObjectRealm")
                            {
                                if (((Type)newNode.Object.GetType()).GetProperty("Realm") != null)
                                {
                                    realm = ParseRealmFromString(newNode.Object.Realm.ToString());
                                }
                                else
                                {
                                    App.Logger.LogError("NMC had an ports realm set to ObjectRealm, yet the type {0} has no Realm property", newNode.Object.GetType().Name);
                                }
                            }
                            else
                            {
                                realm = ParseRealmFromString(realmArg);
                            }
                            newNode.Inputs.Add(new InputViewModel() { Title = title, Type = ConnectionType.Link, Realm = realm });
                        } break;
                        case "OutputEvent":
                        {
                            string cleanArg = arg.Replace("OutputEvent=", "");
                            string title = cleanArg.Split(',')[0];
                            string realmArg = cleanArg.Split(',')[1];
                            ConnectionRealm realm = ConnectionRealm.Invalid;
                            if (realmArg == "ObjectRealm")
                            {
                                if (((Type)newNode.Object.GetType()).GetProperty("Realm") != null)
                                {
                                    realm = ParseRealmFromString(newNode.Object.Realm.ToString());
                                }
                                else
                                {
                                    App.Logger.LogError("NMC had an ports realm set to ObjectRealm, yet the type {0} has no Realm property", newNode.Object.GetType().Name);
                                }
                            }
                            else
                            {
                                realm = ParseRealmFromString(realmArg);
                            }
                            newNode.Outputs.Add(new OutputViewModel() { Title = title, Type = ConnectionType.Event, Realm = realm });
                        } break;
                        case "OutputProperty":
                        {
                            string cleanArg = arg.Replace("OutputProperty=", "");
                            string title = cleanArg.Split(',')[0];
                            string realmArg = cleanArg.Split(',')[1];
                            ConnectionRealm realm = ConnectionRealm.Invalid;
                            if (realmArg == "ObjectRealm")
                            {
                                if (((Type)newNode.Object.GetType()).GetProperty("Realm") != null)
                                {
                                    realm = ParseRealmFromString(newNode.Object.Realm.ToString());
                                }
                                else
                                {
                                    App.Logger.LogError("NMC had an ports realm set to ObjectRealm, yet the type {0} has no Realm property", newNode.Object.GetType().Name);
                                }
                            }
                            else
                            {
                                realm = ParseRealmFromString(realmArg);
                            }
                            newNode.Outputs.Add(new OutputViewModel() { Title = title, Type = ConnectionType.Property, Realm = realm });
                        } break;
                        case "OutputLink":
                        {
                            string cleanArg = arg.Replace("OutputLink=", "");
                            string title = cleanArg.Split(',')[0];
                            string realmArg = cleanArg.Split(',')[1];
                            ConnectionRealm realm = ConnectionRealm.Invalid;
                            if (realmArg == "ObjectRealm")
                            {
                                if (((Type)newNode.Object.GetType()).GetProperty("Realm") != null)
                                {
                                    realm = ParseRealmFromString(newNode.Object.Realm.ToString());
                                }
                                else
                                {
                                    App.Logger.LogError("NMC had an ports realm set to ObjectRealm, yet the type {0} has no Realm property", newNode.Object.GetType().Name);
                                }
                            }
                            else
                            {
                                realm = ParseRealmFromString(realmArg);
                            }
                            newNode.Outputs.Add(new OutputViewModel() { Title = title, Type = ConnectionType.Link, Realm = realm });
                        } break;
                    }
                }
                catch (Exception e)
                {
                    App.Logger.LogError("Arg {0} in Node Mapping {1} was invalid", arg, newNode.ObjectType);
                }
            }
        }
        
        #endregion

        #region C# Extensions

        #region Extension Utils

        public static ConnectionRealm ParseRealmFromString(string str)
        {
            if (str.StartsWith("EventConnectionTargetType_"))
            {
                switch (str)
                {
                    case "EventConnectionTargetType_Invalid":
                    {
                        return ConnectionRealm.Invalid;
                    }
                    case "EventConnectionTargetType_ClientAndServer":
                    {
                        return ConnectionRealm.ClientAndServer;
                    }
                    case "EventConnectionTargetType_Client":
                    {
                        return ConnectionRealm.Client;
                    }
                    case "EventConnectionTargetType_Server":
                    {
                        return ConnectionRealm.Server;
                    }
                    case "EventConnectionTargetType_NetworkedClient":
                    {
                        return ConnectionRealm.NetworkedClient;
                    }
                    case "EventConnectionTargetType_NetworkedClientAndServer":
                    {
                        return ConnectionRealm.NetworkedClientAndServer;
                    }
                    default:
                    {
                        return ConnectionRealm.Invalid;
                    }
                }
            }
            else if (str.StartsWith("Realm_"))
            {
                switch (str)
                {
                    case "Realm_Server":
                    {
                        return ConnectionRealm.Server;
                    }
                    case "Realm_Client":
                    {
                        return ConnectionRealm.Client;
                    }
                    case "Realm_ClientAndServer":
                    {
                        return ConnectionRealm.ClientAndServer;
                    }
                    default:
                    {
                        return ConnectionRealm.Invalid;
                    }
                }
            }
            else //Its probably setup like "Server" "Client" "ClientAndServer"
            {
                switch (str)
                {
                    case "Server":
                    {
                        return ConnectionRealm.Server;
                    }
                    case "Client":
                    {
                        return ConnectionRealm.Client;
                    }
                    case "ClientAndServer":
                    {
                        return ConnectionRealm.ClientAndServer;
                    }
                    case "NetworkedClient":
                    {
                        return ConnectionRealm.NetworkedClient;
                    }
                    case "NetworkedClientAndServer":
                    {
                        return ConnectionRealm.NetworkedClientAndServer;
                    }
                    default:
                    {
                        return ConnectionRealm.Invalid;
                    }
                }
            }
        }
        
        /// <summary>
        /// This will generate probabilistically determined property inputs for a Node
        /// </summary>
        /// <param name="objectType">The type of the object we are generating off of</param>
        /// <param name="nodeBaseModel"></param>
        /// <returns>Inputs for a node</returns>
        public static ObservableCollection<InputViewModel> GenerateNodeInputs(Type objectType, NodeBaseModel nodeBaseModel)
        {
            ObservableCollection<InputViewModel> inputs = new ObservableCollection<InputViewModel>();
            foreach (PropertyInfo property in objectType.GetProperties())
            {
                if (property.Name == "Flags" 
                    || property.Name == "Realm" 
                    || property.Name == "__Id" 
                    || property.Name == "__InstanceGuid" 
                    || property.Name.Contains("RuntimeComponentCount") 
                    || property.Name.Contains("RuntimeTransformationCount")
                    || property.Name.Contains("Components")
                   ) continue;

                inputs.Add(new InputViewModel()
                {
                    Title = property.Name,
                    Type = ConnectionType.Property
                });
            }

            return inputs;
        }

        /// <summary>
        /// This will set a ports realm to be that of the Objects realm. 
        /// </summary>
        /// <param name="obj">The nodes Object</param>
        /// <param name="port">The port which we are setting</param>
        /// <returns>A bool indicating whether or not the operation was a success</returns>
        public static bool PortRealmFromObject(dynamic obj, PortBaseModel port)
        {
            Type objType = obj.GetType();
            if (objType.GetProperty("Realm") != null)
            {
                switch (obj.Realm.ToString())
                {
                    case "Realm_Server":
                    {
                        port.Realm = ConnectionRealm.Server;
                        if (port.DisplayName.EndsWith(")"))
                        {
                            port.DisplayName = port.DisplayName.Remove(port.DisplayName.IndexOf('('));
                        }
                        port.DisplayName += "(Server)";
                        return true;
                    }
                    case "Realm_Client":
                    {
                        port.Realm = ConnectionRealm.Client;
                        if (port.DisplayName.EndsWith(")"))
                        {
                            port.DisplayName = port.DisplayName.Remove(port.DisplayName.IndexOf('('));
                        }
                        port.DisplayName += "(Client)";
                        return true;
                    }
                    case "Realm_ClientAndServer":
                    {
                        port.Realm = ConnectionRealm.ClientAndServer;
                        if (port.DisplayName.EndsWith(")"))
                        {
                            port.DisplayName = port.DisplayName.Remove(port.DisplayName.IndexOf('('));
                        }
                        port.DisplayName += "(ClientAndServer)";
                        return true;
                    }
                    //If its none of these, that means we have an invalid object realm, so we set the port realms to invalid
                    default:
                    {
                        port.Realm = ConnectionRealm.Invalid;
                        if (port.DisplayName.EndsWith(")"))
                        {
                            port.DisplayName = port.DisplayName.Remove(port.DisplayName.IndexOf('('));
                        }
                        port.DisplayName += "(Invalid)";
                        return false;
                    }
                }
            }

            return false;
        }

        public static bool SetupPort(PropertyFlagsHelper flagsHelper, InputViewModel input)
        {
            if (input.Realm == ConnectionRealm.Invalid)
            {
                input.Realm = flagsHelper.Realm;
                input.PropertyConnectionType = flagsHelper.InputType;
            }

            if (!input.DisplayName.EndsWith(")"))
            {
                input.DisplayName += $"({input.Realm})";
            }

            if (input.Realm == ConnectionRealm.Invalid)
            {
                App.Logger.LogError("Input realm is invalid");
                return false;
            }

            return true;
        }

        #endregion

        #endregion

        #region Realm Utilities

        private static readonly List<(ConnectionRealm, ConnectionRealm)> ImplicitConnectionCombos = new List<(ConnectionRealm, ConnectionRealm)>
            {
                (ConnectionRealm.ClientAndServer, ConnectionRealm.Server),
                (ConnectionRealm.ClientAndServer, ConnectionRealm.Client),
                (ConnectionRealm.NetworkedClientAndServer, ConnectionRealm.Server),
                (ConnectionRealm.NetworkedClientAndServer, ConnectionRealm.NetworkedClient),
                (ConnectionRealm.Server, ConnectionRealm.NetworkedClient),
                (ConnectionRealm.NetworkedClient, ConnectionRealm.Client),
                (ConnectionRealm.Client, ConnectionRealm.NetworkedClient),
                (ConnectionRealm.Client, ConnectionRealm.ClientAndServer),
                (ConnectionRealm.Server, ConnectionRealm.ClientAndServer)
            };
        
        public static bool RealmsAreValid(OutputViewModel source, InputViewModel target)
        {
            if (source.Realm == ConnectionRealm.Invalid || target.Realm == ConnectionRealm.Invalid) return false;

            return source.Realm == target.Realm || ImplicitConnectionCombos.Contains((source.Realm, target.Realm));
        }
        
        public static bool RealmsAreValid(PropertyFlagsHelper flagsHelper)
        {
            return flagsHelper.Realm != ConnectionRealm.Invalid && flagsHelper.InputType != PropertyType.Invalid;
        }
        
        public static bool RealmsAreValid(PropertyFlagsHelper flagsHelper, OutputViewModel source)
        {
            return flagsHelper.Realm != ConnectionRealm.Invalid && flagsHelper.InputType != PropertyType.Invalid 
                                                                && (source.Realm == flagsHelper.Realm || ImplicitConnectionCombos.Contains((source.Realm, flagsHelper.Realm)));
        }

        public static bool RealmsAreValid(OutputViewModel source, ObjectFlagsHelper targetFlagsHelper)
        {
            switch (source.Realm)
            {
                case ConnectionRealm.Client:
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Property:
                        {
                            return targetFlagsHelper.ClientProperty;
                        }
                        case ConnectionType.Event:
                        {
                            return targetFlagsHelper.ClientEvent;
                        }
                        case ConnectionType.Link:
                        {
                            return true; //TODO
                        }
                        default:
                        {
                            return false;
                        }
                    }
                }
                case ConnectionRealm.ClientAndServer:
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Property:
                        {
                            return targetFlagsHelper.ClientProperty && targetFlagsHelper.ServerProperty;
                        }
                        case ConnectionType.Event:
                        {
                            return targetFlagsHelper.ClientEvent && targetFlagsHelper.ServerEvent;
                        }
                        case ConnectionType.Link:
                        {
                            return true; //TODO
                        }
                        default:
                        {
                            return false;
                        }
                    }
                }
                case ConnectionRealm.Server:
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Property:
                        {
                            return targetFlagsHelper.ServerProperty;
                        }
                        case ConnectionType.Event:
                        {
                            return targetFlagsHelper.ServerEvent;
                        }
                        case ConnectionType.Link:
                        {
                            return true; //TODO
                        }
                        default:
                        {
                            return false;
                        }
                    }
                }
                case ConnectionRealm.NetworkedClient:
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Property:
                        {
                            return targetFlagsHelper.ClientEvent;
                        }
                        case ConnectionType.Event:
                        {
                            return targetFlagsHelper.ClientEvent;
                        }
                        case ConnectionType.Link:
                        {
                            return true; //TODO
                        }
                        default:
                        {
                            return false;
                        }
                    }
                }
                case ConnectionRealm.NetworkedClientAndServer:
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Property:
                        {
                            return targetFlagsHelper.ClientProperty && targetFlagsHelper.ServerProperty;
                        }
                        case ConnectionType.Event:
                        {
                            return targetFlagsHelper.ClientEvent && targetFlagsHelper.ServerEvent;
                        }
                        case ConnectionType.Link:
                        {
                            return true; //TODO
                        }
                        default:
                        {
                            return false;
                        }
                    }
                }
                default:
                {
                    return false;
                }
            }
        }

        public static bool RealmsAreValid(string realm)
        {
            return ParseRealmFromString(realm) != ConnectionRealm.Invalid;
        }
        
        public static bool RealmsAreValid(string realmName, OutputViewModel source)
        {
            ConnectionRealm realm = ParseRealmFromString(realmName);
            return realm != ConnectionRealm.Invalid && (realm == source.Realm || ImplicitConnectionCombos.Contains((source.Realm, realm)));
        }

        public static bool RealmsAreValid(object sourceObject, object targetObject)
        {
            //If this is the case, we can't actually check via this method, so we just assume its valid
            if (sourceObject.GetType().GetProperty("Realm") == null ||
                targetObject.GetType().GetProperty("Realm") == null) return true;
            
            ConnectionRealm sourceRealm = ParseRealmFromString(((dynamic)sourceObject).Realm.ToString());
            ConnectionRealm targetRealm = ParseRealmFromString(((dynamic)targetObject).Realm.ToString());

            return sourceRealm == targetRealm || ImplicitConnectionCombos.Contains((sourceRealm, targetRealm));
        }

        public static bool RealmsAreValid(ConnectionViewModel connection)
        {
            switch (connection.Type)
            {
                case ConnectionType.Event:
                {
                    ConnectionRealm connectionRealm = ParseRealmFromString(connection.Object.TargetType.ToString());
                    if (connectionRealm == ConnectionRealm.Invalid) return false;

                    return (connection.Source.Realm == connection.Target.Realm && connectionRealm == connection.Target.Realm)
                           ||
                           ImplicitConnectionCombos.Contains((connection.Source.Realm, connection.Target.Realm));
                }
                case ConnectionType.Property:
                {
                    var helper = new PropertyFlagsHelper((uint)connection.Object.Flags);
                    if (helper.Realm == ConnectionRealm.Invalid) return false;
                    
                    return (connection.Source.Realm == connection.Target.Realm && helper.Realm == connection.Target.Realm)
                           ||
                           ImplicitConnectionCombos.Contains((connection.Source.Realm, connection.Target.Realm));
                }
                case ConnectionType.Link:
                {
                    Type objType = connection.SourceNode.Object.GetType();
                    if (objType.GetProperty("Flags") == null) return false;
                    
                    var sourceHelper = new ObjectFlagsHelper((uint)connection.SourceNode.Object.Flags);
                    return (sourceHelper.ClientLinkSource && connection.Target.Realm == ConnectionRealm.Client)
                           || (sourceHelper.ServerLinkSource && connection.Target.Realm == ConnectionRealm.Server);
                }
            }

            return false;
        }

        #endregion

        #region Initialization & updating

        public static void Initialize(ILogger logger = null)
        {
            #region Load internal extensions
            
            EntityNodeExtensions.Add("null", new EntityNode());
            
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                logger?.Log("Loading node extensions...");
                if (type.IsSubclassOf(typeof(EntityNode)))
                {
                    try
                    {
                        var extension = (EntityNode)Activator.CreateInstance(type);
                        if ((extension.ValidForGames == null 
                             || extension.ValidForGames.Contains(ProfilesLibrary.ProfileName)) 
                            && extension.ObjectType != null 
                            && !EntityNodeExtensions.ContainsKey(extension.ObjectType))
                        {
                            EntityNodeExtensions.Add(extension.ObjectType, extension);
                        }
                        else
                        {
                            App.Logger.LogError("Node Extension {0} is invalid, as its ObjectType was either null or is already taken.", extension.GetType().Name);
                            logger?.LogError("Node Extension {0} is invalid, as its name was either null or is already taken.", extension.GetType().Name);
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.LogError("Could not load node extension {0}", type.Name);
                        App.Logger.LogError("Could not load node extension {0}", type.Name);
                    }
                }
                else if (type.IsSubclassOf(typeof(TransientNode)))
                {
                    try
                    {
                        var extension = (TransientNode)Activator.CreateInstance(type);
                        if (extension.Name == null || TransNodeExtensions.ContainsKey(extension.Name))
                        {
                            logger?.LogError("Node Extension {0} is invalid, as its name was either null or is already taken.", extension.GetType().Name);
                            App.Logger.LogError("Node Extension {0} is invalid, as its name was either null or is already taken.", extension.GetType().Name);
                            continue;
                        }
                        TransNodeExtensions.Add(extension.Name, extension);
                    }
                    catch (Exception e)
                    {
                        logger?.LogError("Could not load node extension {0}", type.Name);
                        App.Logger.LogError("Could not load node extension {0}", type.Name);
                    }
                }
            }

            #endregion

            #region Load external extensions

            foreach (string item in Directory.EnumerateFiles("Plugins", "*.dll", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(item);
                Assembly plugin = Assembly.LoadFile(fileInfo.FullName);

                foreach (Attribute attribute in plugin.GetCustomAttributes())
                {
                    if (attribute is RegisterEntityNodeExtension entityRegister)
                    {
                        try
                        {
                            var extension = (EntityNode)Activator.CreateInstance(entityRegister.EntityNodeExtension);
                            if ((extension.ValidForGames == null 
                                 || extension.ValidForGames.Contains(ProfilesLibrary.ProfileName)) 
                                && extension.ObjectType != null 
                                && !EntityNodeExtensions.ContainsKey(extension.ObjectType))
                            {
                                EntityNodeExtensions.Add(extension.ObjectType, extension);
                            }
                            else
                            {
                                App.Logger.LogError("Node Extension {0} is invalid, as its ObjectType was either null or is already taken.", extension.GetType().Name);
                                logger?.LogError("Node Extension {0} is invalid, as its name was either null or is already taken.", extension.GetType().Name);
                            }
                        }
                        catch (Exception e)
                        {
                            logger?.LogError("Could not load node extension {0}", entityRegister.EntityNodeExtension.Name);
                            App.Logger.LogError("Could not load node extension {0}", entityRegister.EntityNodeExtension.Name);
                        }
                    }
                    else if (attribute is RegisterTransientNodeExtension transRegister)
                    {
                        try
                        {
                            var extension = (TransientNode)Activator.CreateInstance(transRegister.TransientNodeExtension);
                            if (extension.Name == null || TransNodeExtensions.ContainsKey(extension.Name))
                            {
                                logger?.LogError("Node Extension {0} is invalid, as its name was either null or is already taken.", extension.GetType().Name);
                                App.Logger.LogError("Node Extension {0} is invalid, as its name was either null or is already taken.", extension.GetType().Name);
                                continue;
                            }
                            TransNodeExtensions.Add(extension.Name, extension);
                        }
                        catch (Exception e)
                        {
                            logger?.LogError("Could not load node extension {0}", transRegister.TransientNodeExtension.Name);
                            App.Logger.LogError("Could not load node extension {0}", transRegister.TransientNodeExtension.Name);
                        }
                    }
                }
            }

            #endregion

            #region Load node mapping configs

            //If the nmc directory doesn't exist we create it
            logger?.Log("Loading Node Mappings...");
            if (!Directory.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\NodeMappings"))
            {
                Directory.CreateDirectory($@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\NodeMappings");
                logger?.LogWarning("Node Mappings directory did not exist and had to be created");
                App.Logger.LogWarning("Node Mappings directory did not exist and had to be created");
            }

            //Read our xml-style NodeMappings
            foreach (string file in Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\NodeMappings", "*.nmc", SearchOption.AllDirectories))
            {
                logger?.Log("Loading Node Mappings...");
                StreamReader sr = new StreamReader(@file);
                string type = null;
                List<string> args = new List<string>();
                
                string currentLine = sr.ReadLine();
                try
                {
                    while (currentLine != null)
                    {
                        switch (currentLine.Replace(" = ", "=").Split('=')[0])
                        {
                            case "Type":
                            {
                                type = currentLine.Replace(" = ", "=").Split('=')[1];
                            } break;
                            case "DisplayName":
                            {
                                args.Add(currentLine.Replace(" = ", "="));
                            } break;
                            case "InputEvent":
                            {
                                args.Add(currentLine.Replace(" = ", "=").Replace(", ", ","));
                            } break;
                            case "InputProperty":
                            {
                                args.Add(currentLine.Replace(" = ", "=").Replace(", ", ","));
                            } break;
                            case "InputLink":
                            {
                                args.Add(currentLine.Replace(" = ", "=").Replace(", ", ","));
                            } break;
                            case "OutputEvent":
                            {
                                args.Add(currentLine.Replace(" = ", "=").Replace(", ", ","));
                            } break;
                            case "OutputProperty":
                            {
                                args.Add(currentLine.Replace(" = ", "=").Replace(", ", ","));
                            } break;
                            case "OutputLink":
                            {
                                args.Add(currentLine.Replace(" = ", "=").Replace(", ", ","));
                            } break;
                            case "ValidGameExecutableName":
                            {
                                args.Add(currentLine.Replace(" = ", "="));
                            } break;
                            case "Documentation":
                            {
                                args.Add(currentLine.Replace(" = ", "="));
                            } break;
                            
                            case " ":
                            case "":
                            {
                                break;
                            }
                            
                            default:
                            {
                                App.Logger.LogError("{1} contains an invalid argument, {0}", currentLine, file);
                            } break;
                        }

                        if (type != null 
                            && !NmcExtensions.ContainsKey(type) 
                            && (args.All(arg => arg.Split('=')[0] != "ValidGameExecutableName") 
                                || args.Any(arg => arg == $"ValidGameExecutableName={ProfilesLibrary.ProfileName}")))
                        {
                            NmcExtensions.Add(type, args);
                        }

                        currentLine = sr.ReadLine();
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError("line \"{0}\" in {1} is invalid", currentLine, file);
                    App.Logger.LogError("line \"{0}\" in {1} is invalid", currentLine, file);
                }
            }

            #endregion
        }

        #endregion
    }
}