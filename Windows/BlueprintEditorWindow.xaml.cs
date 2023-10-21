using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using BlueprintEditor.Models.MenuItems;
using BlueprintEditor.Models.Types;
using BlueprintEditor.Models.Types.BlueprintLoaderTypes;
using BlueprintEditor.Models.Types.NodeTypes;
using BlueprintEditor.Utils;
using Frosty.Controls;
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
        private readonly Random _rng = new Random();
        private readonly EbxAssetEntry _file;
        private NodeTypeViewModel _selectedType;

        public BlueprintEditorWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = $"Ebx Graph({App.EditorWindow.GetOpenedAssetEntry().Filename})";
            _file = App.EditorWindow.GetOpenedAssetEntry() as EbxAssetEntry;
            
            //Setup UI methods
            TypesList_FilterBox.KeyUp += FilterBox_FilterEnter;
            Editor.KeyUp += Editor_ControlInput;
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

            BlueprintBaseLoader loader = new BlueprintBaseLoader();
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(BlueprintBaseLoader)))
                {
                    var extension = (BlueprintBaseLoader)Activator.CreateInstance(type);
                    if (extension.AssetType == App.EditorWindow.GetOpenedAssetEntry().Type)
                    {
                        loader = extension;
                    }
                }
            }
            
            loader.PopulateTypesList(TypesList.Items);
            loader.PopulateNodes(openedProperties);
            loader.CreateConnections(openedProperties);

            EditorUtils.ApplyLayouts(_file);
        }
        

        private void Editor_ControlInput(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                while (EditorUtils.CurrentEditor.SelectedNodes.Count != 0)
                {
                    EditorUtils.CurrentEditor.DeleteNode(EditorUtils.CurrentEditor.SelectedNodes[0]);
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
            while (EditorUtils.CurrentEditor.SelectedNodes.Count != 0)
            {
                EditorUtils.CurrentEditor.DeleteNode(EditorUtils.CurrentEditor.SelectedNodes[0]);
            }
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedType == null) return;

            object obj = TypeLibrary.CreateObject(_selectedType.NodeType.Name);
            EditorUtils.CurrentEditor.CreateNodeFromObject(obj);
            PointerRef pointerRef = new PointerRef(obj);
            AssetClassGuid guid = new AssetClassGuid(FrostySdk.Utils.GenerateDeterministicGuid(
                EditorUtils.CurrentEditor.EditedEbxAsset.Objects,
                _selectedType.NodeType,
                EditorUtils.CurrentEditor.EditedEbxAsset.FileGuid), -1); //TODO: THIS CODE SUCKS! PLEASE UPDATE!
            ((dynamic)pointerRef.Internal).SetInstanceGuid(guid);
            
            //No idea what this does
            if (TypeLibrary.IsSubClassOf(pointerRef.Internal, "DataBusPeer"))
            {
                byte[] b = guid.ExportedGuid.ToByteArray();
                uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                pointerRef.Internal.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(pointerRef.Internal, value);
            }
            
            EditorUtils.CurrentEditor.EditedEbxAsset.AddObject(pointerRef.Internal);
            EditorUtils.CurrentEditor.EditedProperties.Objects.Add(pointerRef);
            
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(EditorUtils.CurrentEditor.EditedEbxAsset.FileGuid).Name, EditorUtils.CurrentEditor.EditedEbxAsset);
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
        
        #region TypesList

        private void TypesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                _selectedType = null;
                return;
            }
            _selectedType = (NodeTypeViewModel)e.AddedItems[0];
        }
        
        #region FilterBox

        private void FilterBox_FilterEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TypesList_UpdateFilter();
            }
        }
        
        private void TypesList_UpdateFilter()
        {
            if (TypesList_FilterBox.Text != "")
            {
                string filterText = TypesList_FilterBox.Text;
                TypesList.Items.Filter = a => ((NodeTypeViewModel)a).Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
                return;
            }

            TypesList.Items.Filter = null;
        }

        #endregion

        #endregion
    }
}