using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Utils;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared
{
    public class EventSwitchEntityData : EntityNode
    {
        public override string Name { get; set; } = "Switch(Event)";
        public override string Documentation { get; } = "A switch which changes what event it outputs depending on the current OutEvent";
        public override string ObjectType { get; set; } = "EventSwitchEntityData";

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
            for (int i = 0; i != (int)((dynamic)Object).OutEvents; i++)
            {
                Outputs.Add(new OutputViewModel() {Title = $"Out{i}", Type = ConnectionType.Event});
            }
            
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
            if (args.Item.Name == "__Id")
            {
                NotifyPropertyChanged(nameof(Name));
                return;
            }
            
            if ((int)((dynamic)Object).OutEvents == 0)
            {
                Outputs.Clear();
                return;
            }
            
            //If the InputCount is a larger number that means we are adding
            if ((int)((dynamic)Object).OutEvents > Outputs.Count)
            {
                for (int i = Outputs.Count; i != (int)((dynamic)Object).OutEvents; i++)
                {
                    Outputs.Add(new OutputViewModel() {Title = $"Out{i}", Type = ConnectionType.Event});
                }
            }
            else //This means the list must be smaller
            {
                for (int i = (int)((dynamic)Object).OutEvents; i != Outputs.Count; i++)
                {
                    OutputViewModel output = Outputs[i];
                    foreach (ConnectionViewModel connection in EditorUtils.CurrentEditor.GetConnections(output))
                    {
                        EditorUtils.CurrentEditor.Disconnect(connection);
                    }

                    Outputs.Remove(output);
                }
            }
            
            //We want to make sure our Inputs and Outputs are the same realm as us, that way our flags compute properly
            foreach (InputViewModel input in Inputs)
            {
                NodeUtils.PortRealmFromObject(Object, input);
            }

            foreach (OutputViewModel output in Outputs)
            {
                NodeUtils.PortRealmFromObject(Object, output);
            }
        }
    }
}