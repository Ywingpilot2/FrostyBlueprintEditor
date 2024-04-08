using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler
{
    public interface IEbxNodeWrangler
    {
        EbxAsset Asset { get; set; }

        void AddVertexTransient(IVertex vertex);
        void AddConnectionTransient(IConnection connection);
    }
}