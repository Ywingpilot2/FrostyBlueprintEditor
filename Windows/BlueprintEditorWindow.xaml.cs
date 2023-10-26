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
using System.Windows.Media;
using BlueprintEditor.Models;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Editor;
using BlueprintEditor.Models.Types;
using BlueprintEditor.Models.Types.EbxEditorTypes;
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

            _editor.NodePropertyGrid = new BlueprintPropertyGrid() { NodeEditor = _editor };
            FrostyTabItem ti = new FrostyTabItem()
            {
                Header = "Property Grid",
                Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Properties.png") as ImageSource,
                Content = _editor.NodePropertyGrid
            };
            TabControl.Items.Add(ti);

            //Setup UI methods
            Editor.KeyDown += Editor_ControlInput;
            Editor.KeyUp += Editor_ControlInput;
            _editor.SelectedNodes.CollectionChanged += UpdatePropertyGrid;
            
            //Mouse capture
            _editor.NodePropertyGrid.GotMouseCapture += BlueprintEditorWindow_OnGotFocus;
            _editor.NodePropertyGrid.GotKeyboardFocus += BlueprintEditorWindow_OnGotFocus;
            ClassSelector.GotMouseCapture += BlueprintEditorWindow_OnGotFocus;
            
            //Editor status
            _editor.EditorStatusChanged += SetEditorStatus;
            _editor.RemoveEditorStatus += RemoveEditorMessage;
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
            if (_loader.HasInterface)
            {
                _editor.InterfacePropertyGrid = new BlueprintPropertyGrid()
                {
                    Object = openedProperties.Interface.Internal, 
                    NodeEditor = _editor
                };
                FrostyTabItem ti = new FrostyTabItem()
                {
                    Header = "Interface",
                    Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Interface.png") as ImageSource,
                    Content = _editor.InterfacePropertyGrid
                };
                TabControl.Items.Add(ti);
            }

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

        #region Editor Status

        private Dictionary<int, string> _errorProblems = new Dictionary<int, string>();
        private Dictionary<int, string> _warningProblems = new Dictionary<int, string>();

        private void SetEditorStatus(object sender, EditorStatusArgs args)
        {
            EditorStatus status = args.Status;
            if (status == EditorStatus.Warning)
            {
                string message = (args.Tooltip ?? "A problem has been found with the blueprint");
                if (!_warningProblems.ContainsKey(args.Identifier))
                {
                    _warningProblems.Add(args.Identifier, message);
                }
            }
            else //Error
            {
                string message = (args.Tooltip ?? "A problem has been found with the blueprint");
                if (!_errorProblems.ContainsKey(args.Identifier))
                {
                    _errorProblems.Add(args.Identifier, message);
                }
            }

            string tooltip = "";
            for (var index = 0; index < _errorProblems.Count; index++)
            {
                var problem = _errorProblems.Values.ElementAt(index);
                tooltip += $"Error: {problem}\n";
            }
            
            for (var index = 0; index < _warningProblems.Count; index++)
            {
                var problem = _warningProblems.Values.ElementAt(index);
                tooltip += $"Warning: {problem}\n";
            }
            
            UpdateEditorStatus(tooltip);
        }

        private void RemoveEditorMessage(object sender, EditorStatusArgs args)
        {
            if (args.Status == EditorStatus.Warning)
            {
                _warningProblems.Remove(args.Identifier);
            }
            else //Error
            {
                _errorProblems.Remove(args.Identifier);
            }

            string tooltip = "";
            for (var index = 0; index < _errorProblems.Count; index++)
            {
                var problem = _errorProblems.Values.ElementAt(index);
                tooltip += $"Error: {problem}\n";
            }
            
            for (var index = 0; index < _warningProblems.Count; index++)
            {
                var problem = _warningProblems.Values.ElementAt(index);
                tooltip += $"Warning: {problem}\n";
            }
            UpdateEditorStatus(tooltip);
        }

        private void UpdateEditorStatus(string tooltip)
        {
            if (_warningProblems.Count == 0 && _errorProblems.Count == 0)
            {
                StatusOhShitImage.Visibility = Visibility.Collapsed;
                StatusBadImage.Visibility = Visibility.Collapsed;
                StatusGoodImage.Visibility = Visibility.Visible;
            }
            else if (_errorProblems.Count != 0)
            {
                StatusBadImage.Visibility = Visibility.Collapsed;
                StatusGoodImage.Visibility = Visibility.Collapsed;
                    
                StatusOhShitImage.Visibility = Visibility.Visible;
                StatusOhShitImage.ToolTip = new ToolTip() { Foreground = new SolidColorBrush(Colors.White), Content = tooltip };
            }
            else
            {
                StatusOhShitImage.Visibility = Visibility.Collapsed;
                StatusGoodImage.Visibility = Visibility.Collapsed;
                StatusBadImage.Visibility = Visibility.Visible;
                StatusBadImage.ToolTip = new ToolTip() { Foreground = new SolidColorBrush(Colors.White), Content = tooltip };
            }
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
            BlueprintEditorWindow_OnGotFocus(this, new RoutedEventArgs());
            AddButton_OnClick(this, new RoutedEventArgs());
        }

        #endregion

        #region Property Grid

        private void UpdatePropertyGrid(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_editor.SelectedNodes.Count == 0)
            {
                _editor.NodePropertyGrid.Object = null;
                return;
            }

            if (_editor.SelectedNodes.First().ObjectType != "EditorInterfaceNode")
            {
                TabControl.SelectedIndex = 0;
                _editor.NodePropertyGrid.Object = _editor.SelectedNodes.First().Object;
            }
            else
            {
                TabControl.SelectedIndex = 1;
            }
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