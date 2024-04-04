using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Shared.Logic.Timing
{
    public class DelayEntityData : EntityNode
    {
        public override string ObjectType => "DelayEntityData";

        public override void OnCreation()
        {
            base.OnCreation();

            AddInput("Delay", ConnectionType.Property, Realm);
            AddInput("In", ConnectionType.Event, Realm);
            AddOutput("Out", ConnectionType.Event, Realm);
            
            AddFooter($"Delay: {TryGetProperty("Delay")}");
            if ((bool)TryGetProperty("AutoStart"))
            {
                AddFooter("Auto starts");
            }
            
            if ((bool)TryGetProperty("RunOnce"))
            {
                AddFooter("Runs once");
            }
        }

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            ClearFooter();
            switch (args.Item.Name)
            {
                case "Delay":
                {
                    AddFooter($"Delay: {TryGetProperty("Delay")}");
                } break;
                case "AutoStart":
                {
                    if ((bool)TryGetProperty("AutoStart"))
                    {
                        AddFooter("Auto starts");
                    }
                } break;
                case "RunOnce":
                {
                    if ((bool)TryGetProperty("RunOnce"))
                    {
                        AddFooter("Runs once");
                    }
                } break;
            }
        }
    }
}