using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;

namespace BlueprintEditorPlugin.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RegisterEntityNode : Attribute
    {
        public Type EntityNodeExtension { get; set; }

        /// <summary>
        /// Registers a mapping for an entity node. 
        /// </summary>
        /// <param name="entityNodeExtension">type of the extension. Must be a subclass of <see cref="EntityNode"/></param>
        public RegisterEntityNode(Type entityNodeExtension)
        {
            EntityNodeExtension = entityNodeExtension;
        }
    }
}