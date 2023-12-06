using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Transient;
using BlueprintEditorPlugin.Utils;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.EbxEditorTypes
{
    /// <summary>
    /// This class is used to edit the EBX itself
    /// So e.g, whenever removing a node, this class is in charge of removing that node in EBX as well
    /// </summary>
    public class EbxBaseEditor
    {
        /// <summary>
        /// This is the asset type that the Blueprint Editor uses
        /// In this case its set to null, but you would want to set it to the asset type
        /// e.g, LogicPrefabBlueprint
        /// TODO: Add in ValidForGame property
        /// </summary>
        public virtual string AssetType { get; } = "null";
        
        public EditorViewModel NodeEditor { get; set; }

        /// <summary>
        /// Instead of using a constructor or initializer, use this method instead.
        /// This gets called whenever an EbxEditor is created
        /// </summary>
        public virtual void OnCreation()
        {
        }

        #region Editing Node Objects

        /// <summary>
        /// Used to add a new Object to ebx
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual object AddNodeObject(Type type)
        {
            dynamic obj = TypeLibrary.CreateObject(type.Name);
            PointerRef pointerRef = new PointerRef(obj);
            AssetClassGuid guid = new AssetClassGuid(FrostySdk.Utils.GenerateDeterministicGuid(
                NodeEditor.EditedEbxAsset.Objects,
                type,
                NodeEditor.EditedEbxAsset.FileGuid), -1); //TODO: THIS CODE SUCKS! PLEASE UPDATE!
            ((dynamic)pointerRef.Internal).SetInstanceGuid(guid);
            
            //No idea what this does
            if (TypeLibrary.IsSubClassOf(pointerRef.Internal, "DataBusPeer"))
            {
                byte[] b = guid.ExportedGuid.ToByteArray();
                uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                pointerRef.Internal.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(pointerRef.Internal, value);
            }
            
            NodeEditor.EditedProperties.Objects.Add(pointerRef);
            NodeEditor.EditedEbxAsset.AddObject(pointerRef.Internal);
            return obj;
        }
        
        /// <summary>
        /// Used to add a new Object to ebx
        /// </summary>
        /// <returns></returns>
        public virtual object AddNodeObject(object obj)
        {
            FrostyClipboard.Current.SetData(obj);
            object copy = FrostyClipboard.Current.GetData();
            
            PointerRef pointerRef = new PointerRef(copy);
            AssetClassGuid guid = new AssetClassGuid(FrostySdk.Utils.GenerateDeterministicGuid(
                NodeEditor.EditedEbxAsset.Objects,
                obj.GetType(),
                NodeEditor.EditedEbxAsset.FileGuid), -1); //TODO: THIS CODE SUCKS! PLEASE UPDATE!
            ((dynamic)pointerRef.Internal).SetInstanceGuid(guid);
            
            //No idea what this does
            if (TypeLibrary.IsSubClassOf(pointerRef.Internal, "DataBusPeer"))
            {
                byte[] b = guid.ExportedGuid.ToByteArray();
                uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                pointerRef.Internal.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(pointerRef.Internal, value);
            }
            
            NodeEditor.EditedProperties.Objects.Add(pointerRef);
            NodeEditor.EditedEbxAsset.AddObject(pointerRef.Internal);
            return copy;
        }

        /// <summary>
        /// This method is used to remove a node from the Ebx
        /// </summary>
        /// <param name="nodeToRemove"></param>
        public virtual void RemoveNodeObject(EntityNode nodeToRemove)
        {
            if (nodeToRemove.PointerRefType == PointerRefType.External)
            {
                App.Logger.LogError("Cannot remove imported objects, as a result the node has simply been removed from all connections in this file.");
                return;
            }
            List<PointerRef> pointerRefs = NodeEditor.EditedProperties.Objects;
            pointerRefs.RemoveAll(pointer => ((dynamic)pointer.Internal).GetInstanceGuid() == nodeToRemove.InternalGuid);
            NodeEditor.EditedEbxAsset.RemoveObject(nodeToRemove.Object);
        }

        #endregion

        #region Editing Connection Objects

        /// <summary>
        /// This method is used to remove connections from Ebx
        /// </summary>
        /// <param name="connection"></param>
        public virtual void RemoveConnectionObject(ConnectionViewModel connection)
        {
            //Check if the nodes are transient
            if (connection.SourceNode.IsTransient || connection.TargetNode.IsTransient)
            {
                //If they are we need them to handle this for us
                if (connection.SourceNode.IsTransient)
                {
                    TransientNode transientNode = connection.SourceNode as TransientNode;
                    transientNode?.RemoveConnectionObject(connection);
                }
                else
                {
                    TransientNode transientNode = connection.TargetNode as TransientNode;
                    transientNode?.RemoveConnectionObject(connection);
                }
            }
            else
            {
                //Otherwise we just do what we normally do for connections
                switch (connection.Type)
                {
                    case ConnectionType.Event:
                    {
                        foreach (dynamic eventConnection in NodeEditor.EditedProperties.EventConnections)
                        {
                            if (!connection.Equals(eventConnection)) continue;
                            NodeEditor.EditedProperties.EventConnections.Remove(eventConnection);
                            
                            //Update object flags
                            Type targetType = connection.TargetNode.Object.GetType();
                            if (targetType.GetProperty("Flags") != null && !NodeEditor.GetConnections(connection.Target).Any(x =>
                                    !x.Equals(connection) 
                                    && x.Source.Realm != connection.Source.Realm))
                            {
                                var helper = new ObjectFlagsHelper((uint)((dynamic)connection.TargetNode.Object).Flags);
                                switch (connection.Target.Realm)
                                {
                                    case ConnectionRealm.Client:
                                    {
                                        helper.ClientEvent = false;
                                    } break;
                                    case ConnectionRealm.Server:
                                    {
                                        helper.ServerEvent = false;
                                    } break;
                                    case ConnectionRealm.ClientAndServer:
                                    {
                                        helper.ClientEvent = false;
                                        helper.ServerEvent = false;
                                    } break;
                                    case ConnectionRealm.NetworkedClient:
                                    {
                                        helper.ClientEvent = false;
                                    } break;
                                    case ConnectionRealm.NetworkedClientAndServer:
                                    {
                                        helper.ClientEvent = false;
                                        helper.ServerEvent = false;
                                    } break;
                                }
                                
                                ((dynamic)connection.TargetNode.Object).Flags = helper.GetAsFlags();
                            }
                            break;
                        }
                    } break;
                    case ConnectionType.Property:
                    {
                        foreach (dynamic propertyConnection in NodeEditor.EditedProperties.PropertyConnections)
                        {
                            if (!connection.Equals(propertyConnection)) continue;
                            NodeEditor.EditedProperties.PropertyConnections.Remove(propertyConnection);
                            
                            //Update object flags
                            Type targetType = connection.TargetNode.Object.GetType();
                            if (targetType.GetProperty("Flags") != null && !NodeEditor.GetConnections(connection.Target).Any(x =>
                                    !x.Equals(connection) 
                                    && x.Source.Realm != connection.Source.Realm))
                            {
                                var helper = new ObjectFlagsHelper((uint)((dynamic)connection.TargetNode.Object).Flags);
                                switch (connection.Target.Realm)
                                {
                                    case ConnectionRealm.Client:
                                    {
                                        helper.ClientProperty = false;
                                    } break;
                                    case ConnectionRealm.Server:
                                    {
                                        helper.ServerProperty = false;
                                    } break;
                                    case ConnectionRealm.ClientAndServer:
                                    {
                                        helper.ClientProperty = false;
                                        helper.ServerProperty = false;
                                    } break;
                                    case ConnectionRealm.NetworkedClient:
                                    {
                                        helper.ClientProperty = false;
                                    } break;
                                    case ConnectionRealm.NetworkedClientAndServer:
                                    {
                                        helper.ClientProperty = false;
                                        helper.ServerProperty = false;
                                    } break;
                                }
                                
                                ((dynamic)connection.TargetNode.Object).Flags = helper.GetAsFlags();
                            }
                            break;
                        }
                    } break;
                    case ConnectionType.Link:
                    {
                        foreach (dynamic linkConnection in NodeEditor.EditedProperties.LinkConnections)
                        {
                            if (!connection.Equals(linkConnection)) continue;
                            NodeEditor.EditedProperties.LinkConnections.Remove(linkConnection);
                            break;
                        }
                    } break;
                }
            }

            switch (connection.ConnectionStatus)
            {
                case EditorStatus.Warning:
                {
                    NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString(connection.ToString()), EditorStatus.Warning);
                } break;
                case EditorStatus.Error:
                {
                    NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString(connection.ToString()), EditorStatus.Error);
                } break;
            }
        }

        /// <summary>
        /// This method is used to create new connections and add them to the ebx
        /// </summary>
        /// <param name="connection"></param>
        public virtual void CreateConnectionObject(ConnectionViewModel connection)
        {
            if (connection == null) return;
            
            if (!connection.SourceNode.IsTransient && !connection.TargetNode.IsTransient)
            {
                switch (connection.Type)
                {
                    case ConnectionType.Event:
                    {
                        dynamic eventConnection = TypeLibrary.CreateObject("EventConnection");

                        eventConnection.Source = ((EntityNode)connection.SourceNode).PointerRefType == PointerRefType.Internal ? new PointerRef(connection.SourceNode.Object) : new PointerRef(new EbxImportReference() {FileGuid = ((EntityNode)connection.SourceNode).FileGuid, ClassGuid = ((EntityNode)connection.SourceNode).FileGuid});
                        eventConnection.Target = ((EntityNode)connection.TargetNode).PointerRefType == PointerRefType.Internal ? new PointerRef(connection.TargetNode.Object) : new PointerRef(new EbxImportReference() {FileGuid = ((EntityNode)connection.TargetNode).FileGuid, ClassGuid = ((EntityNode)connection.TargetNode).FileGuid});
                        
                        eventConnection.SourceEvent.Name = connection.SourceField;
                        eventConnection.TargetEvent.Name = connection.TargetField;
                        
                        //Setup flags
                        Type objType = connection.TargetNode.Object.GetType();
                        ObjectFlagsHelper helper = objType.GetProperty("Flags") != null ? new ObjectFlagsHelper((uint)((dynamic)connection.TargetNode.Object).Flags) : new ObjectFlagsHelper(0);
                        
                        //TODO: THIS CODE FUCKING SUCKS, PLEASE FIX
                        //This is the only way I've found to get the Enum values of a dynamic type, IT SUCKS
                        //This runs really slowly because of heavy reflection usage, the only thing I can think of to speed this up is maybe caching the result?
                        Array realmArray = ((object)TypeLibrary.CreateObject("EventConnectionTargetType")).GetType().GetEnumValues();
                        List<dynamic> realmEnum = new List<dynamic>(realmArray.Cast<dynamic>());

                        if (NodeUtils.RealmsAreValid(connection.Source, connection.Target))
                        {
                            switch (connection.Target.Realm)
                            {
                                case ConnectionRealm.Client:
                                {
                                    helper.ClientEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.Client];
                                } break;
                                case ConnectionRealm.ClientAndServer:
                                {
                                    helper.ClientEvent = true;
                                    helper.ServerEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.ClientAndServer];
                                } break;
                                case ConnectionRealm.Server:
                                {
                                    helper.ServerEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.Server];
                                } break;
                                case ConnectionRealm.NetworkedClient:
                                {
                                    helper.ClientEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.NetworkedClient];
                                } break;
                                case ConnectionRealm.NetworkedClientAndServer:
                                {
                                    helper.ClientEvent = true;
                                    helper.ServerEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.NetworkedClientAndServer];
                                } break;
                            }
                        }
                        else //If realms on either Target or source are invalid, we should create a warning and guess.
                        {
                            ConnectionRealm potentialRealm = connection.Target.Realm != ConnectionRealm.Invalid ? connection.Target.Realm : connection.Source.Realm;

                            switch (potentialRealm)
                            {
                                case ConnectionRealm.Client:
                                {
                                    helper.ClientEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.Client];
                                } break;
                                case ConnectionRealm.ClientAndServer:
                                {
                                    helper.ClientEvent = true;
                                    helper.ServerEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.ClientAndServer];
                                } break;
                                case ConnectionRealm.Server:
                                {
                                    helper.ServerEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.Server];
                                } break;
                                case ConnectionRealm.NetworkedClient:
                                {
                                    helper.ClientEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.NetworkedClient];
                                } break;
                                case ConnectionRealm.NetworkedClientAndServer:
                                {
                                    helper.ClientEvent = true;
                                    helper.ServerEvent = true;
                                    eventConnection.TargetType = realmEnum[(int)ConnectionRealm.NetworkedClientAndServer];
                                } break;
                                case ConnectionRealm.Invalid:
                                {
                                    App.Logger.LogError("Unable to determine the realm of a connection");
                                    NodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString(connection.ToString()), "Unable to determine the realm of a connection");
                                    connection.ConnectionStatus = EditorStatus.Error;
                                } break;
                            }
                        }

                        if (objType.GetProperty("Flags") != null) //Double check to make sure our object has Flags
                        {
                            ((dynamic)connection.TargetNode.Object).Flags = helper.GetAsFlags();
                        }
                        
                        ((dynamic)NodeEditor.EditedEbxAsset.RootObject).EventConnections.Add(eventConnection);
                        connection.Object = eventConnection;
                    } break;
                
                    case ConnectionType.Property:
                    {
                        dynamic propertyConnection = TypeLibrary.CreateObject("PropertyConnection");

                        propertyConnection.Source = ((EntityNode)connection.SourceNode).PointerRefType == PointerRefType.Internal ? new PointerRef(connection.SourceNode.Object) : new PointerRef(new EbxImportReference() {FileGuid = ((EntityNode)connection.SourceNode).FileGuid, ClassGuid = ((EntityNode)connection.SourceNode).FileGuid});
                        propertyConnection.Target = ((EntityNode)connection.TargetNode).PointerRefType == PointerRefType.Internal ? new PointerRef(connection.TargetNode.Object) : new PointerRef(new EbxImportReference() {FileGuid = ((EntityNode)connection.TargetNode).FileGuid, ClassGuid = ((EntityNode)connection.TargetNode).FileGuid});
                        
                        propertyConnection.SourceField = connection.SourceField;
                        propertyConnection.TargetField = connection.TargetField;
                        
                        //Go through and set our flags
                        var flagsHelper = new PropertyFlagsHelper((uint)propertyConnection.Flags);
                        Type objType = connection.TargetNode.Object.GetType();
                        ObjectFlagsHelper objectFlagsHelper = objType.GetProperty("Flags") != null ? new ObjectFlagsHelper((uint)((dynamic)connection.TargetNode.Object).Flags) : new ObjectFlagsHelper(0);
                        
                        if (NodeUtils.RealmsAreValid(connection.Source, connection.Target))
                        {
                            flagsHelper.Realm = connection.Target.Realm;
                            flagsHelper.InputType = connection.Target.PropertyConnectionType;
                            //TODO: Figure out SourceCantBeStatic
                            
                            switch (flagsHelper.Realm)
                            {
                                case ConnectionRealm.Client:
                                {
                                    objectFlagsHelper.ClientProperty = true;
                                } break;
                                case ConnectionRealm.Server:
                                {
                                    objectFlagsHelper.ServerProperty = true;
                                } break;
                                case ConnectionRealm.ClientAndServer:
                                {
                                    objectFlagsHelper.ClientProperty = true;
                                    objectFlagsHelper.ServerProperty = true;
                                } break;
                                case ConnectionRealm.NetworkedClient:
                                {
                                    objectFlagsHelper.ClientProperty = true;
                                } break;
                                case ConnectionRealm.NetworkedClientAndServer:
                                {
                                    objectFlagsHelper.ClientProperty = true;
                                    objectFlagsHelper.ServerProperty = true;
                                } break;
                            }
                        }
                        else //If realms on either Target or source are invalid, we should create a warning and guess.
                        {
                            ConnectionRealm potentialRealm = connection.Target.Realm != ConnectionRealm.Invalid ? connection.Target.Realm : connection.Source.Realm;

                            flagsHelper.Realm = potentialRealm;
                            switch (potentialRealm)
                            {
                                case ConnectionRealm.Client:
                                {
                                    objectFlagsHelper.ClientEvent = true;
                                    
                                    App.Logger.LogWarning("The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString(connection.ToString()), "The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    connection.ConnectionStatus = EditorStatus.Warning;
                                } break;
                                case ConnectionRealm.ClientAndServer:
                                {
                                    objectFlagsHelper.ClientEvent = true;
                                    objectFlagsHelper.ServerEvent = true;
                                    
                                    App.Logger.LogWarning("The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString(connection.ToString()), "The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    connection.ConnectionStatus = EditorStatus.Warning;
                                } break;
                                case ConnectionRealm.Server:
                                {
                                    objectFlagsHelper.ServerEvent = true;
                                    
                                    App.Logger.LogWarning("The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString(connection.ToString()), "The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    connection.ConnectionStatus = EditorStatus.Warning;
                                } break;
                                case ConnectionRealm.NetworkedClient:
                                {
                                    objectFlagsHelper.ClientEvent = true;
                                    
                                    App.Logger.LogWarning("The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString(connection.ToString()), "The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    connection.ConnectionStatus = EditorStatus.Warning;
                                } break;
                                case ConnectionRealm.NetworkedClientAndServer:
                                {
                                    objectFlagsHelper.ClientEvent = true;
                                    objectFlagsHelper.ServerEvent = true;
                                    
                                    App.Logger.LogWarning("The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString(connection.ToString()), "The intended realms for connections is ambiguous, so connection might not have proper realms established.");
                                    connection.ConnectionStatus = EditorStatus.Warning;
                                } break;
                                case ConnectionRealm.Invalid:
                                {
                                    App.Logger.LogError("Unable to determine the realm of a connection");
                                    NodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString(connection.ToString()), "Unable to determine the realm of a connection");
                                    connection.ConnectionStatus = EditorStatus.Error;
                                } break;
                            }
                        }
                        propertyConnection.Flags = (uint)flagsHelper;
                        
                        if (objType.GetProperty("Flags") != null) //Double check to make sure our object has Flags
                        {
                            ((dynamic)connection.TargetNode.Object).Flags = objectFlagsHelper.GetAsFlags();
                        }

                        ((dynamic)NodeEditor.EditedEbxAsset.RootObject).PropertyConnections.Add(propertyConnection);
                        connection.Object = propertyConnection;
                    } break;
                
                    case ConnectionType.Link:
                    {
                        dynamic linkConnection = TypeLibrary.CreateObject("LinkConnection");

                        linkConnection.Source = ((EntityNode)connection.SourceNode).PointerRefType == PointerRefType.Internal ? new PointerRef(connection.SourceNode.Object) : new PointerRef(new EbxImportReference() {FileGuid = ((EntityNode)connection.SourceNode).FileGuid, ClassGuid = ((EntityNode)connection.SourceNode).FileGuid});
                        linkConnection.Target = ((EntityNode)connection.TargetNode).PointerRefType == PointerRefType.Internal ? new PointerRef(connection.TargetNode.Object) : new PointerRef(new EbxImportReference() {FileGuid = ((EntityNode)connection.TargetNode).FileGuid, ClassGuid = ((EntityNode)connection.TargetNode).FileGuid});
                    
                        if (connection.SourceField != "self")
                        {
                            linkConnection.SourceField = connection.SourceField;
                        }

                        if (connection.TargetField != "self")
                        {
                            linkConnection.TargetField = connection.TargetField;
                        }

                        ((dynamic)NodeEditor.EditedEbxAsset.RootObject).LinkConnections.Add(linkConnection);
                        connection.Object = linkConnection;
                    } break;
                }
            }
            else
            {
                //If they are we need them to handle this for us
                if (connection.SourceNode.IsTransient)
                {
                    TransientNode transientNode = connection.SourceNode as TransientNode;
                    transientNode?.RemoveConnectionObject(connection);
                }
                else
                {
                    TransientNode transientNode = connection.TargetNode as TransientNode;
                    transientNode?.RemoveConnectionObject(connection);
                }
            }
        }

        #endregion

        #region General purpose Ebx Editing

        /// <summary>
        /// This method is triggered whenever something is edited in the property grid.
        /// It is one of the most important parts of the EbxEditor, ensuring that the Nodes, Property grid, and the actual Ebx stay in sync.
        /// </summary>
        /// <param name="newObj"></param>
        /// <param name="args"></param>
        public virtual bool EditEbx(object newObj, ItemModifiedEventArgs args = null)
        {
            switch (newObj.GetType().Name)
            {
                #region Interface

                case "InterfaceDescriptorData":
                {
                    NodeEditor.RefreshInterfaceNodes(newObj);
                    NodeEditor.EditedProperties.Interface = new PointerRef(newObj);
                    
                    App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                    return true;
                }

                case "DataField":
                {
                    if (args == null) return false;
                    
                    string name;
                        
                    //If we aren't editing the name then we can just check for the name of the object
                    if (args.Item.Name != "Name")
                    {
                        name = ((dynamic)newObj).Name.ToString();
                    }
                    //If we are editing the name we need to know the old name, that way we can find what to rename
                    else
                    {
                        name = args.OldValue.ToString();
                            
                        //If the new name and the old name are the same, we can just move on
                        if (name == ((dynamic)newObj).Name.ToString())
                        {
                            return true;
                        }
                    }

                    switch (args.Item.Name)
                    {
                        case "AccessType":
                        {
                            List<InterfaceDataNode> list = NodeEditor.GetNode(name);
                            foreach (InterfaceDataNode node in list)
                            {
                                foreach (ConnectionViewModel connection in NodeEditor.GetConnections(node))
                                {
                                    NodeEditor.Disconnect(connection);
                                }

                                NodeEditor.Nodes.Remove(node);
                                NodeEditor.InterfaceInputDataNodes.Remove(name);
                                NodeEditor.InterfaceOutputDataNodes.Remove(name);
                            }

                            FrostyPropertyGridItemData item = args.Item.Parent.Parent;
                            dynamic obj = typeof(PropertyValueBinding).GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Binding);
                            NodeEditor.EditedProperties.Interface = new PointerRef(obj); //Recreate the interface in the Ebx
                            NodeEditor.RefreshInterfaceNodes(obj);

                            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                            return true;
                        } break;
                        case "Name":
                        {
                            FrostyPropertyGridItemData item = args.Item.Parent.Parent;
                            dynamic obj = typeof(PropertyValueBinding).GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Binding);
                                
                            NodeEditor.EditedProperties.Interface = new PointerRef(obj); //Recreate the interface in the Ebx
                                
                            if (!NodeEditor.InterfaceInputDataNodes.ContainsKey(((dynamic)newObj).Name.ToString()) 
                                &&
                                !NodeEditor.InterfaceOutputDataNodes.ContainsKey(((dynamic)newObj).Name.ToString()))
                            {
                                //Its possible for a DataField to be SourceAndTarget so we enumerate over all of the nodes with this name
                                foreach (InterfaceDataNode interfaceDataNode in NodeEditor.GetNode(name))
                                {
                                    interfaceDataNode.OnModified(args);
                                    if (interfaceDataNode.Inputs.Count != 0)
                                    {
                                        NodeEditor.InterfaceInputDataNodes.Remove(name);
                                        NodeEditor.InterfaceInputDataNodes.Add(interfaceDataNode.Inputs[0].DisplayName, interfaceDataNode);
                                    }
                                    else
                                    {
                                        NodeEditor.InterfaceOutputDataNodes.Remove(name);
                                        NodeEditor.InterfaceOutputDataNodes.Add(interfaceDataNode.Outputs[0].DisplayName, interfaceDataNode);
                                    }
                                }
                                    
                                NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString($"{args.OldValue}"), EditorStatus.Error);
                                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                                return true;
                            }
                        } break;
                        default:
                        {
                            FrostyPropertyGridItemData item = args.Item.Parent.Parent;
                            dynamic obj = typeof(PropertyValueBinding).GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Binding);
                        } break;
                    }

                    //If both of these fail and we make it to this point, then something has gone wrong
                    if (NodeEditor.InterfaceInputDataNodes.ContainsKey(((dynamic)newObj).Name.ToString()) || NodeEditor.InterfaceOutputDataNodes.ContainsKey(((dynamic)newObj).Name.ToString()))
                    {
                        App.Logger.LogError("Cannot have multiple interfaces of the same name");
                        NodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString($"{args.OldValue}"), $"Unable to change interface {name} to {((dynamic)newObj).Name.ToString()}");

                        foreach (InterfaceDataNode interfaceDataNode in NodeEditor.GetNode(name))
                        {
                            interfaceDataNode.InterfaceItem.Name = new CString(name);
                        }

                        NodeEditor.InterfacePropertyGrid.Object = new object();
                        NodeEditor.InterfacePropertyGrid.Object = NodeEditor.EditedProperties.Interface.Internal;
                        return false;
                    }
                    else
                    {
                        App.Logger.LogError("An unknown error has occured. Info: {0}, {1}, {2}, {3}, {4}. Please restart the editor.", args.Item.Name, args.OldValue.ToString(), args.NewValue.ToString(), name, ((dynamic)newObj).Name.ToString());
                        NodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString($"{args.OldValue}"), "Unknown error; Check frosty log for more info.");
                    }

                } break;
                    
                case "DynamicLink":
                case "DynamicEvent":
                {
                    if (args == null) return false;
                    
                    string name = args.OldValue.ToString(); //Get the original name
                    InterfaceDataNode node = NodeEditor.GetNode(name)[0];

                    //Whether this is an output or input
                    if (node.Inputs.Count != 0)
                    {
                        //Double check that this name isn't taken
                        if (!NodeEditor.InterfaceInputDataNodes.ContainsKey(((dynamic)newObj).Name.ToString()))
                        {
                            FrostyPropertyGridItemData item = args.Item.Parent.Parent;
                            dynamic obj = typeof(PropertyValueBinding).GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Binding);
                            NodeEditor.EditedProperties.Interface = new PointerRef(obj); //Recreate the interface in the Ebx

                            node.OnModified(args);
                            NodeEditor.InterfaceInputDataNodes.Remove(name);
                            NodeEditor.InterfaceInputDataNodes.Add(node.Inputs[0].DisplayName, node);
                            NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString($"{args.OldValue}"), EditorStatus.Error);
                            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                        }
                        else
                        {
                            App.Logger.LogError("Cannot have multiple interfaces of the same name");
                            NodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString($"{args.OldValue}"), $"Unable to change interface {name} to {((dynamic)newObj).Name.ToString()}");
                            foreach (InterfaceDataNode interfaceDataNode in NodeEditor.GetNode(name))
                            {
                                interfaceDataNode.InterfaceItem.Name = new CString(name);
                            }
                                
                            NodeEditor.InterfacePropertyGrid.Object = new object();
                            NodeEditor.InterfacePropertyGrid.Object = NodeEditor.EditedProperties.Interface.Internal;
                        }
                    }
                    else
                    {
                        //Double check that this name isn't taken
                        if (!NodeEditor.InterfaceOutputDataNodes.ContainsKey(((dynamic)newObj).Name.ToString()))
                        {
                            FrostyPropertyGridItemData item = args.Item.Parent.Parent;
                            dynamic obj = typeof(PropertyValueBinding).GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(item.Binding);
                            NodeEditor.EditedProperties.Interface = new PointerRef(obj); //Recreate the interface in the Ebx

                            node.OnModified(args);
                            NodeEditor.InterfaceOutputDataNodes.Remove(name);
                            NodeEditor.InterfaceOutputDataNodes.Add(node.Outputs[0].DisplayName, node);
                            NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString($"{args.OldValue}"), EditorStatus.Error);
                            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                        }
                        else
                        {
                            App.Logger.LogError("Cannot have multiple interfaces of the same name");
                            NodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString($"{args.OldValue}"), $"Unable to change interface {name} to {((dynamic)newObj).Name.ToString()}");
                            foreach (InterfaceDataNode interfaceDataNode in NodeEditor.GetNode(name))
                            {
                                interfaceDataNode.InterfaceItem.Name = new CString(name);
                            }
                                
                            NodeEditor.InterfacePropertyGrid.Object = new object();
                            NodeEditor.InterfacePropertyGrid.Object = NodeEditor.EditedProperties.Interface.Internal;
                        }
                    }
                } break;

                #endregion

                #region Connection Objects

                case "EventConnection":
                {
                    foreach (dynamic eventConnection in NodeEditor.EditedProperties.EventConnections)
                    {
                        if (eventConnection != newObj) continue;
                        
                        dynamic connectionObj = newObj;
                        eventConnection.SourceEvent = connectionObj.SourceEvent;
                        eventConnection.TargetEvent = connectionObj.TargetEvent;
                        eventConnection.TargetType = connectionObj.TargetType;
                        
                        App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                        return true;
                    }
                } break;
                
                case "PropertyConnection":
                {
                    foreach (dynamic propertyConnection in NodeEditor.EditedProperties.PropertyConnections)
                    {
                        if (propertyConnection != newObj) continue;
                        
                        dynamic connectionObj = newObj;
                        propertyConnection.SourceFieldId = connectionObj.SourceFieldId;
                        propertyConnection.TargetFieldId = connectionObj.TargetFieldId;
                        propertyConnection.SourceField = connectionObj.SourceField;
                        propertyConnection.TargetField = connectionObj.TargetField;
                        propertyConnection.Flags = connectionObj.Flags;
                        
                        App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                        return true;
                    }
                } break;
                
                case "LinkConnection":
                {
                    foreach (dynamic linkConnection in NodeEditor.EditedProperties.LinkConnections)
                    {
                        if (linkConnection != newObj) continue;
                        
                        dynamic connectionObj = newObj;
                        linkConnection.SourceFieldId = connectionObj.SourceFieldId;
                        linkConnection.TargetFieldId = connectionObj.TargetFieldId;
                        linkConnection.SourceField = connectionObj.SourceField;
                        linkConnection.TargetField = connectionObj.TargetField;
                        
                        App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                        return true;
                    }
                } break;

                #endregion

                #region Node Objects

                default:
                {
                    if (NodeEditor.SelectedNodes[0] is EntityNode)
                    {
                        EntityNode node = NodeEditor.SelectedNodes[0] as EntityNode;
                        node.Object = newObj;

                        if (args != null) //If the args are null its not a user action, so we shouldn't notify the node nor check the realm
                        {
                            node.OnModified(args);
                            switch (args.Item.Name)
                            {
                                case "Realm":
                                {
                                    foreach (ConnectionViewModel connection in NodeEditor.GetConnections(node))
                                    {
                                        connection.ConnectionStatus = !NodeUtils.RealmsAreValid(connection)
                                            ? EditorStatus.Error
                                            : EditorStatus.Good;
                                    }
                                } break;
                                case "__Id":
                                {
                                    node.Name = ((dynamic)node.Object).__Id.ToString();
                                } break;
                            }
                        }
                    
                        switch (node.PointerRefType)
                        {
                            case PointerRefType.Internal:
                            {
                                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                            } break;
                            case PointerRefType.External:
                            {
                                EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(node.FileGuid);
                                EbxAsset asset = App.AssetManager.GetEbx(assetEntry);

                                object obj = asset.Objects.FirstOrDefault(assetObject => ((dynamic)assetObject).GetInstanceGuid() == node.InternalGuid);
                            
                                //Hacky way to dynamically set the values
                                for (var index = 0; index < obj.GetType().GetProperties().Length; index++)
                                {
                                    PropertyInfo property = obj.GetType().GetProperties()[index];
                                    if (!property.CanWrite) continue;
                                    property.SetValue(obj, newObj.GetType().GetProperty(property.Name).GetValue(newObj));
                                }

                                App.AssetManager.ModifyEbx(assetEntry.Name, asset);
                            } break;
                        }
                    }

                    return false;
                }

                #endregion
            }

            //If it was nothing in the switch case then it isn't valid
            return false;
        }

        /// <summary>
        /// This method is called whenever we want to update the Flags of one of our nodes
        /// </summary>
        /// <param name="node"></param>
        private void EditEbxFlags(EntityNode node)
        {
            switch (node.PointerRefType)
            {
                case PointerRefType.Internal:
                {
                    //TODO: Update this so we aren't enumerating over every single object in the entire file
                    for (int i = 0; i < NodeEditor.EditedProperties.Objects.Count; i++)
                    {
                        PointerRef pointerRef = NodeEditor.EditedProperties.Objects[i];
                        if (((dynamic)pointerRef.Internal).GetInstanceGuid() != node.InternalGuid) continue;
                                
                        NodeEditor.EditedProperties.Objects[i] = new PointerRef(node.Object);
                        return;
                    }
                    App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(NodeEditor.EditedEbxAsset.FileGuid).Filename, NodeEditor.EditedEbxAsset);
                } break;
                case PointerRefType.External:
                {
                    EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(node.FileGuid);
                    EbxAsset asset = App.AssetManager.GetEbx(assetEntry);

                    object obj = asset.Objects.FirstOrDefault(assetObject => ((dynamic)assetObject).GetInstanceGuid() == node.InternalGuid);
                            
                    //Hacky way to dynamically set the values
                    for (var index = 0; index < obj.GetType().GetProperties().Length; index++)
                    {
                        PropertyInfo property = obj.GetType().GetProperties()[index];
                        if (!property.CanWrite) continue;
                        property.SetValue(obj, node.Object.GetType().GetProperty(property.Name).GetValue(node.Object));
                    }

                    App.AssetManager.ModifyEbx(assetEntry.Name, asset);
                } break;
            }
        }

        #endregion
    }
}