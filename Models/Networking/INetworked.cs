using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;

namespace BlueprintEditorPlugin.Models.Networking
{
    public interface INetworked
    {
        Realm Realm { get; set; }
        Realm ParseRealm(object obj);
        
        /// <summary>
        /// For use when a realm is set to <see cref="Realm"/>.Any or <see cref="Realm"/>.Invalid, this determines a realm based on variables surrounding it.
        /// </summary>
        /// <returns>An assumed realm based off of this items properties or outside factors.</returns>
        Realm DetermineRealm();

        /// <summary>
        /// Fixes a realm based on what <see cref="DetermineRealm"/> decides. If needed, will force a realm of <see cref="Realm"/>.Any to be a proper value.
        /// </summary>
        void FixRealm();
    }
}