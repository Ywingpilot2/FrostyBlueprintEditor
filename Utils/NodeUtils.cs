using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Types;
using BlueprintEditor.Models.Types.Shared;
using Frosty.Core;
using FrostySdk.Ebx;

namespace BlueprintEditor.Utils
{
    public static class NodeUtils
    {
        /// <summary>
        /// A list of extensions for nodes
        /// </summary>
        public static Dictionary<string, NodeBaseModel> NodeExtensions = new Dictionary<string, NodeBaseModel>();
        
        public static AssetClassGuid InterfaceGuid { get; set; }

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
        public static string CleanNodeName(string typeName)
        {
            foreach (string dataType in DataTypes)
            {
                if (typeName.Contains(dataType))
                {
                    return typeName.Replace(dataType, "");
                }
            }

            return typeName;
        }
        
        /// <summary>
        /// This will create a new node from an object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static NodeBaseModel CreateNodeFromObject(object obj)
        {
            string key = obj.GetType().Name;
            if (!NodeExtensions.ContainsKey(key))
            {
                key = "null";
            }

            var newNode = (NodeBaseModel)Activator.CreateInstance(NodeExtensions[key].GetType());
            newNode.Name = obj.GetType().Name;
            newNode.Object = obj;
            
            if (key == "null")
            {
                newNode.Inputs = GenerateNodeInputs(obj.GetType(), newNode);
            }
            
            newNode.OnCreation();

            EditorUtils.Editor.Nodes.Add(newNode);
            return newNode;
        }

        /// <summary>
        /// Deletes a node(and by extension all of its connections).
        /// </summary>
        /// <param name="node">Node to delete</param>
        public static void DeleteNode(NodeBaseModel node)
        {
            #region Interface removal

            if (node.ObjectType == "EditorInterfaceNode")
            {
                //Is this in or out?
                if (node.Inputs.Count != 0)
                {
                    var input = node.Inputs[0];
                    
                    foreach (ConnectionViewModel connection in EditorUtils.Editor.GetConnections(input))
                    {
                        EditorUtils.Editor.Disconnect(connection);
                    }
                    
                    switch (input.Type)
                    {
                        case ConnectionType.Property:
                        {
                            dynamic objToRemove = null;
                            foreach (dynamic field in EditorUtils.Editor.EditedProperties.Interface.Internal.Fields)
                            {
                                if (field.Name.ToString() == input.Title)
                                {
                                    objToRemove = field;
                                }
                            }

                            if (objToRemove != null)
                            {
                                EditorUtils.Editor.EditedProperties.Interface.Internal.Fields.Remove(objToRemove);
                            }
                            break;
                        }
                        case ConnectionType.Event:
                        {
                            dynamic objToRemove = null;
                            foreach (dynamic field in EditorUtils.Editor.EditedProperties.Interface.Internal.OutputEvents)
                            {
                                if (field.Name.ToString() == input.Title)
                                {
                                    objToRemove = field;
                                }
                            }

                            if (objToRemove != null)
                            {
                                EditorUtils.Editor.EditedProperties.Interface.Internal.OutputEvents.Remove(objToRemove);
                            }
                            break;
                        }
                        case ConnectionType.Link:
                        {
                            dynamic objToRemove = null;
                            foreach (dynamic field in EditorUtils.Editor.EditedProperties.Interface.Internal.OutputLinks)
                            {
                                if (field.Name.ToString() == input.Title)
                                {
                                    objToRemove = field;
                                }
                            }

                            if (objToRemove != null)
                            {
                                EditorUtils.Editor.EditedProperties.Interface.Internal.OutputLinks.Remove(objToRemove);
                            }
                            break;
                        }
                    }
                }
                
                else
                {
                    var output = node.Outputs[0];
                    
                    foreach (ConnectionViewModel connection in EditorUtils.Editor.GetConnections(output))
                    {
                        EditorUtils.Editor.Disconnect(connection);
                    }
                    
                    switch (output.Type)
                    {
                        case ConnectionType.Property:
                        {
                            dynamic objToRemove = null;
                            foreach (dynamic field in EditorUtils.Editor.EditedProperties.Interface.Internal.Fields)
                            {
                                if (field.Name.ToString() == output.Title)
                                {
                                    objToRemove = field;
                                }
                            }

                            if (objToRemove != null)
                            {
                                EditorUtils.Editor.EditedProperties.Interface.Internal.Fields.Remove(objToRemove);
                            }
                            break;
                        }
                        case ConnectionType.Event:
                        {
                            dynamic objToRemove = null;
                            foreach (dynamic field in EditorUtils.Editor.EditedProperties.Interface.Internal.InputEvents)
                            {
                                if (field.Name.ToString() == output.Title)
                                {
                                    objToRemove = field;
                                }
                            }

                            if (objToRemove != null)
                            {
                                EditorUtils.Editor.EditedProperties.Interface.Internal.InputEvents.Remove(objToRemove);
                            }
                            break;
                        }
                        case ConnectionType.Link:
                        {
                            dynamic objToRemove = null;
                            foreach (dynamic field in EditorUtils.Editor.EditedProperties.Interface.Internal.InputLinks)
                            {
                                if (field.Name.ToString() == output.Title)
                                {
                                    objToRemove = field;
                                }
                            }

                            if (objToRemove != null)
                            {
                                EditorUtils.Editor.EditedProperties.Interface.Internal.InputLinks.Remove(objToRemove);
                            }
                            break;
                        }
                    }
                }

                EditorUtils.Editor.Nodes.Remove(node);
                
                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(EditorUtils.Editor.EditedEbxAsset.FileGuid).Name, EditorUtils.Editor.EditedEbxAsset);
                App.EditorWindow.DataExplorer.RefreshItems();
                return;
            }

