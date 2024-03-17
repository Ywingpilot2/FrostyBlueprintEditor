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
        public INodeWrangler NodeWrangler { get; }

        #region Visual info

        public virtual string Header { get; set; }
        public virtual string ToolTip { get; set; }
        
        private bool _selected;
        public Size Size { get; set; }

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

        #endregion

        public virtual ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public virtual ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();

        #region Port Management

        private protected Dictionary<string, IPort> CachedInputs { get; } = new Dictionary<string, IPort>();
        private protected Dictionary<string, IPort> CachedOutputs { get; } = new Dictionary<string, IPort>();

        public virtual IPort GetInput(string name)
        {
            if (!CachedInputs.Keys.Contains(name))
                return null;
            return CachedInputs[name];
        }
        public virtual IPort GetOutput(string name)
        {
            if (!CachedOutputs.Keys.Contains(name))
                return null;
            return CachedOutputs[name];
        }

        public virtual void AddInput(IPort input)
        {
            if (CachedInputs.ContainsKey(input.Name))
                return;
            
            Inputs.Add(input as BaseInput);
            CachedInputs.Add(input.Name, input as BaseInput);
        }
        
        public virtual void AddOutput(IPort output)
        {
            if (CachedOutputs.ContainsKey(output.Name))
                return;
            
            Outputs.Add(output as BaseOutput);
            CachedOutputs.Add(output.Name, output as BaseOutput);
        }
        
        public virtual void RemoveInput(IPort input)
        {
            Inputs.Remove(input as BaseInput);
            CachedInputs.Remove(input.Name);
        }
        public virtual void RemoveOutput(IPort output)
        {
            Outputs.Remove(output as BaseOutput);
            CachedOutputs.Remove(output.Name);
        }

        #endregion
        
        public void OnInputUpdated(IPort port)
        {
            
        }

        public void OnOutputUpdated(IPort port)
        {
            
        }

        public void AddPort(IPort port)
        {
            if (port.Direction == PortDirection.In)
            {
                Inputs.Add(port);
            }
            else
            {
                Outputs.Add(port);
            }
        }

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

        /// <summary>
        /// Occurs whenever a node is first created
        /// </summary>
        public void OnCreation()
        {
        }

        protected BaseNode()
        {
        }
        
        public BaseNode(INodeWrangler nodeWrangler)
        {
            NodeWrangler = nodeWrangler;
        }
    }
}