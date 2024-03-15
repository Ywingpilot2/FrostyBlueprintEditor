using System;
using System.Collections.Generic;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler
{
    public class EntityNodeWrangler : BaseNodeWrangler
    {
        public EbxAsset Asset { get; set; }
        private readonly Dictionary<AssetClassGuid, EntityNode> _internalNodeCache = new Dictionary<AssetClassGuid, EntityNode>();
        private readonly Dictionary<(Guid, Guid), EntityNode> _externalNodeCache = new Dictionary<(Guid, Guid), EntityNode>();

        #region Transient edits

        /// <summary>
        /// Add a node to the Graph Editor without editing EBX
        /// </summary>
        /// <param name="node"></param>
        public void AddNodeTransient(INode node)
        {
            Nodes.Add(node);

            if (node is EntityNode entityNode)
            {
                if (entityNode.Type == PointerRefType.Internal)
                {
                    _internalNodeCache.Add(entityNode.InternalGuid, entityNode);
                }
                else
                {
                    _externalNodeCache.Add((entityNode.FileGuid, entityNode.ClassGuid), entityNode);
                }
            }
        }

        public void AddConnectionTransient(EntityConnection connection)
        {
            Connections.Add(connection);
        }
        
        public void AddConnectionTransient(EntityOutput source, EntityInput target, object obj)
        {
            EntityConnection connection = null;
            switch (source.Type)
            {
                case ConnectionType.Event:
                {
                    connection = new EventConnection((EventOutput)source, (EventInput)target, obj);
                } break;
                case ConnectionType.Link:
                {
                    connection = new LinkConnection((LinkOutput)source, (LinkInput)target, obj);
                } break;
                case ConnectionType.Property:
                {
                    connection = new PropertyConnection((PropertyOutput)source, (PropertyInput)target, obj);
                } break;
            }
            
            if (source.Realm == Realm.Invalid)
            {
                source.Realm = connection.Realm;
            }

            if (target.Realm == Realm.Invalid)
            {
                target.Realm = connection.Realm;
            }
            
            Connections.Add(connection);
        }
        
        public void AddConnectionTransient(EntityOutput source, EntityInput target)
        {
            EntityConnection connection = null;
            switch (source.Type)
            {
                case ConnectionType.Event:
                {
                    connection = new EventConnection((EventOutput)source, (EventInput)target);
                } break;
                case ConnectionType.Link:
                {
                    connection = new LinkConnection((LinkOutput)source, (LinkInput)target);
                } break;
                case ConnectionType.Property:
                {
                    connection = new PropertyConnection((PropertyOutput)source, (PropertyInput)target);
                } break;
            }
            
            Connections.Add(connection);
        }

        #endregion

        #region Getting entity nodes

        public EntityNode GetEntityNode(AssetClassGuid internalId)
        {
            if (!_internalNodeCache.ContainsKey(internalId))
                return null;
            
            return _internalNodeCache[internalId];
        }

        public EntityNode GetEntityNode(Guid fileGuid, Guid assetClassGuid)
        {
            if (!_externalNodeCache.ContainsKey((fileGuid, assetClassGuid)))
                return null;
            
            return _externalNodeCache[(fileGuid, assetClassGuid)];
        }

        public EntityNode GetEntityNode(Guid fileGuid, AssetClassGuid internalId)
        {
            if (!_externalNodeCache.ContainsKey((fileGuid, internalId.ExportedGuid)))
                return null;
            
            return _externalNodeCache[(fileGuid, internalId.ExportedGuid)];
        }

        #endregion

        #region Creating connections

        public override void AddConnection(IConnection connection)
        {
            base.AddConnection(connection);
            EntityConnection entityConnection = (EntityConnection)connection;
            switch (entityConnection.Type)
            {
                case ConnectionType.Event:
                {
                    ((dynamic)Asset.RootObject).EventConnections.Add(((EventConnection)connection).Object);
                } break;
                case ConnectionType.Link:
                {
                    ((dynamic)Asset.RootObject).LinkConnections.Add(((LinkConnection)connection).Object);
                } break;
                case ConnectionType.Property:
                {
                    ((dynamic)Asset.RootObject).PropertyConnections.Add(((PropertyConnection)connection).Object);
                } break;
            }
        }

        #endregion

        public EntityNodeWrangler()
        {
            PendingConnection = new EntityPendingConnection(this);
            RemoveConnectionsCommand = new DelegateCommand<IPort>(ClearConnections);
        }
    }
    
    public class EntityPendingConnection : BasePendingConnection
    {
        public ConnectionType Type { get; set; }
        
        public EntityPendingConnection(EntityNodeWrangler wrangler) : base(wrangler)
        {
            Start = new DelegateCommand<IPort>(source =>
            {
                Source = source;
                Type = ((EntityPort)source).Type;
                NotifyPropertyChanged(nameof(Type));
                NotifyPropertyChanged(nameof(CurvePoint1));
                NotifyPropertyChanged(nameof(CurvePoint2));
            });
            Finish = new DelegateCommand<IPort>(target =>
            {
                if (target == null)
                    return;
                EntityPort source = (EntityPort)Source;

                if (source.Type != ((EntityPort)target).Type)
                    return;

                if (Source.Direction == PortDirection.Out && target.Direction == PortDirection.In)
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Event:
                        {
                            wrangler.AddConnection(new EventConnection((EventOutput)source, (EventInput)target));
                        } break;
                        case ConnectionType.Link:
                        {
                            wrangler.AddConnection(new LinkConnection((LinkOutput)source, (LinkInput)target));
                        } break;
                        case ConnectionType.Property:
                        {
                            wrangler.AddConnection(new PropertyConnection((PropertyOutput)source, (PropertyInput)target));
                        } break;
                    }
                }
                else if (Source.Direction == PortDirection.In && target.Direction == PortDirection.Out)
                {
                    switch (source.Type)
                    {
                        case ConnectionType.Event:
                        {
                            wrangler.AddConnection(new EventConnection((EventOutput)target, (EventInput)source));
                        } break;
                        case ConnectionType.Link:
                        {
                            wrangler.AddConnection(new LinkConnection((LinkOutput)target, (LinkInput)source));
                        } break;
                        case ConnectionType.Property:
                        {
                            wrangler.AddConnection(new PropertyConnection((PropertyOutput)target, (PropertyInput)source));
                        } break;
                    }
                }
            });
        }
    }
}