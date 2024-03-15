using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Ports;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Networking;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Models.Status;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes
{
    /// <summary>
    /// A basic implementation of an entity in a node form. For creation, please see <see cref="GetNodeFromEntity(object,BlueprintEditorPlugin.Editors.NodeWrangler.INodeWrangler)"/>
    /// </summary>
    public class EntityNode : INode, INetworked
    {
        private string _header;
        public virtual string Header
        {
            get => _header;
            set
            {
                _header = value;
                NotifyPropertyChanged(nameof(Header));
            }
        }
        
        private string _footer;
        public virtual string Footer
        {
            get => _footer;
            set
            {
                _footer = value;
                NotifyPropertyChanged(nameof(Footer));
            }
        }

        private string _toolTip;
        public virtual string ToolTip
        {
            get => _toolTip;
            set
            {
                _toolTip = value;
                NotifyPropertyChanged(nameof(_toolTip));
            }
        }
        
        public Size Size { get; set; }

        private bool _selected;
        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

        private bool _isFlatted;
        public bool IsFlatted
        {
            get => _isFlatted;
            set
            {
                _isFlatted = value;
                NotifyPropertyChanged(nameof(IsFlatted));
            }
        }
        
        private Point _location;
        public Point Location
        {
            set
            {
                _location = value;
                NotifyPropertyChanged(nameof(Location));
            }
            get => _location;
        }
        
        public ObservableCollection<IPort> Inputs { get; } = new ObservableCollection<IPort>();
        public ObservableCollection<IPort> Outputs { get; } = new ObservableCollection<IPort>();

        private protected readonly INodeWrangler NodeWrangler;

        #region Entity Info

        private Realm _realm;
        public virtual Realm Realm
        {
            get => _realm;
            set
            {
                _realm = value;
                NotifyPropertyChanged(nameof(Realm));
            }
        }

        public object Object { get; }
        public virtual string ObjectType { get; }

        private bool _hasPlayerEvent;
        public virtual bool HasPlayerEvent
        {
            get => _hasPlayerEvent;
            set
            {
                _hasPlayerEvent = value;
                NotifyPropertyChanged(nameof(HasPlayerEvent));
            }
        }

        #region Location

        public PointerRefType Type { get; }
        
        // Internal
        public AssetClassGuid InternalGuid { get; set; }
        
        // External
        public Guid FileGuid { get; }
        public Guid ClassGuid { get; }

        #endregion
        
        public Realm ParseRealm(object obj)
        {
            return (Realm)((int)obj);
        }

        #endregion

        #region Basic node implementation

        #region Property changing

        public event PropertyChangedEventHandler PropertyChanged;
        
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public virtual bool IsValid()
        {
            return true;
        }

        public virtual void OnCreation()
        {
        }
        
        public virtual void OnInputUpdated(IPort port)
        {
        }

        public virtual void OnOutputUpdated(IPort port)
        {
        }

        public void AddPort(IPort port)
        {
            if (port is EntityOutput output)
            {
                AddOutput(output);
            }
            else
            {
                AddInput((EntityInput)port); // Has to be an entity input
            }
        }

        #endregion

        #region Complex node implementation

        private Dictionary<int, EntityInput> _hashCacheInputs = new Dictionary<int, EntityInput>();
        private Dictionary<int, EntityOutput> _hashCacheOutputs = new Dictionary<int, EntityOutput>();

        #region Port retrieval

        public EntityPort GetInput(string name)
        {
            if (name.StartsWith("0x"))
            {
                return GetInput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier));
            }
            
            if (!_hashCacheInputs.ContainsKey(Utils.HashString(name)))
                return null;

            return _hashCacheInputs[Utils.HashString(name)];
        }

        public EntityPort GetInput(int hash)
        {
            if (!_hashCacheInputs.ContainsKey(hash))
                return null;

            return _hashCacheInputs[hash];
        }
        
        public EntityPort GetOutput(string name)
        {
            if (name.StartsWith("0x"))
            {
                return GetOutput(int.Parse(name.Remove(0, 2), NumberStyles.AllowHexSpecifier));
            }
            
            if (!_hashCacheOutputs.ContainsKey(Utils.HashString(name)))
                return null;

            return _hashCacheOutputs[Utils.HashString(name)];
        }

        public EntityPort GetOutput(int hash)
        {
            if (!_hashCacheOutputs.ContainsKey(hash))
                return null;

            return _hashCacheOutputs[hash];
        }

        #endregion

        #region Port adding

        public void AddInput(EntityInput input)
        {
            if (input.Name.StartsWith("0x"))
            {
                int hash = int.Parse(input.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                if (_hashCacheInputs.ContainsKey(hash))
                    return;
                
                Inputs.Add(input);
                _hashCacheInputs.Add(hash, input);
                return;
            }
            
            if (_hashCacheInputs.ContainsKey(Utils.HashString(input.Name)))
                return;
            
            Inputs.Add(input);
            _hashCacheInputs.Add(Utils.HashString(input.Name), input);
        }
        
        public void AddOutput(EntityOutput output)
        {
            if (output.Name.StartsWith("0x"))
            {
                int hash = int.Parse(output.Name.Remove(0, 2), NumberStyles.AllowHexSpecifier);
                if (_hashCacheOutputs.ContainsKey(hash))
                    return;
                
                Outputs.Add(output);
                _hashCacheOutputs.Add(hash, output);
                return;
            }
            
            if (_hashCacheOutputs.ContainsKey(Utils.HashString(output.Name)))
                return;
            
            Outputs.Add(output);
            _hashCacheOutputs.Add(Utils.HashString(output.Name), output);
        }

        #endregion

        #endregion

        #region Status

        public EditorStatusArgs CurrentStatus { get; set; }
        public void CheckStatus()
        {
        }

        public virtual void UpdateStatus()
        {
        }

        public void SetStatus(EditorStatusArgs args)
        {
        }

        #endregion

        #region Basic Construction

        /// <summary>
        /// Create an entity node from an internal object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(object obj, INodeWrangler nodeWrangler)
        {
            if (obj.GetType().GetProperty("Realm") != null)
            {
                Realm = ParseRealm(((dynamic)obj).Realm);
            }

            Header = obj.GetType().Name;
            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;

            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            Type = PointerRefType.Internal;
        }

        /// <summary>
        /// Create an entity node from an external object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fileGuid"></param>
        /// <param name="nodeWrangler"></param>
        public EntityNode(object obj, Guid fileGuid, INodeWrangler nodeWrangler)
        {
            if (obj.GetType().GetProperty("Realm") != null)
            {
                Realm = ParseRealm(((dynamic)obj).Realm);
            }

            Header = obj.GetType().Name;
            Object = obj;
            ObjectType = obj.GetType().Name;
            NodeWrangler = nodeWrangler;

            InternalGuid = ((dynamic)obj).GetInstanceGuid();
            FileGuid = fileGuid;
            ClassGuid = InternalGuid.ExportedGuid;
            Type = PointerRefType.External;
        }

        public EntityNode(INodeWrangler nodeWrangler)
        {
            NodeWrangler = nodeWrangler;
        }

        public EntityNode()
        {
        }

        #endregion

        #region Static construction

        private static Dictionary<string, Type> _extensions = new Dictionary<string, Type>();

        static EntityNode()
        {
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(EntityNode)))
                {
                    try
                    {
                        EntityNode node = (EntityNode)Activator.CreateInstance(type);
                        if (node.IsValid() && !_extensions.ContainsKey(node.ObjectType))
                        {
                            _extensions.Add(node.ObjectType, type);
                        }
                    }
                    catch (Exception e)
                    {
                        App.Logger.LogError("Entity node {0} caused an exception when processing! Exception: {1}", type.Name, e.Message);
                    }
                }
            }
            
            // TODO: External extensions(external extensions should always take priority over internal ones if they are valid)
        }

        /// <summary>
        /// Gets a proper Node from an internal object. This will return subclasses if they are found.  
        /// </summary>
        /// <param name="entity">The object this will be constructed off of</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <returns></returns>
        public static EntityNode GetNodeFromEntity(object entity, INodeWrangler wrangler)
        {
            if (_extensions.ContainsKey(entity.GetType().Name))
            {
                return (EntityNode)Activator.CreateInstance(_extensions[entity.GetType().Name], entity, wrangler);
            }
            else
            {
                return new EntityNode(entity, wrangler);
            }
        }

        /// <summary>
        /// Gets a proper Node from an external object. This will return subclasses if they are found.  
        /// </summary>
        /// <param name="entity">The object this will be constructed off of</param>
        /// <param name="fileGuid">The guid of the file this external object hails from</param>
        /// <param name="wrangler">The <see cref="INodeWrangler"/> this belongs to</param>
        /// <returns></returns>
        public static EntityNode GetNodeFromEntity(object entity, Guid fileGuid, INodeWrangler wrangler)
        {
            if (_extensions.ContainsKey(entity.GetType().Name))
            {
                return (EntityNode)Activator.CreateInstance(_extensions[entity.GetType().Name], entity, fileGuid, wrangler);
            }
            else
            {
                return new EntityNode(entity, wrangler);
            }
        }

        #endregion
        
        public override string ToString()
        {
            return $"{Realm} - {Header}";
        }
    }
}