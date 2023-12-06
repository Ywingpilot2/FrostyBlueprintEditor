using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Utils;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared
{
    public class AndEntityData : EntityNode
    {
        public override string Name { get; set; } = "AND";

        public override string Documentation { get; } = "This outputs a boolean which is true when all of its inputs are true as well";
        public override string ObjectType { get; set; } = "AndEntityData";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } = new ObservableCollection<InputViewModel>();

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>
            {
                new OutputViewModel() { Title = "Out", Type = ConnectionType.Property }
            };

        public override void OnCreation()
        {
            if ((int)((dynamic)Object).InputCount == 0) return;
            
            for (int i = 1; i <= (int)((dynamic)Object).InputCount; i++)
            {
                Inputs.Add(new InputViewModel() {Title = $"In{i}", Type = ConnectionType.Property});
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
            
            if ((int)((dynamic)Object).InputCount == 0)
            {
                Inputs.Clear();
                return;
            }
            
            //If the InputCount is a larger number that means we are adding
            if ((int)((dynamic)Object).InputCount > Inputs.Count)
            {
                for (int i = Inputs.Count; i <= (int)((dynamic)Object).InputCount; i++)
                {
                    Inputs.Add(new InputViewModel() {Title = $"In{i}", Type = ConnectionType.Property});
                }
            }
            else //This means the list must be smaller
            {
                for (int i = (int)((dynamic)Object).InputCount; i != Inputs.Count; i++)
                {
                    InputViewModel input = Inputs[i];
                    foreach (ConnectionViewModel connection in EditorUtils.CurrentEditor.GetConnections(input))
                    {
                        EditorUtils.CurrentEditor.Disconnect(connection);
                    }

                    Inputs.Remove(input);
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