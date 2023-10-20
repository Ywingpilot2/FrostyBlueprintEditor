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