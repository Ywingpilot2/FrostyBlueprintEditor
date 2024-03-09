using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.NodeTest.Nodes;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Status;

namespace BlueprintEditorPlugin.Editors.NodeTest
{
    public partial class NodeTest : UserControl, IGraphEditor
    {
        public INodeWrangler NodeWrangler { get; set; }
        public EditorStatusArgs CurrentStatus { get; set; }
        
        public NodeTest()
        {
            InitializeComponent();
            NodeWrangler = new BaseNodeWrangler();
            
            NodeWrangler.AddNode(new TestNode());
            NodeWrangler.AddNode(new TestNode());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public void CheckStatus()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateStatus()
        {
            throw new System.NotImplementedException();
        }

        public void SetStatus(EditorStatusArgs args)
        {
            throw new System.NotImplementedException();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object addedItem in e.AddedItems)
            {
                ((IVertex)addedItem).IsSelected = true;
            }

            foreach (object removedItem in e.RemovedItems)
            {
                ((IVertex)removedItem).IsSelected = false;
            }
        }
    }
}