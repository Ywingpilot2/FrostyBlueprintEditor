using System;
using System.Collections.Generic;
using System.Windows.Controls;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.MenuItems;
using BlueprintEditor.Models.Types.NodeTypes;
using BlueprintEditor.Utils;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditor.Models.Types.BlueprintLoaderTypes
{
    public class BlueprintBaseLoader
    {
        public virtual string AssetType { get; } = "null";

        public Dictionary<AssetClassGuid, int> NodeIdCache = new Dictionary<AssetClassGuid, int>();

        /// <summary>
        /// Populates the Types List with a list of addable types
        /// </summary>
        /// <param name="itemsCollection"></param>
        public virtual void PopulateTypesList(ItemCollection itemsCollection)
        {
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("GameDataContainer"))
            {
                itemsCollection.Add(new NodeTypeViewModel(type));
            }
        }
        
        /// <summary>
        /// This loads all of the nodes from the RootObject(so whats seen in the property grid) into the graph.
        /// </summary>
        /// <param name="properties"></param>
        public virtual void PopulateNodes(dynamic properties)
        {
            //Create object nodes
            foreach (PointerRef ptr in properties.Objects) 
            {
                object obj = ptr.Internal;
                NodeBaseModel node = EditorUtils.CurrentEditor.CreateNodeFromObject(obj);
                node.Guid = ((dynamic)obj).GetInstanceGuid();
                if (NodeIdCache.ContainsKey(node.Guid))
                {
                    continue;
                }
                NodeIdCache.Add(node.Guid, EditorUtils.CurrentEditor.Nodes.IndexOf(node));
            }
            
            PointerRef interfaceRef = (PointerRef) properties.Interface;
            EditorUtils.CurrentEditor.CreateInterfaceNodes(interfaceRef.Internal);
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
                if (propertyConnection.Source.Internal == null || propertyConnection.Target.Internal == null) continue; 

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)propertyConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)propertyConnection.Target.Internal).GetInstanceGuid();

                //First check if the the node is an interface node
                if (((dynamic)propertyConnection.Source.Internal).GetInstanceGuid() == EditorUtils.CurrentEditor.InterfaceGuid)
                {
                    sourceNode = EditorUtils.CurrentEditor.InterfaceOutputDataNodes[(string)propertyConnection.SourceField.ToString()];
                }
                else
                {
                    sourceNode = EditorUtils.CurrentEditor.Nodes[NodeIdCache[sourceGuid]];
                }


                if (((dynamic)propertyConnection.Target.Internal).GetInstanceGuid() == EditorUtils.CurrentEditor.InterfaceGuid)
                {
                    targetNode = EditorUtils.CurrentEditor.InterfaceInputDataNodes[(string)propertyConnection.TargetField.ToString()];
                }
                else
                {
                    targetNode = EditorUtils.CurrentEditor.Nodes[NodeIdCache[targetGuid]];
                }

                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    continue;
                }
                
                InputViewModel targetInput =
                    targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property, true);
                OutputViewModel sourceOutput = sourceNode.GetOutput(propertyConnection.SourceField,
                    ConnectionType.Property, true);

                var connection = EditorUtils.CurrentEditor.Connect(sourceOutput, targetInput);
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
                if (((dynamic)eventConnection.Source.Internal).GetInstanceGuid() == EditorUtils.CurrentEditor.InterfaceGuid)
                {
                    sourceNode = EditorUtils.CurrentEditor.InterfaceOutputDataNodes[(string)eventConnection.SourceEvent.Name.ToString()];
                }
                else
                {
                    sourceNode = EditorUtils.CurrentEditor.Nodes[NodeIdCache[sourceGuid]];
                }

                if (((dynamic)eventConnection.Target.Internal).GetInstanceGuid() == EditorUtils.CurrentEditor.InterfaceGuid)
                {
                    targetNode = EditorUtils.CurrentEditor.InterfaceInputDataNodes[(string)eventConnection.TargetEvent.Name.ToString()];
                }
                else
                {
                    targetNode = EditorUtils.CurrentEditor.Nodes[NodeIdCache[targetGuid]];
                }


                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    continue;
                }
                
                InputViewModel targetInput =
                    targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event, true);
                OutputViewModel sourceOutput = sourceNode.GetOutput(eventConnection.SourceEvent.Name,
                    ConnectionType.Event, true);
                        
                var connection = EditorUtils.CurrentEditor.Connect(sourceOutput, targetInput);
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
                if (((dynamic)linkConnection.Source.Internal).GetInstanceGuid() == EditorUtils.CurrentEditor.InterfaceGuid)
                {
                    sourceNode = EditorUtils.CurrentEditor.InterfaceOutputDataNodes[(string)linkConnection.SourceField.ToString()];
                }
                else
                {
                    sourceNode = EditorUtils.CurrentEditor.Nodes[NodeIdCache[sourceGuid]];
                }

                if (((dynamic)linkConnection.Target.Internal).GetInstanceGuid() == EditorUtils.CurrentEditor.InterfaceGuid)
                {
                    targetNode = EditorUtils.CurrentEditor.InterfaceInputDataNodes[(string)linkConnection.TargetField.ToString()];
                }
                else
                {
                    targetNode = EditorUtils.CurrentEditor.Nodes[NodeIdCache[targetGuid]];
                }


                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
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
                        
                var connection = EditorUtils.CurrentEditor.Connect(sourceOutput, targetInput);
                connection.Object = linkConnection;
            }
        }
    }
}