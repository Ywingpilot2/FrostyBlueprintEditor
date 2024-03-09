using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Nodes
{
    public interface INode : IVertex, IStatusItem
    {
        ObservableCollection<BaseInput> Inputs { get; }
        ObservableCollection<BaseOutput> Outputs { get; }
    }
}