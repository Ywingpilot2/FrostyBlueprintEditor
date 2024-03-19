using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    /// <summary>
    /// A basic implementation of an entity in a node form. For creation, please see <see cref="GetNodeFromEntity(object,BlueprintEditorPlugin.Editors.NodeWrangler.INodeWrangler)"/>
    /// </summary>
    public class EntityNode : IObjectNode, INetworked
    {
        #region Generic node implementation

        #region Text Data

        private string _header;
        
        /// <summary>
        /// The text displayed at the top of the node. By default set as the Types name.
        /// </summary>
        public virtual string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }
        
        private string _footer;
        
        /// <summary>
        /// Smaller subtext displayed below the <see cref="Header"/>
        /// </summary>
        public virtual string Footer
        {
            get => _footer;
            set
            {
                _footer = value;
                NotifyPropertyChanged(nameof(Footer));
            }
        }

        private string _toolTip;
        
        /// <summary>
        /// The tooltip displayed when hovering over the node. Also used for the docbox
        /// </summary>
        public virtual string ToolTip
        {
            get => _toolTip;
            set
            {
                _toolTip = value;
                NotifyPropertyChanged(nameof(ToolTip));
            }
        }

        #endregion

        #region Positional data

        public Size Size { get; set; }

        private bool _selected;
        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        private bool _isFlatted;
        public bool IsFlatted
        {
            get => _isFlatted;
            set
            {
                _isFlatted = value;
                NotifyPropertyChanged(nameof(IsFlatted));
            }
        }
        
        private Point _location;
        public Point Location
        {
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
            get => _location;
        }

        #endregion

        #region Node data

        public ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();

        public INodeWrangler NodeWrangler { get; }

        #endregion

        #region Commands

        public ICommand CopyCommand => new DelegateCommand(Copy);

        private protected void Copy()
        {
            FrostyClipboard.Current.SetData(Object);
        }
        
        public ICommand AddPortCommand => new DelegateCommand(UserAddPort);

        private protected void UserAddPort()
        {
            AddPortArgs args = new AddPortArgs();
            MessageBoxResult result = EditPromptWindow.Show(args, "Set new Properties");
            if (result == MessageBoxResult.Yes)
            {
                if (args.Direction == PortDirection.In)
                {
                    switch (args.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            AddPort(new EventInput(args.Name, this)
                            {
                                HasPlayer = args.HasPlayer,
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Link:
                        {
                            AddPort(new LinkInput(args.Name, this)
                            {
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Property:
                        {
                            AddPort(new PropertyInput(args.Name, this)
                            {
                                IsInterface = args.IsInterface,
                                Realm = args.Realm
                            });
                        } break;
                    }
                }
                else
                {
                    switch (args.ConnectionType)
                    {
                        case ConnectionType.Event:
                        {
                            AddPort(new EventOutput(args.Name, this)
                            {
                                HasPlayer = args.HasPlayer,
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Link:
                        {
                            AddPort(new LinkOutput(args.Name, this)
                            {
                                Realm = args.Realm
                            });
                        } break;
                        case ConnectionType.Property:
                        {
                            AddPort(new PropertyOutput(args.Name, this)
                            {
                                IsInterface = args.IsInterface,
                                Realm = args.Realm
                            });
                        } break;
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Entity Info

        private Realm _realm;
        public virtual Realm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                NotifyPropertyChanged(nameof(Realm));
            }
        }

        public object Object { get; }
        public virtual string ObjectType { get; }

        private bool _hasPlayerEvent;
        public virtual bool HasPlayerEvent
        {
            get => _hasPlayerEvent;
            set
            {
                _hasPlayerEvent = value;
                NotifyPropertyChanged(nameof(HasPlayerEvent));
            }
        }

        #region Location

        public PointerRefType Type { get; }
        
        // Internal
        public AssetClassGuid InternalGuid { get; set; }
        
        // External
        public Guid FileGuid { get; }
        public Guid ClassGuid { get; }

        #endregion

        #region Networked implementation

        public Realm ParseRealm(object obj)
        {
            switch (obj.ToString())
            {
                case "Realm_Server":
                {
                    return Realm.Server;
                }
                case "Realm_Client":
                {
                    return Realm.Client;
                }
                case "Realm_ClientAndServer":
                {
                    return Realm.ClientAndServer;
                }
                default:
                {
                    return Realm.Invalid;
                }
            }
        }

        
        public virtual Realm DetermineRealm(bool ignoreCurrent = false)
        {
            Realm realm = Realm;

            if ((Realm == Realm.Any || Realm == Realm.Invalid) || ignoreCurrent)
            {
                foreach (EntityConnection connection in NodeWrangler.GetConnections(this))
                {
                    if (connection.Realm != Realm.Any && connection.Realm != Realm.Invalid)
                    {
                        realm = connection.Realm;
                        return realm;
                    }
                }
            }

            return realm;
        }

        public virtual void FixRealm()
        {
            Realm = DetermineRealm();
        }
        
        public virtual void ForceFixRealm()
        {
            Realm = DetermineRealm(true);
        }

        #endregion

        #endregion

        #region Basic node implementation

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public virtual bool IsValid()
        {
            return true;
        }

        public virtual void OnCreation()
        {
            Header = ObjectType;
        }

        public virtual void OnDestruction()
        {
        }

        public virtual void OnInputUpdated(IPort port)
        {
            EntityPort entityPort = (EntityPort)port;
            entityPort.FixRealm();

            object value = TryGetProperty("flags");
            if (value == null)
                return;

            ObjectFlagsHelper flagsHelper = new ObjectFlagsHelper((uint)value);
            
            switch (entityPort.Realm)
            {
                case Realm.NetworkedClientAndServer when entityPort.Type == ConnectionType.Event:
                case Realm.ClientAndServer when entityPort.Type == ConnectionType.Event:
                {
                    flagsHelper.ClientEvent = true;
                    flagsHelper.ServerEvent = true;
                } break;
                case Realm.NetworkedClient when entityPort.Type == ConnectionType.Event:
                case Realm.Client when entityPort.Type == ConnectionType.Event:
                {
                    flagsHelper.ClientEvent = true;
                } break;
                case Realm.Server when entityPort.Type == ConnectionType.Event:
                {
                    flagsHelper.ServerEvent = true;
                } break;
                
                case Realm.NetworkedClientAndServer when entityPort.Type == ConnectionType.Property:
                case Realm.ClientAndServer when entityPort.Type == ConnectionType.Property:
                {
                    flagsHelper.ClientEvent = true;
                    flagsHelper.ServerEvent = true;
                } break;
                case Realm.NetworkedClient when entityPort.Type == ConnectionType.Property:
                case Realm.Client when entityPort.Type == ConnectionType.Property:
                {
                    flagsHelper.ClientEvent = true;
                } break;
                case Realm.Server when entityPort.Type == ConnectionType.Property:
                {
                    flagsHelper.ServerEvent = true;
                } break;
            }
            
            TrySetProperty("flags", flagsHelper.GetAsFlags());
        }

        public virtual void OnOutputUpdated(IPort port)
        {
            EntityPort entityPort = (EntityPort)port;
            entityPort.FixRealm();
            
            if (entityPort.Type != ConnectionType.Link)
                return;
            
            object value = TryGetProperty("flags");
            if (value == null)
                return;
            
            ObjectFlagsHelper flagsHelper = new ObjectFlagsHelper((uint)value);
            
            switch (entityPort.Realm)
            {
                case Realm.NetworkedClientAndServer:
                case Realm.ClientAndServer:
                {
                    flagsHelper.ClientLinkSource = true;
                    flagsHelper.ServerLinkSource = true;
                } break;
                case Realm.NetworkedClient:
                case Realm.Client:
                {
                    flagsHelper.ClientLinkSource = true;
                } break;
                case Realm.Server:
                {
                    flagsHelper.ServerLinkSource = true;
                } break;
            }

            TrySetProperty("flags", flagsHelper.GetAsFlags());
        }
        
        public virtual void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            if (args.Item.Name == "Realm" && TryGetProperty("Realm") != null)
            {
                Realm = ParseRealm(args.NewValue);
                Realm oldRealm = ParseRealm(args.OldValue);
                foreach (EntityPort input in Inputs)
                {
                    if (input.Realm == oldRealm)
                    {
                        input.Realm = Realm;
                    }
                }
                foreach (EntityPort output in Outputs)
                {
                    if (output.Realm == oldRealm)
                    {
                        output.Realm = Realm;
                    }
                }
            }
        }

        #endregion

        #region Complex node implementation

        private Dictionary<int, EntityInput> _hashCacheInputs = new Dictionary<int, EntityInput>();
        private Dictionary<int, EntityOutput> _hashCacheOutputs = new Dictionary<int, EntityOutput>();

        #region Port retrieval

        public EntityInput GetInput(string name)
        {
            if (name.StartsWith("0x"))
            {
                return GetInput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier));
            }
            
            if (!_hashCacheInputs.ContainsKey(Utils.HashString(name)))
                return null;

            return _hashCacheInputs[Utils.HashString(name)];
        }

        public EntityInput GetInput(int hash)
        {
            if (!_hashCacheInputs.ContainsKey(hash))
                return null;

            return _hashCacheInputs[hash];
        }
        
        public EntityOutput GetOutput(string name)
        {
            if (name.StartsWith("0x"))
            {
                return GetOutput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier));
            }
            
            if (!_hashCacheOutputs.ContainsKey(Utils.HashString(name)))
                return null;

            return _hashCacheOutputs[Utils.HashString(name)];
        }

        public EntityOutput GetOutput(int hash)
        {
            if (!_hashCacheOutputs.ContainsKey(hash))
                return null;

            return _hashCacheOutputs[hash];
        }

        #endregion

        #region Port adding
        
        public void AddPort(IPort port)
        {
            if (port is EntityOutput output)
            {
                AddOutput(output);
            }
            else
            {
                AddInput((EntityInput)port); // Has to be an entity input
            }
        }

        public virtual void AddInput(EntityInput input)
        {
            if (input.Name.StartsWith("0x"))
            {
                int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                if (_hashCacheInputs.ContainsKey(hash))
                    return;
                
                Inputs.Add(input);
                _hashCacheInputs.Add(hash, input);
                return;
            }
            
            if (_hashCacheInputs.ContainsKey(Utils.HashString(input.Name)))
                return;

            if (TryGetProperty("Realm") != null && input.Realm == Realm.Invalid)
            {
                input.Realm = Realm;
            }
            
            Inputs.Add(input);
            _hashCacheInputs.Add(Utils.HashString(input.Name), input);
        }

        public virtual void AddOutput(EntityOutput output)
        {
            if (output.Name.StartsWith("0x"))
            {
                int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                if (_hashCacheOutputs.ContainsKey(hash))
                    return;
                
                Outputs.Add(output);
                _hashCacheOutputs.Add(hash, output);
                return;
            }
            
            if (_hashCacheOutputs.ContainsKey(Utils.HashString(output.Name)))
                return;
            
            if (TryGetProperty("Realm") != null && output.Realm == Realm.Invalid)
            {
                output.Realm = Realm;
            }
            
            Outputs.Add(output);
            _hashCacheOutputs.Add(Utils.HashString(output.Name), output);
        }

        #endregion

        #region Port Removing

        public void RemoveInput(string name)
        {
            EntityInput input = GetInput(name);
            if (input == null)
                return;
            
            RemoveInput(input);
        }
        
        public void RemoveInput(EntityInput input)
        {
            NodeWrangler.ClearConnections(input);
            _hashCacheInputs.Remove(Utils.HashString(input.Name));
            Inputs.Remove(input);
        }
        
        public void RemoveOutput(string name)
        {
            EntityOutput output = GetOutput(name);
            if (output == null)
                return;
            
            RemoveOutput(output);
        }
        
        public void RemoveOutput(EntityOutput output)
        {
            NodeWrangler.ClearConnections(output);
            _hashCacheOutputs.Remove(Utils.HashString(output.Name));
            Outputs.Remove(output);
        }

        #endregion

        #endregion

        #region Object Properties

        /// <summary>
        /// This will safely try to set a property on the node's <see cref="Object"/>
        /// </summary>
        /// <param name="name">Name of the property to set</param>
        /// <param name="value">Value of the property</param>
        /// <returns>Whether or not the property was set</returns>
        public bool TrySetProperty(string name, object value)
        {
            PropertyInfo property = Object.GetType().GetProperty(name);
            if (property != null)
            {
                try
                {
                    property.SetValue(Object, value);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get a value from the node's <see cref="Object"/>
        /// </summary>
        /// <param name="name">The name of the property to fetch</param>
        /// <returns>The value of the property. Null if it was not found</returns>
        public object TryGetProperty(string name)
        {
            PropertyInfo property = Object.GetType().GetProperty(name);
            if (property != null)
            {
                return property.GetValue(Object);
            }

            return null;
        }

        #endregion

        #region Basic Construction

        /// <summary>
        /// Create an entity node from an internal object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(object obj, INodeWrangler nodeWrangler)
        {
            if (obj.GetType().GetProperty("Realm") != null)
            {
                Realm = ParseRealm(((dynamic)obj).Realm);
            }
            
            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;

            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            Type = PointerRefType.Internal;
        }
        
        /// <summary>
        /// Create an entity node from an internal object
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(Type type, INodeWrangler nodeWrangler)
        {
            object obj = TypeLibrary.CreateObject(type.Name);
            
            if (obj.GetType().GetProperty("Realm") != null)
            {
                Realm = ParseRealm(((dynamic)obj).Realm);
            }
            
            EntityNodeWrangler entityWrangler = (EntityNodeWrangler)nodeWrangler;
            AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                entityWrangler.Asset.Objects,
                type,
                entityWrangler.Asset.FileGuid), -1); // TODO: Should we always be setting inId as -1?
            ((dynamic)obj).SetInstanceGuid(guid);
            
            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;

            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            Type = PointerRefType.Internal;
        }

        /// <summary>
        /// Create an entity node from an external object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fileGuid"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(object obj, Guid fileGuid, INodeWrangler nodeWrangler)
        {
            if (obj.GetType().GetProperty("Realm") != null)
            {
                Realm = ParseRealm(((dynamic)obj).Realm);
            }

            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;

            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            FileGuid = fileGuid;
            ClassGuid = InternalGuid.ExportedGuid;
            Type = PointerRefType.External;
        }

        public EntityNode(INodeWrangler nodeWrangler)
        {
            NodeWrangler = nodeWrangler;
        }

        public EntityNode()
        {
        }

        #endregion

        #region Static construction

        private static Dictionary<string, Type> _extensions = new Dictionary<string, Type>();

        static EntityNode()
        {
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(EntityNode)))
                {
                    try
                    {
                        EntityNode node = (EntityNode)Activator.CreateInstance(type);
                        if (node.IsValid() && !_extensions.ContainsKey(node.ObjectType))
                        {
                            _extensions.Add(node.ObjectType, type);
                        }
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Entity node {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
            }
            
            // TODO: External extensions(external extensions should always take priority over internal ones if they are valid)
        }

        /// <summary>
        /// Gets a proper Node from an internal object. This will return subclasses if they are found.
        /// </summary>
        /// <param name="entity">The object this will be constructed off of</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <param name="createId">Generate a unique ID based on the assets current GUIDs</param>
        /// <returns>The object as an EntityNode</returns>
        public static EntityNode GetNodeFromEntity(object entity, INodeWrangler wrangler, bool createId = false)
        {
            if (createId)
            {
                EntityNodeWrangler entityWrangler = (EntityNodeWrangler)wrangler;
                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                    entityWrangler.Asset.Objects,
                    entity.GetType(),
                    entityWrangler.Asset.FileGuid), -1); // TODO: Should we always be setting inId as -1?
                ((dynamic)entity).SetInstanceGuid(guid);
            }
            
            if (_extensions.ContainsKey(entity.GetType().Name))
            {
                return (EntityNode)Activator.CreateInstance(_extensions[entity.GetType().Name], entity, wrangler);
            }
            else
            {
                return new EntityNode(entity, wrangler);
            }
        }
        
        /// <summary>
        /// Gets a proper Node from an internal object. This will return subclasses if they are found.  
        /// </summary>
        /// <param name="type">The object type this will be constructed off of</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <returns>The object type as an EntityNode</returns>
        public static EntityNode GetNodeFromEntity(Type type, INodeWrangler wrangler)
        {
            if (_extensions.ContainsKey(type.Name))
            {
                return (EntityNode)Activator.CreateInstance(_extensions[type.Name], type, wrangler);
            }
            else
            {
                return new EntityNode(type, wrangler);
            }
        }

        /// <summary>
        /// Gets a proper Node from an external object. This will return subclasses if they are found.  
        /// </summary>
        /// <param name="entity">The object this will be constructed off of</param>
        /// <param name="fileGuid">The guid of the file this external object hails from</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <param name="createId">Generate a unique ID based on the assets current guids</param>
        /// <returns>The object as an EntityNode</returns>
        public static EntityNode GetNodeFromEntity(object entity, Guid fileGuid, INodeWrangler wrangler, bool createId = false)
        {
            if (createId)
            {
                EntityNodeWrangler entityWrangler = (EntityNodeWrangler)wrangler;
                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(
                    App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(fileGuid)).Objects,
                    entity.GetType(),
                    fileGuid), -1); // TODO: Should we always be setting inId as -1?
                ((dynamic)entity).SetInstanceGuid(guid);
            }
            
            if (_extensions.ContainsKey(entity.GetType().Name))
            {
                return (EntityNode)Activator.CreateInstance(_extensions[entity.GetType().Name], entity, fileGuid, wrangler);
            }
            else
            {
                return new EntityNode(entity, wrangler);
            }
        }

        #endregion
        
        public override string ToString()
        {
            return $"{Realm} - {Header}";
        }
    }
    
    public class ObjectFlagsHelper
    {
        public string GuidMask { get; set; }
        public bool ClientEvent { get; set; }
        public bool ServerEvent { get; set; }
        public bool ClientProperty { get; set; }
        public bool ServerProperty { get; set; }
        public bool ClientLinkSource { get; set; }
        public bool ServerLinkSource { get; set; }
        public bool UnusedFlag { get; set; }

        /// <summary>
        /// Credit to github.com/Mophead01 for the Object Flags parser
        /// </summary>
        /// <param name="flags"></param>
        public ObjectFlagsHelper(uint flags)
        {
            ClientEvent = Convert.ToBoolean((flags & 33554432) != 0 ? 1 : 0);
            ServerEvent = Convert.ToBoolean((flags & 67108864) != 0 ? 1 : 0);
            ClientProperty = Convert.ToBoolean((flags & 134217728) != 0 ? 1 : 0);
            ServerProperty = Convert.ToBoolean((flags & 268435456) != 0 ? 1 : 0);
            ClientLinkSource = Convert.ToBoolean((flags & 536870912) != 0 ? 1 : 0);
            ServerLinkSource = Convert.ToBoolean((flags & 1073741824) != 0 ? 1 : 0);
            UnusedFlag = Convert.ToBoolean((flags & 2147483648) != 0 ? 1 : 0);
            GuidMask = (flags & 33554431).ToString("X2").ToLower();
        }
        
        public static implicit operator uint(ObjectFlagsHelper flagsHelper) => flagsHelper.GetAsFlags();
        public static explicit operator ObjectFlagsHelper(uint flags) => new ObjectFlagsHelper(flags);

        /// <summary>
        /// I'm too lazy to do what I did with PropertyFlagsHelper so we will just use this method to get the flags instead
        /// Credit to github.com/Mophead01 for the Object Flags creation
        /// </summary>
        /// <returns></returns>
        public uint GetAsFlags()
        {
            bool isTooLarge = !uint.TryParse(GuidMask, System.Globalization.NumberStyles.HexNumber, null, out var newFlags);
            if (isTooLarge || newFlags > 33554431)
            {
                newFlags = 0;
                App.Logger.LogWarning("Invalid Guid Mask");
            }

            if (ClientEvent)
            {
                newFlags |= 33554432;
            }
            if (ServerEvent)
            {
                newFlags |= 67108864;
            }
            if (ClientProperty)
            {
                newFlags |= 134217728;
            }
            if (ServerProperty)
            {
                newFlags |= 268435456;
            }
            if (ClientLinkSource)
            {
                newFlags |= 536870912;
            }
            if (ServerLinkSource)
            {
                newFlags |= 1073741824;
            }
            if (UnusedFlag)
            {
                newFlags |= 2147483648;
            }

            return newFlags;
        }
    }
}