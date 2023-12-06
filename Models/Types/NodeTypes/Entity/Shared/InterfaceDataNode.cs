using System.Collections.ObjectModel;
using System.Windows.Media;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Utils;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared
{
    public class InterfaceDataNode : EntityNode
    {
        public override string Name { get; set; } = "";
        public override string ObjectType { get; set; } = "EditorInterfaceNode"; //Not a real type
        public AssetClassGuid Guid { get; set; }

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
            };
        
        public dynamic InterfaceItem { get; private set; }

        /// <summary>
        /// Creates an interface data node from an item in an InterfaceDescriptorData
        /// </summary>
        /// <param name="interfaceItem">DataField, DynamicEvent, DynamicLink</param>
        /// <param name="isOut">The direction this node goes. If set to true, it is an output, false input.</param>
        /// <returns></returns>
        public static InterfaceDataNode CreateInterfaceDataNode(dynamic interfaceItem, bool isOut, AssetClassGuid guid)
        {
            switch (interfaceItem.GetType().Name)
            {
                default: //DataField
                {
                    if (isOut)
                    {
                        var interfaceNode = new InterfaceDataNode()
                        {
                            Guid = guid,
                            HeaderColor =
                                new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#00FF21")), //Property connection color
                            InterfaceItem = interfaceItem,
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Property }
                            },
                            PointerRefType = PointerRefType.Internal
                        };
                        return interfaceNode;
                    }
                    else
                    {
                        var interfaceNode = new InterfaceDataNode()
                        {
                            Guid = guid,
                            HeaderColor =
                                new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#00FF21")), //Property connection color
                            InterfaceItem = interfaceItem,
                            Inputs = new ObservableCollection<InputViewModel>()
                            {
                                new InputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Property, PropertyConnectionType = PropertyType.Interface }
                            },
                            PointerRefType = PointerRefType.Internal
                        };
                        return interfaceNode;
                    }
                }
                case "DynamicEvent":
                {
                    if (isOut)
                    {
                        var interfaceNode = new InterfaceDataNode()
                        {
                            Guid = guid,
                            HeaderColor =
                                new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#F8F8F8")), //Event connection color 5FD95F
                            InterfaceItem = interfaceItem,
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Event }
                            },
                            PointerRefType = PointerRefType.Internal
                        };
                        return interfaceNode;
                    }
                    else
                    {
                        var interfaceNode = new InterfaceDataNode()
                        {
                            Guid = guid,
                            HeaderColor =
                                new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#F8F8F8")), //Event connection color
                            InterfaceItem = interfaceItem,
                            Inputs = new ObservableCollection<InputViewModel>()
                            {
                                new InputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Event, PropertyConnectionType = PropertyType.Interface }
                            },
                            PointerRefType = PointerRefType.Internal
                        };
                        return interfaceNode;
                    }
                }
                case "DynamicLink":
                {
                    if (isOut)
                    {
                        var interfaceNode = new InterfaceDataNode()
                        {
                            Guid = guid,
                            HeaderColor =
                                new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#6FA9CE")), //Link connection color
                            InterfaceItem = interfaceItem,
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Link }
                            },
                            PointerRefType = PointerRefType.Internal
                        };
                        return interfaceNode;
                    }
                    else
                    {
                        var interfaceNode = new InterfaceDataNode()
                        {
                            Guid = guid,
                            HeaderColor =
                                new SolidColorBrush(
                                    (Color)ColorConverter.ConvertFromString("#6FA9CE")), //Link connection color
                            InterfaceItem = interfaceItem,
                            Inputs = new ObservableCollection<InputViewModel>()
                            {
                                new InputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Link }
                            },
                            PointerRefType = PointerRefType.Internal
                        };
                        return interfaceNode;
                    }
                }
            }
        }

        public override void OnModified(ItemModifiedEventArgs args)
        {
            if (Inputs.Count != 0)
            {
                Inputs[0].Title = InterfaceItem.Name.ToString();
            }
            else
            {
                Outputs[0].Title = InterfaceItem.Name.ToString();
            }

            //Really stupid way of updating connections
            //TODO: Create method in EbxBaseEditor for "refreshing" the ebx side of connections
            foreach (ConnectionViewModel connection in EditorUtils.CurrentEditor.GetConnections(this))
            {
                var source = connection.Source;
                var target = connection.Target;
                
                EditorUtils.CurrentEditor.Disconnect(connection);
                var newConnection = EditorUtils.CurrentEditor.Connect(source, target);
                EditorUtils.CurrentEditor.CreateConnectionObject(newConnection);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            
            if (obj.GetType() == GetType())
            {
                return ((InterfaceDataNode)obj).Name == Name && ((InterfaceDataNode)obj).Outputs == Outputs 
                                                             && ((InterfaceDataNode)obj).Inputs == Inputs;
            }
            else if (obj.GetType() == Object.GetType())
            {
                return ((dynamic)obj).Name == ((dynamic)Object).Name;
            }

            return obj.GetType() == Object.GetType() && (bool)Object.Equals(obj);
        }
    }
}