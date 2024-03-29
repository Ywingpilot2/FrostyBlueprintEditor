using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Connections;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.TypeMapping.Comparison
{
    public class CompareBool : EntityNode
    {
        public override string ObjectType => "CompareBoolEntityData";
        public override string ToolTip => "This node sends an output event depending on if the Input property is true";

        public override void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            base.OnObjectModified(sender, args);

            Footer = null;
            
            if ((bool)TryGetProperty("TriggerOnStart"))
            {
                Footer = "Triggers on start";
            }

            if ((bool)TryGetProperty("TriggerOnPropertyChange"))
            {
                if (Footer != null)
                {
                    Footer += ", ";
                }
                else
                {
                    Footer = "Triggers ";
                }

                Footer += "On property changed";
            }

            if (TryGetProperty("AlwaysSend") != null && (bool)TryGetProperty("AlwaysSend"))
            {
                if (Footer != null)
                {
                    Footer += "\n";
                }

                Footer += "Always sends outputs";
            }
            
            if (TryGetProperty("AlwaysSendOnEvent") != null && (bool)TryGetProperty("AlwaysSendOnEvent"))
            {
                if (Footer != null)
                {
                    Footer += "\n";
                }

                Footer += "Always sends when In is triggered";
            }
        }

        public override void OnCreation()
        {
            base.OnCreation();
            AddOutput("OnTrue", ConnectionType.Event, Realm);
            AddOutput("OnFalse", ConnectionType.Event, Realm);
            
            AddInput("Bool", ConnectionType.Property, Realm);
            AddInput("In", ConnectionType.Event, Realm);

            if ((bool)TryGetProperty("TriggerOnStart"))
            {
                Footer = "Triggers on start";
            }

            if ((bool)TryGetProperty("TriggerOnPropertyChange"))
            {
                if (Footer != null)
                {
                    Footer += ", ";
                }
                else
                {
                    Footer = "Triggers ";
                }

                Footer += "On property changed";
            }
            
            if (TryGetProperty("AlwaysSend") != null && (bool)TryGetProperty("AlwaysSend"))
            {
                if (Footer != null)
                {
                    Footer += "\n";
                }

                Footer += "Always sends outputs";
            }
            
            if (TryGetProperty("AlwaysSendOnEvent") != null && (bool)TryGetProperty("AlwaysSendOnEvent"))
            {
                if (Footer != null)
                {
                    Footer += "\n";
                }

                Footer += "Always sends when In is triggered";
            }
        }
    }
}