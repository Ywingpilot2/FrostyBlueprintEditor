using System;
using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Options;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public abstract class EntityPort : IPort, INetworked
    {
        #region Basic info

        public string Name { get; set; }
        public bool IsConnected { get; set; }
        public abstract PortDirection Direction { get; }

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

        #endregion

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
        
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