using System.ComponentModel;
using System.Windows;

namespace BlueprintEditorPlugin.Models.Nodes
{
    public interface IVertex : INotifyPropertyChanged
    {
        string Header { get; set; }
        
        Point Location { get; set; }
        double Width { get; }
        double Height { get; }
        
        bool IsSelected { get; set; }

        bool IsValid();
    }
}