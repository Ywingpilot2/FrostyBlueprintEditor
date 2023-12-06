using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using Frosty.Controls;
using Frosty.Core;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintEditorPortMenu : FrostyDockableWindow
    {
        private PortVisual _portVisual { get; set; }
        
        private EntityNode _node;
        public EntityNode Node
        {
            get => _node;
            set
            {
                _node = value;
                Initiate();
            }
        }
        
        public BlueprintEditorPortMenu()
        {
            InitializeComponent();
        }

        private void Initiate()
        {
            if (_node == null)
            {
                App.Logger.LogError("Node cannot be null");
                Close();
            }

            _portVisual = new PortVisual();
            PropertyGrid.Object = _portVisual;
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_portVisual.Title))
            {
                Close();
                return;
            }
            if (_portVisual.PortDirection == Direction.In)
            {
                InputViewModel input = _portVisual.ToInput();
                _node.Inputs.Add(input);
                _node.OnInputUpdated(input);
            }
            else
            {
                OutputViewModel output = _portVisual.ToOutput();
                _node.Outputs.Add(output);
                _node.OnOutputUpdated(output);
            }
            Close();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    internal enum Direction
    {
        In = 0,
        Out = 1
    }

    internal class PortVisual
    {
        public string Title { get; set; }
        public Direction PortDirection { get; set; }
        public ConnectionRealm Realm { get; set; }
        public ConnectionType Type { get; set; }

        public OutputViewModel ToOutput()
        {
            return new OutputViewModel { Title = Title, Realm = Realm, Type = Type };
        }
        
        public InputViewModel ToInput()
        {
            return new InputViewModel { Title = Title, Realm = Realm, Type = Type };
        }

        public PortBaseModel ToPort()
        {
            return new PortBaseModel { Title = Title, Realm = Realm, Type = Type };
        }

        public PortVisual()
        {
            Title = "";
            PortDirection = Direction.In;
            Realm = ConnectionRealm.ClientAndServer;
            Type = ConnectionType.Event;
        }
    }
}