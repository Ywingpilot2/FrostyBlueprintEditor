using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Editors.GraphEditor
{
    public interface IGraphEditor : IStatusItem
    {
        INodeWrangler NodeWrangler { get; set; }
    }
}