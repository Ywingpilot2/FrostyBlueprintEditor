using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Utils;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.ExampleTypes
{
    /// <summary>
    /// In order to apply, this class needs to be an extension to NodeBaseModel
    /// This extension also grants you all of the methods and properties needed
    /// This CompareBool will act as a simple example of a node with some inputs and outputs of varying types
    /// It will also have the name be whatever the Id is set to in the Property Grid
    /// For a more advanced demonstration, <see cref="SelectEventEntityData"/>
    /// </summary>
    public class CompareBoolEntityData : EntityNode
    {
        /// <summary>
        /// This is the name that will be displayed in the editor.
        /// This can be set to whatever you want, and can also be modified via code.
        /// </summary>
        public override string Name { get; set; } = "Compare Bool";

        /// <summary>
        /// This allows us to add text to the "docbox" at the bottom of the Toolbox
        /// </summary>
        public override string Documentation { get; } = "This node takes a boolean input, then(either on start, when it changes, or always) sends a different event depending on if its true or false.";

        /// <summary>
        /// This is the name of the type this applies to.
        /// This HAS to be the exact name of the type, so in this case, CompareBoolEntityData
        /// This value is static.
        /// </summary>
        public override string ObjectType { get; set; } = "CompareBoolEntityData";

        /// <summary>
        /// These are all of the inputs this has.
        /// Each input allows you to customize the Title, so its name
        /// And its type, so Event, Property, and Link
        /// </summary>
        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "Bool", Type = ConnectionType.Property},
                new InputViewModel() {Title = "In", Type = ConnectionType.Event},
            };

        /// <summary>
        /// These are all of the outputs this has.
        /// Each input allows you to customize the Title, so its name
        /// And its type, so Event, Property, and Link
        /// </summary>
        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "OnTrue", Type = ConnectionType.Event},
                new OutputViewModel() {Title = "OnFalse", Type = ConnectionType.Event}
            };

        /// <summary>
        /// Don't use an initializer/constructor when working with these, instead, override the OnCreation method.
        /// This triggers when the node gets created
        /// that way you can do things like change the Name based on one of its inputs, or one of the objects properties
        /// </summary>
        public override void OnCreation()
        {
            //The "Object" property allows you to fetch the property grid values of this node
            Name = ((dynamic)Object).__Id; //This will fetch the Id and set the name as that

            //We want to make sure our Inputs and Outputs have the same realm as our object, so we can just use the NodeUtils method for it
            //NodeUtils provides a variety of utilities which can help in the process of creating node extensions
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
        /// This triggers whenever the node is modified in the property grid or elsewhere.
        /// In our case, just doing OnCreation over again is fine
        /// </summary>
        public override void OnModified(ItemModifiedEventArgs args)
        {
            OnCreation();
        }
    }
}