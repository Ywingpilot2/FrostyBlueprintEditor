using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Types;
using BlueprintEditor.Utils;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using Nodify;
using Prism.Commands;

namespace BlueprintEditor.Models.Editor
{

    #region Editor

    /// <summary>
    /// This is the editor itself. This collects all of the Nodes and Connections needed to be made
    /// </summary>
    public class EditorViewModel
    {
        public ObservableCollection<NodeBaseModel> Nodes { get; } = new ObservableCollection<NodeBaseModel>();
        public ObservableCollection<NodeBaseModel> SelectedNodes { get; } = new ObservableCollection<NodeBaseModel>();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new ObservableCollection<ConnectionViewModel>();
        public PendingConnectionViewModel PendingConnection { get; }
        public ICommand DisconnectConnectorCommand { get; }

        
        public EbxAsset EditedEbxAsset { get; set; }

        public dynamic EditedProperties => EditedEbxAsset.RootObject;

        public EditorViewModel()
        {
            EditedEbxAsset = App.AssetManager.GetEbx((EbxAssetEntry)App.EditorWindow.GetOpenedAssetEntry());
            
            PendingConnection = new PendingConnectionViewModel(this);
            
            DisconnectConnectorCommand = new DelegateCommand<Object>(connector =>
            {
                //ConnectionViewModel connection = connector.GetType().Name == "InputViewModel" ? Connections.First(x => x.Target == connector) : Connections.First(x => x.Source == connector);
                if (connector.GetType().Name == "InputViewModel")
                {
                    Disconnect(Connections.First(x => x.Target == connector));
                }
                else
                {
                    Disconnect(Connections.First(x => x.Source == connector));
                }
            });

            EditorUtils.Editor = this;
        }
        
        /// <summary>
        /// Connect 2 nodes together.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public ConnectionViewModel Connect(OutputViewModel source, InputViewModel target)
        {
            var connection = new ConnectionViewModel(source, target, source.Type);
            if (Connections.All(x => x.Source != connection.Source || x.Target != connection.Target))
            {
                Connections.Add(connection);
                connection.TargetNode.OnInputUpdated(target);
                connection.SourceNode.OnOutputUpdated(source);
            }

            return connection;
        }
        
        public void Disconnect(ConnectionViewModel connection)
        {
            App.EditorWindow.OpenAsset(App.AssetManager.GetEbxEntry(EditedEbxAsset.FileGuid));
            
            bool sourceConnected = false;
            bool targetConnected = false;
            foreach (ConnectionViewModel connectionViewModel in Connections)
            {
                if (connectionViewModel == connection) continue;
                
                if (connection.Source == connectionViewModel.Source)
                {
                    sourceConnected = true;
                }

                if (connection.Target == connectionViewModel.Target)
                {
                    targetConnected = true;
                }
            }
            connection.Source.IsConnected = sourceConnected;
            connection.Target.IsConnected = targetConnected;
            
            //TODO: This code sucks! Please find a faster way to find the connection and remove it
            switch (connection.Type)
            {
                case ConnectionType.Event:
                {
                    foreach (dynamic eventConnection in EditedProperties.EventConnections)
                    {
                        if (!connection.Equals(eventConnection)) continue;
                        EditedProperties.EventConnections.Remove(eventConnection);
                        break;
                    }
                    break;
                }
                case ConnectionType.Property:
                {
                    foreach (dynamic propertyConnection in EditedProperties.PropertyConnections)
                    {
                        if (!connection.Equals(propertyConnection)) continue;
                        EditedProperties.PropertyConnections.Remove(propertyConnection);
                        break;
                    }
                    break;
                }
                case ConnectionType.Link:
                {
                    foreach (dynamic linkConnection in EditedProperties.LinkConnections)
                    {
                        if (!connection.Equals(linkConnection)) continue;
                        EditedProperties.LinkConnections.Remove(linkConnection);
                        break;
                    }
                    break;
                }
            }
            
            App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(EditedEbxAsset.FileGuid).Name, EditedEbxAsset);
            App.EditorWindow.DataExplorer.RefreshItems();
            Connections.Remove(connection);
        }

        /// <summary>
        /// Gets a list of connections with this <see cref="InputViewModel"/>
        /// </summary>
        /// <param name="inputViewModel">The input view model to find</param>
        /// <returns>A list of all connections found</returns>
        public List<ConnectionViewModel> GetConnections(InputViewModel inputViewModel)
        {
            List<ConnectionViewModel> connections = new List<ConnectionViewModel>();
            Parallel.ForEach(Connections, connection =>
            {
                if (connection.Target == inputViewModel && !connections.Contains(connection))
                {
                    connections.Add(connection);
                }
            });
            return connections;
        }

