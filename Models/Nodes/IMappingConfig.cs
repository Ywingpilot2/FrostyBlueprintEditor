namespace BlueprintEditorPlugin.Models.Nodes
{
    public interface IMappingConfig
    {
        string[] Args { get; set; }

        void Load(string path);
    }
}