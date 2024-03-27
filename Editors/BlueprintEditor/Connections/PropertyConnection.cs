using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Connections
{
    public class PropertyConnection : EntityConnection
    {
        public override ConnectionType Type => ConnectionType.Property;

        private Realm _realm;
        public override Realm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                ((dynamic)Object).Flags = PropertyFlagsHelper.GetAsFlags(Realm, PropType, SourceCantBeStatic);
                NotifyPropertyChanged(nameof(Realm));
                UpdateStatus();
            }
        }

        private bool _isDynamic;
        public bool SourceCantBeStatic
        {
            get => _isDynamic;
            set
            {
                ((dynamic)Object).Flags = PropertyFlagsHelper.GetAsFlags(Realm, PropType, SourceCantBeStatic);
                _isDynamic = value;
                NotifyPropertyChanged(nameof(SourceCantBeStatic));
            }
        }
        
        public sealed override void UpdateSourceRef(EntityPort source)
        {
            base.UpdateSourceRef(source);
            
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
            
            ((dynamic)Object).Source = sourceRef;
            ((dynamic)Object).SourceField = source.Name;
        }

        public sealed override void UpdateTargetRef(EntityPort target)
        {
            base.UpdateTargetRef(target);
            
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
            
            ((dynamic)Object).Target = targetRef;
            ((dynamic)Object).TargetField = target.Name;
        }

        public override void UpdateStatus()
        {
            base.UpdateStatus();

            if (PropType == PropertyType.Invalid)
            {
                SetStatus(EditorStatus.Broken, "Property type is invalid");
            }

            EntityPort target = (EntityPort)Target;
            EntityPort source = (EntityPort)Source;
            if (PropType == PropertyType.Interface && !(target.IsInterface || source.IsInterface))
            {
                SetStatus(EditorStatus.Flawed, "Property type is set to interface, despite not plugging into an interface");
            }
        }

        public PropertyConnection(PropertyOutput source, PropertyInput target, object obj) : base(source, target, obj)
        {
            Object = obj;

            PropertyFlagsHelper.GetAsRealm(((dynamic)Object).Flags, out Realm realm, out  PropertyType propType, out bool isDynamic);

            Realm = realm;
            PropType = propType;
            SourceCantBeStatic = isDynamic;
            
            UpdateStatus();
        }
        
        public PropertyConnection(PropertyOutput source, PropertyInput target) : base(source, target)
        {
            Object = TypeLibrary.CreateObject("PropertyConnection");
            PropType = target.IsInterface || source.IsInterface ? PropertyType.Interface : PropertyType.Default;
            
            UpdateSourceRef();
            UpdateTargetRef();

            FixRealm();
            UpdateStatus();
        }
        
        protected override void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                    UpdateStatus();
                } break;
                case "Name":
                {
                    ((dynamic)Object).SourceField = Source.Name;
                    ((dynamic)Object).TargetField = Target.Name;
                } break;
                case "IsInterface":
                {
                    if (((EntityPort)Target).IsInterface)
                    {
                        PropType = PropertyType.Interface;
                    }
                    else
                    {
                        PropType = PropertyType.Default;
                    }
                } break;
                case "Node":
                {
                    PointerRef sourceRef;

                    EntityPort source = (EntityPort)Source;
                    EntityPort target = (EntityPort)Target;
                    
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
                } break;
            }
        }
    }

    public static class PropertyFlagsHelper
    {
        public static uint GetAsFlags(Realm realm, PropertyType propType, bool sourceCantBeStatic = false)
        {
            uint flags = 0;
            flags |= (uint)realm;
            flags |= ((uint)propType) << 4;
            if (sourceCantBeStatic)
            {
                flags |= 8;
            }

            return flags;
        }

        public static void GetAsRealm(uint flags, out Realm realm, out PropertyType propType, out bool sourceCantBeStatic)
        {
            realm = (Realm)(flags & 7);
            propType = (PropertyType)((flags & 48) >> 4);
            sourceCantBeStatic = Convert.ToBoolean((flags & 8) != 0 ? 1 : 0);
        }
    }
}