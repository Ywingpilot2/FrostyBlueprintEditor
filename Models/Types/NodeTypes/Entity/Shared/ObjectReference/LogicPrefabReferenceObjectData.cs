using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ObjectReference
{
    public class LogicPrefabReferenceObjectData : EntityNode
    {
        public override string Name { get; set; } = "LogicPrefab (null ref)";
        public override string ObjectType { get; set; } = "LogicPrefabReferenceObjectData";

        public override ObservableCollection<InputViewModel> Inputs { get; set; } =
            new ObservableCollection<InputViewModel>()
            {
                new InputViewModel() {Title = "BlueprintTransform", Type = ConnectionType.Property},
            };

        public override ObservableCollection<OutputViewModel> Outputs { get; set; } =
            new ObservableCollection<OutputViewModel>();

        public override void OnCreation()
        {
            PointerRef ptr = Object.Blueprint;

            EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);
            EbxAsset blueprint = App.AssetManager.GetEbx(blueprintAssetEntry);

            Name = $"LogicPrefab ({blueprintAssetEntry.Filename})";

            PointerRef interfaceRef = ((dynamic)blueprint.RootObject).Interface;
                           
            //Populate interface outpts/inputs
            foreach (dynamic field in ((dynamic)interfaceRef.Internal).Fields)
            {
                if (field.AccessType.ToString() == "FieldAccessType_Source") //Source
                {
                    this.Outputs.Add(new OutputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }
                else if (field.AccessType.ToString() == "FieldAccessType_Target") //Target
                {
                    this.Inputs.Add(new InputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }
                else //Source and Target
                {
                    this.Inputs.Add(new InputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                    this.Outputs.Add(new OutputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }
            }

            foreach (dynamic inputEvent in ((dynamic)interfaceRef.Internal).InputEvents)
            {
                this.Inputs.Add(new InputViewModel()
                {
                    Title = inputEvent.Name,
                    Type = ConnectionType.Event
                });
            }
            
            foreach (dynamic outputEvent in ((dynamic)interfaceRef.Internal).OutputEvents)
            {
                this.Outputs.Add(new OutputViewModel()
                {
                    Title = outputEvent.Name,
                    Type = ConnectionType.Event
                });
            }
                
            foreach (dynamic inputLink in ((dynamic)interfaceRef.Internal).InputLinks)
            {
                this.Inputs.Add(new InputViewModel()
                {
                    Title = inputLink.Name,
                    Type = ConnectionType.Link
                });
            }
                
            foreach (dynamic outputLink in ((dynamic)interfaceRef.Internal).OutputLinks)
            {
                this.Outputs.Add(new OutputViewModel()
                {
                    Title = outputLink.Name,
                    Type = ConnectionType.Link
                });
            }            
        }
        
        public override void OnModified() => OnCreation();
    }
}