using System.Windows;
using Frosty.Controls;
using FrostySdk.Managers;
using App = Frosty.Core.App;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintEditorWindow : FrostyWindow
    {
        public BlueprintEditorWindow()
        {
            //This happens before InitializeComponent() explicitly because it needs to be set as early as possible, and InitializeComponent is slow.
            var file = App.EditorWindow.GetOpenedAssetEntry() as EbxAssetEntry;
            App.SelectedAsset = file;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = $"Ebx Graph({file.Filename})";
            GraphEditor.File = file;
            Closing += GraphEditor.BlueprintEditorWindow_OnClosing;
        }
    }
}