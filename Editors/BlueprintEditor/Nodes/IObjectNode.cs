using System;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Nodes;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    /// <summary>
    /// Base implementation for a node tied to an object
    /// </summary>
    public interface IObjectNode : INode
    {
        object Object { get; }
        PointerRefType Type { get; }
        AssetClassGuid InternalGuid { get; set; }
        Guid FileGuid { get; }
        Guid ClassGuid { get; }

        EntityPort GetInput(string name);
        EntityPort GetOutput(string name);

        void AddInput(EntityInput input);
        void AddOutput(EntityOutput output);
    }
}