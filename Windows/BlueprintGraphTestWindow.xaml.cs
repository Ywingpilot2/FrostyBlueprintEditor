using System;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using FrostySdk;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintGraphTestWindow : Window
    {
        public BlueprintGraphTestWindow()
        {
            InitializeComponent();
            GraphEditor.InitializeComponent();
            EntityNodeWrangler entityNodeWrangler = (EntityNodeWrangler)GraphEditor.NodeWrangler;

            EntityNode testnode1 = new EntityNode(TypeLibrary.CreateObject("BoolEntityData"), Guid.NewGuid(), GraphEditor.NodeWrangler);
            EntityNode testnode2 = new EntityNode(TypeLibrary.CreateObject("BoolEntityData"), Guid.NewGuid(), GraphEditor.NodeWrangler);
            
            testnode1.Inputs.Add(new EventInput("testi", testnode1));
            testnode2.Outputs.Add(new EventOutput("testo", testnode2));
            
            entityNodeWrangler.AddNodeTransient(testnode1);
            entityNodeWrangler.AddNodeTransient(testnode2);
        }
    }
}