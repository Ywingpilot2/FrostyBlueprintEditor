using System;
using Frosty.Core;

namespace BlueprintEditorPlugin.Models.Types.EbxEditorTypes
{
    public class ObjectFlagsHelper
    {
        public string GuidMask { get; set; }
        public bool ClientEvent { get; set; }
        public bool ServerEvent { get; set; }
        public bool ClientProperty { get; set; }
        public bool ServerProperty { get; set; }
        public bool ClientLinkSource { get; set; }
        public bool ServerLinkSource { get; set; }
        public bool UnusedFlag { get; set; }

        /// <summary>
        /// Credit to github.com/Mophead01 for the Object Flags parser
        /// </summary>
        /// <param name="flags"></param>
        public ObjectFlagsHelper(uint flags)
        {
            ClientEvent = Convert.ToBoolean((flags & 33554432) != 0 ? 1 : 0);
            ServerEvent = Convert.ToBoolean((flags & 67108864) != 0 ? 1 : 0);
            ClientProperty = Convert.ToBoolean((flags & 134217728) != 0 ? 1 : 0);
            ServerProperty = Convert.ToBoolean((flags & 268435456) != 0 ? 1 : 0);
            ClientLinkSource = Convert.ToBoolean((flags & 536870912) != 0 ? 1 : 0);
            ServerLinkSource = Convert.ToBoolean((flags & 1073741824) != 0 ? 1 : 0);
            UnusedFlag = Convert.ToBoolean((flags & 2147483648) != 0 ? 1 : 0);
            GuidMask = (flags & 33554431).ToString("X2").ToLower();
        }
        
        public static implicit operator uint(ObjectFlagsHelper flagsHelper) => flagsHelper.GetAsFlags();
        public static explicit operator ObjectFlagsHelper(uint flags) => new ObjectFlagsHelper(flags);

        /// <summary>
        /// I'm too lazy to do what I did with PropertyFlagsHelper so we will just use this method to get the flags instead
        /// Credit to github.com/Mophead01 for the Object Flags creation
        /// </summary>
        /// <returns></returns>
        public uint GetAsFlags()
        {
            bool isTooLarge = !uint.TryParse(GuidMask, System.Globalization.NumberStyles.HexNumber, null, out var newFlags);
            if (isTooLarge || newFlags > 33554431)
            {
                newFlags = 0;
                App.Logger.LogWarning("Invalid Guid Mask");
            }

            if (ClientEvent)
            {
                newFlags |= 33554432;
            }
            if (ServerEvent)
            {
                newFlags |= 67108864;
            }
            if (ClientProperty)
            {
                newFlags |= 134217728;
            }
            if (ServerProperty)
            {
                newFlags |= 268435456;
            }
            if (ClientLinkSource)
            {
                newFlags |= 536870912;
            }
            if (ServerLinkSource)
            {
                newFlags |= 1073741824;
            }
            if (UnusedFlag)
            {
                newFlags |= 2147483648;
            }

            return newFlags;
        }
    }
}