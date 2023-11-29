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
using BlueprintEditorPlugin.Models.Connections;
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
using Nodify;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintGraphEditor : UserControl
    {
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
        /// TODO: Add in FrostyTaskWindow so frosty doesn't just freeze
        /// </summary>
        public void Initiate()
        {
            _loader = (EbxBaseLoader)Activator.CreateInstance(EditorUtils.EbxLoaders.ContainsKey(File.AssetType) ? EditorUtils.EbxLoaders[File.AssetType] : EditorUtils.EbxLoaders["null"]);
            _loader.PopulateTypesList(_types); //Populate the types list with our types
            ClassSelector.Types = _types;
            
            foreach (TransientNode transNode in NodeUtils.TransNodeExtensions.Values) //Iterate over all of the transient extensions
            {
                Type type = transNode.GetType();
                if (type.IsSubclassOf(typeof(TransientNode)))
                {
                    _transTypes.Add(type);
                }
            }

            TransientClassSelector.Types = _transTypes;

            //The NodeEditor needs to access the property grid, and vice versa. It's a weird setup to ensure they are in sync, but it works.
            NodeEditor.NodePropertyGrid = new BlueprintPropertyGrid() { NodeEditor = NodeEditor };
            FrostyTabItem ti = new FrostyTabItem //Now we create a tab item which contains our property grid
            {
                Header = "Property Grid",
                Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Properties.png") as ImageSource,
                Content = NodeEditor.NodePropertyGrid
            };
            PropertiesTabControl.Items.Add(ti);

            //Setup UI methods
            NodifyEditor.KeyDown += Editor_ControlInput;
            NodifyEditor.KeyUp += Editor_ControlInput;
            NodifyEditor.MouseRightButtonUp += Editor_MouseInput;
            NodifyEditor.MouseLeftButtonUp += Editor_MouseInput;
            NodeEditor.SelectedNodes.CollectionChanged += NodeSelectionUpdated;
            
            //Mouse capture
            NodeEditor.NodePropertyGrid.GotMouseCapture += BlueprintEditorWindow_OnGotFocus;
            NodeEditor.NodePropertyGrid.GotKeyboardFocus += BlueprintEditorWindow_OnGotFocus;
            ClassSelector.GotMouseCapture += BlueprintEditorWindow_OnGotFocus;
            
            //Editor status
            NodeEditor.EditorStatusChanged += SetEditorStatus;
            NodeEditor.RemoveEditorStatus += RemoveEditorMessage;
            
            dynamic openedProperties = NodeEditor.EditedProperties;

            _loader.NodeEditor = NodeEditor;
            
            _loader.PopulateNodes(openedProperties);
            _loader.CreateConnections(openedProperties);
            
            //If this loader lacks an interface(e.g ScalableEmitterDocuments) we don't want to try and load the interface
            //So we check with the loader before adding the interface editor
            if (_loader.HasInterface && ((PointerRef)openedProperties.Interface).Type != PointerRefType.Null)
            {
                NodeEditor.InterfacePropertyGrid = new BlueprintPropertyGrid()
                {
                    Object = openedProperties.Interface.Internal, 
                    NodeEditor = NodeEditor
                };
                FrostyTabItem iti = new FrostyTabItem()
                {
                    Header = "Interface",
                    Icon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Interface.png") as ImageSource,
                    Content = NodeEditor.InterfacePropertyGrid
                };
                PropertiesTabControl.Items.Add(iti);
            }

            EditorUtils.ApplyLayouts(_file, NodeEditor); //Organize the file
        }

        #region Window events

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
                while (NodeEditor.SelectedNodes.Count != 0)
                {
                    NodeEditor.DeleteNode(NodeEditor.SelectedNodes[0]);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && Keyboard.IsKeyDown(Key.D) && NodeEditor.SelectedNodes.Count != 0)
            {
                List<NodeBaseModel> nodesToDupe = new List<NodeBaseModel>(NodeEditor.SelectedNodes);
                NodeEditor.SelectedNodes.Clear();
                while (nodesToDupe.Count != 0)
                {
                    var nodeToDupe = nodesToDupe[0];
                    if (nodeToDupe.ObjectType != "EditorInterfaceNode")
                    {
                        object dupedObj = NodeEditor.CreateNodeObject(nodeToDupe.Object);
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
                        NodeEditor.SelectedNodes.Add(dupe);
                    }
                    else
                    {
                        App.Logger.LogError("Cannot dupe Interface nodes!");
                    }

                    nodesToDupe.Remove(nodeToDupe);
                }
                App.AssetManager.ModifyEbx(_file.Name, NodeEditor.EditedEbxAsset);
            }
            
        }

        private void Editor_MouseInput(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.ChangedButton == MouseButton.Right && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (_selectedType != null)
                {
                    if (_selectedType != null && !_selectedType.IsSubclassOf(typeof(TransientNode)))
                    {
                        object obj = NodeEditor.CreateNodeObject(_selectedType);

                        var node = _loader.GetNodeFromObject(obj);
                        node.Location = new Point(NodifyEditor.MouseLocation.X, NodifyEditor.MouseLocation.Y);
                        node.OnCreateNew();
                    }
                    else if (_selectedType != null) //Stuff for adding transients
                    {
                        TransientNode node = (TransientNode)Activator.CreateInstance(_selectedType);
                        node.OnCreation();
                        node.Location = new Point(NodifyEditor.MouseLocation.X, NodifyEditor.MouseLocation.Y);
                        NodeEditor.Nodes.Add(node);
                    }

                    App.AssetManager.ModifyEbx(_file.Name, NodeEditor.EditedEbxAsset);
                    App.EditorWindow.DataExplorer.RefreshItems();
                }
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
            EditorUtils.ActiveNodeEditors.Remove(_file.Filename);
            App.EditorWindow.OpenAsset(_file); //TODO: Make this a profile option
        }
        
        /// <summary>
        /// Executes whenever this window is focused
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BlueprintEditorWindow_OnGotFocus(object sender, RoutedEventArgs e)
        {
            EditorUtils.CurrentEditor = NodeEditor;
        }

        #endregion
        
        /// <summary>
        /// This method executes whenever the selection of nodes changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NodeSelectionUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (object oldItem in e.OldItems)
                {
                    NodeBaseModel oldNode = oldItem as NodeBaseModel;
                    oldNode.IsSelected = false;
                }
            }
            
            if (NodeEditor.SelectedNodes.Count == 0)
            {
                NodeEditor.NodePropertyGrid.Object = new object();
                return;
            }

            if (NodeEditor.SelectedNodes.First().ObjectType != "EditorInterfaceNode")
            {
                PropertiesTabControl.SelectedIndex = 0;
                NodeEditor.NodePropertyGrid.Object = NodeEditor.SelectedNodes.First().Object;
            }
            else
            {
                PropertiesTabControl.SelectedIndex = 1;
            }

            foreach (NodeBaseModel node in NodeEditor.SelectedNodes)
            {
                node.IsSelected = true;
            }
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
            while (NodeEditor.SelectedNodes.Count != 0)
            {
                NodeEditor.DeleteNode(NodeEditor.SelectedNodes[0]);
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
                object obj = NodeEditor.CreateNodeObject(_selectedType);
            
                var node = _loader.GetNodeFromObject(obj);
                node.Location = new Point(NodeEditor.ViewportLocation.X + (575 / NodeEditor.ViewportZoom), NodeEditor.ViewportLocation.Y + 287.5 / NodeEditor.ViewportZoom);
                node.OnCreateNew();
            }
            else if (_selectedType != null) //Stuff for adding transients
            {
                TransientNode node = (TransientNode)Activator.CreateInstance(_selectedType);
                node.OnCreation();
                node.Location = new Point(NodeEditor.ViewportLocation.X + (575 / NodeEditor.ViewportZoom), NodeEditor.ViewportLocation.Y + 287.5 / NodeEditor.ViewportZoom);
                NodeEditor.Nodes.Add(node);
            }
            
            App.AssetManager.ModifyEbx(_file.Name, NodeEditor.EditedEbxAsset);
            App.EditorWindow.DataExplorer.RefreshItems();
        }
        
        /// <summary>
        /// This executes the AutoLayout when pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrganizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditorUtils.ApplyAutoLayout(NodeEditor);
        }
        
        private void ImportOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            //Open a file dialog so the user can select a file
            FrostyOpenFileDialog ofd = new FrostyOpenFileDialog("Open Layout", "Blueprint Layout (*.lyt)|*.lyt|Text File (*.txt)|*.txt", "BlueprintLayout");
            if (!ofd.ShowDialog()) return; //I think this means it was cancelled though I don't actually know

            EditorUtils.ApplyExistingLayout(ofd.FileName, NodeEditor);
        }

        private void SaveOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            EditorUtils.SaveLayouts(_file);
            App.Logger.Log("Saved layout!");
        }
        
        private void AutoMapButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (NodeBaseModel selectedNode in NodeEditor.SelectedNodes)
            {
                NodeUtils.GenerateNodeMapping(selectedNode);
            }
        }
        
        private void RefreshMappingButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
        
        private void ControlsMenuVisible_OnClick(object sender, RoutedEventArgs e)
        {
            var controlsWindow = new BlueprintEditorControlsWindow();
            controlsWindow.Show();
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
                if (NodeUtils.EntityNodeExtensions.ContainsKey(_selectedType.Name))
                {
                    DocBoxText.Text = NodeUtils.EntityNodeExtensions[_selectedType.Name].Documentation;
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

        #region Trans-Toolbox

        private void TransientClassSelector_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedType = TransientClassSelector.SelectedClass;
            if (_selectedType == null) return;
            
            DocBoxName.Text = _selectedType.Name;
            DocBoxText.Text = NodeUtils.EntityNodeExtensions.ContainsKey(_selectedType.Name) ? NodeUtils.EntityNodeExtensions[_selectedType.Name].Documentation : "";
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

        #endregion

        #region Property Grid

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
    }
}