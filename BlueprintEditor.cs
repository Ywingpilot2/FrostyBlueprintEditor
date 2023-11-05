using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Extensions;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin
{
    [TemplatePart (Name = GraphEditor, Type = typeof(BlueprintGraphEditor))]
    public class BlueprintEditor : FrostyBaseEditor
    {
        private const string GraphEditor = "GraphEditor";
        private BlueprintGraphEditor _graphEditor;
        public override ImageSource Icon { get; } = ViewBlueprintMenuExtension.iconImageSource;

        static BlueprintEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BlueprintEditor), new FrameworkPropertyMetadata(typeof(BlueprintEditor)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            _graphEditor = GetTemplateChild(GraphEditor) as BlueprintGraphEditor;
            _graphEditor.File = App.SelectedAsset;
            GotMouseCapture += _graphEditor.BlueprintEditorWindow_OnGotFocus;
        }

        public override void Closed()
        {
            _graphEditor.BlueprintEditorWindow_OnClosing(this, new CancelEventArgs());
        }
    }
}