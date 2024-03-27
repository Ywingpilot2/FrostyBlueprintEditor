using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor.LayoutManager.IO;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using BlueprintEditorPlugin.Windows;
using Frosty.Core.Controls;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Prism.Commands;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    public class InterfaceNode : ITransient, IObjectNode
    {
        private string _header;
        private Point _location;
        private bool _isSelected;

        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }

        /// <summary>
        /// The direction this node goes
        /// </summary>
        public PortDirection Direction => Inputs.Count != 0 ? PortDirection.In : PortDirection.Out;

        public ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();

        public Point Location
        {
            get => _location;
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
        }

        public Size Size { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        #region Object implementation

        public object Object { get; }
        
        /// <summary>
        /// The part of the interface <see cref="Object"/> which contains this nodes information
        /// </summary>
        public object SubObject { get; set; }
        
        /// <summary>
        /// blehhh
        /// </summary>
        public EditInterfaceArgs EditArgs { get; set; }

        public PointerRefType Type => PointerRefType.Internal;
        public AssetClassGuid InternalGuid { get; set; }
        
        public Guid FileGuid { get; }
        public Guid ClassGuid { get; }

        public void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            if (args.OldValue == args.NewValue)
                return;
            
            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
            if (wrangler.GetInterfaceNode(EditArgs.Name, Direction) != null)
            {
                App.Logger.LogError("Cannot have 2 interface nodes of the same name.");
                EditArgs.Name = args.OldValue.ToString();
                return;
            }
            
            

            ((dynamic)SubObject).Name = new CString(EditArgs.Name);
            Header = EditArgs.Name;
            if (Direction == PortDirection.In)
            {
                Inputs[0].Name = Header;
            }
            else
            {
                Outputs[0].Name = Header;
            }
        }

        public EntityInput GetInput(string name, ConnectionType type)
        {
            return (EntityInput)Inputs[0];
        }

        public EntityOutput GetOutput(string name, ConnectionType type)
        {
            return (EntityOutput)Outputs[0];
        }

        public void AddInput(EntityInput input)
        {
            throw new NotImplementedException();
        }

        public void AddOutput(EntityOutput output)
        {
            throw new NotImplementedException();
        }

        public ConnectionType ConnectionType { get; set; }

        #endregion

        #region Property Changing

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Basic implementation

        public INodeWrangler NodeWrangler { get; }

        public bool IsValid()
        {
            return true;
        }

        public void OnCreation()
        {
        }

        public void OnDestruction()
        {
        }

        #endregion

        #region Complex implementation

        public void OnInputUpdated(IPort port)
        {
        }

        public void OnOutputUpdated(IPort port)
        {
        }

        public void AddPort(IPort port)
        {
            Inputs.Add(port);
        }

        #endregion

        #region Interface implementation
        
        public bool Load(LayoutReader reader)
        {
            string name = reader.ReadNullTerminatedString();

            EntityNodeWrangler wrangler = (EntityNodeWrangler)NodeWrangler;
            InterfaceNode real;
            if (reader.ReadBoolean()) // Is an output
            {
                real = wrangler.GetInterfaceNode(name, PortDirection.Out);
            }
            else
            {
                real = wrangler.GetInterfaceNode(name, PortDirection.In);
            }

            real.Location = reader.ReadPoint();
            
            // TODO HACK: We create interface nodes before we read layouts. So in order to read transient data from them, we can't add new ones.
            // Resulting in this hack of a method
            return false;
        }

        public void Save(LayoutWriter writer)
        {
            writer.WriteNullTerminatedString(Header);
            writer.Write(Inputs.Count == 0);
            writer.Write(Location);
        }

        #endregion

        #region Networking implementation

        public Realm Realm
        {
            get
            {
                if (Inputs.Count != 0)
                {
                    return ((EntityPort)Inputs[0]).Realm;
                }
                else
                {
                    return ((EntityPort)Outputs[0]).Realm;
                }
            }
        }

        #endregion

        public InterfaceNode(object obj, string name, ConnectionType type, PortDirection direction, INodeWrangler wrangler, Realm realm = Realm.Any)
        {
            Object = obj;
            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            if (name.StartsWith("0x"))
            {
                int hash = int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                Header = Utils.GetString(hash);
            }
            else
            {
                Header = name;
            }
            ConnectionType = type;
            NodeWrangler = wrangler;
            
            if (direction == PortDirection.In)
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        EventInput input = new EventInput(Header, this)
                        {
                            Realm = realm
                        };
                        Inputs.Add(input);
                    } break;
                    case ConnectionType.Link:
                    {
                        LinkInput input = new LinkInput(Header, this)
                        {
                            Realm = realm
                        };
                        Inputs.Add(input);
                    } break;
                    case ConnectionType.Property:
                    {
                        PropertyInput input = new PropertyInput(Header, this)
                        {
                            Realm = realm,
                            IsInterface = true
                        };
                        Inputs.Add(input);
                    } break;
                }
            }
            else
            {
                switch (type)
                {
                    case ConnectionType.Event:
                    {
                        EventOutput input = new EventOutput(Header, this)
                        {
                            Realm = realm
                        };
                        Outputs.Add(input);
                    } break;
                    case ConnectionType.Link:
                    {
                        LinkOutput input = new LinkOutput(Header, this)
                        {
                            Realm = realm
                        };
                        Outputs.Add(input);
                    } break;
                    case ConnectionType.Property:
                    {
                        PropertyOutput input = new PropertyOutput(Header, this)
                        {
                            Realm = realm,
                            IsInterface = true
                        };
                        Outputs.Add(input);
                    } break;
                }
            }

            EditArgs = new EditInterfaceArgs(this);
        }

        public InterfaceNode(INodeWrangler nodeWrangler)
        {
            NodeWrangler = nodeWrangler;
        }

        public InterfaceNode()
        {
        }

        public override string ToString()
        {
            return Header ?? base.ToString();
        }
    }

    public class EditInterfaceArgs
    {
        [DisplayName("Name")]
        [Description("The name of this interface")]
        public string Name { get; set; }

        public EditInterfaceArgs(InterfaceNode node)
        {
            Name = node.Header;
        }

        public EditInterfaceArgs()
        {
            Name = "";
        }
    }
}