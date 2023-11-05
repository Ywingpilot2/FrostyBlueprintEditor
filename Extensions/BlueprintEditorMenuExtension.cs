using System.Windows.Media;
using BlueprintEditorPlugin.Utils;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;

namespace BlueprintEditorPlugin.Extensions
{
    public class ViewBlueprintMenuExtension : MenuExtension
    {
        public static ImageSource iconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/BlueprintEdit.png") as ImageSource;

        public override string TopLevelMenuName => "View";
        public override string SubLevelMenuName => null;

        public override string MenuItemName => "Blueprint Editor";
        public override ImageSource Icon => iconImageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
        {
            if (App.EditorWindow.GetOpenedAssetEntry() != null && !EditorUtils.Editors.ContainsKey(App.EditorWindow.GetOpenedAssetEntry().Filename))
            {
                BlueprintEditorWindow blueprintEditor = new BlueprintEditorWindow();
                blueprintEditor.Show();

            }
            else if (App.EditorWindow.GetOpenedAssetEntry() == null)
            {
                App.Logger.LogError("Please open a blueprint(an asset with Property, Link, and Event connections, as well as Objects).");
            }
            else if (EditorUtils.Editors.ContainsKey(App.EditorWindow.GetOpenedAssetEntry().Filename))
            {
                App.Logger.LogError("This editor is already open.");
            }
        });
    }

    public class ViewBlueprintContextMenuItem : DataExplorerContextMenuExtension
    {
        public override string ContextItemName => "Open as Graph...";
        public override ImageSource Icon => ViewBlueprintMenuExtension.iconImageSource;

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            if (App.SelectedAsset != null && !EditorUtils.Editors.ContainsKey(App.SelectedAsset.Filename))
            {
                App.EditorWindow.OpenEditor($"Ebx Graph({App.SelectedAsset.Filename})", new BlueprintEditor());
            }
            else if (App.SelectedAsset == null)
            {
                App.Logger.LogError("Please open a blueprint(an asset with Property, Link, and Event connections, as well as Objects).");
            }
            else if (EditorUtils.Editors.ContainsKey(App.SelectedAsset.Filename))
            {
                App.Logger.LogError("This editor is already open.");
            }
        });
    }
}