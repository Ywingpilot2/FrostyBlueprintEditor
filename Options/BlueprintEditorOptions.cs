using System.Collections.Generic;
using Frosty.Core;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Misc;
using FrostySdk.Attributes;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Options
{
    public enum ConnectionStyle
    {
        Curvy = 0,
        Straight = 1,
        StartStop = 2
    }
    
    public class ConnectionStyleCombo : FrostyCustomComboDataEditor<ConnectionStyle, string>
    {
    }
    
    [DisplayName("Graph Editor Options")]
    public class BlueprintEditorOptions : OptionsExtension
    {
        private List<ConnectionStyle> _styles = new List<ConnectionStyle>()
        {
            ConnectionStyle.Curvy,
            ConnectionStyle.Straight,
            ConnectionStyle.StartStop
        };
        private List<string> _styleNames = new List<string>()
        {
            "Curvy",
            "Straight",
            "Start-Stop"
        };
        
        [Category("Connections")]
        [DisplayName("Connection Style")]
        [EbxFieldMeta(EbxFieldType.Struct)]
        [Editor(typeof(ConnectionStyleCombo))]
        public CustomComboData<ConnectionStyle, string> CStyle { get; set; }
        
        [Category("Connections")]
        [DisplayName("Thickness")]
        [EbxFieldMeta(EbxFieldType.Float32)]
        [Editor(typeof(FrostySliderEditor))]
        [SliderMinMax(1.0f, 8.0f, 0.1f, 2.0f, true)]
        public float WireThickness { get; set; }
        
        [Category("Connections")]
        [DisplayName("Connections over Nodes")]
        [Description("Whether or not to display connections on top of nodes")]
        public bool WiresOverVerts { get; set; }
        
        [Category("Ports")]
        [DisplayName("Size")]
        [Description("How large a port should be")]
        [EbxFieldMeta(EbxFieldType.Float32)]
        [Editor(typeof(FrostySliderEditor))]
        [SliderMinMax(1.0f, 10.0f, 0.1f, 2.0f, true)]
        public float PortSize { get; set; }
        
        [Category("Ports")]
        [DisplayName("Location")]
        [Description("Where on the horizontal axis of the node should the port be located. 0 is inside, 10 is outside")]
        [EbxFieldMeta(EbxFieldType.Float32)]
        [Editor(typeof(FrostySliderEditor))]
        [SliderMinMax(0.0f, 10.0f, 0.1f, 2.0f, true)]
        public float PortPosition { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Automatically redirect")]
        [Description("Automatically redirects ports if they have weak connections or loops")]
        public bool AutoRedirects { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Horizontal padding")]
        [Description("The amount of horizontal spacing between each node to maintain whenever automatically sorting")]
        public float VertXSpacing { get; set; }
        
        [Category("Layouts")]
        [DisplayName("Vertical padding")]
        [Description("The amount of vertical spacing between each node to maintain whenever automatically sorting")]
        public float VertYSpacing { get; set; }

        public override void Load()
        {
            CStyle = new CustomComboData<ConnectionStyle, string>(_styles, _styleNames);
            switch (Config.Get("ConnectionStyle", "StartStop"))
            {
                case "StartStop":
                {
                    CStyle.SelectedIndex = 2;
                } break;
                case "Straight":
                {
                    CStyle.SelectedIndex = 1;
                } break;
                case "Curvy":
                {
                    CStyle.SelectedIndex = 0;
                } break;
            }

            WireThickness = Config.Get("WireThickness", 4.0f);
            WiresOverVerts = Config.Get("WireOverVert", false);
            
            PortSize = Config.Get("PortSize", 6.0f);
            PortPosition = Config.Get("PortPos", 0.0f);
            
            AutoRedirects = Config.Get("AutoRedirects", false);
            VertXSpacing = Config.Get("VertXSpacing", 64.0f);
            VertYSpacing = Config.Get("VertYSpacing", 16.0f);
        }

        public override void Save()
        {
            switch (CStyle.SelectedValue)
            {
                case ConnectionStyle.Curvy:
                {
                    Config.Add("ConnectionStyle", "Curvy");
                } break;
                case ConnectionStyle.Straight:
                {
                    Config.Add("ConnectionStyle", "Straight");
                } break;
                case ConnectionStyle.StartStop:
                {
                    Config.Add("ConnectionStyle", "StartStop");
                } break;
            }
            
            Config.Add("WireThickness", WireThickness);
            Config.Add("WireOverVert", WiresOverVerts);
            
            Config.Add("PortSize", PortSize);
            Config.Add("PortPos", PortPosition);
            
            Config.Add("AutoRedirects", AutoRedirects);
            Config.Add("VertXSpacing", VertXSpacing);
            Config.Add("VertYSpacing", VertYSpacing);
            
            EditorOptions.Update();
        }
    }

    public static class EditorOptions
    {
        public static ConnectionStyle WireStyle { get; internal set; }
        public static double WireThickness { get; internal set; }
        public static bool WiresOververts { get; internal set; }
        
        public static double PortSize { get; internal set; }
        public static double InputPos { get; internal set; }
        public static double OutputPos { get; internal set; }

        public static bool AutoRedirects { get; internal set; }
        public static double VertXSpacing { get; internal set; }
        public static double VertYSpacing { get; internal set; }

        public static void Update()
        {
            switch (Config.Get("ConnectionStyle", "StartStop"))
            {
                case "StartStop":
                {
                    WireStyle = ConnectionStyle.StartStop;
                } break;
                case "Straight":
                {
                    WireStyle = ConnectionStyle.Straight;
                } break;
                case "Curvy":
                {
                    WireStyle = ConnectionStyle.Curvy;
                } break;
            }
            
            WireThickness = Config.Get("WireThickness", 4.0f);
            WiresOververts = Config.Get("WireOverVert", false);
            
            PortSize = (Config.Get("PortSize", 6.0f) * 0.1) * 15;
            OutputPos = Config.Get("PortPos", 0.0f);
            InputPos = OutputPos * -1.0f;
            
            AutoRedirects = Config.Get("AutoRedirects", false);
            VertXSpacing = Config.Get("VertXSpacing", 64.0f);
            VertYSpacing = Config.Get("VertYSpacing", 16.0f);
        }
    }
}