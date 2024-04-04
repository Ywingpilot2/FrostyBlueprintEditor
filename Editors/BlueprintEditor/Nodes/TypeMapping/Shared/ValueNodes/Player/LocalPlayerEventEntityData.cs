using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Player
{
    public class LocalPlayerEventEntityData : EntityNode
    {
        public override string ObjectType => "LocalPlayerEventEntityData";

        public override string ToolTip => "This turns a normal event into a Player Event using the local player(current client).\nIf AllLocalPlayers is true then this sends an event for each player in the match";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("Out", ConnectionType.Event, Realm, true);

            if ((bool)TryGetProperty("AllLocalPlayers"))
            {
                AddFooter("AllLocalPlayers: True");
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            ClearFooter();
            if ((bool)TryGetProperty("AllLocalPlayers"))
            {
                AddFooter("AllLocalPlayers: True");
            }
        }
    }
}