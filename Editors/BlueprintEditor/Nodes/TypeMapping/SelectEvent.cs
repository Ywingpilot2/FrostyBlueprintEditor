using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using Frosty.Core.Controls;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping
{
    public class SelectEventNode : EntityNode
    {
        public override string ObjectType => "SelectEventEntityData";
        public override string ToolTip => "This node lets you to select between a list of output events";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("Selected", ConnectionType.Property, Realm);

            foreach (CString evnt in (dynamic)TryGetProperty("Events"))
            {
                if (evnt.IsNull())
                    continue;
                
                AddOutput(evnt.ToString(), ConnectionType.Event, Realm);
                AddInput($"Select{evnt.ToString()}", ConnectionType.Event);
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            // An event was edited
            if (args.Item.Parent.Name == "Events")
            {
                EntityInput input = GetInput($"Select{args.OldValue}", ConnectionType.Event);
                EntityOutput output = GetOutput(args.OldValue.ToString(), ConnectionType.Event);
                input.Name = args.NewValue.ToString();
                output.Name = $"Select{args.NewValue}";
            }
            // The list itself was edited
            else if (args.Item.Name == "Events")
            {
                switch (args.ModifiedArgs.Type)
                {
                    case ItemModifiedTypes.Insert:
                    case ItemModifiedTypes.Add:
                    {
                        foreach (CString evnt in (dynamic)TryGetProperty("Events"))
                        {
                            if (evnt.IsNull())
                                continue;
                            
                            if (GetOutput(evnt.ToString(), ConnectionType.Event) != null)
                                continue;

                            AddOutput(evnt.ToString(), ConnectionType.Event);
                            AddInput($"Select{evnt.ToString()}", ConnectionType.Event);
                        }
                    } break;
                    case ItemModifiedTypes.Remove:
                    {
                        CString eventName = (dynamic)args.OldValue;
                        if (eventName.IsNull())
                            break;
                        
                        EntityOutput output = GetOutput(eventName.ToString(), ConnectionType.Event);
                        RemoveOutput(output);

                        EntityInput input = GetInput($"Select{eventName.ToString()}", ConnectionType.Event);
                        RemoveInput(input);
                    } break;
                    case ItemModifiedTypes.Clear:
                    {
                        List<IPort> inputs = Inputs.ToList();
                        List<IPort> outputs = Outputs.ToList();

                        for (var i = 1; i < inputs.Count; i++)
                        {
                            IPort input = inputs[i];
                            RemoveInput((EntityInput)input);
                        }
                        
                        for (var i = 1; i < outputs.Count; i++)
                        {
                            IPort output = outputs[i];
                            RemoveOutput((EntityOutput)output);
                        }
                    } break;
                }
            }
        }
    }
}