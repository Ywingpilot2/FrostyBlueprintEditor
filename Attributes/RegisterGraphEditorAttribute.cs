using System;
using BlueprintEditorPlugin.Editors.GraphEditor;

namespace BlueprintEditorPlugin.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterGraphEditor : Attribute
    {
        public Type GraphType { get; set; }

        /// <summary>
        /// Registers a custom Graph Editor. Type must be an extension of <see cref="IGraphEditor"/>
        /// </summary>
        /// <param name="graphEditorType"></param>
        public RegisterGraphEditor(Type graphEditorType)
        {
            GraphType = graphEditorType;
        }
    }
}