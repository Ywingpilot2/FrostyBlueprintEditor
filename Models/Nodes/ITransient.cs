using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of a transient item, which has it's own methods for saving into layouts.
    /// </summary>
    public interface ITransient : IVertex
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        ITransient Load(NativeReader reader);
        ITransient Save(NativeWriter writer);
    }
}