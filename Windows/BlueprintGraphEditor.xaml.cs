using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.EbxLoaderTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Transient;
using BlueprintEditorPlugin.Utils;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintGraphEditor : UserControl
    {
        private EditorViewModel _editor;
        private EbxBaseLoader _loader;
        
        private readonly Random _rng = new Random();

        private EbxAssetEntry _file;
        public EbxAssetEntry File
        {
            get => _file;
            set
            {
                _file = value;
                Initiate();
            }
        }

        private Type _selectedType;
        private Type _selectedTransType;
        private List<Type> _types = new List<Type>();
        private List<Type> _transTypes = new List<Type>();

        public BlueprintGraphEditor()
        {
            InitializeComponent();
        }
        
        #region Editor

        /// <summary>
        /// Initiates the editor by populating the graph with nodes and connections
        /// TODO: Add in FrostyTaskWindow(with owner set to <see cref="BlueprintWindow"/>) so frosty doesn't just freeze
        /// </summary>
        public void Initiate()
        {
            _editor = EditorUtils.Editors[_file.Filename]; //Get the editor based on what our filename is
            _editor.MouseLocation = Editor.MouseLocation;

            //Create a new loader
            EbxBaseLoader loader = new EbxBaseLoader(); //Base loader will be used if no extensions are found
            foreach (var type in Assembly.GetCallingAssembly().GetTypes()) //Iterate over all of the loader extensions
            {
                if (!type.IsSubclassOf(typeof(EbxBaseLoader))) continue;
                var extension = (EbxBaseLoader)Activator.CreateInstance(type);
                    
                //If the extension type doesn't match our file type then we continue
                if (extension.AssetType != _file.Type) continue;  
                loader = extension; //If it does, we set the loader to be this extension instead and stop searching
                break;
            }

            _loader = loader;
            _loader.PopulateTypesList(_types); //Populate the types list with our types
            ClassSelector.Types = _types;
            
            foreach (var type in Assembly.GetCallingAssembly().GetTypes()) //Iterate over all of the transient extensions
            {
                if (type.IsSubclassOf(typeof(TransientNode)))
                {
                    _transTypes.Add(type);
                }
            }

            TransientClassSelector.Types = _transTypes;

            //The NodeEditor needs to access the property grid, and vice versa. It's a weird setup to ensure they are in sync, but it works.
            _editor.NodePropertyGrid = new BlueprintPropertyGrid() { NodeEditor = _editor };
            FrostyTabItem ti = new FrostyTabItem //Now we create a tab item which contains our property grid
            {
                Header = "Property Grid",
                Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Properties.png") as ImageSource,
                Content = _editor.NodePropertyGrid
            };
            PropertiesTabControl.Items.Add(ti);

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
            
            dynamic openedProperties = _editor.EditedProperties;

            _loader.NodeEditor = _editor;
            
            _loader.PopulateNodes(openedProperties);
            _loader.CreateConnections(openedProperties);
            
            //If this loader lacks an interface(e.g ScalableEmitterDocuments) we don't want to try and load the interface
            //So we check with the loader before adding the interface editor
            if (_loader.HasInterface && ((PointerRef)openedProperties.Interface).Type != PointerRefType.Null)
            {
                _editor.InterfacePropertyGrid = new BlueprintPropertyGrid()
                {
                    Object = openedProperties.Interface.Internal, 
                    NodeEditor = _editor
                };
                FrostyTabItem iti = new FrostyTabItem()
                {
                    Header = "Interface",
                    Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Interface.png") as ImageSource,
                    Content = _editor.InterfacePropertyGrid
                };
                PropertiesTabControl.Items.Add(iti);
            }

            EditorUtils.ApplyLayouts(_file, _editor); //Organize the file
        }
        
        /// <summary>
        /// This method executes whenever a button is inputted into the Editor
        /// To be used for things like keybinds 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The key that was pressed</param>
        private void Editor_ControlInput(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                while (_editor.SelectedNodes.Count != 0)
                {
                    _editor.DeleteNode(_editor.SelectedNodes[0]);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && Keyboard.IsKeyDown(Key.D) && _editor.SelectedNodes.Count != 0)
            {
                List<NodeBaseModel> nodesToDupe = new List<NodeBaseModel>(_editor.SelectedNodes);
                _editor.SelectedNodes.Clear();
                while (nodesToDupe.Count != 0)
                {
                    var nodeToDupe = nodesToDupe[0];
                    if (nodeToDupe.ObjectType != "EditorInterfaceNode")
                    {
                        object dupedObj = _editor.CreateNodeObject(nodeToDupe.Object);
                        NodeBaseModel dupe = _loader.GetNodeFromObject(dupedObj);
                        
                        //This means we need to copy over the inputs/outputs
                        if (dupe.ObjectType == "null")
                        {
                            dupe.Inputs.Clear();
                            dupe.Outputs.Clear();
                            foreach (InputViewModel input in nodeToDupe.Inputs)
                            {
                                InputViewModel dupedInput = new InputViewModel()
                                {
                                    Title = input.Title,
                                    Type = input.Type
                                };
                                dupe.Inputs.Add(dupedInput);
                            }
                            foreach (InputViewModel input in nodeToDupe.Inputs)
                            {
                                InputViewModel dupedInput = new InputViewModel()
                                {
                                    Title = input.Title,
                                    Type = input.Type
                                };
                                dupe.Inputs.Add(dupedInput);
                            }
                        }
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

        /// <summary>
        /// Executes whenever the editor closes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BlueprintEditorWindow_OnClosing(object sender, CancelEventArgs e)
        {
            EditorUtils.SaveLayouts(_file);
            EditorUtils.Editors.Remove(_file.Filename);
            App.EditorWindow.OpenAsset(_file); //TODO: Make this a profile option
        }
        
        /// <summary>
        /// Executes whenever this window is focused
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BlueprintEditorWindow_OnGotFocus(object sender, RoutedEventArgs e)
        {
            EditorUtils.CurrentEditor = _editor;
        }

        #endregion

        #region Editor Status

        private Dictionary<int, string> _errorProblems = new Dictionary<int, string>();
        private Dictionary<int, string> _warningProblems = new Dictionary<int, string>();

        /// <summary>
        /// Sets the editor's problem status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SetEditorStatus(object sender, EditorStatusArgs args)
        {
            EditorStatus status = args.Status;

            //Check if we are setting it to a warning or error. If it's being set to good, we don't need to add anything in this regard
            switch (status)
            {
                case EditorStatus.Warning:
                {
                    string message = (args.Tooltip ?? "A problem has been found with the blueprint");
                    if (!_warningProblems.ContainsKey(args.Identifier))
                    {
                        _warningProblems.Add(args.Identifier, message);
                    }

                    break;
                }
                case EditorStatus.Error:
                {
                    string message = (args.Tooltip ?? "A problem has been found with the blueprint");
                    if (!_errorProblems.ContainsKey(args.Identifier))
                    {
                        _errorProblems.Add(args.Identifier, message);
                    }

                    break;
                }
            }

            //Create the new tooltip(text that is displayed when hovered over)
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

        /// <summary>
        /// This will remove a status error/warning message from the editor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">the args.Status must be set to Warning or Error, otherwise this will do nothing. </param>
        private void RemoveEditorMessage(object sender, EditorStatusArgs args)
        {
            switch (args.Status)
            {
                case EditorStatus.Warning:
                {
                    _warningProblems.Remove(args.Identifier);
                    break;
                }
                //Error
                case EditorStatus.Error:
                {
                    _errorProblems.Remove(args.Identifier);
                    break;
                }
            }

            //Update the tooltip
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
            
            //Update the EditorStatus
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

        /// <summary>
        /// This method executes whenever the Remove Node button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            while (_editor.SelectedNodes.Count != 0)
            {
                _editor.DeleteNode(_editor.SelectedNodes[0]);
            }
        }

        /// <summary>
        /// This method executes whenever the Add Node button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedType != null && !_selectedType.IsSubclassOf(typeof(TransientNode)))
            {
                object obj = _editor.CreateNodeObject(_selectedType);
            
                var node = _loader.GetNodeFromObject(obj);
                node.Location = new Point(_editor.ViewportLocation.X + (575 / _editor.ViewportZoom), _editor.ViewportLocation.Y + 287.5 / _editor.ViewportZoom);
            }
            else if (_selectedType != null) //Stuff for adding transients
            {
                TransientNode node = (TransientNode)Activator.CreateInstance(_selectedType);
                node.OnCreation();
                node.Location = new Point(_editor.ViewportLocation.X + (575 / _editor.ViewportZoom), _editor.ViewportLocation.Y + 287.5 / _editor.ViewportZoom);
                _editor.Nodes.Add(node);
            }
            
            App.AssetManager.ModifyEbx(_file.Name, _editor.EditedEbxAsset);
            App.EditorWindow.DataExplorer.RefreshItems();
        }
        
        /// <summary>
        /// This executes the AutoLayout when pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrganizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditorUtils.ApplyAutoLayout(_editor);
        }
        
        private void ImportOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            //Open a file dialog so the user can select a file
            FrostyOpenFileDialog ofd = new FrostyOpenFileDialog("Open Layout", "Blueprint Layout (*.lyt)|*.lyt|Text File (*.txt)|*.txt", "BlueprintLayout");
            if (!ofd.ShowDialog()) return; //I think this means it was cancelled though I don't actually know

            EditorUtils.ApplyExistingLayout(ofd.FileName, _editor);
        }

        private void SaveOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditorUtils.SaveLayouts(_file);
            App.Logger.Log("Saved layout!");
        }
        
        private void AutoMapButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (NodeBaseModel selectedNode in _editor.SelectedNodes)
            {
                NodeUtils.GenerateNodeMapping(selectedNode);
            }
        }

        #endregion
        
        #region Toolbox

        /// <summary>
        /// This executes whenever the selected item in the TypesList changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                else
                {
                    DocBoxText.Text = "";
                }
            }
        }
        
        /// <summary>
        /// This executes whenever the button to turn toolbox visibility on/off is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// This executes whenever an item is double clicked in the toolbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolboxClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            BlueprintEditorWindow_OnGotFocus(this, new RoutedEventArgs());
            AddButton_OnClick(this, new RoutedEventArgs()); //We just send this over to the AddButton lol
        }

        #endregion

        #region Property Grid

        /// <summary>
        /// This method executes whenever the property grid needs to be changed, so e.g when the selected node changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdatePropertyGrid(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_editor.SelectedNodes.Count == 0)
            {
                _editor.NodePropertyGrid.Object = new object();
                return;
            }

            if (_editor.SelectedNodes.First().ObjectType != "EditorInterfaceNode")
            {
                PropertiesTabControl.SelectedIndex = 0;
                _editor.NodePropertyGrid.Object = _editor.SelectedNodes.First().Object;
            }
            else
            {
                PropertiesTabControl.SelectedIndex = 1;
            }
        }
        
        /// <summary>
        /// This executes whenever the property grid visibility button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        private void TransientClassSelector_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedType = TransientClassSelector.SelectedClass;
            if (_selectedType == null) return;
            
            DocBoxName.Text = _selectedType.Name;
            DocBoxText.Text = NodeUtils.NodeExtensions.ContainsKey(_selectedType.Name) ? NodeUtils.NodeExtensions[_selectedType.Name].Documentation : "";
        }

        private void TransientClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            BlueprintEditorWindow_OnGotFocus(this, new RoutedEventArgs());
            AddButton_OnClick(this, new RoutedEventArgs()); //We just send this over to the AddButton lol
        }

        // private void ClassSelector_OnItemBeginDrag(object sender, RoutedEventArgs e)
        // {
        //     
        // }
    }
}