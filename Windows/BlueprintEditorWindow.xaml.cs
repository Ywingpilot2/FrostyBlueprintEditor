using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlueprintEditor.Models;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Editor;
using BlueprintEditor.Models.Types;
using BlueprintEditor.Models.Types.EbxLoaderTypes;
using BlueprintEditor.Models.Types.NodeTypes;
using BlueprintEditor.Utils;
using Frosty.Controls;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using App = Frosty.Core.App;

namespace BlueprintEditor.Windows
{
    public partial class BlueprintEditorWindow : FrostyWindow
    {
        private readonly EditorViewModel _editor;
        private readonly EbxBaseLoader _loader;
        
        private readonly Random _rng = new Random();
        private readonly EbxAssetEntry _file;

        private Type _selectedType;
        private List<Type> _types = new List<Type>();

        public BlueprintEditorWindow()
        {
            InitializeComponent();
            _editor = EditorUtils.CurrentEditor;
            
            Owner = Application.Current.MainWindow;
            Title = $"Ebx Graph({App.EditorWindow.GetOpenedAssetEntry().Filename})";
            _file = App.EditorWindow.GetOpenedAssetEntry() as EbxAssetEntry;

            EbxBaseLoader loader = new EbxBaseLoader();
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(EbxBaseLoader)))
                {
                    var extension = (EbxBaseLoader)Activator.CreateInstance(type);
                    if (extension.AssetType != App.EditorWindow.GetOpenedAssetEntry().Type) continue;
                    loader = extension;
                    break;
                }
            }

            _loader = loader;
            _loader.PopulateTypesList(_types);
            ClassSelector.Types = _types;

            _editor.PropertyGrid = new BlueprintPropertyGrid() { NodeEditor = _editor };
            FrostyTabItem ti = new FrostyTabItem()
            {
                Header = "Property Grid",
                Content = _editor.PropertyGrid
            };
            TabControl.Items.Add(ti);

            //Setup UI methods
            Editor.KeyDown += Editor_ControlInput;
            Editor.KeyUp += Editor_ControlInput;
            _editor.SelectedNodes.CollectionChanged += UpdatePropertyGrid;
        }

        #region Editor

        /// <summary>
        /// Initiates the editor by populating the graph with nodes and connections
        /// TODO: Add in FrostyTaskWindow(with owner set to <see cref="BlueprintWindow"/>) so frosty doesn't just freeze
        /// </summary>
        public void Initiate()
        {
            var openedAsset = (EbxAssetEntry)App.EditorWindow.GetOpenedAssetEntry();
            
            EbxAsset openedEbx = App.AssetManager.GetEbx(openedAsset);
            dynamic openedProperties = openedEbx.RootObject as dynamic;

            _loader.NodeEditor = _editor;
            
            _loader.PopulateNodes(openedProperties);
            _loader.CreateConnections(openedProperties);

            EditorUtils.ApplyLayouts(_file);
        }
        

        private void Editor_ControlInput(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                while (_editor.SelectedNodes.Count != 0)
                {
                    _editor.DeleteNode(_editor.SelectedNodes[0]);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (Keyboard.IsKeyDown(Key.D))
                {
                    if (_editor.SelectedNodes.Count != 0)
                    {
                        List<NodeBaseModel> nodesToDupe = new List<NodeBaseModel>(_editor.SelectedNodes);
                        _editor.SelectedNodes.Clear();
                        while (nodesToDupe.Count != 0)
                        {
                            var nodeToDupe = nodesToDupe[0];
                            if (nodeToDupe.ObjectType != "EditorInterfaceNode")
                            {
                                object dupedObj = _editor.CreateNodeObject(nodeToDupe.Object);
                                NodeBaseModel dupe = _editor.CreateNodeFromObject(dupedObj);
                                dupe.Location = new Point(nodeToDupe.Location.X + 10, nodeToDupe.Location.Y + 10);
                                _editor.SelectedNodes.Add(dupe);
                            }
                            else
                            {
                                App.Logger.LogError("Cannot dupe Interface nodes!");
                            }

                            nodesToDupe.Remove(nodeToDupe);
                        }
                        App.AssetManager.ModifyEbx(_file.Name, _editor.EditedEbxAsset);
                    }
                }
            }
        }

        private void BlueprintEditorWindow_OnClosing(object sender, CancelEventArgs e)
        {
            App.EditorWindow.OpenAsset(_file);
            EditorUtils.SaveLayouts(_file);

            EditorUtils.Editors.Remove(_file.Filename);
        }
        
        private void BlueprintEditorWindow_OnGotFocus(object sender, RoutedEventArgs e)
        {
            App.EditorWindow.OpenAsset(_file);
        }

        #endregion
        
        #region Buttons

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            while (_editor.SelectedNodes.Count != 0)
            {
                _editor.DeleteNode(_editor.SelectedNodes[0]);
            }
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedType == null) return;

            object obj = _editor.CreateNodeObject(_selectedType);
            
            _editor.CreateNodeFromObject(obj);
            
            App.AssetManager.ModifyEbx(_file.Name, _editor.EditedEbxAsset);
            App.EditorWindow.DataExplorer.RefreshItems();
        }
        
        /// <summary>
        /// This is all I was able to think of for auto organization for now
        /// its not great, and it breaks easily
        /// so the button is hidden for now.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrganizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditorUtils.ApplyAutoLayout();
        }

        #endregion
        
        #region Toolbox

        private void TypesList_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            _selectedType = ClassSelector.SelectedClass;
            if (_selectedType != null)
            {
                DocBoxName.Text = _selectedType.Name;
                if (NodeUtils.NodeExtensions.ContainsKey(_selectedType.Name))
                {
                    DocBoxText.Text = NodeUtils.NodeExtensions[_selectedType.Name].Documentation;
                }
                else if (NodeUtils.NmcExtensions.ContainsKey(_selectedType.Name) && NodeUtils.NmcExtensions[_selectedType.Name].Any(x => x.StartsWith("Documentation")))
                {
                    DocBoxText.Text = NodeUtils.NmcExtensions[_selectedType.Name].Find(x => x.StartsWith("Documentation")).Split('=')[1];
                }
            }
        }
        
        private void ToolboxVisible_OnClick(object sender, RoutedEventArgs e)
        {
            if (Toolbox.Visibility != Visibility.Collapsed)
            {
                Toolbox.Visibility = Visibility.Collapsed;
                ToolboxCollum.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                Toolbox.Visibility = Visibility.Visible;
                ToolboxCollum.Width = new GridLength(140, GridUnitType.Pixel);
            }
        }

        private void ToolboxClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            AddButton_OnClick(this, new RoutedEventArgs());
        }

        #endregion

        #region Property Grid

        private void UpdatePropertyGrid(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_editor.SelectedNodes.Count == 0)
            {
                _editor.PropertyGrid.Object = null;
                return;
            }
            
            _editor.PropertyGrid.Object = _editor.SelectedNodes.First().Object;
        }
        
        private void PropertyGridVisible_OnClick(object sender, RoutedEventArgs e)
        {
            if (PropertyGrid.Visibility != Visibility.Collapsed)
            {
                PropertyGrid.Visibility = Visibility.Collapsed;
                PropertyGridCollum.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                PropertyGrid.Visibility = Visibility.Visible;
                PropertyGridCollum.Width = new GridLength(180, GridUnitType.Pixel);
            }
        }

        #endregion
    }
}