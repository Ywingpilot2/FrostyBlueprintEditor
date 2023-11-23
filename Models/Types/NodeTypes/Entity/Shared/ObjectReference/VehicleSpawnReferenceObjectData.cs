using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ObjectReference
{
    public class VehicleSpawnReferenceObjectData : ObjectReferenceObjectData
    {
        public override string Name { get; set; } = "Vehicle (null ref)";
        public override string ObjectType { get; set; } = "VehicleSpawnReferenceObjectData";
        protected override string ShortName { get; set; } = "Vehicle";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "self", Type = ConnectionType.Link},
                new InputViewModel() {Title = "BlueprintTransform", Type = ConnectionType.Property},
                new InputViewModel() {Title = "Spawn", Type = ConnectionType.Event}
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "OnSpawned", Type = ConnectionType.Event},
                new OutputViewModel() {Title = "OnKilled", Type = ConnectionType.Event},
                new OutputViewModel() {Title = "AlternativeSpawnPoints", Type = ConnectionType.Link}
            };
    }
}