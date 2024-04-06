using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Connections.Pending;
using BlueprintEditorPlugin.Models.Entities.Networking;
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

        private readonly Dictionary<string, InterfaceNode> _interfacePICache = new Dictionary<string, InterfaceNode>();
        private readonly Dictionary<string, InterfaceNode> _interfaceLICache = new Dictionary<string, InterfaceNode>();
        private readonly Dictionary<string, InterfaceNode> _interfaceEICache = new Dictionary<string, InterfaceNode>();
        private readonly Dictionary<string, InterfaceNode> _interfacePOCache = new Dictionary<string, InterfaceNode>();
        private readonly Dictionary<string, InterfaceNode> _interfaceLOCache = new Dictionary<string, InterfaceNode>();
        private readonly Dictionary<string, InterfaceNode> _interfaceEOCache = new Dictionary<string, InterfaceNode>();

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
                    if (_externalNodeCache.ContainsKey((entityNode.FileGuid, entityNode.ClassGuid)))
                    {
                        App.Logger.LogError("Multiple imported items with the same guids detected!");
                        return;
                    }
                    _externalNodeCache.Add((entityNode.FileGuid, entityNode.ClassGuid), entityNode);
                } break;
                case InterfaceNode interfaceNode:
                {
                    if (interfaceNode.Inputs.Count != 0)
                    {
                        switch (interfaceNode.ConnectionType)
                        {
                            case ConnectionType.Event:
                            {
                                if (_interfaceEICache.ContainsKey(interfaceNode.Header))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                _interfaceEICache.Add(interfaceNode.Header, interfaceNode);
                            } break;
                            case ConnectionType.Link:
                            {
                                if (_interfaceLICache.ContainsKey(interfaceNode.Header))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                _interfaceLICache.Add(interfaceNode.Header, interfaceNode);
                            } break;
                            case ConnectionType.Property:
                            {
                                if (_interfacePICache.ContainsKey(interfaceNode.Header))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                _interfacePICache.Add(interfaceNode.Header, interfaceNode);
                            } break;
                        }
                    }
                    else
                    {
                        switch (interfaceNode.ConnectionType)
                        {
                            case ConnectionType.Event:
                            {
                                if (_interfaceEOCache.ContainsKey(interfaceNode.Header))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                _interfaceEOCache.Add(interfaceNode.Header, interfaceNode);
                            } break;
                            case ConnectionType.Link:
                            {
                                if (_interfaceLOCache.ContainsKey(interfaceNode.Header))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                _interfaceLOCache.Add(interfaceNode.Header, interfaceNode);
                            } break;
                            case ConnectionType.Property:
                            {
                                if (_interfacePOCache.ContainsKey(interfaceNode.Header))
                                {
                                    App.Logger.LogError("Multiple copies of {0} interfaces exist!", interfaceNode.Header);
                                }
                                _interfacePOCache.Add(interfaceNode.Header, interfaceNode);
                            } break;
                        }
                    }
                } break;
            }
            
            // TODO: This is a work around to fix UI being on a different thread, causing crashes
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Add(node);
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

        public InterfaceNode GetInterfaceNode(string name, PortDirection direction, ConnectionType type)
        {
            if (direction == PortDirection.In)
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        if (!_interfaceEICache.ContainsKey(name))
                        {
                            if (name.StartsWith("0x"))
                            {
                                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                                return GetInterfaceNode(Utils.GetString(hash), direction, type);
                            }

                            return null;
                        }
                        return _interfaceEICache[name];
                    }
                    case ConnectionType.Link:
                    {
                        if (!_interfaceLICache.ContainsKey(name))
                        {
                            if (name.StartsWith("0x"))
                            {
                                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                                return GetInterfaceNode(Utils.GetString(hash), direction, type);
                            }

                            return null;
                        }
                        return _interfaceLICache[name];
                    }
                    case ConnectionType.Property:
                    {
                        if (!_interfacePICache.ContainsKey(name))
                        {
                            if (name.StartsWith("0x"))
                            {
                                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                                return GetInterfaceNode(Utils.GetString(hash), direction, type);
                            }

                            return null;
                        }
                        return _interfacePICache[name];
                    }
                }
            }
            else
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        if (!_interfaceEOCache.ContainsKey(name))
                        {
                            if (name.StartsWith("0x"))
                            {
                                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                                return GetInterfaceNode(Utils.GetString(hash), direction, type);
                            }

                            return null;
                        }
                        return _interfaceEOCache[name];
                    }
                    case ConnectionType.Link:
                    {
                        if (!_interfaceLOCache.ContainsKey(name))
                        {
                            if (name.StartsWith("0x"))
                            {
                                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                                return GetInterfaceNode(Utils.GetString(hash), direction, type);
                            }

                            return null;
                        }
                        return _interfaceLOCache[name];
                    }
                    case ConnectionType.Property:
                    {
                        if (!_interfacePOCache.ContainsKey(name))
                        {
                            if (name.StartsWith("0x"))
                            {
                                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                                return GetInterfaceNode(Utils.GetString(hash), direction, type);
                            }

                            return null;
                        }
                        return _interfacePOCache[name];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the input interface cache so that the keys align with the names of the interfaces
        /// </summary>
        public void UpdateInputInterfaceCache()
        {
            // Properties
            List<string> keysToUpdate = new List<string>();
            foreach (KeyValuePair<string,InterfaceNode> keyValuePair in _interfacePICache)
            {
                if (keyValuePair.Key == keyValuePair.Value.Header)
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (string key in keysToUpdate)
            {
                InterfaceNode node = _interfacePICache[key];
                _interfacePICache.Remove(key);
                _interfacePICache.Add(node.Header, node);
            }
            
            // Links
            keysToUpdate.Clear();
            foreach (KeyValuePair<string,InterfaceNode> keyValuePair in _interfaceLICache)
            {
                if (keyValuePair.Key == keyValuePair.Value.Header)
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (string key in keysToUpdate)
            {
                InterfaceNode node = _interfaceLICache[key];
                _interfaceLICache.Remove(key);
                _interfaceLICache.Add(node.Header, node);
            }
            
            // Events
            keysToUpdate.Clear();
            foreach (KeyValuePair<string,InterfaceNode> keyValuePair in _interfaceEICache)
            {
                if (keyValuePair.Key == keyValuePair.Value.Header)
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (string key in keysToUpdate)
            {
                InterfaceNode node = _interfaceEICache[key];
                _interfaceEICache.Remove(key);
                _interfaceEICache.Add(node.Header, node);
            }
        }
        
        /// <summary>
        /// Updates the input interface cache so that the keys align with the names of the interfaces
        /// </summary>
        public void UpdateOutputInterfaceCache()
        {
            // Properties
            List<string> keysToUpdate = new List<string>();
            foreach (KeyValuePair<string,InterfaceNode> keyValuePair in _interfacePOCache)
            {
                if (keyValuePair.Key == keyValuePair.Value.Header)
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (string key in keysToUpdate)
            {
                InterfaceNode node = _interfacePOCache[key];
                _interfacePOCache.Remove(key);
                _interfacePOCache.Add(node.Header, node);
            }
            
            // Links
            keysToUpdate.Clear();
            foreach (KeyValuePair<string,InterfaceNode> keyValuePair in _interfaceLOCache)
            {
                if (keyValuePair.Key == keyValuePair.Value.Header)
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (string key in keysToUpdate)
            {
                InterfaceNode node = _interfaceLOCache[key];
                _interfaceLOCache.Remove(key);
                _interfaceLOCache.Add(node.Header, node);
            }
            
            // Events
            keysToUpdate.Clear();
            foreach (KeyValuePair<string,InterfaceNode> keyValuePair in _interfaceEOCache)
            {
                if (keyValuePair.Key == keyValuePair.Value.Header)
                    continue;
                
                keysToUpdate.Add(keyValuePair.Key);
            }

            foreach (string key in keysToUpdate)
            {
                InterfaceNode node = _interfaceEOCache[key];
                _interfaceEOCache.Remove(key);
                _interfaceEOCache.Add(node.Header, node);
            }
        }

        #endregion

        #region Adding Nodes

        public override void AddVertex(IVertex node)
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
                case InterfaceNode interfaceNode:
                {
                    switch (interfaceNode.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            dynamic intrfc = ((dynamic)Asset.RootObject).Interface.Internal;
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                intrfc.OutputEvents.Add((dynamic)interfaceNode.SubObject);
                                _interfaceEICache.Add(interfaceNode.Header, interfaceNode);
                            }
                            else
                            {
                                intrfc.InputEvents.Add((dynamic)interfaceNode.SubObject);
                                _interfaceEOCache.Add(interfaceNode.Header, interfaceNode);
                            }
                        } break;
                        case ConnectionType.Link:
                        {
                            dynamic intrfc = ((dynamic)Asset.RootObject).Interface.Internal;
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                intrfc.OutputLinks.Add((dynamic)interfaceNode.SubObject);
                                _interfaceLICache.Add(interfaceNode.Header, interfaceNode);
                            }
                            else
                            {
                                intrfc.InputLinks.Add((dynamic)interfaceNode.SubObject);
                                _interfaceLOCache.Add(interfaceNode.Header, interfaceNode);
                            }
                        } break;
                        case ConnectionType.Property:
                        {
                            dynamic intrfc = ((dynamic)Asset.RootObject).Interface.Internal;
                            intrfc.Fields.Add((dynamic)interfaceNode.SubObject);
                            
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                _interfacePICache.Add(interfaceNode.Header, interfaceNode);
                            }
                            else
                            {
                                _interfacePOCache.Add(interfaceNode.Header, interfaceNode);
                            }
                        } break;
                    }
                    
                    App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(Asset.FileGuid).Name, Asset);
                } break;
            }

            // TODO: Stupid threading bullshit won't let me access this because it's on a UI thread
            // Asshole!
            Application.Current.Dispatcher.Invoke(() =>
            {
                Vertices.Add(node);
            });

            node.OnCreation();
        }

        #endregion

        #region Removing Nodes

        public override void RemoveVertex(IVertex node)
        {
            base.RemoveVertex(node);
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
                    switch (interfaceNode.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                _interfaceEICache.Remove(interfaceNode.Header);
                                ((dynamic)interfaceNode.Object).OutputEvents.Remove((dynamic)interfaceNode.SubObject);
                            }
                            else
                            {
                                _interfaceEOCache.Remove(interfaceNode.Header);
                                ((dynamic)interfaceNode.Object).InputEvents.Remove((dynamic)interfaceNode.SubObject);
                            }
                        } break;
                        case ConnectionType.Link:
                        {
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                _interfaceLICache.Remove(interfaceNode.Header);
                                ((dynamic)interfaceNode.Object).OutputLinks.Remove((dynamic)interfaceNode.SubObject);
                            }
                            else
                            {
                                _interfaceLOCache.Remove(interfaceNode.Header);
                                ((dynamic)interfaceNode.Object).InputLinks.Remove((dynamic)interfaceNode.SubObject);
                            }
                        } break;
                        case ConnectionType.Property:
                        {
                            if (interfaceNode.Direction == PortDirection.In)
                            {
                                _interfacePICache.Remove(interfaceNode.Header);
                                ((dynamic)interfaceNode.Object).Fields.Remove((dynamic)interfaceNode.SubObject);
                                if (((dynamic)interfaceNode.SubObject).AccessType.ToString() == "FieldAccessType_SourceAndTarget")
                                {
                                    InterfaceNode interfaceNode2 = GetInterfaceNode(interfaceNode.Header, PortDirection.Out, ConnectionType.Property);
                                    
                                    if (interfaceNode2 == null)
                                        break;
                                    
                                    RemoveVertex(interfaceNode2);
                                }
                            }
                            else
                            {
                                _interfacePOCache.Remove(interfaceNode.Header);
                                ((dynamic)interfaceNode.Object).Fields.Remove((dynamic)interfaceNode.SubObject);
                                if (((dynamic)interfaceNode.SubObject).AccessType.ToString() == "FieldAccessType_SourceAndTarget")
                                {
                                    InterfaceNode interfaceNode2 = GetInterfaceNode(interfaceNode.Header, PortDirection.In, ConnectionType.Property);
                                    
                                    if (interfaceNode2 == null)
                                        break;
                                    
                                    RemoveVertex(interfaceNode2);
                                }
                            }
                        } break;
                    }
                } break;
            }
        }

        #endregion

        #region Getting connections

        /// <summary>
        /// Gets a list of real connections, so not <see cref="TransientConnection"/>
        /// </summary>
        /// <returns></returns>
        public List<EntityConnection> GetRealConnections()
        {
            List<EntityConnection> entityConnections = new List<EntityConnection>();

            foreach (EntityConnection connection in Connections)
            {
                if (connection is TransientConnection)
                    continue;
                
                entityConnections.Add(connection);
            }

            return entityConnections;
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
                
                if (source.RedirectNode != null || target.RedirectNode != null)
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