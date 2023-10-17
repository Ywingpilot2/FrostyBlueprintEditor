using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using BlueprintEditor.Models;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Editor;
using BlueprintEditor.Models.MenuItems;
using BlueprintEditor.Models.Types;
using FrostySdk.IO;

namespace BlueprintEditor.Utils
{
    /// <summary>
    /// A bunch of random utilities for Blueprints
    /// </summary>
    public static class EditorUtils
    {
        public static EditorViewModel Editor;

        private static Object typesList_selectedItem;

        /// <summary>
        /// The item that is currently selected in the TypesList
        /// </summary>
        public static NodeTypeViewModel TypesViewModelListSelectedItem
        {
            get
            {
                if (typesList_selectedItem == null) return null;

                return (NodeTypeViewModel)typesList_selectedItem;
            }
            set => typesList_selectedItem = value;
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