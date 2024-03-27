using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.ValueNodes.Synced
{
    public class SyncedBool : SyncedItem
    {
        public override string ObjectType => "SyncedBoolEntityData";
        public override string ToolTip => "This node syncs a boolean between client and server.";
    }
}