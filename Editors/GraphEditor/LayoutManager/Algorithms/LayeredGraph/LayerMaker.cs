using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph
{
    public class LayerMaker : DepthFirstSearch
    {
        private List<IVertex> _vertices;
        private Dictionary<int, NodeLayer> _layers;

        private int _lvl = -1;

        protected override void RemoveNode(INode node)
        {
            base.RemoveNode(node);
            _vertices.Remove(node);
        }

        /// <summary>
        /// This creates a list of <see cref="NodeLayer"/>s from the list of nodes and connections.
        /// </summary>
        /// <returns></returns>
        public List<NodeLayer> CreateLayers()
        {
            while (_vertices.Count != 0)
            {
                _lvl++;
            
                NodeLayer layer = new NodeLayer(new List<INode>());
                foreach (IVertex vertex in _vertices)
                {
                    // First we get only the Source nodes, as the first layer should only be sources
                    if (vertex is INode node)
                    {
                        if (GetConnections(node, PortDirection.In).Any())
                            continue;
                    
                        layer.Nodes.Add(node);
                    }
                }
                _layers.Add(_lvl, layer);
            
                foreach (INode node in _layers[_lvl].Nodes)
                {
                    RemoveNode(node);
                }
            }

            return _layers.Values.ToList();
        }

        public LayerMaker(List<IVertex> vertices, List<IConnection> connections) : base(connections)
        {
            _vertices = new List<IVertex>(vertices);
            Connections = new List<IConnection>(connections);
            
            _layers = new Dictionary<int, NodeLayer>();
        }
    }

    public struct NodeLayer
    {
        public List<INode> Nodes { get; }

        public double GetHeight()
        {
            return Nodes.Max(node => node.Size.Height);
        }
        
        /// <summary>
        /// Gets the width of this layer.
        /// </summary>
        /// <returns>The height of the highest node in this layer</returns>
        public double GetWidth()
        {
            return Nodes.Max(node => node.Size.Width);
        }
        
        public NodeLayer(List<INode> nodes)
        {
            Nodes = nodes;
        }

        public NodeLayer(INode node)
        {
            Nodes = new List<INode> { node };
        }
    }
}