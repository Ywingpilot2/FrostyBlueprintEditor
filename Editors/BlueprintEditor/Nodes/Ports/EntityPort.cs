using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Options;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Windows;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public abstract class EntityPort : IPort, INetworked
    {
        #region Basic info

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }

        private bool _isConnected;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                NotifyPropertyChanged(nameof(IsConnected));
            }
        }
        public abstract PortDirection Direction { get; }

        #endregion

        #region Commands

        public ICommand DeleteCommand => new DelegateCommand(Remove);

        private protected void Remove()
        {
            if (Direction == PortDirection.In)
            {
                ((EntityNode)Node).RemoveInput((EntityInput)this);
            }
            else
            {
                ((EntityNode)Node).RemoveOutput((EntityOutput)this);
            }
        }

        public ICommand EditCommand => new DelegateCommand(Edit);
        
        private protected void Edit()
        {
            EditPortArgs portArgs = new EditPortArgs(this);
            if (EditPromptWindow.Show(portArgs, $"Edit {Direction}put {Name}") == MessageBoxResult.Yes)
            {
                FrostyTaskWindow.Show("Applying changes...", "", task =>
                {
                    if (Name != portArgs.Name)
                    {
                        Name = portArgs.Name;
                    }
                    if (Realm != portArgs.Realm)
                    {
                        Realm = portArgs.Realm;
                    }

                    if (Type == ConnectionType.Event && HasPlayer != portArgs.HasPlayer)
                    {
                        HasPlayer = portArgs.HasPlayer;
                    }
                    
                    if (Type == ConnectionType.Property && IsInterface != portArgs.IsInterface)
                    {
                        IsInterface = portArgs.IsInterface;
                    }
                });
                EntityNodeWrangler wrangler = (EntityNodeWrangler)Node.NodeWrangler;
                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(wrangler.Asset.FileGuid).Name, wrangler.Asset);
            }
        }

        #endregion

        #region Complex info

        protected Point _anchor;
        public virtual Point Anchor
        {
            get => _anchor;
            set
            {
                _anchor = value;
                NotifyPropertyChanged(nameof(Anchor));
            }
        }
        public INode Node { get; protected set; }

        #endregion

        #region Entity Info

        public abstract ConnectionType Type { get; }

        private Realm _realm;
        public Realm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                NotifyPropertyChanged(nameof(Realm));
            }
        }

        private bool _hasPlayer;
        public bool HasPlayer
        {
            get => _hasPlayer;
            set
            {
                _hasPlayer = value;
                NotifyPropertyChanged(nameof(HasPlayer));
            }
        }

        private bool _isInterface;

        public bool IsInterface
        {
            get => _isInterface;
            set
            {
                _isInterface = value;
                NotifyPropertyChanged(nameof(IsInterface));
            }
        }

        #endregion

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Networked implementation

        public Realm ParseRealm(object obj)
        {
            Type type = obj.GetType();
            if (type.IsEnum)
            {
                return (Realm)((int)obj);
            }
            else if (type.Name == "string")
            {
                // TODO: this is a fatty! Please make it smaller!
                switch ((string)obj)
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
                    case "EventConnectionTargetType_ClientAndServer":
                    {
                        return Realm.ClientAndServer;
                    }
                    case "EventConnectionTargetType_Client":
                    {
                        return Realm.Client;
                    }
                    case "EventConnectionTargetType_Server":
                    {
                        return Realm.Server;
                    }
                    case "EventConnectionTargetType_NetworkedClient":
                    {
                        return Realm.NetworkedClient;
                    }
                    case "EventConnectionTargetType_NetworkedClientAndServer":
                    {
                        return Realm.NetworkedClientAndServer;
                    }
                    default:
                    {
                        return Realm.Invalid;
                    }
                }
            }

            return Realm.Invalid;
        }

        public Realm DetermineRealm()
        {
            Realm realm = Realm;
            if (realm == Realm.Any || realm == Realm.Invalid)
            {
                EntityNode entityNode = (EntityNode)Node;
                if (entityNode.Realm != Realm.Any && entityNode.Realm != Realm.Invalid)
                {
                    realm = entityNode.Realm;
                }
                else
                {
                    foreach (EntityConnection connection in entityNode.NodeWrangler.GetConnections(this))
                    {
                        if (connection.Realm != Realm.Any)
                        {
                            realm = connection.Realm;
                            return realm;
                        }
                    }
                }
            }

            return realm;
        }
        
        public void FixRealm()
        {
            Realm = DetermineRealm();
        }

        #endregion

        public override string ToString()
        {
            return $"{Realm} {Type} {Direction}put - {Name}";
        }

        public EntityPort(string name, INode node)
        {
            Name = name;
            Node = node;
        }
    }

    public abstract class EntityInput : EntityPort
    {
        public override PortDirection Direction => PortDirection.In;
        public override Point Anchor
        {
            get => _anchor;
            set
            {
                _anchor = new Point(value.X - EditorOptions.OutputPos, value.Y);
                NotifyPropertyChanged(nameof(Anchor));
            }
        }

        public EntityInput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
    
    public abstract class EntityOutput : EntityPort
    {
        public override PortDirection Direction => PortDirection.Out;
        public override Point Anchor
        {
            get => _anchor;
            set
            {
                _anchor = new Point(value.X + EditorOptions.OutputPos, value.Y);
                NotifyPropertyChanged(nameof(Anchor));
            }
        }

        public EntityOutput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
}