using System.Collections.Generic;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.Sugiyama;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.CheapGraph
{
    /// <summary>
    /// 
    /// </summary>
    public class CheapMethod : IGraphAlgorithm
    {
        private List<IVertex> _assignedVerts = new List<IVertex>();
        private INodeWrangler _nodeWrangler;

        private double _current = 0.0;
        private int _currentRow = 0;
        private double _currentY = 0.0;
        private int _idx = 0;
        public void SortGraph()
        {
            for (; _idx < _nodeWrangler.Nodes.Count; _idx++)
            {
                IVertex vertex = _nodeWrangler.Nodes[_idx];
                
                if (_assignedVerts.Contains(vertex))
                    continue;
                
                _assignedVerts.Add(vertex);
                vertex.Location = new Point(_current + vertex.Size.Width, _currentY + vertex.Size.Height);

                _current += EditorOptions.VertXSpacing + vertex.Size.Width;
                if (_idx - 25 >= _currentRow)
                {
                    _currentY += EditorOptions.VertYSpacing * 4 + vertex.Size.Height;
                    _currentRow += 1;
                }
            }
        }

        public void SortGraph(IVertex vertex)
        {
            if (_assignedVerts.Contains(vertex))
                return;

            _idx++;
                
            _assignedVerts.Add(vertex);
            vertex.Location = new Point(_current + vertex.Size.Width, _currentY + vertex.Size.Height);

            _current += EditorOptions.VertXSpacing + vertex.Size.Width;
            if (_idx - 25 >= _currentRow)
            {
                _currentY += EditorOptions.VertYSpacing * 4 + vertex.Size.Height;
                _currentRow += 1;
            }
        }

        public CheapMethod(INodeWrangler wrangler)
        {
            _nodeWrangler = wrangler;
        }
    }
}