using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.ValueNodes
{
    public class BoolNode : EntityNode
    {
        public override string ObjectType => "BoolEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            if ((bool)TryGetProperty("DefaultValue"))
            {
                Footer = "Defaults to true";
            }
            else
            {
                Footer = "Defaults to false";
            }

            AddInput("SetTrue", ConnectionType.Event, Realm);
            AddInput("SetFalse", ConnectionType.Event, Realm);

            AddOutput("Value", ConnectionType.Property, Realm);
        }
    }
}