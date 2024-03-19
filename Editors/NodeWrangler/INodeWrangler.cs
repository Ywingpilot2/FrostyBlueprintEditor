using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.NodeWrangler
{
    public interface INodeWrangler
    {
        ObservableCollection<IVertex> Nodes { get; }
        ObservableCollection<IVertex> SelectedNodes { get; }
        ObservableCollection<IConnection> Connections { get; }
        IPendingConnection PendingConnection { get; }
        ICommand RemoveConnectionsCommand { get; }

        void AddNode(IVertex vertex);
        void RemoveNode(IVertex vertex);

        void AddConnection(IConnection connection);
        void RemoveConnection(IConnection connection);

        void ClearConnections(INode node);
        void ClearConnections(IPort port);

        IEnumerable<IConnection> GetConnections(INode node);
        IEnumerable<IConnection> GetConnections(IPort port);
    }
}