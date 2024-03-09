using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes
{
    public interface ITransient : IVertex, IStatusItem
    {
        void Load(NativeReader reader);
        void Save(NativeWriter writer);
    }
}