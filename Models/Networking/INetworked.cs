using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;

namespace BlueprintEditorPlugin.Models.Networking
{
    public interface INetworked
    {
        Realm Realm { get; set; }
        Realm ParseRealm(object obj);
        void DetermineRealm();
    }
}