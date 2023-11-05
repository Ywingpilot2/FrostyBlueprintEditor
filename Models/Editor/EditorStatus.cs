namespace BlueprintEditorPlugin.Models.Editor
{
    public enum EditorStatus
    {
        Good = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// Args for updating the EditorStatus
    /// </summary>
    public class EditorStatusArgs
    {
        public EditorStatus Status;
        public string Tooltip;
        public int Identifier;

        public EditorStatusArgs(EditorStatus status, int id, string tooltip = null)
        {
            Status = status;
            Tooltip = tooltip;
            Identifier = id;
        }

        public EditorStatusArgs(int id, EditorStatus status)
        {
            Identifier = id;
            Status = status;
        }
    }
}