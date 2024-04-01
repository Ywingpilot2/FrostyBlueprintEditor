using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes;

namespace BlueprintEditorPlugin.Models.Entities
{
    /// <summary>
    /// Base implementation for a node tied to an entity object
    /// </summary>
    public interface IObjectNode : INode, IEntityObject
    {
        EntityInput GetInput(string name, ConnectionType type);
        EntityOutput GetOutput(string name, ConnectionType type);

        void AddInput(EntityInput input);
        void AddOutput(EntityOutput output);
    }
}