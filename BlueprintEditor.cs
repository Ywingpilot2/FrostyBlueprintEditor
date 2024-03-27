using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Extensions;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin
{
    [TemplatePart (Name = GraphEditor, Type = typeof(BlueprintGraphEditor))]
    public class BlueprintEditor : FrostyBaseEditor
    {
        private const string GraphEditor = "GraphEditor";
        private BlueprintGraphEditor _graphEditor;
        public override ImageSource Icon => ViewBlueprintContextMenuItem.IconImageSource;

        static BlueprintEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BlueprintEditor), new FrameworkPropertyMetadata(typeof(BlueprintEditor)));
        }
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _graphEditor = GetTemplateChild(GraphEditor) as BlueprintGraphEditor;
        }

        private bool _loaded = false;
        public void LoadBlueprint(EbxAssetEntry assetEntry)
        {
            // Stupid fuck why does this happen????
            if (_loaded == true)
                return;
            
            _loaded = true;
#if DEVELOPER___DEBUG
            _graphEditor.LoadAsset(assetEntry);
#else
            FrostyTaskWindow.Show("Loading blueprint...", "", task =>
            {
                _graphEditor.LoadAsset(assetEntry);
            });
#endif
        }

        public override void Closed()
        {
            base.Closed();
        }
    }
}