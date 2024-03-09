using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.NodeTest.Nodes
{
    public class TestNode : BaseNode
    {
        public override string Header => "test";

        public override ObservableCollection<BaseInput> Inputs { get; }
        public override ObservableCollection<BaseOutput> Outputs { get; }

        public TestNode()
        {
            Inputs = new ObservableCollection<BaseInput>()
            {
                new BaseInput("testi", this)
            };

            Outputs = new ObservableCollection<BaseOutput>()
            {
                new BaseOutput("testo", this)
            };
        }
    }
}