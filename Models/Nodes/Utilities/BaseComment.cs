using System.ComponentModel;
using System.Windows;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Nodes.Utilities
{
    public abstract class BaseComment : ITransient
    {
        #region Vertex data

        private string _header;
        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }

        private Point _location;
        public Point Location
        {
            get => _location;
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
        }

        private Size _size;
        public Size Size
        {
            get => _size;
            set
            {
                _size = value;
            }
        }

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
        
        public INodeWrangler NodeWrangler { get; }
        
        public virtual bool IsValid()
        {
            return true;
        }

        public virtual void OnCreation()
        {
        }

        public void OnDestruction()
        {
        }

        public abstract ITransient Load(NativeReader reader);

        public abstract void Save(NativeWriter writer);

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public override string ToString()
        {
            return Header ?? base.ToString();
        }
    }
}