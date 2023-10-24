using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using BlueprintEditor.Models.Connections;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditor.Models.Types.NodeTypes
{
    /// <summary>
    /// A single node
    /// </summary>
    public class NodeBaseModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The name that will be displayed
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The documentation for this node
        /// </summary>
        public virtual string Documentation => "";
        
        /// <summary>
        /// The games this node is valid for. If this node is valid for all games, don't override.
        /// This should be the name as seen in <see cref="ProfilesLibrary"/>
        /// </summary>
        public virtual List<string> ValidForGames { get; set; } = null; 

        public virtual SolidColorBrush HeaderColor { get; set; } =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F3F"));
        public virtual string ObjectType { get; } = "null";
        public AssetClassGuid Guid { get; set; }

        /// <summary>
        /// The object this node belongs to.
        /// </summary>
        public dynamic Object { get; set; }
        
        private Point _location;

        public Point Location
        {
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
            get => _location;
        }

        public double RealWidth { get; private set; }
        
        /// <summary>
        /// Don't use this, instead use <see cref="RealWidth"/> (I fucked something up in WPF so this is the result).
        /// TODO: Fix this so DataBinding is to RealWidth instead(WPF should only set the value, but not get from it)
        /// </summary>
        public double _width
        {
            set => RealWidth = value;
        }
        
        public virtual ObservableCollection<InputViewModel> Inputs { get; set; } = new ObservableCollection<InputViewModel>();
        public virtual ObservableCollection<OutputViewModel> Outputs { get; set; } = new ObservableCollection<OutputViewModel>();
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Get Methods

        /// <summary>
        /// Gets the input of the specified name from the node
        /// </summary>
        /// <param name="name"></param>
        /// <param name="createIfNotFound">If set to true, if the method cannot find the specified input then it will create a new input of the name</param>
        /// <returns></returns>
        public InputViewModel GetInput(string name, ConnectionType type, bool createIfNotFound = false)
        {
            foreach (InputViewModel input in Inputs)
            {
                if (input.Type != type) continue;
                
                if (input.Title == name)
                {
                    return input;
                }
                
                //Check if the hashes match
                if (FrostySdk.Utils.HashString(input.Title).ToString() == name)
                {
                    return input;
                }
            }
            
            if (createIfNotFound)
            {
                var newInput = new InputViewModel() { Title = name, Type = type };
                Inputs.Add(newInput);
                return newInput;
            }

            return null;
        }

        /// <summary>
        /// Gets the input of the specified name from the node
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="createIfNotFound">If set to true, if the method cannot find the specified input then it will create a new input of the name</param>
        /// <returns></returns>
        public OutputViewModel GetOutput(string name, ConnectionType type, bool createIfNotFound = false)
        {
            foreach (OutputViewModel output in Outputs)
            {
                if (output.Type != type) continue;
                
                if (output.Title == name)
                {
                    return output;
                }
                
                //Check if the hashes match
                if ($"0x{FrostySdk.Utils.HashString(output.Title):x8}" == name)
                {
                    return output;
                }
            }

            if (createIfNotFound)
            {
                var newOutput = new OutputViewModel() { Title = name, Type = type };
                Outputs.Add(newOutput);
                return newOutput;
            }

            return null;
        }

        #endregion

        #region Event methods

        /// <summary>
        /// This method executes whenever the node is created
        /// </summary>
        public virtual void OnCreation()
        {
            
        }

        /// <summary>
        /// This method executes whenever the nodes Inputs are updated
        /// </summary>
        /// <param name="input">The input that was updated</param>
        public virtual void OnInputUpdated(InputViewModel input)
        {
            
        }

        /// <summary>
        /// This method executes whenever the nodes Outputs are updated
        /// </summary>
        /// <param name="output">The output that was updated</param>
        public virtual void OnOutputUpdated(OutputViewModel output)
        {
            
        }

        /// <summary>
        /// This method executes whenever the node is modified
        /// </summary>
        public virtual void OnModified()
        {
            
        }

        #endregion

        #region Comparison Methods

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            dynamic objectNode = null;
            if (obj.GetType() == GetType())
            {
                objectNode = ((NodeBaseModel)obj).Object;
            }
            else if (obj.GetType() == Object.GetType())
            {
                objectNode = obj;
            }

            return objectNode != null && objectNode.GetInstanceGuid() == Object.GetInstanceGuid();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ ObjectType.GetHashCode();
                return hash;
            }
        }

        #endregion

    }
    
    #region Inputs and Outputs

    /// <summary>
    /// A port you can plug in/out of
    /// </summary>
    public class InputViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public ConnectionType Type { get; set; } = ConnectionType.Property;

        public SolidColorBrush ConnectionColor
        {
            get
            {
                switch (Type)
                {
                    case ConnectionType.Event:
                        return new SolidColorBrush(Colors.White); 
                        break;
                    case ConnectionType.Property:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF21"));
                        break;
                    case ConnectionType.Link:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0094FF"));
                        break;
                    default:
                        return new SolidColorBrush(Colors.White);
                        break;
                }
            }
        }


        private Point _anchor;
        public Point Anchor
        {
            set
            {
                _anchor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
            }
            get => _anchor;
        }
        
        private bool _isConnected;
        public bool IsConnected
        {
            set
            {
                _isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
            }
            get => _isConnected;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    
    /// <summary>
    /// A port you can plug in/out of
    /// </summary>
    public class OutputViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public ConnectionType Type { get; set; } = ConnectionType.Property;
        public SolidColorBrush ConnectionColor
        {
            get
            {
                switch (Type)
                {
                    case ConnectionType.Event:
                        return new SolidColorBrush(Colors.White); 
                        break;
                    case ConnectionType.Property:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF21"));
                        break;
                    case ConnectionType.Link:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0094FF"));
                        break;
                    default:
                        return new SolidColorBrush(Colors.White);
                        break;
                }
            }
        }
        
        private Point _anchor;
        public Point Anchor
        {
            set
            {
                _anchor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
            }
            get => _anchor;
        }
        
        private bool _isConnected;
        public bool IsConnected
        {
            set
            {
                _isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
            }
            get => _isConnected;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    #endregion
}