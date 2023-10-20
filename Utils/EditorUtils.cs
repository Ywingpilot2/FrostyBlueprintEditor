using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using BlueprintEditor.Models;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Editor;
using BlueprintEditor.Models.MenuItems;
using BlueprintEditor.Models.Types;
using Frosty.Core;
using FrostySdk.IO;
using Nodify;

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