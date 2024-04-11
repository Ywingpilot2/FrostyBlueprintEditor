using Frosty.Core;
using App = FrostyEditor.App;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Extensions
{
    public class TestMenuExtension : BlueprintMenuItemExtension
    {
        public override string DisplayName => "test";
        public override string SubLevelMenuName => "testSub";
        public override string ToolTip => "This is a test";

        public override RelayCommand ButtonClicked => new RelayCommand(o =>
        {
            App.Logger.Log("It worked!");
        });
    }
}