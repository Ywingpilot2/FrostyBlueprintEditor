using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Options;
using BlueprintEditorPlugin.Utils;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Models.Connections
{
    #region Connections

    /// <summary>
    /// A connection.
    /// </summary>
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        #region Properties

        public dynamic Object { get; set; }
        
        #region Source & Target

        public OutputViewModel Source { get; }

        public string SourceField
        {
            get
            {
                if (Source.Type == ConnectionType.Link && Source.Title == "self")
                {
                    return "0x00000000";
                }

                return Source.Title;
            }
        }

        public NodeBaseModel SourceNode { get; private set; }

        public InputViewModel Target { get; }
        public string TargetField
        {
            get
            {
                if (Target.Type == ConnectionType.Link && Target.Title == "self")
                {
                    return "0x00000000";
                }

                return Target.Title;
            }
        }

        public NodeBaseModel TargetNode { get; private set; }

        #endregion

        #region Connection Color

        public ConnectionType Type { get; }

        private bool _isHighlighted;
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                OnPropertyChanged(nameof(IsHighlighted));
                OnPropertyChanged(nameof(ConnectionColor));
            }
        }
        public SolidColorBrush ConnectionColor
        {
            get
            {
                if (!_isHighlighted)
                {
                    switch (Type)
                    {
                        case ConnectionType.Event:
                            return new SolidColorBrush(Colors.White); 
                        case ConnectionType.Property:
                            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF21"));
                        case ConnectionType.Link:
                            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0094FF"));
                        default:
                            return new SolidColorBrush(Colors.White);
                    }
                }
                else
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEE8AB"));
                }
            }
        }

        #endregion

        #region Connection Status

        private EditorStatus _status;

        public EditorStatus ConnectionStatus
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(ConnectionStatus));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusThickness));
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                switch (_status)
                {
                    case EditorStatus.Good:
                    {
                        return new SolidColorBrush(Colors.Black);
                    }
                    case EditorStatus.Warning:
                    {
                        return new SolidColorBrush(Colors.Goldenrod);
                    }
                    case EditorStatus.Error:
                    {
                        return new SolidColorBrush(Colors.Red);
                    }
                    default:
                    {
                        return new SolidColorBrush(Colors.Red);
                    }
                }
            }
        }

        public double StatusThickness
        {
            get
            {
                switch (_status)
                {
                    case EditorStatus.Good:
                    {
                        return 0.15;
                    }
                    case EditorStatus.Warning:
                    {
                        return 7;
                    }
                    case EditorStatus.Error:
                    {
                        return 8;
                    }
                    default:
                    {
                        return 0.25;
                    }
                }
            }
        }

        #endregion

        #region Curve

        public Point CurvePoint1
        {
            get
            {
                if (EditorUtils.CStyle == ConnectionStyle.Curvy)
                {
                    //The curve point is just the average of the 2 points
                    return new Point(Source.Anchor.X + 85,
                        Source.Anchor.Y);
                }
                else
                {
                    return new Point(Source.Anchor.X + 25,
                        Source.Anchor.Y);
                }
            }
        }

        public Point CurvePoint2
        {
            get
            {
                if (EditorUtils.CStyle == ConnectionStyle.Curvy)
                {
                    //The curve point is just the average of the 2 points
                    return new Point(Target.Anchor.X - 85,
                        Target.Anchor.Y);
                }
                else
                {
                    return new Point(Target.Anchor.X - 25,
                        Target.Anchor.Y);
                }
            }
        }

        #endregion

        #endregion

        #region Comparison & Constructor

        public ConnectionViewModel(OutputViewModel source, InputViewModel target, ConnectionType type = ConnectionType.Property)
        {
            Source = source;
            Target = target;

            Source.IsConnected = true;
            Target.IsConnected = true;

            Parallel.ForEach(EditorUtils.CurrentEditor.Nodes, node =>
            {
                if (node.Inputs.Contains(Target))
                {
                    TargetNode = node;
                }
                if (node.Outputs.Contains(Source))
                {
                    SourceNode = node;
                }
            });

            Type = type;
            
            Source.PropertyChanged += OnPropertyChanged;
            Target.PropertyChanged += OnPropertyChanged;
            SourceNode.PropertyChanged += OnPropertyChanged;
            TargetNode.PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// Compares if a connection object or <see cref="ConnectionViewModel"/> is equal to this.
        /// </summary>
        /// <param name="obj">any connection(property link or event) or <see cref="ConnectionViewModel"/></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            dynamic connectionB = null;
            
            //Some transient connections may have their objects as null
            if (Object == null)
            {
                //In which case we need to call the base equals
                return base.Equals(obj);
            }
            
            if (obj != null)
            {
                if (obj.GetType() == GetType())
                {
                    ConnectionViewModel connection2 = ((ConnectionViewModel)obj);
                    if (connection2.Type == Type)
                    {
                        connectionB = connection2.Object;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (obj.GetType() == Object.GetType())
                {
                    connectionB = obj;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            PointerRef sourceA = Object.Source;
            PointerRef sourceB = connectionB.Source;
            
            PointerRef targetA = Object.Target;
            PointerRef targetB = connectionB.Target;

            if (targetA == targetB
                && sourceA == sourceB)
            {
                switch (Type)
                {
                    default:
                    {
                        if (Object.SourceField.ToString() ==
                            connectionB.SourceField.ToString()
                            && Object.TargetField.ToString() ==
                            connectionB.TargetField.ToString())
                        {
                            return true;
                        }
                        break;   
                    }
                    case ConnectionType.Event:
                    {
                        if (Object.SourceEvent.Name.ToString() ==
                            connectionB.SourceEvent.Name.ToString()
                            && Object.TargetEvent.Name.ToString() ==
                            connectionB.TargetEvent.Name.ToString())
                        {
                            return true;
                        }
                        break;  
                    }
                }
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ Type.GetHashCode();
                hash = (hash * 16777619) ^ SourceField.GetHashCode();
                hash = (hash * 16777619) ^ TargetField.GetHashCode();
                hash = (hash * 16777619) ^ Source.GetHashCode();
                hash = (hash * 16777619) ^ Target.GetHashCode();
                
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{SourceNode.ObjectType}/{SourceNode.Name}-{Source}->{TargetNode.ObjectType}/{TargetNode.Name}-{Target}";
        }

        #endregion

        #region Property Changing

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// Basically just a way to update CurvePoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Anchor":
                {
                    OnPropertyChanged(nameof(CurvePoint1));
                    OnPropertyChanged(nameof(CurvePoint2));
                } break;
                case "IsSelected":
                {
                    _isHighlighted = SourceNode.IsSelected || TargetNode.IsSelected; 
                    OnPropertyChanged(nameof(IsHighlighted));
                    OnPropertyChanged(nameof(ConnectionColor));
                } break;
                default:
                {
                    PropertyChanged?.Invoke(sender, e);
                } break;
            }
        }
        
        #endregion
    }

    #endregion
}