using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.NodeWrangler
{
    public class BaseNodeWrangler : INodeWrangler
    {
        public ObservableCollection<INode> Nodes { get; protected set; } = new ObservableCollection<INode>();
        public ObservableCollection<INode> SelectedNodes { get; protected set; } = new ObservableCollection<INode>();
        public ObservableCollection<IConnection> Connections { get; protected set; } = new ObservableCollection<IConnection>();
        
        public IPendingConnection PendingConnection { get; protected set; }
        public ICommand RemoveConnectionsCommand { get; protected set; }

        #region Node

        public virtual void AddNode(INode node)
        {
            Nodes.Add(node);
        }

        public virtual void RemoveNode(INode node)
        {
            Nodes.Remove(node);
        }

        #endregion

        #region Connection Editing

        public virtual void AddConnection(IConnection connection)
        {
            Connections.Add(connection);
        }

        public virtual void RemoveConnection(IConnection connection)
        {
            connection.Source.IsConnected = false;
            connection.Target.IsConnected = false;
            Connections.Remove(connection);
        }

        public virtual void ClearConnections(INode node)
        {
            List<IConnection> connections = GetConnections(node).ToList();
            foreach (IConnection connection in connections)
            {
                Connections.Remove(connection);
            }
        }
        
        public virtual void ClearConnections(IPort port)
        {
            List<IConnection> connections = GetConnections(port).ToList();
            foreach (IConnection connection in connections)
            {
                RemoveConnection(connection);
            }
        }

        public virtual IEnumerable<IConnection> GetConnections(INode node)
        {
            foreach (IConnection connection in Connections)
            {
                if (connection.Source.Node == node || connection.Target.Node == node)
                {
                    yield return connection;
                }
            }
        }

        public virtual IEnumerable<IConnection> GetConnections(IPort port)
        {
            foreach (IConnection connection in Connections)
            {
                if (connection.Source == port || connection.Target == port)
                {
                    yield return connection;
                }
            }
        }

        #endregion
        
        public BaseNodeWrangler()
        {
            PendingConnection = new BasePendingConnection(this);
            RemoveConnectionsCommand = new DelegateCommand<IPort>(ClearConnections);
        }
    }
}