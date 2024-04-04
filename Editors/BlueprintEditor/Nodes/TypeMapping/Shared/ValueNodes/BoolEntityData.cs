using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes
{
    public class BoolNode : EntityNode
    {
        public override string ObjectType => "BoolEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            if ((bool)TryGetProperty("DefaultValue"))
            {
                Footer = "Default: True";
            }
            else
            {
                Footer = "Default: False";
            }

            AddInput("SetTrue", ConnectionType.Event, Realm);
            AddInput("SetFalse", ConnectionType.Event, Realm);

            AddOutput("Value", ConnectionType.Property, Realm);
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            if ((bool)TryGetProperty("DefaultValue"))
            {
                Footer = "Default: True";
            }
            else
            {
                Footer = "Default: False";
            }
        }
    }
}