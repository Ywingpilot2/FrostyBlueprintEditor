using System.Collections.Generic;
using BlueprintEditorPlugin.Utils;
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
            EditorUtils.UpdateSettings();
        }
    }
}