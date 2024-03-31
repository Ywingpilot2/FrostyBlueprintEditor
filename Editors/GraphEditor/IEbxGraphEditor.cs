using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Editors.GraphEditor
{
    public interface IEbxGraphEditor : IGraphEditor
    {
        void LoadAsset(EbxAssetEntry assetEntry);
    }
}