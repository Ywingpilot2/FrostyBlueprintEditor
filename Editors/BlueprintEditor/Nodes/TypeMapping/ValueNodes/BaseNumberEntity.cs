using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.ValueNodes
{
    public abstract class BaseNumberNode : EntityNode
    {
        public override string ToolTip => "This node stores a number value which can be incremented and decremented";

        public override void OnCreation()
        {
            base.OnCreation();

            Footer = $"Default Value: {TryGetProperty("DefaultValue")}\nAdjustment Value: {TryGetProperty("IncDecValue")}";
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            Footer = $"Default Value: {TryGetProperty("DefaultValue")}\nAdjustment Value: {TryGetProperty("IncDecValue")}";
        }

        public BaseNumberNode()
        {
            Inputs = new ObservableCollection<IPort>
            {
                new EventInput("Increment", this),
                new EventInput("Decrement", this),
            };

            Outputs = new ObservableCollection<IPort>
            {
                new PropertyOutput("Value", this)
            };
        }
    }
}