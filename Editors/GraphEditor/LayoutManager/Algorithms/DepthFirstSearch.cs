using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms
{
    /// <summary>
    /// A simple algorithm for implementing DFS, a way of searching down all connection paths.
    /// </summary>
    public abstract class DepthFirstSearch
    {
        public List<INode> VisitedNodes = new List<INode>();
        protected List<IConnection> Connections;
        
        protected List<IConnection> GetConnections(INode node)
        {
            List<IConnection> connections = new List<IConnection>();
            foreach (IConnection connection in Connections)
            {
                if (connection.Source.Node == node)
                {
                    connections.Add(connection);
                }
            }

            return connections;
        }
        
        protected List<IConnection> GetConnections(INode node, PortDirection direction)
        {
            List<IConnection> connections = new List<IConnection>();
            foreach (IConnection connection in Connections)
            {
                if (connection.Target.Node == node && direction == PortDirection.In)
                {
                    connections.Add(connection);
                }

                if (connection.Source.Node == node && direction == PortDirection.Out)
                {
                    connections.Add(connection);
                }
            }

            return connections;
        }

        protected virtual void RemoveNode(INode node)
        {
            VisitedNodes.Remove(node);
            foreach (IConnection connection in GetConnections(node))
            {
                Connections.Remove(connection);
            }
        }
        
        public virtual void DepthSearch(INode node)
        {
            if (VisitedNodes.Contains(node))
            {
                return;
            }
            
            VisitedNodes.Add(node);
            foreach (IConnection connection in GetConnections(node))
            {
                DepthSearch(connection.Target.Node);
            }
        }

        public virtual void DepthSearch(IConnection start)
        {
            if (VisitedNodes.Contains(start.Target.Node))
            {
                return;
            }
            
            VisitedNodes.Add(start.Target.Node);
            foreach (IConnection connection in GetConnections(start.Target.Node))
            {
                DepthSearch(connection);
            }
        }

        public DepthFirstSearch(List<IConnection> connections)
        {
            Connections = connections;
        }
    }
}