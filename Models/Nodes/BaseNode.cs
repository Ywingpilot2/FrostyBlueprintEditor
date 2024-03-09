using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Models.Nodes
{
    public abstract class BaseNode : INode
    {
        protected BaseNodeWrangler NodeWrangler;

        #region Visual info

        public virtual string Header { get; set; }
        
        private bool _selected;
        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        #endregion

        #region Positional data

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

        private double _width;
        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                NotifyPropertyChanged(nameof(Width));
            }
            
        }
        private double _height;
        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                NotifyPropertyChanged(nameof(Height));
            }
            
        }

        #endregion

        public virtual ObservableCollection<BaseInput> Inputs { get; } = new ObservableCollection<BaseInput>();
        public virtual ObservableCollection<BaseOutput> Outputs { get; } = new ObservableCollection<BaseOutput>();

        #region Port Management

        private protected Dictionary<string, BaseInput> CachedInputs { get; } = new Dictionary<string, BaseInput>();
        private protected Dictionary<string, BaseOutput> CachedOutputs { get; } = new Dictionary<string, BaseOutput>();

        public virtual BaseInput GetInput(string name)
        {
            if (!CachedInputs.Keys.Contains(name))
                return null;
            return CachedInputs[name];
        }
        public virtual BaseOutput GetOutput(string name)
        {
            if (!CachedOutputs.Keys.Contains(name))
                return null;
            return CachedOutputs[name];
        }

        public virtual void AddPort(BaseInput input)
        {
            if (CachedInputs.ContainsKey(input.Name))
                return;
            
            Inputs.Add(input);
            CachedInputs.Add(input.Name, input);
        }
        
        public virtual void AddPort(BaseOutput output)
        {
            if (CachedOutputs.ContainsKey(output.Name))
                return;
            
            Outputs.Add(output);
            CachedOutputs.Add(output.Name, output);
        }
        
        public virtual void RemovePort(BaseInput input)
        {
            Inputs.Remove(input);
            CachedInputs.Remove(input.Name);
        }
        public virtual void RemovePort(BaseOutput output)
        {
            Outputs.Remove(output);
            CachedOutputs.Remove(output.Name);
        }

        #endregion

        #region Property Changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Status management
        
        public EditorStatusArgs CurrentStatus { get; set; }

        public void CheckStatus()
        {
            NotifyPropertyChanged(nameof(CurrentStatus));
        }

        public void UpdateStatus()
        {
        }

        public void SetStatus(EditorStatusArgs args)
        {
            CurrentStatus = args;
            CheckStatus();
        }

        #endregion
        
        public virtual bool IsValid()
        {
            return true;
        }

        public BaseNode()
        {
        }
        
        public BaseNode(BaseNodeWrangler nodeWrangler)
        {
            NodeWrangler = nodeWrangler;
        }
    }
}