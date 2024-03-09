using System.ComponentModel;

namespace BlueprintEditorPlugin.Models.Status
{
    public interface IStatusItem : INotifyPropertyChanged
    {
        EditorStatusArgs CurrentStatus { get; set; }
        
        /// <summary>
        /// Double check what our current status is
        /// </summary>
        void CheckStatus();
        
        /// <summary>
        /// Update our status depending on our internals
        /// </summary>
        void UpdateStatus();
        
        /// <summary>
        /// Force set our status
        /// </summary>
        /// <param name="args"></param>
        void SetStatus(EditorStatusArgs args);
    }
}