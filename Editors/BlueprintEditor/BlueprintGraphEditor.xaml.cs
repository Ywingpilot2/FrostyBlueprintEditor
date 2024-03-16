using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.NodeTest.Nodes;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using Frosty.Core;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor
{
    public partial class BlueprintGraphEditor : UserControl, IGraphEditor
    {
        public INodeWrangler NodeWrangler { get; set; }

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
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.In));
                            } break;
                            case "FieldAccessType_Target":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.Out));
                            } break;
                            case "FieldAccessType_SourceAndTarget":
                            {
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.In));
                                wrangler.AddNodeTransient(new InterfaceNode(assetObject, field.Name, ConnectionType.Property, PortDirection.Out));
                            } break;
                        }
                    }

                    foreach (dynamic inputEvent in ((dynamic)assetObject).InputEvents)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, inputEvent.Name, ConnectionType.Event, PortDirection.Out));
                    }
                    foreach (dynamic outputEvent in ((dynamic)assetObject).OutputEvents)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, outputEvent.Name, ConnectionType.Event, PortDirection.In));
                    }
                    
                    foreach (dynamic inputLink in ((dynamic)assetObject).InputLinks)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, inputLink.Name, ConnectionType.Event, PortDirection.Out));
                    }
                    foreach (dynamic outputLink in ((dynamic)assetObject).OutputLinks)
                    {
                        wrangler.AddNodeTransient(new InterfaceNode(assetObject, outputLink.Name, ConnectionType.Event, PortDirection.In));
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
                        sourceNode = wrangler.GetEntityNode(source.External.FileGuid, source.External.ClassGuid);
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
                        targetNode = wrangler.GetEntityNode(target.External.FileGuid, target.External.ClassGuid);
                    } break;
                }
                
                if (sourceNode.GetOutput(propertyConnection.SourceField) == null)
                {
                    sourceNode.AddOutput(new PropertyOutput(propertyConnection.SourceField, sourceNode));
                }
                
                if (targetNode.GetInput(propertyConnection.TargetField) == null)
                {
                    targetNode.AddInput(new PropertyInput(propertyConnection.TargetField, targetNode));
                }

                PropertyOutput output = (PropertyOutput)sourceNode.GetOutput(propertyConnection.SourceField);
                PropertyInput input = (PropertyInput)targetNode.GetInput(propertyConnection.TargetField);
                
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
                        sourceNode = wrangler.GetEntityNode(source.External.FileGuid, source.External.ClassGuid);
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
                        targetNode = wrangler.GetEntityNode(target.External.FileGuid, target.External.ClassGuid);
                    } break;
                }
                
                if (sourceNode.GetOutput(linkConnection.SourceField) == null)
                {
                    sourceNode.AddOutput(new LinkOutput(linkConnection.SourceField, sourceNode));
                }
                
                if (targetNode.GetInput(linkConnection.TargetField) == null)
                {
                    targetNode.AddInput(new LinkInput(linkConnection.TargetField, targetNode));
                }

                LinkOutput output = (LinkOutput)sourceNode.GetOutput(linkConnection.SourceField);
                LinkInput input = (LinkInput)targetNode.GetInput(linkConnection.TargetField);
                
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
                        sourceNode = wrangler.GetEntityNode(source.External.FileGuid, source.External.ClassGuid);
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
                        targetNode = wrangler.GetEntityNode(target.External.FileGuid, target.External.ClassGuid);
                    } break;
                }
                
                if (sourceNode.GetOutput(eventConnection.SourceEvent.Name) == null)
                {
                    sourceNode.AddOutput(new EventOutput(eventConnection.SourceEvent.Name, sourceNode));
                }
                
                if (targetNode.GetInput(eventConnection.TargetEvent.Name) == null)
                {
                    targetNode.AddInput(new EventInput(eventConnection.TargetEvent.Name, targetNode));
                }

                EventOutput output = (EventOutput)sourceNode.GetOutput(eventConnection.SourceEvent.Name);
                EventInput input = (EventInput)targetNode.GetInput(eventConnection.TargetEvent.Name);

                wrangler.AddConnectionTransient(output, input, eventConnection);
            }

            #endregion
        }

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
        }

        private void NodeFlatten_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            EntityNode node = (EntityNode)button.DataContext;
            node.IsFlatted = !node.IsFlatted;
        }
    }
}