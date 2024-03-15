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

        public override void UpdateStatus()
        {
            base.UpdateStatus();

            if (PropType == PropertyType.Invalid)
            {
                SetStatus(new EditorStatusArgs(EditorStatus.Broken, "Property type is invalid"));
            }

            EntityPort target = (EntityPort)Target;
            if (PropType == PropertyType.Interface && !target.IsInterface)
            {
                SetStatus(new EditorStatusArgs(EditorStatus.Flawed, "Property type is set to interface, despite not plugging into an interface"));
            }
        }

        public PropertyConnection(PropertyOutput source, PropertyInput target, object obj) : base(source, target, obj)
        {
            Object = obj;
            
            PointerRef sourceRef;
            if (((EntityNode)source.Node).Type == PointerRefType.Internal)
            {
                sourceRef = new PointerRef(((EntityNode)source.Node).Object);
            }
            else
            {
                sourceRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((EntityNode)source.Node).FileGuid,
                    ClassGuid = ((EntityNode)source.Node).ClassGuid
                });
            }
            
            PointerRef targetRef;
            if (((EntityNode)target.Node).Type == PointerRefType.Internal)
            {
                targetRef = new PointerRef(((EntityNode)target.Node).Object);
            }
            else
            {
                targetRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((EntityNode)target.Node).FileGuid,
                    ClassGuid = ((EntityNode)target.Node).ClassGuid
                });
            }
            
            PropertyFlagsHelper.GetAsRealm(((dynamic)Object).Flags, out Realm realm, out  PropertyType propType, out bool isDynamic);

            Realm = realm;
            PropType = propType;
            SourceCantBeStatic = isDynamic;
            
            UpdateStatus();
        }
        
        public PropertyConnection(PropertyOutput source, PropertyInput target) : base(source, target)
        {
            Object = TypeLibrary.CreateObject("PropertyConnection");
            PropType = target.IsInterface ? PropertyType.Interface : PropertyType.Default;
            
            PointerRef sourceRef;
            if (((EntityNode)source.Node).Type == PointerRefType.Internal)
            {
                sourceRef = new PointerRef(((EntityNode)source.Node).Object);
            }
            else
            {
                sourceRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((EntityNode)source.Node).FileGuid,
                    ClassGuid = ((EntityNode)source.Node).ClassGuid
                });
            }
            
            PointerRef targetRef;
            if (((EntityNode)target.Node).Type == PointerRefType.Internal)
            {
                targetRef = new PointerRef(((EntityNode)target.Node).Object);
            }
            else
            {
                targetRef = new PointerRef(new EbxImportReference()
                {
                    FileGuid = ((EntityNode)target.Node).FileGuid,
                    ClassGuid = ((EntityNode)target.Node).ClassGuid
                });
            }

            ((dynamic)Object).Source = sourceRef;
            ((dynamic)Object).Target = targetRef;
            ((dynamic)Object).SourceField = source.Name;
            ((dynamic)Object).TargetField = target.Name;

            Realm = target.Realm;
            UpdateStatus();
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