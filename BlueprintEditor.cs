using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Extensions;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin
{
    [TemplatePart (Name = ContentPresenter, Type = typeof(ContentPresenter))]
    public class BlueprintEditor : FrostyBaseEditor
    {
        private const string ContentPresenter = "ContentPresenter";
        private ContentPresenter _presenter;
        public override ImageSource Icon => ViewBlueprintContextMenuItem.IconImageSource;

        static BlueprintEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BlueprintEditor), new FrameworkPropertyMetadata(typeof(BlueprintEditor)));
        }
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _presenter = GetTemplateChild(ContentPresenter) as ContentPresenter;
        }

        private bool _loaded;
        public void LoadBlueprint(EbxAssetEntry assetEntry, IEbxGraphEditor graphEditor)
        {
            // Stupid fuck why does this happen????
            if (_loaded)
                return;

            _presenter.Content = graphEditor;

            _loaded = true;
#if DEVELOPER___DEBUG
            graphEditor.LoadAsset(assetEntry);
#else
            FrostyTaskWindow.Show("Loading Blueprint...", "", task =>
            {
                graphEditor.LoadAsset(assetEntry);
            });
#endif
        }

        public override void Closed()
        {
            base.Closed();
        }
    }
}