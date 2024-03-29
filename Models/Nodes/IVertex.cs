using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;

namespace BlueprintEditorPlugin.Models.Nodes
{
    /// <summary>
    /// Base implementation of an object which can be selected and moved in a graph editor.
    /// </summary>
    public interface IVertex : INotifyPropertyChanged
    {
        Point Location { get; set; }
        Size Size { get; set; }
        bool IsSelected { get; set; }
        INodeWrangler NodeWrangler { get; }

        bool IsValid();
        void OnCreation();
        void OnDestruction();
    }
}