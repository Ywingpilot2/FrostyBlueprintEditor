using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Editors.NodeWrangler.LayoutManager;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Editors.GraphEditor
{
    public interface IGraphEditor
    {
        INodeWrangler NodeWrangler { get; set; }
        ILayoutManager LayoutManager { get; set; }

        /// <summary>
        /// This method is used to determine if a GraphEditor is valid
        /// </summary>
        /// <returns>True if it is valid, false if it is not.</returns>
        bool IsValid();
        
        /// <summary>
        /// This method is used to determine if a GraphEditor is valid for editing an Ebx Asset
        /// </summary>
        /// <param name="assetEntry"></param>
        /// <returns>True if it is valid, false if it is not.</returns>
        bool IsValid(EbxAssetEntry assetEntry);

        /// <summary>
        /// This method is used to determine if a GraphEditor is valid, using a variety of arguments to decide.
        /// </summary>
        /// <param name="objs"></param>
        /// <returns>True if it is valid, false if it is not.</returns>
        bool IsValid(params object[] args);
    }
}