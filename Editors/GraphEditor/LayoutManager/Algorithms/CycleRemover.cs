using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms
{
    /// <summary>
    /// This class removes cycles found in a graph using Depth First search 
    /// </summary>
    public class CycleRemover : DepthFirstSearch
    {
        /// <summary>
        /// Remove all cycles for this branch
        /// </summary>
        /// <param name="start">The node to start at</param>
        public void RemoveCycles(INode start)
        {
            VisitedNodes.Add(start);
            foreach (IConnection connection in GetConnections(start))
            {
                DepthSearch(connection);
            }
        }

        public override void DepthSearch(IConnection start)
        {
            if (VisitedNodes.Contains(start.Target.Node))
            {
                Connections.Remove(start);
            }
            
            base.DepthSearch(start);
        }

        public CycleRemover(List<IConnection> connections) : base(connections)
        {
        }
    }
}