using System.Diagnostics;
using System.Windows;
using Frosty.Controls;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintEditorControlsWindow : FrostyDockableWindow
    {
        public BlueprintEditorControlsWindow()
        {
            InitializeComponent();
            ControlsText.Text = "Delete: Removes the selected nodes " +
                                "\nShift + D: Duplicate the selected nodes" +
                                "\nShift + Left Click: Add a node to the current selection" +
                                "\nAlt + Left Click: Remove a node from the current selection" +
                                "\nShift + Right Click: Place the currently selected node in the toolbox at the mouse position";

            CreditsText.Text = "Emanuel Miroiu - Nodify library used for Node Editor controls\n" +
                               "MagixGames - Optimizations to loading Ebx into graphed form\n" +
                               "Mophead01 - Original ObjectFlags calculation implementation\n" +
                               "CosmicDreams - Assistance with determining realms automatically\n" +
                               "GalaxyMan2015 - Original implementation of a BlueprintEditor\n" +
                               "CadeEvs - Graph sorting algorithm\n" +
                               "Y wingpilot2 - Creator of the Blueprint Editor";
        }

        private void GithubButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Ywingpilot2/FrostyBlueprintEditor");
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}