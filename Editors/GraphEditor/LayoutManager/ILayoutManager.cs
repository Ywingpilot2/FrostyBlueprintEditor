using BlueprintEditorPlugin.Editors.NodeWrangler;

namespace BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager
{
    /// <summary>
    /// Base implementation for a Layout manager
    /// </summary>
    public interface ILayoutManager
    {
        INodeWrangler NodeWrangler { get; set; }

        /// <summary>
        /// Automatically sorts the Graph
        /// </summary>
        void SortLayout();

        bool LayoutExists(string path);
        bool LoadLayout(string path);
        bool SaveLayout(string path);
    }
}