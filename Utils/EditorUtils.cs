using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Managers;
using App = Frosty.Core.App;

namespace BlueprintEditorPlugin.Utils
{
    /// <summary>
    /// A bunch of random utilities for Blueprints
    /// </summary>
    public static class EditorUtils
    {
        /// <summary>
        /// A list of all Editors
        /// </summary>
        public static Dictionary<string, EditorViewModel> Editors = new Dictionary<string, EditorViewModel>();

        /// <summary>
        /// This gets the currently open <see cref="EditorViewModel"/>
        /// </summary>
        public static EditorViewModel CurrentEditor { get; set; }

        #region Layouts

        private static string LayoutsPath
        {
            get
            {
                //Get the MainWindow so we can grab the Project File
                MainWindow frosty = null;
                
                //Do this to ensure task windows work correctly
                Application.Current.Dispatcher.Invoke(() =>
                {
                    frosty = Application.Current.MainWindow as MainWindow;
                });
                
                //Get the name of the project(make sure to remove .fbproject)
                string projectName = frosty.Project.DisplayName.Split('.')[0];
                return $@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\BlueprintLayouts\{ProfilesLibrary.ProfileName}\";
            }
        }

        /// <summary>
        /// Applies either an existing layout or if one cannot be found an automatic layout
        /// </summary>
        public static void ApplyLayouts(EbxAssetEntry file, EditorViewModel nodeEditor)
        {
            //Replace all occurences of "/" with "\" as to avoid issues with creating the filepath
            string assetPath = file.Name.Replace("/", @"\");

            if (File.Exists($@"{LayoutsPath}{assetPath}_Layout.lyt"))
            {
                //Import our layout and check if it has failed
                if (!ApplyExistingLayout($@"{LayoutsPath}{assetPath}_Layout.lyt", nodeEditor))
                {
                    //If the layout importing failed, then we just apply the auto layout
                    ApplyAutoLayout(nodeEditor);
                }
            }
            else
            {
                ApplyAutoLayout(nodeEditor);
            }
        }
        
        /// <summary>
        /// Saves the current Editor layout
        /// </summary>
        /// <param name="file"></param>
        public static void SaveLayouts(EbxAssetEntry file)
        {
            //Replace all occurences of "/" with "\" as to avoid issues with creating the filepath
            string assetPath = file.Name.Replace("/", @"\");
            
            //Now we can create the filepath
            string filePath = $@"{LayoutsPath}{assetPath}_Layout.lyt";
            
            //First check if filepath exists, if it doesn't we create it
            FileInfo fi = new FileInfo(filePath);
            if (fi.Directory != null && !fi.Directory.Exists) 
            { 
                Directory.CreateDirectory(fi.DirectoryName); 
            }
            
            //Now we can FINALLY create the file with stream writer
            StreamWriter sw = new StreamWriter(filePath);

            //Write down the version
            sw.WriteLine("layver=1");
            
            //Now we populate it
            for (var i = 0; i < CurrentEditor.Nodes.Count; i++)
            {
                sw.WriteLine($"{i},{CurrentEditor.Nodes[i].Location.X.ToString()},{CurrentEditor.Nodes[i].Location.Y.ToString()}");
            }
            sw.Close();
        }

        /// <summary>
        /// This will import a layout from a layout file
        /// </summary>
        /// <param name="filePath">The path to the file. This path must be valid</param>
        /// <param name="nodeEditor"></param>
        /// <returns>A bool indicating whether the operation was successful</returns>
        public static bool ApplyExistingLayout(string filePath, EditorViewModel nodeEditor)
        {
            StreamReader sr = new StreamReader($@"{filePath}");
            
            string line = sr.ReadLine(); //The first line will always be layver={LayoutVersion}, we have this so that we know what version of layouts we are working with
            if (line == null) //First check if the file is empty
            {
                nodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString($"{filePath}_null"), $"There was a problem loading the layout, please check the log for details.");
                App.Logger.LogError("Unable to load layout at {0}, are you sure the layout is properly formatted?", filePath);
                return false;
            }
            
            //The first line will always be the layout version, we need to do different things depending on the layout version in order to support legacy layouts
            switch (line.Split('=')[1])
            {
                case "1":
                {
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        int idx = int.Parse(line.Split(',')[0]);
                        
                        //If this node is invalid
                        if (CurrentEditor.Nodes.Count <= idx)
                        {
                            App.Logger.LogError("Node in saved layout was invalid. Issues may occur!");
                            line = sr.ReadLine();
                            continue;
                        }
                        
                        double x = double.Parse(line.Split(',')[1]);
                        double y = double.Parse(line.Split(',')[2]);

                        CurrentEditor.Nodes[idx].Location = new Point(x, y);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                    return true;
                }
                default:
                {
                    nodeEditor.SetEditorStatus(EditorStatus.Error, FrostySdk.Utils.HashString($"{filePath}_inver"), $"There was a problem loading the layout, please check the log for details.");
                    App.Logger.LogError("Unable to load the layout due to version number {0} being invalid, are you sure the layout is properly formatted?", line);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Applies auto layout as made & used by LevelEditor
        /// </summary>
        public static void ApplyAutoLayout(EditorViewModel nodeEditor)
        {
            // TODO: Find a more precise way to do this
            // Credit to github.com/CadeEvs for source(is temp, and will be replaced, though is a good placeholder)
            //Gather node data
            Dictionary<NodeBaseModel, List<NodeBaseModel>> ancestors = new Dictionary<NodeBaseModel, List<NodeBaseModel>>();
            Dictionary<NodeBaseModel, List<NodeBaseModel>> children = new Dictionary<NodeBaseModel, List<NodeBaseModel>>();

            foreach (NodeBaseModel node in nodeEditor.Nodes)
            {
                ancestors.Add(node, new List<NodeBaseModel>());
                children.Add(node, new List<NodeBaseModel>());
            }

            foreach (ConnectionViewModel connection in nodeEditor.Connections)
            {
                ancestors[connection.TargetNode].Add(connection.SourceNode);
                children[connection.SourceNode].Add(connection.TargetNode);
            }

            List<List<NodeBaseModel>> columns = new List<List<NodeBaseModel>>();
            List<NodeBaseModel> alreadyProcessed = new List<NodeBaseModel>();

            int columnIdx = 1;
            columns.Add(new List<NodeBaseModel>());

            foreach (NodeBaseModel node in nodeEditor.Nodes)
            {
                if (ancestors[node].Count == 0 && children[node].Count == 0)
                {
                    alreadyProcessed.Add(node);
                    columns[0].Add(node);
                    continue;
                }

                if (ancestors[node].Count == 0)
                {
                    AutoLayoutNodes(node, children, columns, alreadyProcessed, columnIdx);
                }
            }

            columnIdx = 1;
            foreach (NodeBaseModel node in nodeEditor.Nodes)
            {
                if (!alreadyProcessed.Contains(node))
                {
                    AutoLayoutNodes(node, children, columns, alreadyProcessed, columnIdx);
                }
            }

            double x = 100.0;
            double width = 0.0;

            foreach (List<NodeBaseModel> column in columns)
            {
                double y = 96.0;
                foreach (NodeBaseModel node in column)
                {
                    x -= (x % 8);
                    y -= (y % 8);
                    node.Location = new Point(x, y);

                    double curWidth = Math.Floor((node.RealWidth + 40.0) / 4.0) * 8.0;
                    double curHeight = Math.Floor(((15 + node.Inputs.Count * 14) + 70.0) / 8.0) * 8.0;

                    y += curHeight + 56.0;

                    if (curWidth > width)
                    {
                        width = curWidth;
                    }
                }

                x += width + 280.0;
            }
        }

        private static int AutoLayoutNodes(NodeBaseModel node, Dictionary<NodeBaseModel, List<NodeBaseModel>> children, List<List<NodeBaseModel>> columns, List<NodeBaseModel> alreadyProcessed, int column)
        {
            if (alreadyProcessed.Contains(node))
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Contains(node))
                        return i;
                }
            }

            alreadyProcessed.Add(node);
            if (columns.Count <= column)
                columns.Add(new List<NodeBaseModel>());
            columns[column++].Add(node);

            int minimumColumn = 0;
            foreach (NodeBaseModel child in children[node])
            {
                int tmp = AutoLayoutNodes(child, children, columns, alreadyProcessed, column);
                if (tmp < minimumColumn || minimumColumn == 0)
                    minimumColumn = tmp;
            }

            if (minimumColumn > (column + 1))
            {
                columns[column - 1].Remove(node);
                columns[minimumColumn - 1].Add(node);
            }

            return column;
        }

        #endregion
    }
}