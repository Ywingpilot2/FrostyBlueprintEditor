using System;
using BlueprintEditor.Utils;

namespace BlueprintEditor.Models.MenuItems
{
    /// <summary>
    /// A type which you can select in the NodeTypes list
    /// </summary>
    public class NodeTypeViewModel
    {
        /// <summary>
        /// The clean name of the type
        /// </summary>
        public string Name => NodeUtils.CleanNodeName(NodeType.Name);

        /// <summary>
        /// The type itself
        /// </summary>
        public Type NodeType { get; set; }

        public NodeTypeViewModel(Type type)
        {
            NodeType = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}