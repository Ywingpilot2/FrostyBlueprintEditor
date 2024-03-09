using System.ComponentModel;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Connections
{
    public interface IConnection : INotifyPropertyChanged, IStatusItem
    {
        IPort Source { get; }
        IPort Target { get; }
        bool IsSelected { get; set; }
    }
}