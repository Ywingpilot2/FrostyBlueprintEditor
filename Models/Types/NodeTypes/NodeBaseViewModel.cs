using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Types.EbxEditorTypes;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes
{
    /// <summary>
    /// A single node
    /// </summary>
    public class NodeBaseModel : INotifyPropertyChanged
    {
        #region Node Display

        private string _name;
        /// <summary>
        /// The name that will be displayed
        /// </summary>
        public virtual string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// The documentation for this node
        /// </summary>
        public virtual string Documentation => "";

        public virtual SolidColorBrush HeaderColor { get; set; } =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F3F"));

        #endregion

        #region Object Info

        public virtual string ObjectType { get; set; } = "null";
        public virtual bool IsTransient => false;

        /// <summary>
        /// The object this node belongs to/is displayed in the property grid.
        /// </summary>
        public object Object { get; set; }

        #endregion

        #region Node Info

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

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        public virtual ObservableCollection<InputViewModel> Inputs { get; set; } = new ObservableCollection<InputViewModel>();
        public virtual ObservableCollection<OutputViewModel> Outputs { get; set; } = new ObservableCollection<OutputViewModel>();

        #endregion

        #region Property Changed

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// Gets the input of the specified name from the node
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
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
                if (FrostySdk.Utils.HashString(input.Title).ToString("x8") == name.Replace("0x", "")
                    || FrostySdk.Utils.HashString(name).ToString("x8") == input.Title.Replace("0x", ""))
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
                if (FrostySdk.Utils.HashString(output.Title).ToString("x8") == name.Replace("0x", "")
                    || FrostySdk.Utils.HashString(name).ToString("x8") == output.Title.Replace("0x", ""))
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
        /// This method executes whenever a completely fresh and new instance of the node is created.
        /// </summary>
        public virtual void OnCreateNew()
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
        public virtual void OnModified(ItemModifiedEventArgs args)
        {
            
        }

        #endregion

        #region Comparison Methods

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
    
    #region Inputs and Outputs

    public class PortBaseModel : INotifyPropertyChanged
    {
        private string _displayname;
        
        /// <summary>
        /// The name of this input that is displayed in the UI
        /// </summary>
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayname) ? _displayname : Title;
            set
            {
                _displayname = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }

        private string _title;
        /// <summary>
        /// The actual name of this input
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                _displayname = null;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }
        
        public ConnectionType Type { get; set; } = ConnectionType.Property;
        public ConnectionRealm Realm { get; set; } = ConnectionRealm.Invalid;

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

        public override string ToString()
        {
            return Title;
        }
    }

    /// <summary>
    /// A port you can plug into
    /// </summary>
    public class InputViewModel : PortBaseModel
    {
        public PropertyType PropertyConnectionType { get; set; }
    }
    
    /// <summary>
    /// A port you can get an output from
    /// </summary>
    public class OutputViewModel : PortBaseModel
    {
    }

    #endregion
}