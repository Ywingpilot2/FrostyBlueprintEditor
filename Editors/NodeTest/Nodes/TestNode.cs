using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.NodeTest.Nodes
{
    public class TestNode : BaseNode
    {
        public override string Header => "test";

        public override ObservableCollection<IPort> Inputs { get; }
        public override ObservableCollection<IPort> Outputs { get; }

        public TestNode()
        {
            Inputs = new ObservableCollection<IPort>()
            {
                new BaseInput("testi", this)
            };

            Outputs = new ObservableCollection<IPort>()
            {
                new BaseOutput("testo", this)
            };
        }
    }
}