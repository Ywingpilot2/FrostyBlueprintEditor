using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BlueprintEditor.Models.Connections;
using FrostySdk.Ebx;

namespace BlueprintEditor.Models.Types.Shared
{
    public class InterfaceDataNode : NodeBaseModel
    {
        public override string Name { get; set; } = "";
        public override string ObjectType { get; } = "EditorInterfaceNode"; //Not a real type

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
            };

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
                                Object = interfaceItem,
                                Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Property }
                            }
                            };
                            InterfaceOutputDataNodes.Add(interfaceItem.Name, interfaceNode);
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
                                Object = interfaceItem,
                                Inputs = new ObservableCollection<InputViewModel>()
                            {
                                new InputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Property }
                            }
                            };
                            InterfaceInputDataNodes.Add(interfaceItem.Name, interfaceNode);
                            return interfaceNode;
                        }

                        break;
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
                                        (Color)ColorConverter.ConvertFromString("#FFFFFF")), //Event connection color
                                Object = interfaceItem,
                                Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Event }
                            }
                            };
                            InterfaceOutputDataNodes.Add(interfaceItem.Name, interfaceNode);
                            return interfaceNode;
                        }
                        else
                        {
                            var interfaceNode = new InterfaceDataNode()
                            {
                                Guid = guid,
                                HeaderColor =
                                    new SolidColorBrush(
                                        (Color)ColorConverter.ConvertFromString("#FFFFFF")), //Event connection color
                                Object = interfaceItem,
                                Inputs = new ObservableCollection<InputViewModel>()
                            {
                                new InputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Event }
                            },
                            };
                            InterfaceInputDataNodes.Add(interfaceItem.Name, interfaceNode);
                            return interfaceNode;
                        }
                        break;
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
                                        (Color)ColorConverter.ConvertFromString("#0094FF")), //Link connection color
                                Object = interfaceItem,
                                Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Link }
                            }
                            };
                            InterfaceOutputDataNodes.Add(interfaceItem.Name, interfaceNode);
                            return interfaceNode;
                        }
                        else
                        {
                            var interfaceNode = new InterfaceDataNode()
                            {
                                Guid = guid,
                                HeaderColor =
                                    new SolidColorBrush(
                                        (Color)ColorConverter.ConvertFromString("#0094FF")), //Link connection color
                                Object = interfaceItem,
                                Inputs = new ObservableCollection<InputViewModel>()
                            {
                                new InputViewModel() { Title = interfaceItem.Name, Type = ConnectionType.Link }
                            }
                            };
                            InterfaceInputDataNodes.Add(interfaceItem.Name, interfaceNode);
                            return interfaceNode;
                        }
                        break;
                    }
            }
        }

        public static Dictionary<string, InterfaceDataNode> InterfaceInputDataNodes = new Dictionary<string, InterfaceDataNode>();
        public static Dictionary<string, InterfaceDataNode> InterfaceOutputDataNodes = new Dictionary<string, InterfaceDataNode>();

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            
            if (obj.GetType() == GetType())
            {
                return ((InterfaceDataNode)obj).Name == Name && ((InterfaceDataNode)obj).Outputs == Outputs 
                                                             && ((InterfaceDataNode)obj).Inputs == Inputs;
            }

            return obj.GetType() == Object.GetType() && (bool)Object.Equals(obj);
        }
    }
}