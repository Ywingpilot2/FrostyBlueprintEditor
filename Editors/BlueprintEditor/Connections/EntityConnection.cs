using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public abstract class EntityConnection : BaseConnection, INetworked
    {
        private static readonly List<(Realm, Realm)> ImplicitConnectionCombos = new List<(Realm, Realm)>
        {
            (Realm.ClientAndServer, Realm.Server),
            (Realm.ClientAndServer, Realm.Client),
            (Realm.NetworkedClientAndServer, Realm.Server),
            (Realm.NetworkedClientAndServer, Realm.NetworkedClient),
            (Realm.Server, Realm.NetworkedClient),
            (Realm.NetworkedClient, Realm.Client),
            (Realm.Client, Realm.NetworkedClient),
            (Realm.Client, Realm.ClientAndServer),
            (Realm.Server, Realm.ClientAndServer),
            (Realm.NetworkedClientAndServer, Realm.ClientAndServer),
            (Realm.Server, Realm.Client)
        };
        public virtual Realm Realm { get; set; }

        #region Commands

        public ICommand EditCommand => new DelegateCommand(Edit);

        public void Edit()
        {
            if (Type == ConnectionType.Property)
            {
                EditPropConnectionArgs editArgs = new EditPropConnectionArgs(this);
                MessageBoxResult result = EditPromptWindow.Show(editArgs, $"Edit {this}");
                if (result == MessageBoxResult.Yes)
                {
                    Realm = editArgs.Realm;
                    PropType = editArgs.PropertyType;
                }
            }
            else
            {
                EditConnectionArgs editArgs = new EditConnectionArgs(this);
                MessageBoxResult result = EditPromptWindow.Show(editArgs, $"Edit {this}");
                if (result == MessageBoxResult.Yes)
                {
                    Realm = editArgs.Realm;
                }
            }

            EbxAsset asset = ((EntityNodeWrangler)Target.Node.NodeWrangler).Asset;
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(asset.FileGuid).Name, asset);
        }
        
        public ICommand RemoveCommand => new DelegateCommand(Remove);

        private void Remove()
        {
            Target.Node.NodeWrangler.RemoveConnection(this);
        }
        
        public ICommand FixCommand => new DelegateCommand(UserFix);

        public void UserFix()
        {
            EntityPort target = (EntityPort)Target;
            EntityPort source = (EntityPort)Source;
            
            FrostyTaskWindow.Show("Fixing problems...", "", task =>
            {
                // We get 16 attempts to solve all problems... Hopefully they work!
                for (int i = 0; i < 16; i++)
                {
                    if (CurrentStatus.Status == EditorStatus.Alright)
                        break; // Yay!
                    
                    switch (CurrentStatus.ToolTip)
                    {
                        case "Connection source or target is not an EntityPort. Please use EntityPorts for Blueprints.":
                        {
                            Target.Node.NodeWrangler.RemoveConnection(this); // Fuck you
                            
                            #if DEVELOPER___DEBUG
                            App.Logger.LogError("Fuck you");
                            #endif
                        } break;
                        case "Connection realm should be the same as target realm":
                        {
                            if (target.Realm != Realm.Any || target.Realm != Realm.Invalid)
                            {
                                Realm = target.Realm;
                            }
                            else
                            {
                                target.ForceFixRealm();
                            }
                        } break;
                        case "Cannot implicitly determine the realms of this connection based on ports. Please manually set realms":
                        {
                            source.ForceFixRealm();
                            target.ForceFixRealm();
                            ForceFixRealm();
                        } break;
                        case "Property type is invalid":
                        {
                            if (target.IsInterface)
                            {
                                PropType = PropertyType.Interface;
                            }
                            else
                            {
                                PropType = PropertyType.Default;
                            }
                        } break;
                        case "Property type is set to interface, despite not plugging into an interface":
                        {
                            PropType = PropertyType.Default;
                        } break;
                        default:
                        {
                            if (CurrentStatus.ToolTip == $"{source.Realm} to {target.Realm} is not a valid combination of realms")
                            {
                                source.ForceFixRealm();
                                target.ForceFixRealm();
                            }
                            ForceFixRealm();
                        } break;
                    }
                    
                    UpdateStatus();
                }

                if (CurrentStatus.Status == EditorStatus.Flawed || CurrentStatus.Status == EditorStatus.Broken)
                {
                    if (CurrentStatus.ToolTip == $"{source.Realm} to {target.Realm} is not a valid combination of realms")
                    {
                        App.Logger.LogError($"Unable to implicitly determine intended realms for {source} to {target}, please manually update their realms.");
                        return;
                    }
                    App.Logger.LogError("Unable to solve all problems with this connection. Please either manually solve issues, or try again.");
                }
            });
        }

        #endregion

        #region Entity data

        public abstract ConnectionType Type { get; }
        public object Object { get; set; }

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

        private PropertyType _propType;
        public PropertyType PropType
        {
            get => _propType;
            set
            {
                _propType = value;
                ((dynamic)Object).Flags = PropertyFlagsHelper.GetAsFlags(Realm, PropType);
                NotifyPropertyChanged(nameof(PropType));
                UpdateStatus();
            }
        }

        #endregion

        #region Networked implementation

        public Realm ParseRealm(object obj)
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

        public Realm DetermineRealm(bool ignoreCurrent = false)
        {
            Realm realm = Realm;

            if ((Realm == Realm.Any || Realm == Realm.Invalid) || ignoreCurrent)
            {
                EntityPort source = (EntityPort)Source;
                EntityPort target = (EntityPort)Target;
                
                if (target.Realm != Realm.Any && target.Realm != Realm.Invalid)
                {
                    realm = target.Realm;
                }
                else if (source.Realm != Realm.Any && source.Realm != Realm.Invalid)
                {
                    realm = source.Realm;
                }
                // Fuck you
                else
                {
                    source.FixRealm();
                    target.FixRealm();
                
                    if (target.Realm != Realm.Any && target.Realm != Realm.Invalid)
                    {
                        realm = target.Realm;
                    }
                }
            }

            return realm;
        }
        
        public void FixRealm()
        {
            Realm = DetermineRealm();
            EbxAsset asset = ((EntityNodeWrangler)Target.Node.NodeWrangler).Asset;
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(asset.FileGuid).Name, asset);
        }

        public void ForceFixRealm()
        {
            Realm = DetermineRealm(true);
            EbxAsset asset = ((EntityNodeWrangler)Target.Node.NodeWrangler).Asset;
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(asset.FileGuid).Name, asset);
        }

        #endregion

        public override void UpdateStatus()
        {
            SetStatus(EditorStatus.Alright, "");
            
            if (Realm == Realm.Invalid)
            {
                SetStatus(EditorStatus.Broken, "Connection realm is invalid!");
                return;
            }

            EntityPort source = Source as EntityPort;
            EntityPort target = Target as EntityPort;

            if (source == null || target == null)
            {
                SetStatus(EditorStatus.Broken, "Connection source or target is not an EntityPort. Please use EntityPorts for Blueprints.");
                
                #if DEVELOPER___DEBUG
                App.Logger.LogError("HEY DUMBASS YOU'RE USING THE WRONG TYPES FOR CONNECTION {0} USE ENTITYPORTS INSTEAD GOD DAMMIT", ToString());
                #endif
                return;
            }
            
            if (source.Realm == Realm.Any && target.Realm == Realm.Any)
            {
                SetStatus(EditorStatus.Flawed, "Cannot implicitly determine the realms of this connection based on ports. Please manually set realms");
                return;
            }

            if (source.Realm != target.Realm && !ImplicitConnectionCombos.Contains((source.Realm, target.Realm)) && (source.Realm != Realm.Any && target.Realm != Realm.Any))
            {
                SetStatus(EditorStatus.Flawed, $"{source.Realm} to {target.Realm} is not a valid combination of realms");
                return;
            }

            if (Realm != target.Realm && target.Realm != Realm.Any)
            {
                SetStatus(EditorStatus.Flawed, "Connection realm should be the same as target realm");
                return;
            }
            
        }

        #region Construction

        public EntityConnection(IPort source, IPort target, object obj) : base(source, target)
        {
            Object = obj;
            EntityPort entitySource = (EntityPort)source;
            EntityPort entityTarget = (EntityPort)target;
        }
        
        public EntityConnection(IPort source, IPort target) : base(source, target)
        {
        }

        protected EntityConnection()
        {
        }

        #endregion

        public override string ToString()
        {
            return $"Connection {Source.Node}({Source.Name}) -> {Target.Node}({Target.Name})";
        }
    }

    public enum ConnectionType
    {
        Event = 0,
        Link = 1,
        Property = 2
    }
}