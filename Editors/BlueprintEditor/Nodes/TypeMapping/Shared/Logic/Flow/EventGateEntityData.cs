using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Flow
{
    public class EventGateNode : EntityNode
    {
        public override string ObjectType => "EventGateEntityData";

        public override void OnCreation()
        {
            base.OnCreation();
            
            AddInput("In", ConnectionType.Event);
            AddInput("Open", ConnectionType.Event);
            AddInput("Close", ConnectionType.Event);

            AddOutput("Out", ConnectionType.Event);

            if ((bool)TryGetProperty("Default"))
            {
                AddFooter("Default: Open");
            }
            else
            {
                AddFooter("Default: Closed");
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            if ((bool)TryGetProperty("Default"))
            {
                AddFooter("Default: Open");
            }
            else
            {
                RemoveFooter("Default: Closed");
            }
        }
    }
}