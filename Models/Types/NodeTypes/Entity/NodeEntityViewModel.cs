using System.Collections.Generic;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity
{
    public class EntityNode : NodeBaseModel
    {
        /// <summary>
        /// The games this node is valid for. If this node is valid for all games, don't override.
        /// This should be the name as seen in <see cref="ProfilesLibrary"/>
        /// </summary>
        public virtual List<string> ValidForGames { get; set; } = null;

        public AssetClassGuid Guid { get; set; }

        #region Comparison

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            //Just use the standard Equals method
            if (Object == null)
            {
                return base.Equals(obj);
            }

            dynamic objectNode = null;
            if (obj.GetType() == GetType())
            {
                objectNode = ((EntityNode)obj).Object;
            }
            else if (obj.GetType() == Object.GetType())
            {
                objectNode = obj;
            }

            return objectNode != null && objectNode.GetInstanceGuid() == Object.GetInstanceGuid();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ ObjectType.GetHashCode();
                return hash;
            }
        }

        #endregion
    }
}