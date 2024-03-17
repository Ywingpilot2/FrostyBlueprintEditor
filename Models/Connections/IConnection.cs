using System.ComponentModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Connections
{
    public interface IConnection : INotifyPropertyChanged
    {
        IPort Source { get; }
        IPort Target { get; }
        bool IsSelected { get; set; }
    }
}