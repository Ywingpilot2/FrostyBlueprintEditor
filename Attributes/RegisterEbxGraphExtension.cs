using System;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;

namespace BlueprintEditorPlugin.Attributes
{
    /// <summary>
    /// This registers a custom extension to an <see cref="EntityNode"/> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterEbxLoaderExtension : Attribute
    {
        public Type EbxLoaderExtension { get; private set; }

        public RegisterEbxLoaderExtension (Type extension)
        {
            EbxLoaderExtension = extension;
        }
    }
    
    /// <summary>
    /// This registers a custom extension to an <see cref="EntityNode"/> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterEbxEditorExtension : Attribute
    {
        public Type EbxEditorExtension { get; private set; }

        public RegisterEbxEditorExtension (Type extension)
        {
            EbxEditorExtension = extension;
        }
    }
}