using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.ExampleTypes;
using BlueprintEditorPlugin.Utils;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.Hubs
{
    /// <summary>
    /// This is a more advanced demonstration, for a simple demonstration <see cref="CompareBoolEntityData"/>
    /// This demonstrates creating events and properties based off of the property grid
    /// </summary>
    public class FloatHubEntityData : EntityNode
    {
        /// <summary>
        /// This is the name that will be displayed in the editor.
        /// This can be set to whatever you want, and can also be modified via code.
        /// </summary>
        public override string Name { get; set; } = "Float Hub";
        
        /// <summary>
        /// This is the name of the type this applies to.
        /// This HAS to be the exact name of the type, so in this case, CompareBoolEntityData
        /// This value is static.
        /// </summary>
        public override string ObjectType { get; set; } = "FloatHubEntityData";

        public override string Documentation { get; } = "This node outputs the input at the SelectedIndex.";

        /// <summary>
        /// These are all of the inputs this has.
        /// Each input allows you to customize the Title, so its name
        /// And its type, so Event, Property, and Link
        /// </summary>
        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "InputSelect", Type = ConnectionType.Property}
            };

        /// <summary>
        /// These are all of the outputs this has.
        /// Each input allows you to customize the Title, so its name
        /// And its type, so Event, Property, and Link
        /// </summary>
        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "Out", Type = ConnectionType.Property}
            };

        /// <summary>
        /// Don't use an initializer when working with these, instead, override the OnCreation method.
        /// This triggers when the node gets created
        /// that way you can do things like change the Name based on one of its inputs, or one of the objects properties
        /// </summary>
        public override void OnCreation()
        {
            base.OnCreation();
            foreach (UInt32 eventHash in ((dynamic)Object).HashedInput) //Go through all of the Events this SelectEvent has
            {
                //And for each one, add it to our Outputs
                Inputs.Add(new InputViewModel() {Title = $"0x{eventHash:x8}", Type = ConnectionType.Property});
            }
            
            foreach (InputViewModel input in Inputs)
            {
                NodeUtils.PortRealmFromObject(Object, input);
            }

            foreach (OutputViewModel output in Outputs)
            {
                NodeUtils.PortRealmFromObject(Object, output);
            }
        }

        /// <summary>
        /// This will trigger whenever the SelectEvent is modified
        /// Since we want to make sure our SelectEvent is in sync with the property grid, we redo our OnCreation
        /// </summary>
        public override void OnModified(ItemModifiedEventArgs args)
        {
            List<UInt32> events = ((dynamic)Object).HashedInput;
            switch (args.Item.Name)
            {
                case "HashedInput":
                {
                    switch (args.ModifiedArgs.Type)
                    {
                        case ItemModifiedTypes.Add:
                        {
                            UInt32 eventHash = events.Last();

                            Inputs.Add(new InputViewModel()
                                { Title = $"0x{eventHash:x8}", Type = ConnectionType.Property });
                        }
                            break;
                        case ItemModifiedTypes.Insert:
                        {
                            UInt32 eventHash = (UInt32)args.NewValue;

                            Inputs.Add(new InputViewModel()
                                { Title = $"0x{eventHash:x8}", Type = ConnectionType.Property });
                        }
                            break;
                        case ItemModifiedTypes.Clear:
                        {
                            for (var i = 0; i < Outputs.Count; i++)
                            {
                                InputViewModel input = Inputs[i];
                                if (input.Title.StartsWith("0x"))
                                {
                                    Inputs.Remove(input);
                                }
                            }
                        }
                            break;
                        case ItemModifiedTypes.Remove:
                        {
                            UInt32 eventHash = (UInt32)args.OldValue;

                            foreach (dynamic connection in EditorUtils.CurrentEditor.GetConnections(
                                         GetInput($"0x{eventHash:x8}", ConnectionType.Property)))
                            {
                                EditorUtils.CurrentEditor.Disconnect(connection);
                            }

                            Inputs.Remove(GetInput($"0x{eventHash:x8}", ConnectionType.Property));
                        }
                            break;
                    }
                } break;
                case "__Id":
                {
                    NotifyPropertyChanged(nameof(Name));
                } break;
                default:
                {
                    if (args.Item.Parent.Name == "HashedInput")
                    {
                        UInt32 eventHash = (UInt32)args.NewValue;
                        UInt32 oldHash = (UInt32)args.OldValue;
                        var input = GetInput($"0x{oldHash:x8}", ConnectionType.Property);
                        input.Title = $"0x{eventHash:x8}";
                        input.DisplayName = $"0x{eventHash:x8}";
                
                        //TODO: Update connections
                        foreach (ConnectionViewModel connection in EditorUtils.CurrentEditor.GetConnections(input))
                        {
                            EditorUtils.CurrentEditor.Disconnect(connection);
                        }
                    }
                } break;
            }
            
            foreach (InputViewModel input in Inputs)
            {
                NodeUtils.PortRealmFromObject(Object, input);
            }

            foreach (OutputViewModel output in Outputs)
            {
                NodeUtils.PortRealmFromObject(Object, output);
            }
        }
    }
}