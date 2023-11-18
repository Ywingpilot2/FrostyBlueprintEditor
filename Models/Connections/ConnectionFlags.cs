using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Utils;

namespace BlueprintEditorPlugin.Models.Connections
{
    public class PropertyFlagsHelper : INotifyPropertyChanged
    {
        #region Flag Properties

        private ConnectionRealm _realm;
        public ConnectionRealm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                OnPropertyChanged(nameof(Realm));
            }
        }

        private PropertyType _inputType;

        public PropertyType InputType
        {
            get => _inputType;
            set
            {
                _inputType = value;
                OnPropertyChanged(nameof(InputType));
            }
        }

        private bool _isntStatic;

        public bool SourceCantBeStatic //TODO: Figure out when this is used
        {
            get => _isntStatic;
            set
            {
                _isntStatic = value;
                OnPropertyChanged(nameof(SourceCantBeStatic));
            }
        }
        
        private uint _flags;

        #endregion

        #region Constructors

        public PropertyFlagsHelper(uint flags)
        {
            Realm = (ConnectionRealm)(flags & 7);
            InputType = (PropertyType)((flags & 48) >> 4);
            SourceCantBeStatic = Convert.ToBoolean((flags & 8) != 0 ? 1 : 0);
            _flags = flags;
        }

        public static implicit operator uint(PropertyFlagsHelper flagsHelper) => flagsHelper._flags;
        public static explicit operator PropertyFlagsHelper(uint flags) => new PropertyFlagsHelper(flags);

        #endregion

        #region Updating Properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            //Update our flags
            _flags = 0;
            _flags |= (uint)Realm;
            _flags |= ((uint)InputType) << 4;
            if (SourceCantBeStatic)
            {
                _flags |= 8;
            }
        }

        #endregion

        public override string ToString()
        {
            switch (_realm)
            {
                case ConnectionRealm.Client:
                {
                    return "Client";
                }
                case ConnectionRealm.Server:
                {
                    return "Server";
                }
                case ConnectionRealm.ClientAndServer:
                {
                    return "ClientAndServer";
                }
                case ConnectionRealm.NetworkedClient:
                {
                    return "NetworkedClient";
                }
                case ConnectionRealm.NetworkedClientAndServer:
                {
                    return "NetworkedClientAndServer";
                }
                
                case ConnectionRealm.Invalid:
                default:
                {
                    return "Invalid";
                }
            }
        }
    }

    public enum ConnectionRealm
    {
        Invalid = 0,
        ClientAndServer = 1,
        Client = 2, 
        Server = 3,
        NetworkedClient = 4,
        NetworkedClientAndServer = 5,
        //Any = -1
    }

    public enum PropertyType
    {
        Default = 0,
        Interface = 1,
        Exposed = 2, //TODO: Figure out when Exposed is used
        Invalid = 3
    }
}