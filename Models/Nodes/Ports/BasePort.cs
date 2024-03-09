using System.ComponentModel;
using System.Windows;

namespace BlueprintEditorPlugin.Models.Nodes.Ports
{
    public abstract class BasePort : IPort
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name { get; set; }
        public abstract PortDirection Direction { get; }

        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set
            {
                _anchor = value;
                NotifyPropertyChanged(nameof(Anchor));
            }
        }
        public INode Node { get; protected set; }
        
        private bool _isConnected;
        public bool IsConnected
        {
            set
            {
                _isConnected = value;
                NotifyPropertyChanged(nameof(IsConnected));
            }
            get => _isConnected;
        }
        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class BaseInput : BasePort
    {
        public override PortDirection Direction => PortDirection.In;
        public BaseInput(string name, INode node)
        {
            Name = name;
            Node = node;
        }
    }
    
    public class BaseOutput : BasePort
    {
        public override PortDirection Direction => PortDirection.Out;
        public BaseOutput(string name, INode node)
        {
            Name = name;
            Node = node;
        }
    }
}