            #endregion

            #region Object Removal
            
            foreach (ConnectionViewModel connection in EditorUtils.Editor.GetConnections(node))
            {
                EditorUtils.Editor.Disconnect(connection);
            }

            //Remove the object pointer
            List<PointerRef> pointerRefs = EditorUtils.Editor.EditedProperties.Objects;
            pointerRefs.RemoveAll(pointer => ((dynamic)pointer.Internal).GetInstanceGuid() == node.Object.GetInstanceGuid());
            
            EditorUtils.Editor.EditedEbxAsset.RemoveObject(node.Object);
            EditorUtils.Editor.Nodes.Remove(node);
            
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(EditorUtils.Editor.EditedEbxAsset.FileGuid).Name, EditorUtils.Editor.EditedEbxAsset);
            App.EditorWindow.DataExplorer.RefreshItems();

            #endregion
        }

        /// <summary>
        /// Creates an interface node from a InterfaceDescriptorData
        /// </summary>
        /// <param name="obj">InterfaceDescriptorData</param>
        /// <returns></returns>
        public static void CreateInterfaceNodes(object obj)
        {
            InterfaceGuid = ((dynamic)obj).GetInstanceGuid();
            
            foreach (dynamic field in ((dynamic)obj).Fields)
            {
                if (field.AccessType.ToString() == "FieldAccessType_Source") //Source
                {
                    NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(field, false, InterfaceGuid);
                    node.Object = obj;
                    EditorUtils.Editor.Nodes.Add(node);
                }
                else if (field.AccessType.ToString() == "FieldAccessType_Target") //Target
                {
                    NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(field, true, InterfaceGuid);
                    node.Object = obj;
                    EditorUtils.Editor.Nodes.Add(node);
                }
                else //Source and Target
                {
                    NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(field, true, InterfaceGuid);
                    node.Object = obj;
                    EditorUtils.Editor.Nodes.Add(node);
                    
                    node = InterfaceDataNode.CreateInterfaceDataNode(field, false, InterfaceGuid);
                    node.Object = obj;
                    EditorUtils.Editor.Nodes.Add(node);
                }
            }

            foreach (dynamic inputEvent in ((dynamic)obj).InputEvents)
            {
                NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(inputEvent, true, InterfaceGuid);
                node.Object = obj;
                EditorUtils.Editor.Nodes.Add(node);
            }
                
            foreach (dynamic outputEvent in ((dynamic)obj).OutputEvents)
            {
                NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(outputEvent, false, InterfaceGuid);
                node.Object = obj;
                EditorUtils.Editor.Nodes.Add(node);
            }
                
            foreach (dynamic inputLink in ((dynamic)obj).InputLinks)
            {
                NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(inputLink, true, InterfaceGuid);
                node.Object = obj;
                EditorUtils.Editor.Nodes.Add(node);
            }
                
            foreach (dynamic outputLink in ((dynamic)obj).OutputLinks)
            {
                NodeBaseModel node = InterfaceDataNode.CreateInterfaceDataNode(outputLink, false, InterfaceGuid);
                node.Object = obj;
                EditorUtils.Editor.Nodes.Add(node);
            }
        }
        
        /// <summary>
        /// This will generate probabilistically determined property inputs for a Node
        /// </summary>
        /// <param name="objectType">The type of the object we are generating off of</param>
        /// <param name="nodeBaseModel"></param>
        /// <returns>Inputs for a node</returns>
        public static ObservableCollection<InputViewModel> GenerateNodeInputs(Type objectType,
            NodeBaseModel nodeBaseModel)
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

        static NodeUtils()
        {
            NodeExtensions.Add("null", new NodeBaseModel());
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(NodeBaseModel)))
                {
                    var extension = (NodeBaseModel)Activator.CreateInstance(type);
                    NodeExtensions.Add(extension.ObjectType, extension);
                }
            }
        }
    }
}