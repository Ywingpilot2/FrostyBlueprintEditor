using System.IO;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Transient
{
    /// <summary>
    /// The base for a Transient node
    /// </summary>
    public class TransientNode : NodeBaseModel
    {
        public override bool IsTransient => true;
        public override string ObjectType { get; set; } = "Transient";

        /// <summary>
        /// Loads transient data for this node from a line
        /// </summary>
        /// <param name="reader"></param>
        public virtual void LoadTransientData(StreamReader reader)
        {
            
        }

        /// <summary>
        /// Modifies transient data based on an object
        /// </summary>
        public virtual void ModifyTransientData(object obj, ItemModifiedEventArgs args)
        {
            
        }

        public virtual void RemoveNodeObject()
        {
            
        }

        /// <summary>
        /// This is in charge of editing the ebx of the connections connected to this
        /// If both the source and target are both transient, the source will take priority.
        /// </summary>
        public virtual void CreateConnectionObject(ConnectionViewModel connection)
        {
            
        }

        /// <summary>
        /// This is in charge of editing the ebx of the connections connected to this
        /// If both the source and target are both transient, the source will take priority.
        /// </summary>
        public virtual void RemoveConnectionObject(ConnectionViewModel connection)
        {
            
        }

        /// <summary>
        /// Saves this transient data into a layout file
        /// </summary>
        public virtual void SaveTransientData(StreamWriter writer)
        {
            
        }
    }
}