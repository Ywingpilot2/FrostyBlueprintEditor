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

        public override void UpdateStatus()
        {
            base.UpdateStatus();

            if (PropType == PropertyType.Invalid)
            {
                SetStatus(new EditorStatusArgs(EditorStatus.Broken, "Property type is invalid"));
            }
        }

        public PropertyConnection(PropertyOutput source, PropertyInput target, object obj) : base(source, target, obj)
        {
            PropType = target.IsInterface ? PropertyType.Interface : PropertyType.Default;
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

            Realm = target.Realm;
            UpdateStatus();
        }
        
        public PropertyConnection(PropertyOutput source, PropertyInput target) : base(source, target)
        {
            PropType = target.IsInterface ? PropertyType.Interface : PropertyType.Default;
            Object = TypeLibrary.CreateObject("PropertyConnection");
            
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
}