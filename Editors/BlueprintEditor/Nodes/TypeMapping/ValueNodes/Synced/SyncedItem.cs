using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.ValueNodes.Synced
{
    public abstract class SyncedItem : EntityNode
    {
        public override string ToolTip => "This node syncs a value between client and server.";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Property);
            AddOutput("Out", ConnectionType.Property, Realm.Server);
        }
    }
}