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
            
            PointerRef ptr = ((dynamic)Object).Blueprint;
            EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);

            if (Object.GetType().GetProperty("Template") != null)
            {
                PointerRef templatePointer = ((dynamic)Object).Template;

                if (templatePointer.External.FileGuid == System.Guid.Empty) return;
                EbxAssetEntry templateAssetEntry = App.AssetManager.GetEbxEntry(templatePointer.External.FileGuid);
                Name = $"Character ({blueprintAssetEntry.Filename}, {templateAssetEntry.Filename})";
            }
        }

        public override void OnCreateNew()
        {
            base.OnCreateNew();
            ((dynamic)Object).Enabled = true;
            ((dynamic)Object).QueueSpawnEvent = true;
            ((dynamic)Object).SpawnDelay = 0.1f;
            ((dynamic)Object).MaxCount = -1;
            ((dynamic)Object).MaxCountSimultaneously = -1;
            ((dynamic)Object).TotalCountSimultaneouslyOfType = -1;
            ((dynamic)Object).SpawnProtectionCheckAllTeams = true;
            ((dynamic)Object).ClearBangersOnSpawn = true;
            ((dynamic)Object).OnlySendEventForHumanPlayers = true;
            ((dynamic)Object).TryToSpawnOutOfSight = true;
            ((dynamic)Object).TakeControlOnTransformChange = true;
            ((dynamic)Object).ReturnControlOnIdle = true;
            ((dynamic)Object).SpawnWithHumanLikeAI = true;
            ((dynamic)Object).SpawnVisible = true;
            ((dynamic)Object).HumanTargetPreference = -1.0f;
            ((dynamic)Object).IsTarget = true;
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
                    PointerRef ptr = ((dynamic)Object).Blueprint;
                    if (ptr.Type == PointerRefType.Null) return;
            
                    EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);
                    PointerRef templatePointer = ((dynamic)Object).Template;

                    if (templatePointer.External.FileGuid == System.Guid.Empty) return;
                    EbxAssetEntry templateAssetEntry = App.AssetManager.GetEbxEntry(templatePointer.External.FileGuid);
                    Name = $"Character ({blueprintAssetEntry.Filename}, {templateAssetEntry.Filename})";
                    NotifyPropertyChanged(nameof(Name));
                } break;
            }
        }
    }
}