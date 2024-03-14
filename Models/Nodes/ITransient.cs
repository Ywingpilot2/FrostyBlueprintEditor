using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of a dynamic transient item, which has it's own methods for saving into layouts.
    /// </summary>
    public interface ITransient : IVertex
    {
        void Load(NativeReader reader);
        void Save(NativeWriter writer);
    }
}