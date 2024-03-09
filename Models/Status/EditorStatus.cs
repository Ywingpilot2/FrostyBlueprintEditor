namespace BlueprintEditorPlugin.Models.Status
{
    public enum EditorStatus
    {
        Alright = 0,
        Flawed = 1,
        Broken = 2
    }

    public struct EditorStatusArgs
    {
        public EditorStatus Status { get; set; }
        public string ToolTip { get; set; }

        public EditorStatusArgs(EditorStatus status, string tooltip = "")
        {
            Status = status;
            ToolTip = tooltip;
        }
    }
}