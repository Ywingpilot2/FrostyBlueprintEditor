using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ObjectReference
{
    public class SpatialPrefabReferenceObjectData : ObjectReferenceObjectData
    {
        public override string Name { get; set; } = "SpatialPrefab (null ref)";
        public override string ObjectType { get; set; } = "SpatialPrefabReferenceObjectData";
        protected override string ShortName { get; set; } = "Spatial Prefab";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "BlueprintTransform", Type = ConnectionType.Property},
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>();
    }
}