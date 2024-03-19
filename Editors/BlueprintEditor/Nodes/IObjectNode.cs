using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    /// <summary>
    /// Base implementation for a node tied to an entity object
    /// </summary>
    public interface IObjectNode : INode, IEntityObject
    {
        EntityInput GetInput(string name);
        EntityOutput GetOutput(string name);

        void AddInput(EntityInput input);
        void AddOutput(EntityOutput output);
    }
}