        /// <summary>
        /// Gets a list of connections with this <see cref="OutputViewModel"/>
        /// </summary>
        /// <param name="output">The output view model to find</param>
        /// <returns>A list of all connections found</returns>
        public List<ConnectionViewModel> GetConnections(OutputViewModel output)
        {
            List<ConnectionViewModel> connections = new List<ConnectionViewModel>();
            Parallel.ForEach(Connections, connection =>
            {
                if (connection.Source == output && !connections.Contains(connection))
                {
                    connections.Add(connection);
                }
            });
            return connections;
        }

        /// <summary>
        /// Gets a list of connections with this <see cref="OutputViewModel"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns>A list of all connections found</returns>
        public List<ConnectionViewModel> GetConnections(NodeBaseModel node)
        {
            List<ConnectionViewModel> connections = new List<ConnectionViewModel>();
            Parallel.ForEach(Connections, connection =>
            {
                if ((connection.SourceNode == node || connection.TargetNode == node) && !connections.Contains(connection))
                {
                    connections.Add(connection);
                }
            });
            return connections;
        }
    }
    
    #endregion

    #region Pending Connection

    /// <summary>
    /// This executes <see cref="StartCommand"/> when we first drag an output
    /// Then executes <see cref="FinishCommand"/> when we let go of the output
    /// </summary>
    public class PendingConnectionViewModel
    {
        private OutputViewModel _source;
        private InputViewModel _target;

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
        
        public PendingConnectionViewModel(EditorViewModel editor)
        {
            StartCommand = new DelegateCommand<Object>(source =>
            {
                //Open the asset when editing in order to ensure the least issues
                App.EditorWindow.OpenAsset(App.AssetManager.GetEbxEntry(editor.EditedEbxAsset.FileGuid));
                if (source.GetType().Name == "OutputViewModel")
                {
                    _source = (OutputViewModel)source;
                }
                else
                {
                    _target = (InputViewModel)source;
                }
            });
            FinishCommand = new DelegateCommand<Object>(target =>
            {
                ConnectionViewModel connection = null;
                if (target != null && target.GetType().Name != "OutputViewModel" && _source != null && _source.Type == ((InputViewModel)target).Type)
                {
                    connection = editor.Connect(_source, (InputViewModel)target);
                    _source = null; //Set these values to null that way they aren't saved in memory
                }
                else if (target != null && target.GetType().Name == "OutputViewModel" && _target != null && _target.Type == ((OutputViewModel)target).Type)
                {
                    connection = editor.Connect((OutputViewModel)target, _target);
                    _target = null;
                }

                #region Edit Ebx

                if (connection != null)
                    switch (connection.Type)
                    {
                        case ConnectionType.Event:
                        {
                            dynamic eventConnection = TypeLibrary.CreateObject("EventConnection");

                            eventConnection.Source = new PointerRef(connection.SourceNode.Object);
                            eventConnection.Target = new PointerRef(connection.TargetNode.Object);
                            eventConnection.SourceEvent.Name = connection.SourceField;
                            eventConnection.TargetEvent.Name = connection.TargetField;

                            ((dynamic)editor.EditedEbxAsset.RootObject).EventConnections
                                .Add(eventConnection);
                            connection.Object = eventConnection;
                            break;
                        }
                        case ConnectionType.Property:
                        {
                            dynamic propertyConnection = TypeLibrary.CreateObject("PropertyConnection");

                            propertyConnection.Source = new PointerRef(connection.SourceNode.Object);
                            propertyConnection.Target = new PointerRef(connection.TargetNode.Object);
                            propertyConnection.SourceField = connection.SourceField;
                            propertyConnection.TargetField = connection.TargetField;

                            ((dynamic)editor.EditedEbxAsset.RootObject).PropertyConnections
                                .Add(propertyConnection);
                            connection.Object = propertyConnection;

                            break;
                        }
                        case ConnectionType.Link:
                        {
                            dynamic linkConnection = TypeLibrary.CreateObject("LinkConnection");

                            linkConnection.Source = new PointerRef(connection.SourceNode.Object);
                            linkConnection.Target = new PointerRef(connection.TargetNode.Object);
                            linkConnection.SourceField = connection.SourceField;
                            linkConnection.TargetField = connection.TargetField;

                            ((dynamic)editor.EditedEbxAsset.RootObject).LinkConnections.Add(
                                linkConnection);
                            connection.Object = linkConnection;

                            break;
                        }
                    }

                App.AssetManager.ModifyEbx(App.AssetManager.GetEbxEntry(editor.EditedEbxAsset.FileGuid).Name, editor.EditedEbxAsset);
                App.EditorWindow.DataExplorer.RefreshItems();

                #endregion
            });
        }
    }

    #endregion
}