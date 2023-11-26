using System;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Transient;

namespace BlueprintEditorPlugin.Attributes
{
    /// <summary>
    /// This registers a custom extension to an <see cref="EntityNode"/> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterEntityNodeExtension : Attribute
    {
        public Type EntityNodeExtension { get; private set; }

        public RegisterEntityNodeExtension (Type extension)
        {
            EntityNodeExtension = extension;
        }
    }
    
    /// <summary>
    /// This registers a custom extension to an <see cref="TransientNode"/> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterTransientNodeExtension : Attribute
    {
        public Type TransientNodeExtension { get; private set; }

        public RegisterTransientNodeExtension (Type extension)
        {
            TransientNodeExtension = extension;
        }
    }
}