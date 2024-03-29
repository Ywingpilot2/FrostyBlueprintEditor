using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Entities;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using Nodify;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor
{
    public partial class BlueprintGraphEditor : UserControl, IGraphEditor
    {
        #region Graph Editor Implementation

        public INodeWrangler NodeWrangler { get; set; }
        public ILayoutManager LayoutManager { get; set; }

        public bool IsValid()
        {
            return true;
        }

        public bool IsValid(EbxAssetEntry assetEntry)
        {
            EbxAsset asset = App.AssetManager.GetEbx(assetEntry);
            Type assetType = asset.RootObject.GetType();
            
            // We only check property connections since we can assume if it has property connections it has everything else
            if (assetType.GetProperty("Objects") != null 
                && assetType.GetProperty("PropertyConnections") != null 
                && assetType.GetProperty("Interface") != null)
            {
                return true;
            }
            
            return false;
        }

        public bool IsValid(params object[] args)
        {
            return false; // Only valid for assets
        }

        public void Closed()
        {
            EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid);
            LayoutManager.SaveLayout($"{assetEntry.Name}.lyt");
        }

        #endregion

        public BlueprintGraphEditor()
        {
            NodeWrangler = new EntityNodeWrangler();
            
            InitializeComponent();
            NodePropertyGrid.NodeWrangler = NodeWrangler;
            LayoutManager = new EntityLayoutManager(NodeWrangler);
        }

        public void LoadAsset(EbxAssetEntry assetEntry)
        {
            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
            wrangler.Asset = App.AssetManager.GetEbx(assetEntry);

            foreach (object assetObject in wrangler.Asset.Objects)
            {
                if (assetObject == wrangler.Asset.RootObject)
                    continue;

                if (assetObject.GetType().Name == "InterfaceDescriptorData")
                {
                    wrangler.InterfaceGuid = ((dynamic)assetObject).GetInstanceGuid();
                    foreach (dynamic field in ((dynamic)assetObject).Fields)
                    {
                        switch (field.AccessType.ToString())
                        {
                            case "FieldAccessType_Source":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.In, NodeWrangler)
                                {
                                    SubObject = field
                                });
                            } break;
                            case "FieldAccessType_Target":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.Out, NodeWrangler)
                                {
                                    SubObject = field
                                });
                            } break;
                            case "FieldAccessType_SourceAndTarget":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.In, NodeWrangler)
                                {
                                    SubObject = field
                                });
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.Out, NodeWrangler)
                                {
                                    SubObject = field
                                });
                            } break;
                        }
                    }

                    foreach (dynamic inputEvent in ((dynamic)assetObject).InputEvents)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, inputEvent.Name, ConnectionType.Event, PortDirection.Out, NodeWrangler)
                        {
                            SubObject = inputEvent
                        });
                    }
                    foreach (dynamic outputEvent in ((dynamic)assetObject).OutputEvents)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, outputEvent.Name, ConnectionType.Event, PortDirection.In, NodeWrangler)
                        {
                            SubObject = outputEvent
                        });
                    }
                    
                    foreach (dynamic inputLink in ((dynamic)assetObject).InputLinks)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, inputLink.Name, ConnectionType.Link, PortDirection.Out, NodeWrangler)
                        {
                            SubObject = inputLink
                        });
                    }
                    foreach (dynamic outputLink in ((dynamic)assetObject).OutputLinks)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, outputLink.Name, ConnectionType.Link, PortDirection.In, NodeWrangler)
                        {
                            SubObject = outputLink
                        });
                    }
                    
                    continue;
                }
                
                wrangler.AddNodeTransient(EntityNode.GetNodeFromEntity(assetObject, NodeWrangler));
            }

            #region Populating connections

            foreach (dynamic propertyConnection in ((dynamic)wrangler.Asset.RootObject).PropertyConnections)
            {
                PointerRef source = propertyConnection.Source;
                PointerRef target = propertyConnection.Target;

                IObjectNode sourceNode = null;
                IObjectNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in property connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(propertyConnection.SourceField, PortDirection.Out);
                        }
                        else
                        {
                            sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        // Import the node
                        EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(source.External.FileGuid));
                        sourceNode = EntityNode.GetNodeFromEntity(asset.GetObject(source.External.ClassGuid), source.External.FileGuid, NodeWrangler);
                        
                        wrangler.AddNodeTransient(sourceNode);
                    } break;
                }
                
                switch (target.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in property connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(propertyConnection.TargetField, PortDirection.In);
                        }
                        else
                        {
                            targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        // Import the node
                        EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(target.External.FileGuid));
                        targetNode = EntityNode.GetNodeFromEntity(asset.GetObject(target.External.ClassGuid), target.External.FileGuid, NodeWrangler);
                        
                        wrangler.AddNodeTransient(targetNode);
                    } break;
                }
                
                if (sourceNode.GetOutput(propertyConnection.SourceField, ConnectionType.Property) == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        sourceNode.AddOutput(new PropertyOutput(propertyConnection.SourceField, sourceNode));
                    });
                }
                
                if (targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property) == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        targetNode.AddInput(new PropertyInput(propertyConnection.TargetField, targetNode));
                    });
                }

                PropertyOutput output = (PropertyOutput)sourceNode.GetOutput(propertyConnection.SourceField, ConnectionType.Property);
                PropertyInput input = (PropertyInput)targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property);
                
                wrangler.AddConnectionTransient(output, input, propertyConnection);
            }
            
            foreach (dynamic linkConnection in ((dynamic)wrangler.Asset.RootObject).LinkConnections)
            {
                PointerRef source = linkConnection.Source;
                PointerRef target = linkConnection.Target;

                IObjectNode sourceNode = null;
                IObjectNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in link connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(linkConnection.SourceField, PortDirection.Out);
                        }
                        else
                        {
                            sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        // Import the node
                        EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(source.External.FileGuid));
                        sourceNode = EntityNode.GetNodeFromEntity(asset.GetObject(source.External.ClassGuid), source.External.FileGuid, NodeWrangler);

                        wrangler.AddNodeTransient(sourceNode);
                    } break;
                }
                
                switch (target.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in link connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(linkConnection.TargetField, PortDirection.In);
                        }
                        else
                        {
                            targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        // Import the node
                        EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(target.External.FileGuid));
                        targetNode = EntityNode.GetNodeFromEntity(asset.GetObject(target.External.ClassGuid), target.External.FileGuid, NodeWrangler);
                        
                        wrangler.AddNodeTransient(targetNode);
                    } break;
                }
                
                if (sourceNode.GetOutput(linkConnection.SourceField, ConnectionType.Link) == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        sourceNode.AddOutput(new LinkOutput(linkConnection.SourceField, sourceNode));
                    });
                }
                
                if (targetNode.GetInput(linkConnection.TargetField, ConnectionType.Link) == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        targetNode.AddInput(new LinkInput(linkConnection.TargetField, targetNode));
                    });
                }

                LinkOutput output = (LinkOutput)sourceNode.GetOutput(linkConnection.SourceField, ConnectionType.Link);
                LinkInput input = (LinkInput)targetNode.GetInput(linkConnection.TargetField, ConnectionType.Link);
                
                wrangler.AddConnectionTransient(output, input, linkConnection);
            }
            
            foreach (dynamic eventConnection in ((dynamic)wrangler.Asset.RootObject).EventConnections)
            {
                PointerRef source = eventConnection.Source;
                PointerRef target = eventConnection.Target;

                IObjectNode sourceNode = null;
                IObjectNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in event connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)source.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            sourceNode = wrangler.GetInterfaceNode(eventConnection.SourceEvent.Name, PortDirection.Out);
                        }
                        else
                        {
                            sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        // Import the node
                        EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(source.External.FileGuid));
                        sourceNode = EntityNode.GetNodeFromEntity(asset.GetObject(source.External.ClassGuid), source.External.FileGuid, NodeWrangler);

                        wrangler.AddNodeTransient(sourceNode);
                    } break;
                }
                
                switch (target.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in event connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        if (((dynamic)target.Internal).GetInstanceGuid() == wrangler.InterfaceGuid)
                        {
                            targetNode = wrangler.GetInterfaceNode(eventConnection.TargetEvent.Name, PortDirection.In);
                        }
                        else
                        {
                            targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
                        }
                    } break;
                    case PointerRefType.External:
                    {
                        // Import the node
                        EbxAsset asset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(target.External.FileGuid));
                        targetNode = EntityNode.GetNodeFromEntity(asset.GetObject(target.External.ClassGuid), target.External.FileGuid, NodeWrangler);
                        
                        wrangler.AddNodeTransient(targetNode);
                    } break;
                }
                
                if (sourceNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event) == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        sourceNode.AddOutput(new EventOutput(eventConnection.SourceEvent.Name, sourceNode));
                    });
                }
                
                if (targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event) == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        targetNode.AddInput(new EventInput(eventConnection.TargetEvent.Name, targetNode));
                    });
                }

                EventOutput output = (EventOutput)sourceNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event);
                EventInput input = (EventInput)targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event);

                wrangler.AddConnectionTransient(output, input, eventConnection);
            }

            #endregion

            if (LayoutManager.LayoutExists($"{assetEntry.Name}.lyt"))
            {
                LayoutManager.LoadLayoutRelative($"{assetEntry.Name}.lyt");
            }
            else
            {
                LayoutManager.SortLayout();
            }
        }

        #region Static

        public static List<Type> Types = new List<Type>();
        public static List<IVertex> NodeUtils = new List<IVertex>();

        static BlueprintGraphEditor()
        {
            NodifyEditor.EnableRenderingContainersOptimizations = true;
            NodifyEditor.OptimizeRenderingMinimumContainers = 10;
            NodifyEditor.OptimizeRenderingZoomOutPercent = 0.1;
            
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("GameDataContainer"))
            {
                Types.Add(type);
            }
            
            NodeUtils.Add(new EntityComment("Comment"));
        }

        #endregion

        #region Nodes

        #region Visuals

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object addedItem in e.AddedItems)
            {
                ((IVertex)addedItem).IsSelected = true;
            }

            foreach (object removedItem in e.RemovedItems)
            {
                ((IVertex)removedItem).IsSelected = false;
            }

            if (e.AddedItems.Count != 0)
            {
                switch (e.AddedItems[0])
                {
                    case EntityNode entityNode:
                    {
                        NodePropertyGrid.Object = entityNode.Object;
                    } break;
                    case EntityComment comment:
                    {
                        NodePropertyGrid.Object = comment.Object;
                    } break;
                    case InterfaceNode interfaceNode:
                    {
                        NodePropertyGrid.Object = interfaceNode.EditArgs;
                    } break;
                    case IObjectContainer container:
                    {
                        NodePropertyGrid.Object = container.Object;
                    } break;
                }
            }
            else
            {
                NodePropertyGrid.Object = new object();
            }
        }

        private void NodeFlatten_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            EntityNode node = (EntityNode)button.DataContext;
            node.IsFlatted = !node.IsFlatted;
        }

        #endregion

        private void NodePropertyGrid_OnOnModified(object sender, ItemModifiedEventArgs e)
        {
            // If the user holds down alt that means all selected nodes should have their properties set the same
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                FrostyTaskWindow.Show("Updating properties...", "", task =>
                {
                    foreach (IVertex selectedNode in NodeWrangler.SelectedNodes)
                    {
                        switch (selectedNode)
                        {
                            case EntityNode node:
                            {
                                node.TrySetProperty(e.Item.Name, e.NewValue);
                                node.OnObjectModified(sender, e);
                                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid).Name, ((EntityNodeWrangler)NodeWrangler).Asset);
                            } break;
                            case InterfaceNode interfaceNode:
                            {
                                interfaceNode.OnObjectModified(sender, e);
                                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid).Name, ((EntityNodeWrangler)NodeWrangler).Asset);
                            } break;
                            case EntityComment comment:
                            {
                                comment.OnObjectModified(sender, e);
                            } break;
                            case IObjectContainer container:
                            {
                                container.OnObjectModified(sender, e);
                            } break;
                        }
                    }
                });
            }
            else
            {
                switch (NodeWrangler.SelectedNodes[0])
                {
                    case EntityNode node:
                    {
                        node.OnObjectModified(sender, e);
                        App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid).Name, ((EntityNodeWrangler)NodeWrangler).Asset);
                    } break;
                    case InterfaceNode interfaceNode:
                    {
                        interfaceNode.OnObjectModified(sender, e);
                        App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid).Name, ((EntityNodeWrangler)NodeWrangler).Asset);
                    } break;
                    case EntityComment comment:
                    {
                        comment.OnObjectModified(sender, e);
                    } break;
                    case IObjectContainer container:
                    {
                        container.OnObjectModified(sender, e);
                    } break;
                }
            }
        }
        
        #region Adding & removing nodes
        private void DeleteNode_OnClick(object sender, RoutedEventArgs e)
        {
            List<IVertex> oldSelection = new List<IVertex>(NodeWrangler.SelectedNodes);
            foreach (IVertex selectedNode in oldSelection)
            {
                if (selectedNode is IRedirect redirect)
                {
                    if (redirect.SourceRedirect != null)
                    {
                        NodeWrangler.RemoveNode(redirect.SourceRedirect);
                    }
                    else
                    {
                        NodeWrangler.RemoveNode(redirect.TargetRedirect);
                    }
                }
                
                NodeWrangler.RemoveNode(selectedNode);
            }
        }
        
        private void DuplicateNode_OnClick(object sender, RoutedEventArgs e)
        {
            List<IVertex> oldSelection = new List<IVertex>(NodeWrangler.SelectedNodes);
            NodeWrangler.SelectedNodes.Clear();
            
            foreach (IVertex selectedNode in oldSelection)
            {
                if (selectedNode is EntityNode entityNode)
                {
                    FrostyClipboard.Current.SetData(entityNode.Object); // TODO: Work around, need to copy data
                    EntityNode newNode = EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
                    newNode.Location = new Point(selectedNode.Location.X + 15, selectedNode.Location.Y + 15);
                    NodeWrangler.AddNode(newNode);
                    NodeWrangler.SelectedNodes.Add(newNode);
                }

                if (selectedNode is InterfaceNode)
                {
                    App.Logger.LogError("Cannot duplicate interface nodes.");
                }
            }
        }

        #endregion
        
        private void PasteObject(object sender, RoutedEventArgs e)
        {
            // Item is not valid for pasting
            if (FrostyClipboard.Current.GetData() == null)
                return;
            
            if (!TypeLibrary.IsSubClassOf(FrostyClipboard.Current.GetData(), "GameDataContainer"))
                return;
            
            EntityNode node = EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
            node.Location = Editor.MouseLocation;
            
            NodeWrangler.AddNode(node);
            NodeWrangler.SelectedNodes.Add(node);
        }

        #endregion

        #region Class Selector

        private void ClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (ClassList.SelectedClass == null)
                return;
            
            EntityNode node = EntityNode.GetNodeFromEntity(ClassList.SelectedClass, NodeWrangler);
            node.Location = new Point(Editor.ViewportLocation.X + (575 / Editor.ViewportZoom), Editor.ViewportLocation.Y + 287.5 / Editor.ViewportZoom);
            NodeWrangler.AddNode(node);
        }

        private void TransClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (TransTypeList.SelectedItem == null)
                return;
            
            Type type = TransTypeList.SelectedItem.GetType();
            IVertex vertex = (IVertex)Activator.CreateInstance(type);
            vertex.Location = new Point(Editor.ViewportLocation.X + (575 / Editor.ViewportZoom), Editor.ViewportLocation.Y + 287.5 / Editor.ViewportZoom);
            
            NodeWrangler.AddNode(vertex);
        }
        
        private void SearchForClass_Click(object sender, RoutedEventArgs e)
        {
            ClassSelector classSelector = new ClassSelector(Types.ToArray());
            if (classSelector.ShowDialog() == true)
            {
                if (classSelector.SelectedClass == null)
                    return;
                
                EntityNode node = EntityNode.GetNodeFromEntity(classSelector.SelectedClass, NodeWrangler);
                node.Location = new Point(Editor.ViewportLocation.X + (575 / Editor.ViewportZoom), Editor.ViewportLocation.Y + 287.5 / Editor.ViewportZoom);
                NodeWrangler.AddNode(node);
            }
        }

        #endregion

        #region Layouts

        private void OrganizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            LayoutManager.SortLayout();
        }

        private void SaveOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid);
            LayoutManager.SaveLayout($"{assetEntry.Name}.lyt");
        }

        #endregion

        #region Visibility

        private void ControlsMenuVisible_OnClick(object sender, RoutedEventArgs e)
        {
            BlueprintEditorControlsWindow controlsWindow = new BlueprintEditorControlsWindow();
            controlsWindow.Show();
        }
        
        private void ToolboxVisible_OnClick(object sender, RoutedEventArgs e)
        {
            if (ToolboxPanel.Visibility == Visibility.Visible)
            {
                ToolboxPanel.Visibility = Visibility.Collapsed;
                ToolboxCollum.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                ToolboxPanel.Visibility = Visibility.Visible;
                ToolboxCollum.Width = new GridLength(225, GridUnitType.Pixel);
            }
        }
        
        private void PropertyGridVisible_OnClick(object sender, RoutedEventArgs e)
        {
            if (PropertyPanel.Visibility == Visibility.Visible)
            {
                PropertyPanel.Visibility = Visibility.Collapsed;
                PropertyCollum.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                PropertyPanel.Visibility = Visibility.Visible;
                PropertyCollum.Width = new GridLength(425, GridUnitType.Pixel);
            }
        }

        #endregion

        #region Controls

        private void Editor_OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                {
                    while (NodeWrangler.SelectedNodes.Count != 0)
                    {
                        NodeWrangler.RemoveNode(NodeWrangler.SelectedNodes[0]);
                    }
                } break;
                case Key.D when (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                {
                    List<IVertex> oldSelection = new List<IVertex>(NodeWrangler.SelectedNodes);
                    NodeWrangler.SelectedNodes.Clear();
            
                    foreach (IVertex selectedNode in oldSelection)
                    {
                        if (selectedNode is EntityNode entityNode)
                        {
                            FrostyClipboard.Current.SetData(entityNode.Object); // TODO: Work around, need to copy data
                            EntityNode newNode = EntityNode.GetNodeFromEntity(FrostyClipboard.Current.GetData(), NodeWrangler, true);
                            newNode.Location = new Point(selectedNode.Location.X + 15, selectedNode.Location.Y + 15);
                            NodeWrangler.AddNode(newNode);
                            NodeWrangler.SelectedNodes.Add(newNode);
                        }

                        if (selectedNode is InterfaceNode)
                        {
                            App.Logger.LogError("Cannot duplicate interface nodes.");
                        }
                    }
                } break;
                case Key.S when (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift:
                {
                    EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(((EntityNodeWrangler)NodeWrangler).Asset.FileGuid);
                    LayoutManager.SaveLayout($"{assetEntry.Name}.lyt");
                } break;
            }
        }

        private void Editor_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (TabControl.SelectedIndex == 0)
                {
                    if (ClassList.SelectedClass == null)
                        return;
                    
                    EntityNode node = EntityNode.GetNodeFromEntity(ClassList.SelectedClass, NodeWrangler);
                    node.Location = Editor.MouseLocation;
                    NodeWrangler.AddNode(node);
                }
                else
                {
                    if (TransTypeList.SelectedItem == null)
                        return;
            
                    Type type = TransTypeList.SelectedItem.GetType();
                    IVertex vertex = (IVertex)Activator.CreateInstance(type);
                    vertex.Location = Editor.MouseLocation;
            
                    NodeWrangler.AddNode(vertex);
                }
            }
        }

        #endregion
        
        private void ImportOrganizationButton_OnClick(object sender, RoutedEventArgs e)
        {
            FrostyOpenFileDialog ofd = new FrostyOpenFileDialog("Open Layout", "Blueprint Layout (*.lyt)|*.lyt|Text File (*.txt)|*.txt", "BlueprintLayout");
            if (!ofd.ShowDialog())
                return;

            LayoutManager.LoadLayout(ofd.FileName);
        }
    }
}