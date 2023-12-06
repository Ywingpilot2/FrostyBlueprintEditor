using System;
using System.Collections.Generic;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity
{
    public class EntityNode : NodeBaseModel
    {
        /// <summary>
        /// The games this node is valid for. If this node is valid for all games, don't override.
        /// This should be the name as seen in <see cref="ProfilesLibrary"/>
        /// </summary>
        public virtual List<string> ValidForGames { get; set; } = null;

        /// <summary>
        /// The guid of the file this node is in. This value is typically null if <see cref="PointerRefType"/> is Internal.
        /// </summary>
        public Guid FileGuid { get; set; }
        /// <summary>
        /// If <see cref="PointerRefType"/> is External, this is the Class Guid from the PointerRef. This value is typically null if <see cref="PointerRefType"/> is Internal.
        /// </summary>
        public Guid ClassGuid { get; set; }
        
        /// <summary>
        /// The internal guid of the object
        /// </summary>
        public AssetClassGuid InternalGuid { get; set; }
        
        public PointerRefType PointerRefType { get; set; }
    }
}