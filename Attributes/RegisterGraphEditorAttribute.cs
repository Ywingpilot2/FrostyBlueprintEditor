using System;
using BlueprintEditorPlugin.Editors.GraphEditor;

namespace BlueprintEditorPlugin.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterEbxGraphEditor : Attribute
    {
        public Type GraphType { get; set; }

        /// <summary>
        /// Registers a custom Graph Editor. Type must be an extension of <see cref="IEbxGraphEditor"/>
        /// </summary>
        /// <param name="graphEditorType"></param>
        public RegisterEbxGraphEditor(Type graphEditorType)
        {
            GraphType = graphEditorType;
        }
    }
}