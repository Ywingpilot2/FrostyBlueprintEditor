using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports
{
    public abstract class EntityPort : BasePort, INetworked
    {
        public abstract override PortDirection Direction { get; }
        public abstract ConnectionType Type { get; }

        public Realm Realm { get; set; }
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

        public EntityPort(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }

    public abstract class EntityInput : EntityPort
    {
        public override PortDirection Direction => PortDirection.In;

        public EntityInput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
    
    public abstract class EntityOutput : EntityPort
    {
        public override PortDirection Direction => PortDirection.Out;

        public EntityOutput(string name, INode node) : base(name, node)
        {
            Name = name;
            Node = node;
        }
    }
}