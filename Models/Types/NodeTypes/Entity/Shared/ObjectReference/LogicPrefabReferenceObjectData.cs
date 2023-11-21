using System.Collections.ObjectModel;
using BlueprintEditorPlugin.Models.Connections;
using Frosty.Core;
using Frosty.Core.Controls;
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
                    Outputs.Add(new OutputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }
                else if (field.AccessType.ToString() == "FieldAccessType_Target") //Target
                {
                    Inputs.Add(new InputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }
                else //Source and Target
                {
                    Inputs.Add(new InputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                    Outputs.Add(new OutputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }
            }

            foreach (dynamic inputEvent in ((dynamic)interfaceRef.Internal).InputEvents)
            {
                Inputs.Add(new InputViewModel()
                {
                    Title = inputEvent.Name,
                    Type = ConnectionType.Event
                });
            }
            
            foreach (dynamic outputEvent in ((dynamic)interfaceRef.Internal).OutputEvents)
            {
                Outputs.Add(new OutputViewModel()
                {
                    Title = outputEvent.Name,
                    Type = ConnectionType.Event
                });
            }
                
            foreach (dynamic inputLink in ((dynamic)interfaceRef.Internal).InputLinks)
            {
                Inputs.Add(new InputViewModel()
                {
                    Title = inputLink.Name,
                    Type = ConnectionType.Link
                });
            }

            foreach (dynamic outputLink in ((dynamic)interfaceRef.Internal).OutputLinks)
            {
                Outputs.Add(new OutputViewModel()
                {
                    Title = outputLink.Name,
                    Type = ConnectionType.Link
                });
            }
        }

        public override void OnModified(ItemModifiedEventArgs args)
        {
            switch (args.Item.Name)
            {
                case "Blueprint":
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
                            if (GetOutput(field.Name, ConnectionType.Property) != null) continue;
                            Outputs.Add(new OutputViewModel()
                            {
                                Title = field.Name,
                                Type = ConnectionType.Property
                            });
                        }
                        else if (field.AccessType.ToString() == "FieldAccessType_Target") //Target
                        {
                            if (GetInput(field.Name, ConnectionType.Property) != null) continue;
                            Inputs.Add(new InputViewModel()
                            {
                                Title = field.Name,
                                Type = ConnectionType.Property
                            });
                        }
                        else //Source and Target
                        {
                            if (GetInput(field.Name, ConnectionType.Property) != null) continue;
                            Inputs.Add(new InputViewModel()
                            {
                                Title = field.Name,
                                Type = ConnectionType.Property
                            });
                    
                            if (GetOutput(field.Name, ConnectionType.Property) != null) continue;
                            Outputs.Add(new OutputViewModel()
                            {
                                Title = field.Name,
                                Type = ConnectionType.Property
                            });
                        }
                    }

                    foreach (dynamic inputEvent in ((dynamic)interfaceRef.Internal).InputEvents)
                    {
                        if (GetInput(inputEvent.Name, ConnectionType.Event) != null) continue;
                        Inputs.Add(new InputViewModel()
                        {
                            Title = inputEvent.Name,
                            Type = ConnectionType.Event
                        });
                    }
            
                    foreach (dynamic outputEvent in ((dynamic)interfaceRef.Internal).OutputEvents)
                    {
                        if (GetOutput(outputEvent.Name, ConnectionType.Event) != null) continue;
                        Outputs.Add(new OutputViewModel()
                        {
                            Title = outputEvent.Name,
                            Type = ConnectionType.Event
                        });
                    }
                
                    foreach (dynamic inputLink in ((dynamic)interfaceRef.Internal).InputLinks)
                    {
                        if (GetInput(inputLink.Name, ConnectionType.Link) != null) continue;
                        Inputs.Add(new InputViewModel()
                        {
                            Title = inputLink.Name,
                            Type = ConnectionType.Link
                        });
                    }

                    foreach (dynamic outputLink in ((dynamic)interfaceRef.Internal).OutputLinks)
                    {
                        if (GetInput(outputLink.Name, ConnectionType.Link) != null) continue;
                        Outputs.Add(new OutputViewModel()
                        {
                            Title = outputLink.Name,
                            Type = ConnectionType.Link
                        });
                    }
                } break;
            }
        }
    }
}