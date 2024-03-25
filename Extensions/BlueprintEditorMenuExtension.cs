using System.Windows.Media;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Windows;

namespace BlueprintEditorPlugin.Extensions
{
    public class ViewBlueprintContextMenuItem : DataExplorerContextMenuExtension
    {
        public static readonly ImageSource IconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/BlueprintEdit.png") as ImageSource;
        
        public override string ContextItemName => "Open as Graph...";
        public override ImageSource Icon => IconImageSource;

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            if (App.SelectedAsset != null)
            {
                BlueprintEditor editor = new BlueprintEditor();
                App.EditorWindow.OpenEditor($"{App.SelectedAsset.Filename} (Ebx Graph)", editor);
                
                editor.Loaded += (sender, args) =>
                {
                    editor.LoadBlueprint(App.SelectedAsset);
                };
            }
            else
            {
                App.Logger.LogError("Please open a blueprint(an asset with Property, Link, and Event connections, as well as Objects).");
            }
        });
    }
}