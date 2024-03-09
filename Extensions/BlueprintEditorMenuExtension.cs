using System.Windows.Media;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;

namespace BlueprintEditorPlugin.Extensions
{
    public class ViewBlueprintContextMenuItem : DataExplorerContextMenuExtension
    {
        public static readonly ImageSource IconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/BlueprintEdit.png") as ImageSource;
        
        public override string ContextItemName => "Open as Graph...";
        public override ImageSource Icon => IconImageSource;

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            /*if (App.SelectedAsset != null && !EditorUtils.ActiveNodeEditors.ContainsKey(App.SelectedAsset.Filename))
            {
                App.EditorWindow.OpenEditor($"{App.SelectedAsset.Filename} (Ebx Graph)", new BlueprintEditor());
            }
            else if (App.SelectedAsset == null)
            {
                App.Logger.LogError("Please open a blueprint(an asset with Property, Link, and Event connections, as well as Objects).");
            }
            else if (EditorUtils.ActiveNodeEditors.ContainsKey(App.SelectedAsset.Filename))
            {
                App.Logger.LogError("This editor is already open.");
            }*/
        });
    }
    
    public class ViewTestGraph : MenuExtension
    {
        public static ImageSource iconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/BlueprintEdit.png") as ImageSource;

        public override string TopLevelMenuName => "View";
        public override string SubLevelMenuName => "Blueprint Editor Dev";

        public override string MenuItemName => "View test graph";
        public override ImageSource Icon => iconImageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
        {
            TestGraphWindow graphWindow = new TestGraphWindow();
            graphWindow.Show();
        });
    }
}