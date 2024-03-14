namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    public enum Realm
    {
        Invalid = 0,
        ClientAndServer = 1,
        Client = 2, 
        Server = 3,
        NetworkedClient = 4,
        NetworkedClientAndServer = 5,
        //Any = -1
    }

    public enum PropertyType
    {
        Default = 0,
        Interface = 1,
        Exposed = 2, //TODO: Figure out when Exposed is used
        Invalid = 3
    }
}