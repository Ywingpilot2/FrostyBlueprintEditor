using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of a node, or a vertex with a header, inputs, and outputs.
    /// </summary>
    public interface INode : IVertex, IStatusItem
    {
        string Header { get; set; }
        ObservableCollection<IPort> Inputs { get; }
        ObservableCollection<IPort> Outputs { get; }

        void OnInputUpdated(IPort port);
        void OnOutputUpdated(IPort port);

        void AddPort(IPort port);
    }
}