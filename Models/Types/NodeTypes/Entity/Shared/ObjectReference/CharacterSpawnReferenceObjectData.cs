using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ObjectReference
{
    public class CharacterSpawnReferenceObjectData : ObjectReferenceObjectData
    {
        public override string Name { get; set; } = "Character (null ref)";
        public override string ObjectType { get; set; } = "CharacterSpawnReferenceObjectData";
        protected override string ShortName { get; set; } = "Character";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "self", Type = ConnectionType.Link, Realm = ConnectionRealm.Server},
                new InputViewModel() {Title = "BlueprintTransform", Type = ConnectionType.Property, Realm = ConnectionRealm.Server},
                new InputViewModel() {Title = "Spawn", Type = ConnectionType.Event, Realm = ConnectionRealm.Server}
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>()
            {
                new OutputViewModel() {Title = "AlternativeSpawnPoints", Type = ConnectionType.Link, Realm = ConnectionRealm.Server},
                new OutputViewModel() {Title = "Vehicle", Type = ConnectionType.Link, Realm = ConnectionRealm.Server},
                new OutputViewModel() {Title = "OnSpawned", Type = ConnectionType.Event, Realm = ConnectionRealm.Server},
                new OutputViewModel() {Title = "OnKilled", Type = ConnectionType.Event, Realm = ConnectionRealm.Server},
            };

        public override void OnCreation()
        {
            base.OnCreation();
            
            PointerRef ptr = Object.Blueprint;
            EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);

            if (Object.GetType().GetProperty("Template") != null)
            {
                PointerRef templatePointer = Object.Template;

                if (templatePointer.External.FileGuid == System.Guid.Empty) return;
                EbxAssetEntry templateAssetEntry = App.AssetManager.GetEbxEntry(templatePointer.External.FileGuid);
                Name = $"Character ({blueprintAssetEntry.Filename}, {templateAssetEntry.Filename})";
            }
        }

        public override void OnCreateNew()
        {
            base.OnCreateNew();
            Object.Enabled = true;
            Object.QueueSpawnEvent = true;
            Object.SpawnDelay = 0.1f;
            Object.MaxCount = -1;
            Object.MaxCountSimultaneously = -1;
            Object.TotalCountSimultaneouslyOfType = -1;
            Object.SpawnProtectionCheckAllTeams = true;
            Object.ClearBangersOnSpawn = true;
            Object.OnlySendEventForHumanPlayers = true;
            Object.TryToSpawnOutOfSight = true;
            Object.TakeControlOnTransformChange = true;
            Object.ReturnControlOnIdle = true;
            Object.SpawnWithHumanLikeAI = true;
            Object.SpawnVisible = true;
            Object.HumanTargetPreference = -1.0f;
            Object.IsTarget = true;
        }

        public override void OnModified(ItemModifiedEventArgs args)
        {
            switch (args.Item.Name)
            {
                case "Blueprint":
                {
                    //TODO: Fix our inputs being cleared(e.g Spawn)
                    base.OnModified(args);
                } break;
                case "Template":
                {
                    PointerRef ptr = Object.Blueprint;
                    if (ptr.Type == PointerRefType.Null) return;
            
                    EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);
                    PointerRef templatePointer = Object.Template;

                    if (templatePointer.External.FileGuid == System.Guid.Empty) return;
                    EbxAssetEntry templateAssetEntry = App.AssetManager.GetEbxEntry(templatePointer.External.FileGuid);
                    Name = $"Character ({blueprintAssetEntry.Filename}, {templateAssetEntry.Filename})";
                    NotifyPropertyChanged(nameof(Name));
                } break;
            }
        }
    }
}