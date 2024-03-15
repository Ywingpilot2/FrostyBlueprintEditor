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
                
                wrangler.AddNodeTransient(EntityNode.GetNodeFromEntity(assetObject, NodeWrangler));
            }

            #region Populating connections

            foreach (dynamic propertyConnection in ((dynamic)wrangler.Asset.RootObject).PropertyConnections)
            {
                PointerRef source = propertyConnection.Source;
                PointerRef target = propertyConnection.Target;

                EntityNode sourceNode = null;
                EntityNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in property connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
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
                        targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
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

                EntityNode sourceNode = null;
                EntityNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in link connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
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
                        targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
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

                EntityNode sourceNode = null;
                EntityNode targetNode = null;

                switch (source.Type)
                {
                    case PointerRefType.Null:
                    {
                        App.Logger.LogError("Pointer ref in event connection was null!");
                        continue;
                    }
                    case PointerRefType.Internal:
                    {
                        sourceNode = wrangler.GetEntityNode(((dynamic)source.Internal).GetInstanceGuid());
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
                        targetNode = wrangler.GetEntityNode(((dynamic)target.Internal).GetInstanceGuid());
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