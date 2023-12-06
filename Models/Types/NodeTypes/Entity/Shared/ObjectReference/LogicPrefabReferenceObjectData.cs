using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ObjectReference
{
    public class LogicPrefabReferenceObjectData : ObjectReferenceObjectData
    {
        public override string Name { get; set; } = "LogicPrefab (null ref)";
        public override string ObjectType { get; set; } = "LogicPrefabReferenceObjectData";
        protected override string ShortName { get; set; } = "Logic Prefab";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "BlueprintTransform", Type = ConnectionType.Property},
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>();

        public override void OnCreateNew()
        {
            ((dynamic)Object).CastSunShadowEnable = true;
            ((dynamic)Object).CastReflectionEnable = true;
            ((dynamic)Object).CastEnvmapEnable = true;
            
            Array localPlayerIdArray = ((object)TypeLibrary.CreateObject("LocalPlayerId")).GetType().GetEnumValues();
            List<dynamic> localPlayerIdEnum = new List<dynamic>(localPlayerIdArray.Cast<dynamic>());
            ((dynamic)Object).LocalPlayerId = localPlayerIdEnum[8];
        }
    }
}