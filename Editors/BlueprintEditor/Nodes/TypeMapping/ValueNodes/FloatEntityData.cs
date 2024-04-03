using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.ValueNodes
{
    public class FloatNode : BaseNumberNode
    {
        public override string ObjectType => "FloatEntityData";
        public override string ToolTip => "This node stores a float, or a number with a decimal, with an adjustable value";
    }
}