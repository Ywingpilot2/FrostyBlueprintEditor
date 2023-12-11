using System;
using System.Collections.Generic;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Entity;
using FrostySdk;

namespace BlueprintEditorPlugin.Attributes
{
    /// <summary>
    /// This registers a custom extension to an <see cref="EntityNode"/> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterEbxLoaderExtension : Attribute
    {
        public Type EbxLoaderExtension { get; }
        
        /// <summary>
        /// The games this editor is valid for. If this node is valid for all games, don't override.
        /// This should be the name as seen in <see cref="ProfilesLibrary"/>
        /// </summary>
        public virtual List<string> ValidForGames { get; }
        
        /// <param name="extension"></param>
        /// <param name="validGames">This should be the name as seen in <see cref="ProfilesLibrary"/></param>
        public RegisterEbxLoaderExtension (Type extension, List<string> validGames = null)
        {
            EbxLoaderExtension = extension;
            ValidForGames = validGames;
        }
    }
    
    /// <summary>
    /// This registers a custom extension to an <see cref="EntityNode"/> 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterEbxEditorExtension : Attribute
    {
        public Type EbxEditorExtension { get; }
        
        /// <summary>
        /// The games this editor is valid for. If this node is valid for all games, don't override.
        /// This should be the name as seen in <see cref="ProfilesLibrary"/>
        /// </summary>
        public virtual List<string> ValidForGames { get; }

        /// <param name="extension"></param>
        /// <param name="validGames">This should be the name as seen in <see cref="ProfilesLibrary"/></param>
        public RegisterEbxEditorExtension (Type extension, List<string> validGames = null)
        {
            EbxEditorExtension = extension;
            ValidForGames = validGames;
        }
    }
}