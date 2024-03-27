using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms
{
    /// <summary>
    /// Detects whether or not cycles exist in this graph
    /// </summary>
    public class CycleDetecter : DepthFirstSearch
    {
        private bool _hasCycles = false;

        public override void DepthSearch(IConnection start)
        {
            if (VisitedNodes.Contains(start.Target.Node))
            {
                _hasCycles = true;
            }
            base.DepthSearch(start);
        }

        public bool Detect(INode start)
        {
            VisitedNodes.Add(start);
            foreach (IConnection connection in GetConnections(start, PortDirection.Out))
            {
                DepthSearch(connection);
            }
            return _hasCycles;
        }
        
        public CycleDetecter(List<IConnection> connections) : base(connections)
        {
        }
    }
}