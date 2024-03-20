using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;

namespace BlueprintEditorPlugin.Editors.NodeWrangler.LayoutManager
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

        bool LoadLayout(string path);
        bool SaveLayout(string path);
    }
}