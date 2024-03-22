using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.BlueprintEditor.PropertyGrid;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.Algorithms;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using BlueprintEditorPlugin.Models.Status;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
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
            return true;
        }

        public bool IsValid(params object[] args)
        {
            return true;
        }

        #endregion

        public BlueprintGraphEditor()
        {
            NodeWrangler = new EntityNodeWrangler();
        }

        public void OpenAsset(EbxAssetEntry assetEntry)
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
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.In, NodeWrangler));
                            } break;
                            case "FieldAccessType_Target":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.Out, NodeWrangler));
                            } break;
                            case "FieldAccessType_SourceAndTarget":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.In, NodeWrangler));
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.Out, NodeWrangler));
                            } break;
                        }
                    }

                    foreach (dynamic inputEvent in ((dynamic)assetObject).InputEvents)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, inputEvent.Name, ConnectionType.Event, PortDirection.Out, NodeWrangler));
                    }
                    foreach (dynamic outputEvent in ((dynamic)assetObject).OutputEvents)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, outputEvent.Name, ConnectionType.Event, PortDirection.In, NodeWrangler));
                    }
                    
                    foreach (dynamic inputLink in ((dynamic)assetObject).InputLinks)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, inputLink.Name, ConnectionType.Link, PortDirection.Out, NodeWrangler));
                    }
                    foreach (dynamic outputLink in ((dynamic)assetObject).OutputLinks)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, outputLink.Name, ConnectionType.Link, PortDirection.In, NodeWrangler));
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
                    sourceNode.AddOutput(new PropertyOutput(propertyConnection.SourceField, sourceNode));
                }
                
                if (targetNode.GetInput(propertyConnection.TargetField, ConnectionType.Property) == null)
                {
                    targetNode.AddInput(new PropertyInput(propertyConnection.TargetField, targetNode));
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
                    sourceNode.AddOutput(new LinkOutput(linkConnection.SourceField, sourceNode));
                }
                
                if (targetNode.GetInput(linkConnection.TargetField, ConnectionType.Link) == null)
                {
                    targetNode.AddInput(new LinkInput(linkConnection.TargetField, targetNode));
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
                    sourceNode.AddOutput(new EventOutput(eventConnection.SourceEvent.Name, sourceNode));
                }
                
                if (targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event) == null)
                {
                    targetNode.AddInput(new EventInput(eventConnection.TargetEvent.Name, targetNode));
                }

                EventOutput output = (EventOutput)sourceNode.GetOutput(eventConnection.SourceEvent.Name, ConnectionType.Event);
                EventInput input = (EventInput)targetNode.GetInput(eventConnection.TargetEvent.Name, ConnectionType.Event);

                wrangler.AddConnectionTransient(output, input, eventConnection);
            }

            #endregion

            InitializeComponent();

            NodePropertyGrid.GraphEditor = this;
        }

        #region Static

        public static List<Type> Types = new List<Type>();
        public static List<IVertex> NodeUtils = new List<IVertex>();

        static BlueprintGraphEditor()
        {
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("GameDataContainer"))
            {
                Types.Add(type);
            }
            
            NodeUtils.Add(new EntityComment("Comment"));
        }

        #endregion

        #region Nodes

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
                if (e.AddedItems[0] is EntityNode entityNode)
                {
                    NodePropertyGrid.Object = entityNode.Object;
                }
                
                if (e.AddedItems[0] is EntityComment comment)
                {
                    NodePropertyGrid.Object = comment.Object;
                }

                if (e.AddedItems[0] is IObjectContainer container)
                {
                    NodePropertyGrid.Object = container.Object;
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
                    NodeWrangler.AddNode(newNode);
                    NodeWrangler.SelectedNodes.Add(newNode);
                }

                if (selectedNode is InterfaceNode interfaceNode)
                {
                    App.Logger.LogError("Cannot duplicate interface nodes.");
                }
            }
        }
        
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

        #endregion

        #region Class Selector

        private void ClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            NodeWrangler.AddNode(EntityNode.GetNodeFromEntity(ClassSelector.SelectedClass, NodeWrangler));
        }

        private void TransClassSelector_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (TransTypeList.SelectedItem == null)
                return;
            
            Type type = TransTypeList.SelectedItem.GetType();
            NodeWrangler.AddNode((IVertex)Activator.CreateInstance(type));
        }

        #endregion

        private void OrganizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Use ILayoutManager
            SugiyamaMethod method = new SugiyamaMethod(NodeWrangler.Connections.ToList(), NodeWrangler.Nodes.ToList());
            method.SortGraph();
        }
    }
}