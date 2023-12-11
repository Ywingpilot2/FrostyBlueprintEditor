using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using BlueprintEditorPlugin.Attributes;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Editor;
using BlueprintEditorPlugin.Models.Types.EbxEditorTypes;
using BlueprintEditorPlugin.Models.Types.EbxLoaderTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes;
using BlueprintEditorPlugin.Models.Types.NodeTypes.Transient;
using BlueprintEditorPlugin.Options;
using Frosty.Core;
using FrostyEditor;
using FrostySdk;
using FrostySdk.Interfaces;
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
        public static Dictionary<string, EditorViewModel> ActiveNodeEditors = new Dictionary<string, EditorViewModel>();

        public static Dictionary<string, Type> EbxLoaders = new Dictionary<string, Type>();
        public static Dictionary<string, Type> EbxEditors = new Dictionary<string, Type>();

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
                return $@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintEditor\BlueprintLayouts\{ProfilesLibrary.ProfileName}\{projectName}\";
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
                if (!CurrentEditor.Nodes[i].IsTransient)
                {
                    sw.WriteLine($"{i},{CurrentEditor.Nodes[i].Location.X.ToString()},{CurrentEditor.Nodes[i].Location.Y.ToString()}");
                }
                else
                {
                    TransientNode transientNode = CurrentEditor.Nodes[i] as TransientNode;
                    transientNode.SaveTransientData(sw);
                }
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

                    double curWidth = node.Name.Length * 2;
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

        #region EditorSettings

        public static ConnectionStyle CStyle { get; set; }

        public static void UpdateSettings()
        {
            switch (Config.Get("ConnectionStyle", "StartStop"))
            {
                case "StartStop":
                {
                    CStyle = ConnectionStyle.StartStop;
                } break;
                case "Straight":
                {
                    CStyle = ConnectionStyle.Straight;
                } break;
                case "Curvy":
                {
                    CStyle = ConnectionStyle.Curvy;
                } break;
            }
        }

        #endregion

        #region Initialization

        public static void Initialize(ILogger logger = null)
        {
            logger?.Log("Getting user preferences...");
            UpdateSettings();
            
            logger?.Log("Initializing Loader extensions...");
            EbxLoaders.Add("null", typeof(EbxBaseLoader));
            
            //Load internal loaders
            foreach (var type in Assembly.GetCallingAssembly().GetTypes()) //Iterate over all of the loader extensions
            {
                if (!type.IsSubclassOf(typeof(EbxBaseLoader))) continue;
                
                var extension = (EbxBaseLoader)Activator.CreateInstance(type);
                EbxLoaders.Add(extension.AssetType, type);
            }
            
            logger?.Log("Initializing Editor extensions...");
            EbxEditors.Add("null", typeof(EbxBaseEditor));
            
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (!type.IsSubclassOf(typeof(EbxBaseEditor))) continue;
                
                EbxBaseEditor extension = (EbxBaseEditor)Activator.CreateInstance(type);
                EbxEditors.Add(extension.AssetType, type);
            }
            
            logger?.Log("Loading external extensions...");
            foreach (string item in Directory.EnumerateFiles("Plugins", "*.dll", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(item);
                Assembly plugin = Assembly.LoadFile(fileInfo.FullName);

                foreach (Attribute attribute in plugin.GetCustomAttributes())
                {
                    if (attribute is RegisterEbxLoaderExtension registerLoader && registerLoader.ValidForGames != null && registerLoader.ValidForGames.Contains(ProfilesLibrary.ProfileName))
                    {
                        EbxBaseLoader loader = (EbxBaseLoader)Activator.CreateInstance(registerLoader.EbxLoaderExtension);
                        EbxLoaders.Add(loader.AssetType, registerLoader.EbxLoaderExtension);
                    }
                    else if (attribute is RegisterEbxEditorExtension registerEditor && registerEditor.ValidForGames != null && registerEditor.ValidForGames.Contains(ProfilesLibrary.ProfileName))
                    {
                        EbxBaseEditor editor = (EbxBaseEditor)Activator.CreateInstance(registerEditor.EbxEditorExtension);
                        EbxEditors.Add(editor.AssetType, registerEditor.EbxEditorExtension);
                    }
                }
            }
        }

        #endregion
    }
}