using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Entities.Networking;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Synced
{
    public abstract class SyncedNode : EntityNode
    {
        public override string ToolTip => "This node syncs a value between client and server.";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Property, Realm.Client);
            AddOutput("Out", ConnectionType.Property, Realm.Server);
        }
    }
}