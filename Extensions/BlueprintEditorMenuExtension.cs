using System;
using System.Windows.Media;
using BlueprintEditor.Utils;
using BlueprintEditor.Windows;
using Frosty.Core;

namespace BlueprintEditor.Extensions
{
    public class ViewBlueprintMenuExtension : MenuExtension
    {
        public static ImageSource iconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditor;component/Images/BlueprintEdit.png") as ImageSource;

        public override string TopLevelMenuName => "View";
        public override string SubLevelMenuName => null;

        public override string MenuItemName => "Blueprint Editor";
        public override ImageSource Icon => iconImageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
        {
            if (App.EditorWindow.GetOpenedAssetEntry() != null && EditorUtils.Editor == null)
            {
                BlueprintEditorWindow blueprintEditor = new BlueprintEditorWindow();
                blueprintEditor.Show();
                blueprintEditor.Initiate();

            }
            else if (App.EditorWindow.GetOpenedAssetEntry() == null)
            {
                App.Logger.LogError("Please open a blueprint(an asset with Property, Link, and Event connections, as well as Objects).");
            }
            else if (EditorUtils.Editor != null)
            {
                App.Logger.LogWarning("There can only be one blueprint editor open at a time.");
            }
        });
    }
}