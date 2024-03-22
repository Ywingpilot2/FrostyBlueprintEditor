using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms.LayeredGraph
{
    /// <summary>
    /// FAKE! NOT REAL!
    /// These nodes act as "buffers" between nodes which need to be on different layers.
    /// We could just always put nodes on the layer we find, but it looks prettier to do this sometimes
    /// </summary>
    public class BufferNode : INode
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Point Location { get; set; }
        public Size Size { get; set; }
        public bool IsSelected { get; set; }
        public INodeWrangler NodeWrangler { get; }
        public bool IsValid()
        {
            return true;
        }

        public void OnCreation()
        {
            throw new System.NotImplementedException();
        }

        public void OnDestruction()
        {
            throw new System.NotImplementedException();
        }

        public string Header { get; set; }
        public ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();

        public void OnInputUpdated(IPort port)
        {
            throw new System.NotImplementedException();
        }

        public void OnOutputUpdated(IPort port)
        {
            throw new System.NotImplementedException();
        }

        public void AddPort(IPort port)
        {
            throw new System.NotImplementedException();
        }

        public BufferNode()
        {
            Inputs.Add(new BaseInput("i", this));
            Outputs.Add(new BaseOutput("o", this));
        }
    }
}