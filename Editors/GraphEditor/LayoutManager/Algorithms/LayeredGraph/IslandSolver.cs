using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph
{
    /// <summary>
    /// Gets an island from this node. Returns null if the island was already found
    /// </summary>
    public class IslandSolver : DepthFirstSearch
    {
        private VertexIsland _island;
        
        public override void DepthSearch(INode node)
        {
            if (VisitedNodes.Contains(node))
                return;
            
            VisitedNodes.Add(node);
            _island.Vertices.Add(node);
            
            foreach (IConnection connection in GetConnections(node))
            {
                if (connection.Target.Node != node)
                {
                    DepthSearch(connection.Target.Node);
                }
                else
                {
                    DepthSearch(connection.Source.Node);
                }
            }
        }

        public VertexIsland GetIsland(INode node)
        {
            // In another castle
            if (VisitedNodes.Contains(node))
                return null;
            
            _island = new VertexIsland();
            
            DepthSearch(node);

            return _island;
        }

        public IslandSolver(List<IConnection> connections) : base(connections)
        {
        }
    }

    public class VertexIsland
    {
        public List<IVertex> Vertices { get; }

        public VertexIsland()
        {
            Vertices = new List<IVertex>();
        }
    }
}