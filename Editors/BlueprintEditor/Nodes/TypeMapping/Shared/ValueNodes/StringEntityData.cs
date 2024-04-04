using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.ValueNodes
{
    public class StringEntityData : EntityNode
    {
        public override string ObjectType => "StringEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            Footer = $"Default string: {TryGetProperty("DefaultString")}";
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);
            
            Footer = $"Default string: {TryGetProperty("DefaultString")}";
        }
    }
}