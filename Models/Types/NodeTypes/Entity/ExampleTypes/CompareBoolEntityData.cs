using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;

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
        /// Don't use an initializer when working with these, instead, override the OnCreation method.
        /// This triggers when the node gets created
        /// that way you can do things like change the Name based on one of its inputs, or one of the objects properties
        /// </summary>
        public override void OnCreation()
        {
            //The "Object" property allows you to fetch the property grid values of this node
            Name = Object.__Id; //This will fetch the Id and set the name as that
        }
    }
}