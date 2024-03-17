using System;
using System.ComponentModel;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public class EventConnection : EntityConnection
    {
        public override ConnectionType Type => ConnectionType.Event;

        private Realm _realm;
        public override Realm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                Type realmType = ((dynamic)Object).TargetType.GetType();
                ((dynamic)Object).TargetType = (dynamic)Enum.Parse(realmType, $"EventConnectionTargetType_{_realm.ToString()}");
                NotifyPropertyChanged(nameof(Realm));
                UpdateStatus();
            }
        }

        public EventConnection(EventOutput source, EventInput target) : base(source, target)
        {
            Object = TypeLibrary.CreateObject("EventConnection");
            
            PointerRef sourceRef;
            if (((IObjectNode)source.Node).Type == PointerRefType.Internal)
            {
                sourceRef = new PointerRef(((IObjectNode)source.Node).Object);
            }
            else
            {
                sourceRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((IObjectNode)source.Node).FileGuid,
                    ClassGuid = ((IObjectNode)source.Node).ClassGuid
                });
            }
            
            PointerRef targetRef;
            if (((IObjectNode)target.Node).Type == PointerRefType.Internal)
            {
                targetRef = new PointerRef(((IObjectNode)target.Node).Object);
            }
            else
            {
                targetRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((IObjectNode)target.Node).FileGuid,
                    ClassGuid = ((IObjectNode)target.Node).ClassGuid
                });
            }

            ((dynamic)Object).Source = sourceRef;
            ((dynamic)Object).Target = targetRef;
            ((dynamic)Object).SourceEvent.Name = source.Name;
            ((dynamic)Object).TargetEvent.Name = target.Name;

            HasPlayer = source.HasPlayer;

            FixRealm();
            UpdateStatus();
        }
        
        public EventConnection(EventOutput source, EventInput target, object obj) : base(source, target, obj)
        {
            Object = obj;
            
            PointerRef sourceRef;
            if (((IObjectNode)source.Node).Type == PointerRefType.Internal)
            {
                sourceRef = new PointerRef(((IObjectNode)source.Node).Object);
            }
            else
            {
                sourceRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((IObjectNode)source.Node).FileGuid,
                    ClassGuid = ((IObjectNode)source.Node).ClassGuid
                });
            }
            
            PointerRef targetRef;
            if (((IObjectNode)target.Node).Type == PointerRefType.Internal)
            {
                targetRef = new PointerRef(((IObjectNode)target.Node).Object);
            }
            else
            {
                targetRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((IObjectNode)target.Node).FileGuid,
                    ClassGuid = ((IObjectNode)target.Node).ClassGuid
                });
            }

            HasPlayer = source.HasPlayer;

            Realm = ParseRealm(((dynamic)Object).TargetType.ToString());
            UpdateStatus();
        }

        public override void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Anchor":
                {
                    NotifyPropertyChanged(nameof(CurvePoint1));
                    NotifyPropertyChanged(nameof(CurvePoint2));
                } break;
                case "IsSelected":
                {
                    IsSelected = Source.Node.IsSelected || Target.Node.IsSelected;
                    NotifyPropertyChanged(nameof(IsSelected));
                } break;
                case "Realm":
                {
                    FixRealm();
                    UpdateStatus();
                } break;
                case "Name":
                {
                    ((dynamic)Object).SourceEvent.Name = Source.Name;
                    ((dynamic)Object).TargetEvent.Name = Target.Name;
                } break;
                case "HasPlayerEvent":
                case "HasPlayer":
                {
                    if (sender == Target || sender == Target.Node)
                        break;
                    
                    HasPlayer = ((EntityPort)Source).HasPlayer || ((EntityNode)Source.Node).HasPlayerEvent;
                    if (Target.Node is EntityNode entityNode)
                    {
                        entityNode.HasPlayerEvent = HasPlayer;
                    }
                } break;
            }
        }
    }
}