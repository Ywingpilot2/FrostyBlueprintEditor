using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintEditorPlugin.Models.Connections;
using BlueprintEditorPlugin.Models.Types.EbxEditorTypes;
using BlueprintEditorPlugin.Utils;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace BlueprintEditorPlugin.Models.Types.NodeTypes.Entity.Shared.ObjectReference
{
    public class ObjectReferenceObjectData : EntityNode
    {
        public override string Name { get; set; } = "Object (null ref)";
        public override string ObjectType { get; set; } = "ObjectReferenceObjectData";
        protected virtual string ShortName { get; set; } = "Object";

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
            if (ptr.Type == PointerRefType.Null) return;

            EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);
            EbxAsset blueprint = App.AssetManager.GetEbx(blueprintAssetEntry);

            Name = $"{ShortName} ({blueprintAssetEntry.Filename})";

            PointerRef interfaceRef = ((dynamic)blueprint.RootObject).Interface;
            if (interfaceRef.Type == PointerRefType.Null) return;

            //Flags
            bool hasProperty = false;
            bool hasEvent = false;
            bool hasLink = false;

            #region Populate interface outpts/inputs
            
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
                        Type = ConnectionType.Property,
                        PropertyConnectionType = PropertyType.Interface
                    });
                }
                else //Source and Target
                {
                    Inputs.Add(new InputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property,
                        PropertyConnectionType = PropertyType.Interface
                    });
                    Outputs.Add(new OutputViewModel()
                    {
                        Title = field.Name,
                        Type = ConnectionType.Property
                    });
                }

                hasProperty = true;
            }

            foreach (dynamic inputEvent in ((dynamic)interfaceRef.Internal).InputEvents)
            {
                Inputs.Add(new InputViewModel()
                {
                    Title = inputEvent.Name,
                    Type = ConnectionType.Event
                });
                hasEvent = true;
            }
            
            foreach (dynamic outputEvent in ((dynamic)interfaceRef.Internal).OutputEvents)
            {
                Outputs.Add(new OutputViewModel()
                {
                    Title = outputEvent.Name,
                    Type = ConnectionType.Event
                });
                hasEvent = true;
            }
                
            foreach (dynamic inputLink in ((dynamic)interfaceRef.Internal).InputLinks)
            {
                Inputs.Add(new InputViewModel()
                {
                    Title = inputLink.Name,
                    Type = ConnectionType.Link
                });
                hasLink = true;
            }
                
            foreach (dynamic outputLink in ((dynamic)interfaceRef.Internal).OutputLinks)
            {
                Outputs.Add(new OutputViewModel()
                {
                    Title = outputLink.Name,
                    Type = ConnectionType.Link
                });
                hasLink = true;
            }

            #endregion

            #region Setup realms

            if (hasProperty)
            {
                foreach (dynamic connection in ((dynamic)blueprint.RootObject).PropertyConnections)
                {
                    if (interfaceRef.Internal == connection.Source.Internal)
                    {
                        var helper = new PropertyFlagsHelper(connection.Flags);
                        GetInput((string)connection.SourceField.ToString(), ConnectionType.Property).Realm = helper.Realm;
                    }
                    else if (interfaceRef.Internal == connection.Target.Internal)
                    {
                        var helper = new PropertyFlagsHelper(connection.Flags);
                        GetOutput((string)connection.TargetField.ToString(), ConnectionType.Property).Realm = helper.Realm;
                    }
                }
            }

            if (hasEvent)
            {
                foreach (dynamic connection in ((dynamic)blueprint.RootObject).EventConnections)
                {
                    if (interfaceRef.Internal == connection.Source.Internal)
                    {
                        ConnectionRealm connectionRealm = NodeUtils.ParseRealmFromString(connection.TargetType.ToString());
                        GetInput((string)connection.SourceEvent.Name.ToString(), ConnectionType.Event).Realm = connectionRealm;
                    }
                    else if (interfaceRef.Internal == connection.Target.Internal)
                    {
                        ConnectionRealm connectionRealm = NodeUtils.ParseRealmFromString(connection.TargetType.ToString());
                        GetOutput((string)connection.TargetEvent.Name.ToString(), ConnectionType.Event).Realm = connectionRealm;
                    }
                }
            }

            if (hasLink)
            {
                foreach (dynamic connection in ((dynamic)blueprint.RootObject).LinkConnections)
                {
                    if (interfaceRef.Internal != connection.Target.Internal) continue;
                    
                    Type objType = connection.Source.Internal.GetType();
                    if (objType.GetProperty("Flags") == null) continue;
                        
                    var helper = new ObjectFlagsHelper((uint)connection.Source.Internal.Flags);
                    if (helper.ClientLinkSource)
                    {
                        GetOutput((string)connection.TargetField.ToString(), ConnectionType.Link).Realm = ConnectionRealm.Client;
                    }
                    else if (helper.ServerLinkSource)
                    {
                        GetOutput((string)connection.TargetField.ToString(), ConnectionType.Link).Realm = ConnectionRealm.Server;
                    }
                }
            }

            #endregion
        }

        public override void OnCreateNew()
        {
            Object.CastSunShadowEnable = true;
            Object.CastReflectionEnable = true;
            Object.CastEnvmapEnable = true;
            
            Array localPlayerIdArray = ((object)TypeLibrary.CreateObject("LocalPlayerId")).GetType().GetEnumValues();
            List<dynamic> localPlayerIdEnum = new List<dynamic>(localPlayerIdArray.Cast<dynamic>());
            Object.LocalPlayerId = localPlayerIdEnum[8];
            Object.LightmapResolutionScale = 1;
        }

        public override void OnModified(ItemModifiedEventArgs args)
        {
            switch (args.Item.Name)
            {
                case "Blueprint":
                {
                    //Clear out our original inputs/outputs
                    foreach (ConnectionViewModel connection in EditorUtils.CurrentEditor.GetConnections(this))
                    {
                        EditorUtils.CurrentEditor.Disconnect(connection);
                    }
                    Inputs.Clear();
                    Outputs.Clear();
                    
                    PointerRef ptr = Object.Blueprint;

                    if (ptr.Type == PointerRefType.Null)
                    {
                        Name = $"{ShortName} (null ref)";
                        return;
                    }

                    EbxAssetEntry blueprintAssetEntry = App.AssetManager.GetEbxEntry(ptr.External.FileGuid);
                    EbxAsset blueprint = App.AssetManager.GetEbx(blueprintAssetEntry);

                    Name = $"{ShortName} ({blueprintAssetEntry.Filename})";
                    NotifyPropertyChanged(nameof(Name));

                    PointerRef interfaceRef = ((dynamic)blueprint.RootObject).Interface;

                    bool hasProperty = false;
                    bool hasEvent = false;
                    bool hasLink = false;

                    #region Populate interface outpts/inputs
                    
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

                        hasProperty = true;
                    }

                    foreach (dynamic inputEvent in ((dynamic)interfaceRef.Internal).InputEvents)
                    {
                        if (GetInput(inputEvent.Name, ConnectionType.Event) != null) continue;
                        Inputs.Add(new InputViewModel()
                        {
                            Title = inputEvent.Name,
                            Type = ConnectionType.Event
                        });

                        hasEvent = true;
                    }
            
                    foreach (dynamic outputEvent in ((dynamic)interfaceRef.Internal).OutputEvents)
                    {
                        if (GetOutput(outputEvent.Name, ConnectionType.Event) != null) continue;
                        Outputs.Add(new OutputViewModel()
                        {
                            Title = outputEvent.Name,
                            Type = ConnectionType.Event
                        });

                        hasEvent = true;
                    }
                
                    foreach (dynamic inputLink in ((dynamic)interfaceRef.Internal).InputLinks)
                    {
                        if (GetInput(inputLink.Name, ConnectionType.Link) != null) continue;
                        Inputs.Add(new InputViewModel()
                        {
                            Title = inputLink.Name,
                            Type = ConnectionType.Link
                        });

                        hasLink = true;
                    }

                    foreach (dynamic outputLink in ((dynamic)interfaceRef.Internal).OutputLinks)
                    {
                        if (GetInput(outputLink.Name, ConnectionType.Link) != null) continue;
                        Outputs.Add(new OutputViewModel()
                        {
                            Title = outputLink.Name,
                            Type = ConnectionType.Link
                        });
                        
                        hasLink = true;
                    }

                    #endregion

                    #region Setup realms

                    if (hasProperty)
                    {
                        foreach (dynamic connection in ((dynamic)blueprint.RootObject).PropertyConnections)
                        {
                            if (interfaceRef.Internal == connection.Source.Internal)
                            {
                                var helper = new PropertyFlagsHelper(connection.Flags);
                                GetInput((string)connection.SourceField.ToString(), ConnectionType.Property).Realm = helper.Realm;
                            }
                            else if (interfaceRef.Internal == connection.Target.Internal)
                            {
                                var helper = new PropertyFlagsHelper(connection.Flags);
                                GetOutput((string)connection.TargetField.ToString(), ConnectionType.Property).Realm = helper.Realm;
                            }
                        }
                    }

                    if (hasEvent)
                    {
                        foreach (dynamic connection in ((dynamic)blueprint.RootObject).EventConnections)
                        {
                            if (interfaceRef.Internal == connection.Source.Internal)
                            {
                                ConnectionRealm connectionRealm = NodeUtils.ParseRealmFromString(connection.TargetType.ToString());
                                GetInput((string)connection.SourceEvent.Name.ToString(), ConnectionType.Event).Realm = connectionRealm;
                            }
                            else if (interfaceRef.Internal == connection.Target.Internal)
                            {
                                ConnectionRealm connectionRealm = NodeUtils.ParseRealmFromString(connection.TargetType.ToString());
                                GetOutput((string)connection.TargetEvent.Name.ToString(), ConnectionType.Event).Realm = connectionRealm;
                            }
                        }
                    }

                    if (hasLink)
                    {
                        foreach (dynamic connection in ((dynamic)blueprint.RootObject).LinkConnections)
                        {
                            if (interfaceRef.Internal != connection.Target.Internal) continue;
                    
                            Type objType = connection.Source.Internal.GetType();
                            if (objType.GetProperty("Flags") == null) continue;
                        
                            var helper = new ObjectFlagsHelper((uint)connection.Source.Internal.Flags);
                            if (helper.ClientLinkSource)
                            {
                                GetOutput((string)connection.TargetField.ToString(), ConnectionType.Link).Realm = ConnectionRealm.Client;
                            }
                            else if (helper.ServerLinkSource)
                            {
                                GetOutput((string)connection.TargetField.ToString(), ConnectionType.Link).Realm = ConnectionRealm.Server;
                            }
                        }
                    }

                    #endregion
                } break;
                case "__Id":
                {
                    NotifyPropertyChanged(nameof(Name));
                } break;
            }
        }
    }
}