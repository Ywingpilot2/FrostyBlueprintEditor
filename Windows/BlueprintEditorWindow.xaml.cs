using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
using BlueprintEditor.Models.Types.Shared;
using BlueprintEditor.Utils;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditor.Windows
{
    public partial class BlueprintEditorWindow : FrostyWindow
    {
        private readonly Random _rng = new Random();

        public BlueprintEditorWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = $"Ebx Graph({App.EditorWindow.GetOpenedAssetEntry().Filename})";
            
            //Setup UI methods
            TypesList_FilterBox.KeyUp += FilterBox_FilterEnter;
            Editor.KeyUp += Editor_ControlInput;
            
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("GameDataContainer"))
            {
                TypesList.Items.Add(new NodeTypeViewModel(type));
            }
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

            //Create object nodes
            foreach (PointerRef ptr in openedProperties.Objects) 
            {
                object obj = ptr.Internal;
                NodeBaseModel node = NodeUtils.CreateNodeFromObject(obj);
                node.Guid = ((dynamic)obj).GetInstanceGuid();
            }

            // cache shit
            Dictionary<AssetClassGuid, int> instanceGuids = new Dictionary<AssetClassGuid, int>();

            for (int i = 0; i < EditorUtils.Editor.Nodes.Count; i++)
            {
                if (instanceGuids.ContainsKey(EditorUtils.Editor.Nodes[i].Guid))
                {
                    continue;
                }
                instanceGuids.Add(EditorUtils.Editor.Nodes[i].Guid, i);
            }

            #region Create interface node

            PointerRef interfaceRef = (PointerRef) openedProperties.Interface;

            NodeUtils.CreateInterfaceNodes(interfaceRef.Internal);

            #endregion
                
            #region Create Connections

            //Create property connections
            foreach (dynamic propertyConnection in openedProperties.PropertyConnections)
            {
                //TODO: Update to check if external ref
                if (propertyConnection.Source.Internal == null || propertyConnection.Target.Internal == null) continue; 

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)propertyConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)propertyConnection.Target.Internal).GetInstanceGuid();

                if (sourceNode != null && targetNode != null) break;

                string asdasd = (string)propertyConnection.SourceField.ToString();
                string asdasdasd = (string)propertyConnection.TargetField.ToString();

                //First check if the the node is an interface node
                if (((dynamic)propertyConnection.Source.Internal).GetInstanceGuid() == NodeUtils.InterfaceGuid)
                {
                    sourceNode = InterfaceDataNode.InterfaceOutputDataNodes[(string)propertyConnection.SourceField.ToString()];
                }
                else
                {
                    sourceNode = EditorUtils.Editor.Nodes[instanceGuids[sourceGuid]];
                }


                if (((dynamic)propertyConnection.Target.Internal).GetInstanceGuid() == NodeUtils.InterfaceGuid)
                {
                    targetNode = InterfaceDataNode.InterfaceInputDataNodes[(string)propertyConnection.TargetField.ToString()];
                }
                else
                {
                    targetNode = EditorUtils.Editor.Nodes[instanceGuids[targetGuid]];
                }

                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    continue;
                }
                
                InputViewModel targetInput =
                    targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property, true);
                OutputViewModel sourceOutput = sourceNode.GetOutput(propertyConnection.SourceField,
                    ConnectionType.Property, true);

                var connection = EditorUtils.Editor.Connect(sourceOutput, targetInput);
                connection.Object = propertyConnection;
            }
                
            //Create event connections
            foreach (dynamic eventConnection in openedProperties.EventConnections)
            {

                if (eventConnection.Source.Internal == null || eventConnection.Target.Internal == null) continue;

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)eventConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)eventConnection.Target.Internal).GetInstanceGuid();

                if (sourceNode != null && targetNode != null) break;
                    
                //First check if the the node is an interface node
                if (((dynamic)eventConnection.Source.Internal).GetInstanceGuid() == NodeUtils.InterfaceGuid)
                {
                    sourceNode = InterfaceDataNode.InterfaceOutputDataNodes[(string)eventConnection.SourceEvent.Name.ToString()];
                }
                else
                {
                    sourceNode = EditorUtils.Editor.Nodes[instanceGuids[sourceGuid]];
                }

                if (((dynamic)eventConnection.Target.Internal).GetInstanceGuid() == NodeUtils.InterfaceGuid)
                {
                    targetNode = InterfaceDataNode.InterfaceInputDataNodes[(string)eventConnection.TargetEvent.Name.ToString()];
                }
                else
                {
                    targetNode = EditorUtils.Editor.Nodes[instanceGuids[targetGuid]];
                }


                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    continue;
                }
                
                InputViewModel targetInput =
                    targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event, true);
                OutputViewModel sourceOutput = sourceNode.GetOutput(eventConnection.SourceEvent.Name,
                    ConnectionType.Event, true);
                        
                var connection = EditorUtils.Editor.Connect(sourceOutput, targetInput);
                connection.Object = eventConnection;
            }
                
            //Create link connections
            foreach (dynamic linkConnection in openedProperties.LinkConnections)
            {
                //TODO: Update to check if external ref
                if (linkConnection.Source.Internal == null || linkConnection.Target.Internal == null) continue; 

                NodeBaseModel sourceNode = null;
                NodeBaseModel targetNode = null;

                AssetClassGuid sourceGuid = (AssetClassGuid)((dynamic)linkConnection.Source.Internal).GetInstanceGuid();
                AssetClassGuid targetGuid = (AssetClassGuid)((dynamic)linkConnection.Target.Internal).GetInstanceGuid();

                //First check if the the node is an interface node
                if (((dynamic)linkConnection.Source.Internal).GetInstanceGuid() == NodeUtils.InterfaceGuid)
                {
                    sourceNode = InterfaceDataNode.InterfaceOutputDataNodes[(string)linkConnection.SourceField.ToString()];
                }
                else
                {
                    sourceNode = EditorUtils.Editor.Nodes[instanceGuids[sourceGuid]];
                }

                if (((dynamic)linkConnection.Target.Internal).GetInstanceGuid() == NodeUtils.InterfaceGuid)
                {
                    targetNode = InterfaceDataNode.InterfaceInputDataNodes[(string)linkConnection.TargetField.ToString()];
                }
                else
                {
                    targetNode = EditorUtils.Editor.Nodes[instanceGuids[targetGuid]];
                }


                //We encountered an error
                if (sourceNode == null || targetNode == null)
                {
                    App.Logger.LogError("Node was null!");
                    continue;
                }
                
                string sourceField = linkConnection.SourceField;
                string targetField = linkConnection.TargetField;
                if (sourceField == "0x00000000")
                {
                    sourceField = "self";
                }

                if (targetField == "0x00000000")
                {
                    targetField = "self";
                }
                        
                InputViewModel targetInput =
                    targetNode.GetInput(sourceField, ConnectionType.Link, true);
                OutputViewModel sourceOutput = sourceNode.GetOutput(targetField,
                    ConnectionType.Link, true);
                        
                var connection = EditorUtils.Editor.Connect(sourceOutput, targetInput);
                connection.Object = linkConnection;
            }

            #endregion

            InterfaceDataNode.InterfaceInputDataNodes.Clear();
            InterfaceDataNode.InterfaceOutputDataNodes.Clear();

            instanceGuids.Clear();
        }

        private void Editor_ControlInput(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                while (EditorUtils.Editor.SelectedNodes.Count != 0)
                {
                    NodeUtils.DeleteNode(EditorUtils.Editor.SelectedNodes[0]);
                }
            }
        }

        private void BlueprintEditorWindow_OnClosing(object sender, CancelEventArgs e)
        {
            EditorUtils.Editor = null;
        }

        #endregion
        
        #region TypesList

        private void TypesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                EditorUtils.TypesViewModelListSelectedItem = null;
                return;
            }
            EditorUtils.TypesViewModelListSelectedItem = (NodeTypeViewModel)e.AddedItems[0];
        }

        #endregion

        #region Buttons

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            while (EditorUtils.Editor.SelectedNodes.Count != 0)
            {
                NodeUtils.DeleteNode(EditorUtils.Editor.SelectedNodes[0]);
            }
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (EditorUtils.TypesViewModelListSelectedItem == null) return;

            object obj = TypeLibrary.CreateObject(EditorUtils.TypesViewModelListSelectedItem.NodeType.Name);
            NodeUtils.CreateNodeFromObject(obj);
            PointerRef pointerRef = new PointerRef(obj);
            AssetClassGuid guid = new AssetClassGuid(FrostySdk.Utils.GenerateDeterministicGuid(
                EditorUtils.Editor.EditedEbxAsset.Objects,
                EditorUtils.TypesViewModelListSelectedItem.NodeType,
                EditorUtils.Editor.EditedEbxAsset.FileGuid), -1); //TODO: THIS CODE SUCKS! PLEASE UPDATE!
            ((dynamic)pointerRef.Internal).SetInstanceGuid(guid);
            
            //No idea what this does
            if (TypeLibrary.IsSubClassOf(pointerRef.Internal, "DataBusPeer"))
            {
                byte[] b = guid.ExportedGuid.ToByteArray();
                uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                pointerRef.Internal.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(pointerRef.Internal, value);
            }
            
            EditorUtils.Editor.EditedEbxAsset.AddObject(pointerRef.Internal);
            EditorUtils.Editor.EditedProperties.Objects.Add(pointerRef);
            
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(EditorUtils.Editor.EditedEbxAsset.FileGuid).Name, EditorUtils.Editor.EditedEbxAsset);
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
            //TODO: Find a more precise way to do this
            //Credit to github.com/CadeEvs for source(is temp, and will be replaced, though is a good placeholder)
            
            FrostyTaskWindow.Show(BlueprintWindow, "Sorting nodes...", "", task =>
            { 
                //Gather node data
                Dictionary<NodeBaseModel, List<NodeBaseModel>> ancestors = new Dictionary<NodeBaseModel, List<NodeBaseModel>>();
                Dictionary<NodeBaseModel, List<NodeBaseModel>> children = new Dictionary<NodeBaseModel, List<NodeBaseModel>>();

                foreach (NodeBaseModel node in EditorUtils.Editor.Nodes)
                {
                    ancestors.Add(node, new List<NodeBaseModel>());
                    children.Add(node, new List<NodeBaseModel>());
                }

                foreach (ConnectionViewModel connection in EditorUtils.Editor.Connections)
                {
                    ancestors[connection.TargetNode].Add(connection.SourceNode);
                    children[connection.SourceNode].Add(connection.TargetNode);
                }
            
                List<List<NodeBaseModel>> columns = new List<List<NodeBaseModel>>();
                List<NodeBaseModel> alreadyProcessed = new List<NodeBaseModel>();

                int columnIdx = 1;
                columns.Add(new List<NodeBaseModel>());

                foreach (NodeBaseModel node in EditorUtils.Editor.Nodes)
                {
                    if (ancestors[node].Count == 0 && children[node].Count == 0)
                    {
                        alreadyProcessed.Add(node);
                        columns[0].Add(node);
                        continue;
                    }

                    if (ancestors[node].Count == 0)
                    {
                        EditorUtils.LayoutNodes(node, children, columns, alreadyProcessed, columnIdx);
                    }
                }

                columnIdx = 1;
                foreach (NodeBaseModel node in EditorUtils.Editor.Nodes)
                {
                    if (!alreadyProcessed.Contains(node))
                    {
                        EditorUtils.LayoutNodes(node, children, columns, alreadyProcessed, columnIdx);
                    }
                }

                double x = 100.0;
                double width = 0.0;

                foreach (List<NodeBaseModel> column in columns)
                {
                    double y = 96.0;
                    foreach (NodeBaseModel node in column)
                    {
                        x -= (x % 8);
                        y -= (y % 8);
                        node.Location = new Point(x, y); 

                        double curWidth = Math.Floor((node.RealWidth + 40.0) / 4.0) * 8.0;
                        double curHeight = Math.Floor(((node.Inputs.Count * 14) + 70.0) / 8.0) * 8.0;

                        y += curHeight + 56.0;

                        if (curWidth > width)
                        {
                            width = curWidth;
                        }
                    }

                    x += width + 280.0;
                } 
            });
        }

        #endregion

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
    }
}