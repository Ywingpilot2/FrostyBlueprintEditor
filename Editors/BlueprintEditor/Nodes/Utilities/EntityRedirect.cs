using System;
using System.ComponentModel;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    public class EntityInputRedirect : BaseRedirect, IObjectContainer
    {
        public object Object { get; }

        public ConnectionType ConnectionType
        {
            get
            {
                if (Inputs.Count != 0)
                {
                    return ((EntityPort)Inputs[0]).Type;
                }
                else
                {
                    return ((EntityPort)Outputs[0]).Type;
                }
            }
        }

        public void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            EditRedirectArgs edit = (EditRedirectArgs)Object;
            Header = string.IsNullOrEmpty(edit.Header) ? null : edit.Header;
        }

        public override ITransient Load(NativeReader reader)
        {
            throw new System.NotImplementedException();
        }

        public override void Save(NativeWriter writer)
        {
            throw new System.NotImplementedException();
        }

        protected override void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.NotifyPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case "IsInterface":
                {
                    if (Direction == PortDirection.In)
                    {
                        ((EntityPort)Inputs[0]).IsInterface = ((EntityPort)RedirectTarget).IsInterface;
                    }
                } break;
                case "Realm":
                {
                    if (Direction == PortDirection.In)
                    {
                        ((EntityPort)Inputs[0]).Realm = ((EntityPort)RedirectTarget).Realm;
                    }
                } break;
            }
        }

        public EntityInputRedirect(EntityPort redirectTarget, PortDirection direction, INodeWrangler wrangler)
        {
            RedirectTarget = redirectTarget;
            Direction = direction;
            NodeWrangler = wrangler;

            if (Direction == PortDirection.In)
            {
                switch (redirectTarget.Type)
                {
                    case ConnectionType.Event:
                    {
                        Inputs.Add(new EventInput(redirectTarget.Name, redirectTarget.Node)
                        {
                            HasPlayer = redirectTarget.HasPlayer,
                            Realm = redirectTarget.Realm
                        });
                    } break;
                    case ConnectionType.Link:
                    {
                        Inputs.Add(new LinkInput(redirectTarget.Name, redirectTarget.Node)
                        {
                            Realm = redirectTarget.Realm
                        });
                    } break;
                    case ConnectionType.Property:
                    {
                        Inputs.Add(new PropertyInput(redirectTarget.Name, redirectTarget.Node)
                        {
                            IsInterface = redirectTarget.IsInterface,
                            Realm = redirectTarget.Realm
                        });
                    } break;
                }
            }
            else
            {
                switch (redirectTarget.Type)
                {
                    case ConnectionType.Event:
                    {
                        Outputs.Add(new EventOutput(redirectTarget.Name, this)
                        {
                            HasPlayer = redirectTarget.HasPlayer,
                            Realm = Realm.Any
                        });
                    } break;
                    case ConnectionType.Link:
                    {
                        Outputs.Add(new LinkOutput(redirectTarget.Name, this)
                        {
                            Realm = Realm.Any
                        });
                    } break;
                    case ConnectionType.Property:
                    {
                        Outputs.Add(new PropertyOutput(redirectTarget.Name, this)
                        {
                            IsInterface = redirectTarget.IsInterface,
                            Realm = Realm.Any
                        });
                    } break;
                }
            }

            RedirectTarget.PropertyChanged += NotifyPropertyChanged;
            Object = new EditRedirectArgs(this);
        }
    }
    
    public class EntityOutputRedirect : BaseRedirect, IObjectContainer
    {
        public object Object { get; }

        public ConnectionType ConnectionType
        {
            get
            {
                if (Inputs.Count != 0)
                {
                    return ((EntityPort)Inputs[0]).Type;
                }
                else
                {
                    return ((EntityPort)Outputs[0]).Type;
                }
            }
        }

        public override void OnDestruction()
        {
            if (Direction == PortDirection.Out)
            {
                foreach (BaseConnection connection in NodeWrangler.GetConnections(Outputs[0]))
                {
                    connection.Source = RedirectTarget;
                }
            }
            else
            {
                IConnection connection = NodeWrangler.GetConnections(Inputs[0]).First();
                NodeWrangler.RemoveConnection(connection);
            }
        }

        public void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            EditRedirectArgs edit = (EditRedirectArgs)Object;
            Header = string.IsNullOrEmpty(edit.Header) ? null : edit.Header;
        }

        public override ITransient Load(NativeReader reader)
        {
            throw new System.NotImplementedException();
        }

        public override void Save(NativeWriter writer)
        {
            throw new System.NotImplementedException();
        }

        protected override void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                {
                    if (Direction == PortDirection.Out)
                    {
                        Outputs[0].Name = RedirectTarget.Name;
                    }
                    else
                    {
                        Inputs[0].Name = RedirectTarget.Name;
                    }
                } break;
                case "Node":
                {
                    App.Logger.LogError("Fuck! Someone tell ywingpilot2 that the port node changed on a redirect...");
                    throw new NotImplementedException("Fuck! Someone tell ywingpilot2 that the port node changed on a redirect...");
                }
                case "IsInterface":
                {
                    if (Direction == PortDirection.In)
                    {
                        ((EntityPort)Outputs[0]).IsInterface = ((EntityPort)RedirectTarget).IsInterface;
                    }
                } break;
                case "Realm":
                {
                    if (Direction == PortDirection.In)
                    {
                        ((EntityPort)Outputs[0]).Realm = ((EntityPort)RedirectTarget).Realm;
                    }
                } break;
            }
        }

        public EntityOutputRedirect(EntityPort redirectTarget, PortDirection direction, INodeWrangler wrangler)
        {
            RedirectTarget = redirectTarget;
            Direction = direction;
            NodeWrangler = wrangler;

            if (Direction == PortDirection.In)
            {
                switch (redirectTarget.Type)
                {
                    case ConnectionType.Event:
                    {
                        Inputs.Add(new EventInput(redirectTarget.Name, this)
                        {
                            HasPlayer = redirectTarget.HasPlayer,
                            Realm = Realm.Any
                        });
                    } break;
                    case ConnectionType.Link:
                    {
                        Inputs.Add(new LinkInput(redirectTarget.Name, this)
                        {
                            Realm = Realm.Any
                        });
                    } break;
                    case ConnectionType.Property:
                    {
                        Inputs.Add(new PropertyInput(redirectTarget.Name, this)
                        {
                            IsInterface = redirectTarget.IsInterface,
                            Realm = Realm.Any
                        });
                    } break;
                }
            }
            else
            {
                switch (redirectTarget.Type)
                {
                    case ConnectionType.Event:
                    {
                        Outputs.Add(new EventOutput(redirectTarget.Name, redirectTarget.Node)
                        {
                            HasPlayer = redirectTarget.HasPlayer,
                            Realm = redirectTarget.Realm
                        });
                    } break;
                    case ConnectionType.Link:
                    {
                        Outputs.Add(new LinkOutput(redirectTarget.Name, redirectTarget.Node)
                        {
                            Realm = redirectTarget.Realm
                        });
                    } break;
                    case ConnectionType.Property:
                    {
                        Outputs.Add(new PropertyOutput(redirectTarget.Name, redirectTarget.Node)
                        {
                            IsInterface = redirectTarget.IsInterface,
                            Realm = redirectTarget.Realm
                        });
                    } break;
                }
            }

            RedirectTarget.PropertyChanged += NotifyPropertyChanged;
            Object = new EditRedirectArgs(this);
        }
    }
    
    public class EditRedirectArgs
    {
        public string Header { get; set; }

        public EditRedirectArgs(BaseRedirect redirect)
        {
            if (redirect.Header == null)
            {
                Header = "";
                return;
            }
            
            Header = redirect.Header;
        }

        public EditRedirectArgs()
        {
        }
    }
}