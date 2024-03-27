using System.ComponentModel;
using System.Windows;

namespace BlueprintEditorPlugin.Models.Nodes.Ports
{
    public interface IPort : INotifyPropertyChanged
    {
        string Name { get; set; }
        PortDirection Direction { get; }
        Point Anchor { get; set; }
        INode Node { get; }
        bool IsConnected { get; set; }
    }

    public enum PortDirection
    {
        In = 0,
        Out = 1
    }
}