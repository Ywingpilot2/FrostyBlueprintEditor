using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ScalableEmitterDocument
{
    public class EmitterTemplateData : EntityNode
    {
        public override string Name { get; set; } = "Emitter Core";
        public override string ObjectType { get; set; } = "EmitterTemplateData";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>();

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "RootProcessor", Type = ConnectionType.Link}
            };
    }
}