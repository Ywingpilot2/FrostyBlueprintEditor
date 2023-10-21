using System.Collections.ObjectModel;
using BlueprintEditor.Models.Connections;

namespace BlueprintEditor.Models.Types.NodeTypes.Shared.ScalableEmitterDocument
{
    public class EmitterTemplateData : NodeBaseModel
    {
        public override string Name { get; set; } = "Emitter Core";
        public override string ObjectType { get; } = "EmitterTemplateData";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>();

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "RootProcessor", Type = ConnectionType.Link}
            };
    }
}