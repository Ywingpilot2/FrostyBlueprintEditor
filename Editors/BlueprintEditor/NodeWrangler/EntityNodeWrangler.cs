using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler
{
    public class EntityNodeWrangler : BaseNodeWrangler
    {
        public EbxAsset Asset { get; set; }
        public AssetClassGuid InterfaceGuid { get; set; }
        private readonly Dictionary<AssetClassGuid, EntityNode> _internalNodeCache = new Dictionary<AssetClassGuid, EntityNode>();
        private readonly Dictionary<(Guid, Guid), EntityNode> _externalNodeCache = new Dictionary<(Guid, Guid), EntityNode>();
        private readonly Dictionary<string, InterfaceNode> _interfaceInputCache = new Dictionary<string, InterfaceNode>();
        private readonly Dictionary<string, InterfaceNode> _interfaceOutputCache = new Dictionary<string, InterfaceNode>();

        #region Transient edits

        /// <summary>
        /// Add a node to the Graph Editor without editing EBX
        /// </summary>
        /// <param name="node"></param>
        public void AddNodeTransient(INode node)
        {
            switch (node)
            {
                case EntityNode entityNode when entityNode.Type == PointerRefType.Internal:
                {
                    if (_internalNodeCache.ContainsKey(entityNode.InternalGuid))
                    {
                        App.Logger.LogError("An item with the AssetClassGuid {0} has already been added!", entityNode.InternalGuid.ToString());
                        return;
                    }
                    _internalNodeCache.Add(entityNode.InternalGuid, entityNode);
                } break;
                case EntityNode entityNode:
                {
                    _externalNodeCache.Add((entityNode.FileGuid, entityNode.ClassGuid), entityNode);
                } break;
                case InterfaceNode interfaceNode:
                {
                    if (interfaceNode.Inputs.Count != 0)
                    {
                        if (_interfaceInputCache.ContainsKey(interfaceNode.Header))
                        {
                            App.Logger.LogError("An interface node with the name {0} has already been added!", interfaceNode.Header);
                            return;
                        }
                        _interfaceInputCache.Add(interfaceNode.Header, interfaceNode);
                    }
                    else
                    {
                        if (_interfaceOutputCache.ContainsKey(interfaceNode.Header))
                        {
                            App.Logger.LogError("An interface node with the name {0} has already been added!", interfaceNode.Header);
                            return;
                        }
                        _interfaceOutputCache.Add(interfaceNode.Header, interfaceNode);
                    }
                } break;
            }
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Nodes.Add(node);
            });
            
            // Incase extensions aren't threadsafe
            Application.Current.Dispatcher.Invoke(node.OnCreation);
        }

        #region Adding Connections

        public void AddConnectionTransient(EntityConnection connection)
        {
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
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
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
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
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Add(connection);
            });
        }

        #endregion

        #region Removing connections

        public void RemoveConnectionTransient(EntityConnection connection)
        {
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Connections.Remove(connection);
            });
        }

        #endregion

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

        #region Getting InterfaceNodes

        public InterfaceNode GetInterfaceNode(string name, PortDirection direction)
        {
            if (direction == PortDirection.In)
            {
                if (!_interfaceInputCache.ContainsKey(name))
                {
                    if (name.StartsWith("0x"))
                    {
                        int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        return GetInterfaceNode(Utils.GetString(hash), direction);
                    }

                    return null;
                }
                return _interfaceInputCache[name];
            }
            else
            {
                if (!_interfaceOutputCache.ContainsKey(name))
                {
                    if (name.StartsWith("0x"))
                    {
                        int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                        return GetInterfaceNode(Utils.GetString(hash), direction);
                    }
                    
                    return null;
                }
                return _interfaceOutputCache[name];
            }
        }

        #endregion

        #region Adding Nodes

        public override void AddNode(IVertex node)
        {
            switch (node)
            {
                case EntityNode entityNode:
                {
                    if (_internalNodeCache.ContainsKey(entityNode.InternalGuid))
                    {
                        App.Logger.LogError("An item with the AssetClassGuid {0} has already been added!", entityNode.InternalGuid.ToString());
                        return;
                    }
                    
                    _internalNodeCache.Add(entityNode.InternalGuid, entityNode);
                    Asset.AddObject(entityNode.Object);
                    PointerRef pointerRef = new PointerRef(entityNode.Object);
                    ((dynamic)Asset.RootObject).Objects.Add(pointerRef);
                    
                    App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(Asset.FileGuid).Name, Asset);
                } break;
            }
            
            Nodes.Add(node);
            
            node.OnCreation();
        }

        #endregion

        #region Removing Nodes

        public override void RemoveNode(IVertex node)
        {
            base.RemoveNode(node);
            switch (node)
            {
                case EntityNode entityNode when entityNode.Type == PointerRefType.Internal:
                {
                    _internalNodeCache.Remove(entityNode.InternalGuid);
                    Asset.RemoveObject(entityNode.Object);
                    PointerRef pointerRef = new PointerRef(entityNode.Object);
                    ((dynamic)Asset.RootObject).Objects.Remove(pointerRef);
                    App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(Asset.FileGuid).Name, Asset);
                } break;
                case InterfaceNode interfaceNode:
                {
                    throw new NotImplementedException();
                } break;
            }
        }

        #endregion

        #region Creating connections

        public override void AddConnection(IConnection connection)
        {
            base.AddConnection(connection);
            
            // If this connection is transient we shouldn't saved it
            if (connection is TransientConnection)
                return;
            
            EntityConnection entityConnection = (EntityConnection)connection;
            switch (entityConnection.Type)
            {
                case ConnectionType.Event:
                {
                    EventConnection eventConnection = ((EventConnection)connection);
                    ((dynamic)Asset.RootObject).EventConnections.Add((dynamic)eventConnection.Object);
                } break;
                case ConnectionType.Link:
                {
                    LinkConnection linkConnection = (LinkConnection)connection;
                    ((dynamic)Asset.RootObject).LinkConnections.Add((dynamic)linkConnection.Object);
                } break;
                case ConnectionType.Property:
                {
                    PropertyConnection propertyConnection = ((PropertyConnection)connection);
                    ((dynamic)Asset.RootObject).PropertyConnections.Add((dynamic)propertyConnection.Object);
                } break;
            }
            
            entityConnection.Source.Node.OnOutputUpdated(entityConnection.Source);
            entityConnection.Target.Node.OnOutputUpdated(entityConnection.Target);
            
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(Asset.FileGuid).Name, Asset);
        }

        #endregion

        #region Removing connections

        public override void RemoveConnection(IConnection connection)
        {
            base.RemoveConnection(connection);

            if (connection is TransientConnection)
                return;

            dynamic root = (dynamic)Asset.RootObject;
            EntityConnection entityConnection = (EntityConnection)connection;

            switch (entityConnection.Type)
            {
                case ConnectionType.Event:
                {
                    root.EventConnections.Remove((dynamic)entityConnection.Object);
                } break;
                case ConnectionType.Link:
                {
                    root.LinkConnections.Remove((dynamic)entityConnection.Object);
                } break;
                case ConnectionType.Property:
                {
                    root.PropertyConnections.Remove((dynamic)entityConnection.Object);
                } break;
            }
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(Asset.FileGuid).Name, Asset);
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
                
                if (source.Node is IRedirect || target.Node is IRedirect)
                    return;

                switch (Source.Direction)
                {
                    case PortDirection.Out when target.Direction == PortDirection.In:
                    {
                        switch (source.Type)
                        {
                            case ConnectionType.Event:
                            {
                                wrangler.AddConnection(new EventConnection((EventOutput)source, (EventInput)target));
                            }
                                break;
                            case ConnectionType.Link:
                            {
                                wrangler.AddConnection(new LinkConnection((LinkOutput)source, (LinkInput)target));
                            }
                                break;
                            case ConnectionType.Property:
                            {
                                wrangler.AddConnection(new PropertyConnection((PropertyOutput)source,
                                    (PropertyInput)target));
                            }
                                break;
                        }
                    } break;
                    case PortDirection.In when target.Direction == PortDirection.Out:
                    {
                        switch (source.Type)
                        {
                            case ConnectionType.Event:
                            {
                                wrangler.AddConnection(new EventConnection((EventOutput)target, (EventInput)source));
                            }
                                break;
                            case ConnectionType.Link:
                            {
                                wrangler.AddConnection(new LinkConnection((LinkOutput)target, (LinkInput)source));
                            }
                                break;
                            case ConnectionType.Property:
                            {
                                wrangler.AddConnection(new PropertyConnection((PropertyOutput)target,
                                    (PropertyInput)source));
                            }
                                break;
                        }
                    } break;
                }
            });
        }
    }
}