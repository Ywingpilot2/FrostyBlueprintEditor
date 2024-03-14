using System.Windows.Media;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;

namespace BlueprintEditorPlugin.Extensions
{
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
    
    public class ViewTestBlueprintGraph : MenuExtension
    {
        public static ImageSource iconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/BlueprintEdit.png") as ImageSource;

        public override string TopLevelMenuName => "View";
        public override string SubLevelMenuName => "Blueprint Editor Dev";

        public override string MenuItemName => "View test Blueprint graph";
        public override ImageSource Icon => iconImageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
        {
            BlueprintGraphTestWindow testWindow = new BlueprintGraphTestWindow();
            testWindow.Show();
        });
    }
}