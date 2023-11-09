using System;
using System.Collections.Generic;
using System.Reflection;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Transient;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;

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
        /// </summary>
        public virtual string AssetType { get; } = "null";
        
        public EditorViewModel NodeEditor { get; set; }

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
            List<PointerRef> pointerRefs = NodeEditor.EditedProperties.Objects;
            pointerRefs.RemoveAll(pointer => ((dynamic)pointer.Internal).GetInstanceGuid() == nodeToRemove.Guid);
            NodeEditor.EditedEbxAsset.RemoveObject(nodeToRemove.Object);
        }

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
                            break;
                        }

                        break;
                    }
                    case ConnectionType.Property:
                    {
                        foreach (dynamic propertyConnection in NodeEditor.EditedProperties.PropertyConnections)
                        {
                            if (!connection.Equals(propertyConnection)) continue;
                            NodeEditor.EditedProperties.PropertyConnections.Remove(propertyConnection);
                            break;
                        }

                        break;
                    }
                    case ConnectionType.Link:
                    {
                        foreach (dynamic linkConnection in NodeEditor.EditedProperties.LinkConnections)
                        {
                            if (!connection.Equals(linkConnection)) continue;
                            NodeEditor.EditedProperties.LinkConnections.Remove(linkConnection);
                            break;
                        }

                        break;
                    }
                }
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

                        eventConnection.Source = new PointerRef(connection.SourceNode.Object);
                        eventConnection.Target = new PointerRef(connection.TargetNode.Object);
                        eventConnection.SourceEvent.Name = connection.SourceField;
                        eventConnection.TargetEvent.Name = connection.TargetField;

                        ((dynamic)NodeEditor.EditedEbxAsset.RootObject).EventConnections
                            .Add(eventConnection);
                        connection.Object = eventConnection;
                    } break;
                
                    case ConnectionType.Property:
                    {
                        dynamic propertyConnection = TypeLibrary.CreateObject("PropertyConnection");

                        propertyConnection.Source = new PointerRef(connection.SourceNode.Object);
                        propertyConnection.Target = new PointerRef(connection.TargetNode.Object);
                        propertyConnection.SourceField = connection.SourceField;
                        propertyConnection.TargetField = connection.TargetField;

                        ((dynamic)NodeEditor.EditedEbxAsset.RootObject).PropertyConnections
                            .Add(propertyConnection);
                        connection.Object = propertyConnection;
                    } break;
                
                    case ConnectionType.Link:
                    {
                        dynamic linkConnection = TypeLibrary.CreateObject("LinkConnection");

                        linkConnection.Source = new PointerRef(connection.SourceNode.Object);
                        linkConnection.Target = new PointerRef(connection.TargetNode.Object);
                    
                        if (connection.SourceField != "self")
                        {
                            linkConnection.SourceField = connection.SourceField;
                        }

                        if (connection.TargetField != "self")
                        {
                            linkConnection.TargetField = connection.TargetField;
                        }

                        ((dynamic)NodeEditor.EditedEbxAsset.RootObject).LinkConnections.Add(
                            linkConnection);
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

        /// <summary>
        /// This method is triggered whenever something is edited in the property grid.
        /// It is one of the most important parts of the EbxEditor, ensuring that the Nodes, Property grid, and the actual Ebx stay in sync.
        /// </summary>
        /// <param name="newObj"></param>
        public virtual bool EditEbx(object newObj, ItemModifiedEventArgs args)
        {
            if (newObj.GetType().Name != "InterfaceDescriptorData" && newObj.GetType().Name != "DataField" && newObj.GetType().Name != "DynamicEvent" && newObj.GetType().Name != "DynamicLink")
            {
                dynamic nodeProperties = newObj;
                AssetClassGuid nodeGuid = nodeProperties.GetInstanceGuid();
                NodeBaseModel node = NodeEditor.GetNode(nodeGuid);
                node.Object = newObj;
                node.Name = node.Object.__Id.ToString();
                node.OnModified();
            
                //TODO: Update this so we aren't enumerating over every single asset in the entire file
                for (int i = 0; i < NodeEditor.EditedProperties.Objects.Count; i++)
                {
                    PointerRef pointerRef = NodeEditor.EditedProperties.Objects[i];
                    if (((dynamic)pointerRef.Internal).GetInstanceGuid() == nodeGuid)
                    {
                        NodeEditor.EditedProperties.Objects[i] = new PointerRef(newObj);
                        return true;
                    }
                }

                return false;
            }
            else
            {
                switch (newObj.GetType().Name)
                {
                    case "InterfaceDescriptorData":
                    {
                        NodeEditor.RefreshInterfaceNodes(newObj);
                        NodeEditor.EditedProperties.Interface = new PointerRef(newObj);
                        return true;
                    } break;
                    
                    case "DataField":
                    {
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
                                        interfaceDataNode.OnModified();
                                        if (interfaceDataNode.Inputs.Count != 0)
                                        {
                                            NodeEditor.InterfaceInputDataNodes.Remove(name);
                                            NodeEditor.InterfaceInputDataNodes.Add(interfaceDataNode.Inputs[0].Title, interfaceDataNode);
                                        }
                                        else
                                        {
                                            NodeEditor.InterfaceOutputDataNodes.Remove(name);
                                            NodeEditor.InterfaceOutputDataNodes.Add(interfaceDataNode.Outputs[0].Title, interfaceDataNode);
                                        }
                                    }
                                    
                                    NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString($"{args.OldValue}"), EditorStatus.Error);
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

                                node.OnModified();
                                NodeEditor.InterfaceInputDataNodes.Remove(name);
                                NodeEditor.InterfaceInputDataNodes.Add(node.Inputs[0].Title, node);
                                NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString($"{args.OldValue}"), EditorStatus.Error);
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

                                node.OnModified();
                                NodeEditor.InterfaceOutputDataNodes.Remove(name);
                                NodeEditor.InterfaceOutputDataNodes.Add(node.Outputs[0].Title, node);
                                NodeEditor.ResetEditorStatus(FrostySdk.Utils.HashString($"{args.OldValue}"), EditorStatus.Error);
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
                }
            }

            return false;
        }
    }
}