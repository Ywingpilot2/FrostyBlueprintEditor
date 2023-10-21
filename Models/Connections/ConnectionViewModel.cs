using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using BlueprintEditor.Models.Types;
using BlueprintEditor.Models.Types.NodeTypes;
using BlueprintEditor.Utils;
using FrostySdk.Ebx;

namespace BlueprintEditor.Models.Connections
{
    #region Connections

    /// <summary>
    /// A connection.
    /// </summary>
    public class ConnectionViewModel
    {
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

        private NodeBaseModel _sourceNode;
        public NodeBaseModel SourceNode => _sourceNode;

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

        private NodeBaseModel _targetNode;
        public NodeBaseModel TargetNode => _targetNode;

        public dynamic Object { get; set; }
        
        public ConnectionType Type { get; }
        public SolidColorBrush ConnectionColor
        {
            get
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
        }

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
                    _targetNode = node;
                }
                if (node.Outputs.Contains(Source))
                {
                    _sourceNode = node;
                }
            });

            Type = type;
        }

        /// <summary>
        /// Compares if a connection object or <see cref="ConnectionViewModel"/> is equal to this.
        /// NOTICE: DO NOT USE THIS METHOD FOR THINGS OTHER THEN EVENT CONNECTIONS, PROPERTY CONNECTIONS, OR LINK CONNECTIONS
        /// (so e.g, if you are editing a file that isn't a blueprint, such as a ScalableEmitterDocument)
        /// </summary>
        /// <param name="obj">any connection(property link or event) or <see cref="ConnectionViewModel"/></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            dynamic connectionB = null;
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

            if (((dynamic)sourceA.Internal).GetInstanceGuid() == ((dynamic)sourceB.Internal).GetInstanceGuid()
                && ((dynamic)targetA.Internal).GetInstanceGuid() == ((dynamic)targetB.Internal).GetInstanceGuid()
                && sourceA.Internal.GetType().ToString() == sourceB.Internal.GetType().ToString())
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
    }
    
    #endregion
}