using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using BlueprintEditorPlugin.Utils;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Models.Types.EbxLoaderTypes
{
    /// <summary>
    /// This is the base class for a Blueprint Loader
    /// If you ever wish to create a graphed form of a specific asset type(which this does not support)
    /// you would want to make an extension of this
    /// </summary>
    public class EbxBaseLoader
    {
        /// <summary>
        /// This is the asset type that the Blueprint Loader uses
        /// In this case its set to null, but you would want to set it to the asset type
        /// e.g, LogicPrefabBlueprint
        /// </summary>
        public virtual string AssetType { get; } = "null";

        /// <summary>
        /// Whether or not this type has an Interface
        /// </summary>
        public virtual bool HasInterface => true;
        public EditorViewModel NodeEditor { get; set; }

        /// <summary>
        /// Contains a list of AssetClassGuid's and the EditorViewModel.Nodes[] index
        /// </summary>
        public Dictionary<AssetClassGuid, int> NodeIdCache = new Dictionary<AssetClassGuid, int>();

        /// <summary>
        /// This method is used to fill the Types list with types
        /// </summary>
        public virtual void PopulateTypesList(List<Type> typesList)
        {
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("GameDataContainer"))
            {
                typesList.Add(type);
            }
        }

        /// <summary>
        /// This gets an Object and turns it into a node which we can manipulate with <see cref="EbxEditorTypes"/>
        /// </summary>
        /// <param name="obj">The object we are using to create this node, e.g compare bool entity</param>
        /// <returns></returns>
        public virtual EntityNode GetNodeFromObject(object obj)
        {
            //We need to ensure that there isn't a node extension for this type, so we first get this object's type and see if there is an extension for it
            string key = obj.GetType().Name;
            EntityNode newNode;
            
            //Check if a C# extension exists
            if (NodeUtils.NodeExtensions.ContainsKey(key))
            {
                newNode = (EntityNode)Activator.CreateInstance(NodeUtils.NodeExtensions[key].GetType());
                newNode.Object = obj;
            }
            //If not, check if a NMC exists
            else if (NodeUtils.NmcExtensions.ContainsKey(key))
            {
                newNode = new EntityNode
                {
                    ObjectType = key,
                    Object = obj
                };
                NodeUtils.ApplyNodeMapping(newNode);
            }
            else //We could not find an extension, so we just arbitrarily determine inputs and set its name
            {
                newNode = new EntityNode() {Object = obj};
                newNode.Inputs = NodeUtils.GenerateNodeInputs(obj.GetType(), newNode);
                newNode.Name = ((dynamic)obj).__Id.ToString(); //Set the name to be the nodes __Id
            }
            
            newNode.Guid = ((dynamic)obj).GetInstanceGuid();
            newNode.OnCreation();

            //Add the realm to the port names(if they do not already have it)
            foreach (InputViewModel input in newNode.Inputs)
            {
                if (!input.DisplayName.EndsWith(")"))
                {
                    input.DisplayName += $"({input.Realm})";
                }
            }
            
            foreach (OutputViewModel output in newNode.Outputs)
            {
                if (!output.DisplayName.EndsWith(")"))
                {
                    output.DisplayName += $"({output.Realm})";
                }
            }

            NodeEditor.Nodes.Add(newNode);
            return newNode;
        }
        
        /// <summary>
        /// This loads all of the nodes from the RootObject(so whats seen in the property grid) into the graph.
        /// </summary>
        /// <param name="properties">The properties from the RootObject. These are what you would see from the property grid when opening an asset</param>
        public virtual void PopulateNodes(dynamic properties)
        {
            //Create object nodes
            foreach (PointerRef ptr in properties.Objects) 
            {
                object obj = ptr.Internal;
                EntityNode node = GetNodeFromObject(obj);
                if (NodeIdCache.ContainsKey(node.Guid))
                {
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{node.Guid}_identical"), $"Multiple nodes share the guid {node.Guid}");
                    continue;
                }
                NodeIdCache.Add(node.Guid, NodeEditor.Nodes.IndexOf(node));
            }
            
            PointerRef interfaceRef = (PointerRef) properties.Interface;
            NodeEditor.CreateInterfaceNodes(interfaceRef.Internal);
        }

        /// <summary>
        /// Loads all of the Connections from the RootObject(whats seen in the property grid) into the graph
        /// </summary>
        /// <param name="properties"></param>
        public virtual void CreateConnections(dynamic properties)
        {
            //Create property connections
            foreach (dynamic propertyConnection in properties.PropertyConnections)
            {
                //TODO: Update to check if external ref
                if (propertyConnection.Source.Internal == null || propertyConnection.Target.Internal == null)
                {
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 0, "Some connections in this file contain null references, certain connections maybe missing from this ui as a result.");
                    continue;
                }

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)propertyConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)propertyConnection.Target.Internal).GetInstanceGuid();

                //First check if the the node is an interface node
                if (((dynamic)propertyConnection.Source.Internal).GetInstanceGuid() == NodeEditor.InterfaceGuid)
                {
                    if (NodeEditor.InterfaceOutputDataNodes.ContainsKey(propertyConnection.SourceField.ToString()))
                    {
                        sourceNode = NodeEditor.InterfaceOutputDataNodes[propertyConnection.SourceField.ToString()];
                    }
                    else if (NodeEditor.InterfaceOutputDataNodes.Any(x => 
                                 FrostySdk.Utils.HashString(x.Key)
                                 == 
                                 propertyConnection.SourceFieldId))
                    {
                        sourceNode = NodeEditor.InterfaceOutputDataNodes.First(x => FrostySdk.Utils.HashString(x.Key).ToString("x8") == propertyConnection.SourceField.ToString()).Value;
                    }
                    else
                    {
                        NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{propertyConnection.SourceField.ToString()}_missing"), $"The interface node {propertyConnection.SourceField.ToString()} does not exist, yet is referenced in the Property Connections of this ebx. Some connections might be missing as a result");
                    }
                }
                else
                {
                    sourceNode = NodeEditor.Nodes[NodeIdCache[sourceGuid]];
                }


                if (((dynamic)propertyConnection.Target.Internal).GetInstanceGuid() == NodeEditor.InterfaceGuid)
                {
                    if (NodeEditor.InterfaceInputDataNodes.ContainsKey(propertyConnection.TargetField.ToString()))
                    {
                        targetNode = NodeEditor.InterfaceInputDataNodes[propertyConnection.TargetField.ToString()];
                    }
                    else if (NodeEditor.InterfaceInputDataNodes.Any(x => 
                                 FrostySdk.Utils.HashString(x.Key)
                                 == 
                                 propertyConnection.TargetFieldId))
                    {
                        targetNode = NodeEditor.InterfaceInputDataNodes.First(x => FrostySdk.Utils.HashString(x.Key) == propertyConnection.TargetFieldId).Value;
                    }
                    else
                    {
                        NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{propertyConnection.TargetField.ToString()}_missing"), $"The interface node {propertyConnection.TargetField.ToString()} does not exist, yet is referenced in the Property Connections of this ebx. Some connections might be missing as a result");
                    }
                }
                else
                {
                    targetNode = NodeEditor.Nodes[NodeIdCache[targetGuid]];
                }

                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 0, "Some connections in this file are invalid, certain connections maybe missing from this ui as a result.");
                    continue;
                }
                
                InputViewModel targetInput = targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property, true);

                OutputViewModel sourceOutput = sourceNode.GetOutput(propertyConnection.SourceField, ConnectionType.Property, true);

                //Setup the flags
                var flagHelper = new PropertyFlagsHelper(propertyConnection.Flags);

                if (NodeUtils.RealmsAreValid(flagHelper))
                {
                    if (targetInput.Realm == ConnectionRealm.Invalid)
                    {
                        targetInput.Realm = flagHelper.Realm;
                    }
                
                    if (sourceOutput.Realm == ConnectionRealm.Invalid)
                    {
                        sourceOutput.Realm = flagHelper.Realm;
                    }
                    SetupPortRealms(sourceOutput, targetInput);
                }
                else
                {
                    //TODO: Update this problem ID so that it specifically references this connection
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 1, "Some connections in this file have invalid realms");
                }

                //Check that the realms of the source and target objects are valid
                if (!NodeUtils.RealmsAreValid(sourceNode.Object, targetNode.Object))
                {
                    //TODO: Update this problem ID so that it specifically references this connection
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 1, "Some connections in this file have invalid realms");
                }

                ConnectionViewModel connection = NodeEditor.Connect(sourceOutput, targetInput);
                connection.Object = propertyConnection;
            }
                
            //Create event connections
            foreach (dynamic eventConnection in properties.EventConnections)
            {

                if (eventConnection.Source.Internal == null || eventConnection.Target.Internal == null) continue;

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)eventConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)eventConnection.Target.Internal).GetInstanceGuid();

                //First check if the the node is an interface node
                if (((dynamic)eventConnection.Source.Internal).GetInstanceGuid() == NodeEditor.InterfaceGuid)
                {
                    if (NodeEditor.InterfaceOutputDataNodes.ContainsKey(eventConnection.SourceEvent.Name.ToString()))
                    {
                        sourceNode = NodeEditor.InterfaceOutputDataNodes[eventConnection.SourceEvent.Name.ToString()];
                    }
                    else if (NodeEditor.InterfaceOutputDataNodes.Any(x => 
                                 FrostySdk.Utils.HashString(x.Key)
                                 == 
                                 eventConnection.SourceEvent.Id))
                    {
                        sourceNode = NodeEditor.InterfaceOutputDataNodes.First(x => FrostySdk.Utils.HashString(x.Key) == eventConnection.SourceEvent.Id).Value;
                    }
                    else
                    {
                        NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{eventConnection.SourceEvent.Name.ToString()}_missing"), $"The interface node {eventConnection.SourceEvent.Name.ToString()} does not exist, yet is referenced in the Event Connections of this ebx. Some connections might be missing as a result");
                    }
                }
                else
                {
                    sourceNode = NodeEditor.Nodes[NodeIdCache[sourceGuid]];
                }

                if (((dynamic)eventConnection.Target.Internal).GetInstanceGuid() == NodeEditor.InterfaceGuid)
                {
                    if (NodeEditor.InterfaceInputDataNodes.ContainsKey(eventConnection.TargetEvent.Name.ToString()))
                    {
                        targetNode = NodeEditor.InterfaceInputDataNodes[eventConnection.TargetEvent.Name.ToString()];
                    }
                    else if (NodeEditor.InterfaceInputDataNodes.Any(x => 
                                 FrostySdk.Utils.HashString(x.Key)
                                 == 
                                 eventConnection.TargetEvent.Id))
                    {
                        targetNode = NodeEditor.InterfaceInputDataNodes.First(x => FrostySdk.Utils.HashString(x.Key) == eventConnection.TargetEvent.Id).Value;
                    }
                    else
                    {
                        NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{eventConnection.TargetEvent.Name.ToString()}_missing"), $"The interface node {eventConnection.TargetEvent.Name.ToString()} does not exist, yet is referenced in the Event Connections of this ebx. Some connections might be missing as a result");
                    }
                }
                else
                {
                    targetNode = NodeEditor.Nodes[NodeIdCache[targetGuid]];
                }


                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 0, $"Some connections in this file contain null references, certain connections maybe missing from this ui as a result.");
                    continue;
                }
                
                InputViewModel targetInput = targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event, true);

                OutputViewModel sourceOutput = sourceNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event, true);

                if (NodeUtils.RealmsAreValid(eventConnection.TargetType.ToString()))
                {
                    targetInput.Realm = NodeUtils.ParseRealmFromString(eventConnection.TargetType.ToString());
                    sourceOutput.Realm = NodeUtils.ParseRealmFromString(eventConnection.TargetType.ToString());
                    SetupPortRealms(sourceOutput, targetInput);
                }
                else
                {
                    //TODO: Update this problem ID so that it specifically references this connection
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 1, "Some connections in this file have invalid realms");
                }
                
                //Check that the realms of the source and target objects are valid
                if (!NodeUtils.RealmsAreValid(sourceNode.Object, targetNode.Object))
                {
                    //TODO: Update this problem ID so that it specifically references this connection
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 1, "Some connections in this file have invalid realms");
                }

                var connection = NodeEditor.Connect(sourceOutput, targetInput);
                connection.Object = eventConnection;
            }
                
            //Create link connections
            foreach (dynamic linkConnection in properties.LinkConnections)
            {
                //TODO: Update to check if external ref
                if (linkConnection.Source.Internal == null || linkConnection.Target.Internal == null) continue; 

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)linkConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)linkConnection.Target.Internal).GetInstanceGuid();

                //First check if the the node is an interface node
                if (((dynamic)linkConnection.Source.Internal).GetInstanceGuid() == NodeEditor.InterfaceGuid)
                {
                    if (NodeEditor.InterfaceOutputDataNodes.ContainsKey(linkConnection.SourceField.ToString()))
                    {
                        sourceNode = NodeEditor.InterfaceOutputDataNodes[linkConnection.SourceField.ToString()];
                    }
                    else if (NodeEditor.InterfaceOutputDataNodes.Any(x => 
                                 FrostySdk.Utils.HashString(x.Key)
                                 == 
                                 linkConnection.SourceFieldId))
                    {
                        sourceNode = NodeEditor.InterfaceOutputDataNodes.First(x => FrostySdk.Utils.HashString(x.Key) == linkConnection.SourceFieldId).Value;
                    }
                    else
                    {
                        NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{linkConnection.SourceField.ToString()}_missing"), $"The interface node {linkConnection.SourceField.ToString()} does not exist, yet is referenced in the Link Connections of this ebx. Some connections might be missing as a result");
                    }
                }
                else
                {
                    sourceNode = NodeEditor.Nodes[NodeIdCache[sourceGuid]];
                }


                if (((dynamic)linkConnection.Target.Internal).GetInstanceGuid() == NodeEditor.InterfaceGuid)
                {
                    if (NodeEditor.InterfaceInputDataNodes.ContainsKey(linkConnection.TargetField.ToString()))
                    {
                        targetNode = NodeEditor.InterfaceInputDataNodes[linkConnection.TargetField.ToString()];
                    }
                    else if (NodeEditor.InterfaceInputDataNodes.Any(x => 
                                 FrostySdk.Utils.HashString(x.Key) 
                                 == 
                                 linkConnection.TargetFieldId))
                    {
                        targetNode = NodeEditor.InterfaceInputDataNodes.First(x => FrostySdk.Utils.HashString(x.Key) == linkConnection.TargetFieldId).Value;
                    }
                    else
                    {
                        NodeEditor.SetEditorStatus(EditorStatus.Warning, FrostySdk.Utils.HashString($"{linkConnection.TargetField.ToString()}_missing"), $"The interface node {linkConnection.TargetField.ToString()} does not exist, yet is referenced in the Link Connections of this ebx. Some connections might be missing as a result");
                    }
                }
                else
                {
                    targetNode = NodeEditor.Nodes[NodeIdCache[targetGuid]];
                }


                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    NodeEditor.SetEditorStatus(EditorStatus.Warning, 0, $"Some connections in this file contain null references, certain connections maybe missing from this ui as a result.");
                    continue;
                }
                
                string sourceField = linkConnection.SourceField;
                string targetField = linkConnection.TargetField;
                if (sourceField == "0x00000000")
                {
                    sourceField = "self";
                }

                if (targetField == "0x00000000")
                {
                    targetField = "self";
                }
                        
                InputViewModel targetInput =
                    targetNode.GetInput(sourceField, ConnectionType.Link, true);
                OutputViewModel sourceOutput = sourceNode.GetOutput(targetField,
                    ConnectionType.Link, true);
                        
                var connection = NodeEditor.Connect(sourceOutput, targetInput);
                connection.Object = linkConnection;
            }
        }

        private void SetupPortRealms(OutputViewModel sourceOutput, InputViewModel targetInput)
        {
            if (!targetInput.DisplayName.EndsWith(")"))
            {
                targetInput.DisplayName += $"({targetInput.Realm})";
            }
            else if (targetInput.DisplayName.EndsWith("(Invalid)"))
            {
                targetInput.DisplayName = targetInput.DisplayName.Replace("(Invalid)", $"({targetInput.Realm})");
            }
                
            if (!sourceOutput.DisplayName.EndsWith(")"))
            {
                sourceOutput.DisplayName += $"({sourceOutput.Realm})";
            }
            else if (sourceOutput.DisplayName.EndsWith("(Invalid)"))
            {
                sourceOutput.DisplayName = targetInput.DisplayName.Replace("(Invalid)", $"({sourceOutput.Realm})");
            }

            if (!NodeUtils.RealmsAreValid(sourceOutput, targetInput))
            {
                //TODO: Update this problem ID so that it specifically references this connection
                NodeEditor.SetEditorStatus(EditorStatus.Warning, 1, "Some connections in this file have invalid realms");
            }
        }
    }
}