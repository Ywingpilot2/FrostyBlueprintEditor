using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Utils;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared
{
    public class BoolEntityData : EntityNode
    {
        public override string Name { get; set; } = "Bool";
        public override string Documentation { get; } = "A container of a true/false statement";
        public override string ObjectType { get; set; } = "BoolEntityData";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "Value", Type = ConnectionType.Property},
                new InputViewModel() {Title = "SetTrue", Type = ConnectionType.Event},
                new InputViewModel() {Title = "SetFalse", Type = ConnectionType.Event}
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "Value", Type = ConnectionType.Property}
            };

        public override void OnCreation()
        {
            foreach (InputViewModel input in Inputs)
            {
                NodeUtils.PortRealmFromObject(Object, input);
            }

            foreach (OutputViewModel output in Outputs)
            {
                NodeUtils.PortRealmFromObject(Object, output);
            }
        }

        public override void OnModified(ItemModifiedEventArgs args)
        {
            NotifyPropertyChanged(Name);
            OnCreation();
        }
    }
}