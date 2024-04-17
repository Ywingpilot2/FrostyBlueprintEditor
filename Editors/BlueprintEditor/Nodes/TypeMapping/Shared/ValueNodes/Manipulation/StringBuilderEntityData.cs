using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes.Manipulation
{
    public class StringBuilderEntityData : EntityNode
    {
        public override string ObjectType => "StringBuilderEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            AddOutput("Output", ConnectionType.Property, Realm);
            
        }
    }
}