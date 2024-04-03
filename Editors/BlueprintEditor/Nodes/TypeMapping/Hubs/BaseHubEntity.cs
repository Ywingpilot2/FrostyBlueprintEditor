using System;
using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostySdk;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Hubs
{
    /// <summary>
    /// Base implementation for a hub
    /// </summary>
    public abstract class BaseHubEntity : EntityNode
    {
        public override string ToolTip => "This node outputs the input at the SelectedIndex.";

        public override void OnCreation()
        {
            base.OnCreation();

            dynamic hashedInputs = TryGetProperty("HashedInput");
            foreach (UInt32 hashedInput in hashedInputs)
            {
                string unHash = Utils.GetString((int)hashedInput);

                AddInput(new PropertyInput(unHash, this) { Realm = Realm });
            }
        }

        public BaseHubEntity()
        {
            Inputs = new ObservableCollection<IPort>()
            {
                new PropertyInput("InputSelect", this)
            };

            Outputs = new ObservableCollection<IPort>
            {
                new PropertyOutput("Out", this)
            };
        }
    }
}