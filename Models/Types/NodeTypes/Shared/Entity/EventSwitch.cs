using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Shared.Entity
{
    public class EventSwitchEntityData : NodeBaseModel
    {
        public override string Name { get; set; } = "Switch(Event)";
        public override string ObjectType { get; } = "EventSwitchEntityData";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "In", Type = ConnectionType.Event},
                new InputViewModel() {Title = "NextOut", Type = ConnectionType.Event},
                new InputViewModel() {Title = "Reset", Type = ConnectionType.Event}
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } = new ObservableCollection<OutputViewModel>();

        public override void OnCreation()
        {
            for (int i = 0; i != (int)Object.OutEvents; i++)
            {
                Outputs.Add(new OutputViewModel() {Title = $"Out{i}", Type = ConnectionType.Event});
            }
        }
        
        public override void OnModified() => OnCreation();
    }
}