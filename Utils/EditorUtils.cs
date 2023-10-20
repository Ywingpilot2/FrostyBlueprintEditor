using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using BlueprintEditor.Models;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Editor;
using BlueprintEditor.Models.MenuItems;
using BlueprintEditor.Models.Types;
using FrostyEditor;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using Nodify;
using App = Frosty.Core.App;

namespace BlueprintEditor.Utils
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
        public static EditorViewModel CurrentEditor
        {
            get
            {
                EditorViewModel editor = null;
                //So frosty task window doesn't fucksplode
                Application.Current.Dispatcher.Invoke(() =>
                {
                    editor = Editors[App.EditorWindow.GetOpenedAssetEntry().Filename];
                });
                return editor;
            }
        }

        /// <summary>
        /// Applies either an existing layout or if one cannot be found an automatic layout
        /// </summary>
        public static void ApplyLayouts(EbxAssetEntry file)
        {
            //Get the MainWindow so we can grab the Project File
            MainWindow frosty = Application.Current.MainWindow as MainWindow;

            //Get the name of the project(make sure to remove .fbproject)
            string projectName = frosty.Project.DisplayName.Split()[0];
            
            //Replace all occurences of "/" with "\" as to avoid issues with creating the filepath
            string assetPath = file.Name.Replace("/", @"\");

            if (File.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintLayouts\{ProfilesLibrary.ProfileName}\{projectName}\{assetPath}_Layout.txt"))
            {
                StreamReader sr = new StreamReader($@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintLayouts\{ProfilesLibrary.ProfileName}\{projectName}\{assetPath}_Layout.txt");
                
                string line = sr.ReadLine();
                while (line != null)
                {
                    int idx = int.Parse(line.Split(',')[0]);
                    double x = double.Parse(line.Split(',')[1]);
                    double y = double.Parse(line.Split(',')[2]);

                    CurrentEditor.Nodes[idx].Location = new Point(x, y);
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            else
            {
                ApplyAutoLayout();
            }
        }
        
        /// <summary>
        /// Saves the current Editor layout
        /// </summary>
        /// <param name="file"></param>
        public static void SaveLayouts(EbxAssetEntry file)
        {
            //Get the MainWindow so we can grab the Project File
            MainWindow frosty = Application.Current.MainWindow as MainWindow;

            //Get the name of the project(make sure to remove .fbproject)
            string projectName = frosty.Project.DisplayName.Split()[0];

            //Replace all occurences of "/" with "\" as to avoid issues with creating the filepath
            string assetPath = file.Name.Replace("/", @"\");
            
            //Now we can create the filepath
            string filePath = $@"{AppDomain.CurrentDomain.BaseDirectory}BlueprintLayouts\{ProfilesLibrary.ProfileName}\{projectName}\{assetPath}_Layout.txt";
            
            //First check if filepath exists, if it doesn't we create it
            FileInfo fi = new FileInfo(filePath);
            if (fi.Directory != null && !fi.Directory.Exists) 
            { 
                Directory.CreateDirectory(fi.DirectoryName); 
            }
            
            //Now we can FINALLY create the file with stream writer
            StreamWriter sw = new StreamWriter(filePath);
            
            //Create a dictionary which stores the index of the node and the position of the node
            //The index *should* be the same on each startup, unless the file was modified.
            //TODO: Find way to account for Property grid modifications(so when the index has changed)
            Dictionary<int, Point> points = new Dictionary<int, Point>();

            //Now we populate it
            for (var i = 0; i < CurrentEditor.Nodes.Count; i++)
            {
                sw.WriteLine($"{i},{CurrentEditor.Nodes[i].Location.X.ToString()},{CurrentEditor.Nodes[i].Location.Y.ToString()}");
            }
            sw.Close();
        }
        
        /// <summary>
        /// Applies auto layout as made & used by LevelEditor
        /// </summary>
        public static void ApplyAutoLayout()
        {
            // TODO: Find a more precise way to do this
            // Credit to github.com/CadeEvs for source(is temp, and will be replaced, though is a good placeholder)
            //Gather node data
            Dictionary<NodeBaseModel, List<NodeBaseModel>> ancestors = new Dictionary<NodeBaseModel, List<NodeBaseModel>>();
            Dictionary<NodeBaseModel, List<NodeBaseModel>> children = new Dictionary<NodeBaseModel, List<NodeBaseModel>>();

            foreach (NodeBaseModel node in CurrentEditor.Nodes)
            {
                ancestors.Add(node, new List<NodeBaseModel>());
                children.Add(node, new List<NodeBaseModel>());
            }

            foreach (ConnectionViewModel connection in CurrentEditor.Connections)
            {
                ancestors[connection.TargetNode].Add(connection.SourceNode);
                children[connection.SourceNode].Add(connection.TargetNode);
            }

            List<List<NodeBaseModel>> columns = new List<List<NodeBaseModel>>();
            List<NodeBaseModel> alreadyProcessed = new List<NodeBaseModel>();

            int columnIdx = 1;
            columns.Add(new List<NodeBaseModel>());

            foreach (NodeBaseModel node in CurrentEditor.Nodes)
            {
                if (ancestors[node].Count == 0 && children[node].Count == 0)
                {
                    alreadyProcessed.Add(node);
                    columns[0].Add(node);
                    continue;
                }

                if (ancestors[node].Count == 0)
                {
                    LayoutNodes(node, children, columns, alreadyProcessed, columnIdx);
                }
            }

            columnIdx = 1;
            foreach (NodeBaseModel node in CurrentEditor.Nodes)
            {
                if (!alreadyProcessed.Contains(node))
                {
                    LayoutNodes(node, children, columns, alreadyProcessed, columnIdx);
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
                    double curHeight = Math.Floor(((node.Inputs.Count * 14) + 70.0) / 8.0) * 8.0;

                    y += curHeight + 56.0;

                    if (curWidth > width)
                    {
                        width = curWidth;
                    }
                }

                x += width + 280.0;
            }
        }

        public static int LayoutNodes(NodeBaseModel node, Dictionary<NodeBaseModel, List<NodeBaseModel>> children, List<List<NodeBaseModel>> columns, List<NodeBaseModel> alreadyProcessed, int column)
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
                int tmp = LayoutNodes(child, children, columns, alreadyProcessed, column);
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
    }
}