using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.ExampleTypes;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.Hubs
{
    /// <summary>
    /// This is a more advanced demonstration, for a simple demonstration <see cref="CompareBoolEntityData"/>
    /// This demonstrates creating events and properties based off of the property grid
    /// </summary>
    public class IntHubEntityData : FloatHubEntityData
    {
        public override string Name { get; set; } = "Int Hub";
        
        public override string ObjectType { get; set; } = "IntHubEntityData";
    }
}