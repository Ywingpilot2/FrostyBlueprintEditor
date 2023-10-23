using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using BlueprintEditor.Models.Connections;
using BlueprintEditor.Models.Types.NodeTypes;
using BlueprintEditor.Models.Types.NodeTypes.Shared.ScalableEmitterDocument;
using BlueprintEditor.Utils;
using FrostySdk;
using FrostySdk.Ebx;

namespace BlueprintEditor.Models.Types.EbxLoaderTypes
{
    /// <summary>
    /// For documentation please see <see cref="EbxBaseLoader"/>
    /// </summary>
    public class ScalableEmitterDocumentLoader : EbxBaseLoader
    {
        public override string AssetType => "ScalableEmitterDocument";

        public override void PopulateTypesList(List<Type> typesList)
        {
            //populate types list
            foreach (Type type in TypeLibrary.GetTypes("ProcessorData"))
            {
                typesList.Add(type);
            }
        }

        public override void PopulateNodes(dynamic properties)
        {
            List<AssetClassGuid> emitters = new List<AssetClassGuid>();
            
            if (!emitters.Contains(properties.TemplateDataLow.Internal.GetInstanceGuid()))
            {
                //Create the template
                EmitterTemplateData emitter = NodeEditor.CreateNodeFromObject(properties.TemplateDataLow.Internal);
                emitter.Name = "Emitter Template(Low)";
                emitters.Add(emitter.Guid);

                //Create the Root processor's node
                object currentProcessor = ((dynamic)properties.TemplateDataUltra.Internal).RootProcessor.Internal;
                dynamic currentProperties = currentProcessor as dynamic;
            
                NodeBaseModel node = new NodeBaseModel()
                {
                    Guid = currentProperties.GetInstanceGuid(), 
                    Object = currentProcessor,
                    Name = currentProcessor.GetType().Name,
                    Inputs = new ObservableCollection<InputViewModel>()
                    {
                        new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                        new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                        new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                    },
                    Outputs = new ObservableCollection<OutputViewModel>()
                    {
                        new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                    }
                };
            
                node.OnCreation();
                NodeEditor.Nodes.Add(node);
            
                //Connect the Template to the RootProcessor
                NodeEditor.Connect(emitter.Outputs[0], node.Inputs[2]);

                //Now we create the rest of the file.
                object nextProcessor = ((dynamic)currentProcessor).NextProcessor.Internal;
                while (nextProcessor != null) //We do this by iterating over all of the Processors until we hit a null pointerref
                {
                    dynamic processorProperties = nextProcessor as dynamic;
                
                    //Create the next processor as a node
                    NodeBaseModel nextNode = new NodeBaseModel()
                    {
                        Guid = processorProperties.GetInstanceGuid(), 
                        Object = nextProcessor,
                        Name = nextProcessor.GetType().Name,
                        Inputs = new ObservableCollection<InputViewModel>()
                        {
                            new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                            new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                            new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                        },
                        Outputs = new ObservableCollection<OutputViewModel>()
                        {
                            new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                        }
                    };
                    nextNode.OnCreation(); //So that extensions don't break
                    NodeEditor.Nodes.Add(nextNode);
                
                    //If this has Pre(Compute?) stuff we create a node out of it then connect it too
                    if (processorProperties.Pre.Internal != null)
                    {
                        dynamic precompute = processorProperties.Pre.Internal;
                        NodeBaseModel preComputeNode = new NodeBaseModel()
                        {
                            Guid = precompute.GetInstanceGuid(), 
                            Object = precompute,
                            Name = precompute.GetType().Name,
                            Inputs = new ObservableCollection<InputViewModel>(),
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() {Title = "Processor", Type = ConnectionType.Link}
                            }
                        };
                        preComputeNode.OnCreation(); //So that extensions don't break
                        NodeEditor.Nodes.Add(preComputeNode);
                    
                        //Connect the last node with this current node
                        NodeEditor.Connect(preComputeNode.Outputs[0], nextNode.Inputs[1]);
                    }

                    //Connect the last node with this current node
                    NodeEditor.Connect(node.Outputs[0], nextNode.Inputs[0]);
                    node = nextNode; //Assign the last node as this current node
                    nextProcessor = processorProperties.NextProcessor.Internal as dynamic; //Get the next processor, so we can enumerate over it
                }
            }

            if (!emitters.Contains(properties.TemplateDataMedium.Internal.GetInstanceGuid()))
            {
                //Create the template
                EmitterTemplateData emitter = NodeEditor.CreateNodeFromObject(properties.TemplateDataMedium.Internal);
                emitter.Name = "Emitter Template(Medium)";
                emitters.Add(emitter.Guid);

                //Create the Root processor's node
                object currentProcessor = ((dynamic)properties.TemplateDataUltra.Internal).RootProcessor.Internal;
                dynamic currentProperties = currentProcessor as dynamic;
            
                NodeBaseModel node = new NodeBaseModel()
                {
                    Guid = currentProperties.GetInstanceGuid(), 
                    Object = currentProcessor,
                    Name = currentProcessor.GetType().Name,
                    Inputs = new ObservableCollection<InputViewModel>()
                    {
                        new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                        new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                        new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                    },
                    Outputs = new ObservableCollection<OutputViewModel>()
                    {
                        new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                    }
                };
            
                node.OnCreation();
                NodeEditor.Nodes.Add(node);
            
                //Connect the Template to the RootProcessor
                NodeEditor.Connect(emitter.Outputs[0], node.Inputs[2]);

                //Now we create the rest of the file.
                object nextProcessor = ((dynamic)currentProcessor).NextProcessor.Internal;
                while (nextProcessor != null) //We do this by iterating over all of the Processors until we hit a null pointerref
                {
                    dynamic processorProperties = nextProcessor as dynamic;
                
                    //Create the next processor as a node
                    NodeBaseModel nextNode = new NodeBaseModel()
                    {
                        Guid = processorProperties.GetInstanceGuid(), 
                        Object = nextProcessor,
                        Name = nextProcessor.GetType().Name,
                        Inputs = new ObservableCollection<InputViewModel>()
                        {
                            new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                            new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                            new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                        },
                        Outputs = new ObservableCollection<OutputViewModel>()
                        {
                            new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                        }
                    };
                    nextNode.OnCreation(); //So that extensions don't break
                    NodeEditor.Nodes.Add(nextNode);
                
                    //If this has Pre(Compute?) stuff we create a node out of it then connect it too
                    if (processorProperties.Pre.Internal != null)
                    {
                        dynamic precompute = processorProperties.Pre.Internal;
                        NodeBaseModel preComputeNode = new NodeBaseModel()
                        {
                            Guid = precompute.GetInstanceGuid(), 
                            Object = precompute,
                            Name = precompute.GetType().Name,
                            Inputs = new ObservableCollection<InputViewModel>(),
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() {Title = "Processor", Type = ConnectionType.Link}
                            }
                        };
                        preComputeNode.OnCreation(); //So that extensions don't break
                        NodeEditor.Nodes.Add(preComputeNode);
                    
                        //Connect the last node with this current node
                        NodeEditor.Connect(preComputeNode.Outputs[0], nextNode.Inputs[1]);
                    }

                    //Connect the last node with this current node
                    NodeEditor.Connect(node.Outputs[0], nextNode.Inputs[0]);
                    node = nextNode; //Assign the last node as this current node
                    nextProcessor = processorProperties.NextProcessor.Internal as dynamic; //Get the next processor, so we can enumerate over it
                }
            }

            if (!emitters.Contains(properties.TemplateDataHigh.Internal.GetInstanceGuid()))
            {
                //Create the template
                EmitterTemplateData emitter = NodeEditor.CreateNodeFromObject(properties.TemplateDataHigh.Internal);
                emitter.Name = "Emitter Template(High)";
                emitters.Add(emitter.Guid);

                //Create the Root processor's node
                object currentProcessor = ((dynamic)properties.TemplateDataUltra.Internal).RootProcessor.Internal;
                dynamic currentProperties = currentProcessor as dynamic;
            
                NodeBaseModel node = new NodeBaseModel()
                {
                    Guid = currentProperties.GetInstanceGuid(), 
                    Object = currentProcessor,
                    Name = currentProcessor.GetType().Name,
                    Inputs = new ObservableCollection<InputViewModel>()
                    {
                        new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                        new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                        new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                    },
                    Outputs = new ObservableCollection<OutputViewModel>()
                    {
                        new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                    }
                };
            
                node.OnCreation();
                NodeEditor.Nodes.Add(node);
            
                //Connect the Template to the RootProcessor
                NodeEditor.Connect(emitter.Outputs[0], node.Inputs[2]);

                //Now we create the rest of the file.
                object nextProcessor = ((dynamic)currentProcessor).NextProcessor.Internal;
                while (nextProcessor != null) //We do this by iterating over all of the Processors until we hit a null pointerref
                {
                    dynamic processorProperties = nextProcessor as dynamic;
                
                    //Create the next processor as a node
                    NodeBaseModel nextNode = new NodeBaseModel()
                    {
                        Guid = processorProperties.GetInstanceGuid(), 
                        Object = nextProcessor,
                        Name = nextProcessor.GetType().Name,
                        Inputs = new ObservableCollection<InputViewModel>()
                        {
                            new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                            new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                            new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                        },
                        Outputs = new ObservableCollection<OutputViewModel>()
                        {
                            new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                        }
                    };
                    nextNode.OnCreation(); //So that extensions don't break
                    NodeEditor.Nodes.Add(nextNode);
                
                    //If this has Pre(Compute?) stuff we create a node out of it then connect it too
                    if (processorProperties.Pre.Internal != null)
                    {
                        dynamic precompute = processorProperties.Pre.Internal;
                        NodeBaseModel preComputeNode = new NodeBaseModel()
                        {
                            Guid = precompute.GetInstanceGuid(), 
                            Object = precompute,
                            Name = precompute.GetType().Name,
                            Inputs = new ObservableCollection<InputViewModel>(),
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() {Title = "Processor", Type = ConnectionType.Link}
                            }
                        };
                        preComputeNode.OnCreation(); //So that extensions don't break
                        NodeEditor.Nodes.Add(preComputeNode);
                    
                        //Connect the last node with this current node
                        NodeEditor.Connect(preComputeNode.Outputs[0], nextNode.Inputs[1]);
                    }

                    //Connect the last node with this current node
                    NodeEditor.Connect(node.Outputs[0], nextNode.Inputs[0]);
                    node = nextNode; //Assign the last node as this current node
                    nextProcessor = processorProperties.NextProcessor.Internal as dynamic; //Get the next processor, so we can enumerate over it
                }
            }

            if (!emitters.Contains(properties.TemplateDataUltra.Internal.GetInstanceGuid()))
            {
                //Create the template
                EmitterTemplateData emitter = NodeEditor.CreateNodeFromObject(properties.TemplateDataUltra.Internal);
                emitter.Name = "Emitter Template(Ultra)";
                emitters.Add(emitter.Guid);

                //Create the Root processor's node
                object currentProcessor = ((dynamic)properties.TemplateDataUltra.Internal).RootProcessor.Internal;
                dynamic currentProperties = currentProcessor as dynamic;
            
                NodeBaseModel node = new NodeBaseModel()
                {
                    Guid = currentProperties.GetInstanceGuid(), 
                    Object = currentProcessor,
                    Name = currentProcessor.GetType().Name,
                    Inputs = new ObservableCollection<InputViewModel>()
                    {
                        new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                        new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                        new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                    },
                    Outputs = new ObservableCollection<OutputViewModel>()
                    {
                        new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                    }
                };
            
                node.OnCreation();
                NodeEditor.Nodes.Add(node);
            
                //Connect the Template to the RootProcessor
                NodeEditor.Connect(emitter.Outputs[0], node.Inputs[2]);

                //Now we create the rest of the file.
                object nextProcessor = ((dynamic)currentProcessor).NextProcessor.Internal;
                while (nextProcessor != null) //We do this by iterating over all of the Processors until we hit a null pointerref
                {
                    dynamic processorProperties = nextProcessor as dynamic;
                
                    //Create the next processor as a node
                    NodeBaseModel nextNode = new NodeBaseModel()
                    {
                        Guid = processorProperties.GetInstanceGuid(), 
                        Object = nextProcessor,
                        Name = nextProcessor.GetType().Name,
                        Inputs = new ObservableCollection<InputViewModel>()
                        {
                            new InputViewModel() {Title = "Compute", Type = ConnectionType.Event},
                            new InputViewModel() {Title = "Pre", Type = ConnectionType.Link},
                            new InputViewModel() {Title = "self", Type = ConnectionType.Link}
                        },
                        Outputs = new ObservableCollection<OutputViewModel>()
                        {
                            new OutputViewModel() {Title = "Compute Next", Type = ConnectionType.Event}
                        }
                    };
                    nextNode.OnCreation(); //So that extensions don't break
                    NodeEditor.Nodes.Add(nextNode);
                
                    //If this has Pre(Compute?) stuff we create a node out of it then connect it too
                    if (processorProperties.Pre.Internal != null)
                    {
                        dynamic precompute = processorProperties.Pre.Internal;
                        NodeBaseModel preComputeNode = new NodeBaseModel()
                        {
                            Guid = precompute.GetInstanceGuid(), 
                            Object = precompute,
                            Name = precompute.GetType().Name,
                            Inputs = new ObservableCollection<InputViewModel>(),
                            Outputs = new ObservableCollection<OutputViewModel>()
                            {
                                new OutputViewModel() {Title = "Processor", Type = ConnectionType.Link}
                            }
                        };
                        preComputeNode.OnCreation(); //So that extensions don't break
                        NodeEditor.Nodes.Add(preComputeNode);
                    
                        //Connect the last node with this current node
                        NodeEditor.Connect(preComputeNode.Outputs[0], nextNode.Inputs[1]);
                    }

                    //Connect the last node with this current node
                    NodeEditor.Connect(node.Outputs[0], nextNode.Inputs[0]);
                    node = nextNode; //Assign the last node as this current node
                    nextProcessor = processorProperties.NextProcessor.Internal as dynamic; //Get the next processor, so we can enumerate over it
                }
            }
        }

        public override void CreateConnections(dynamic properties)
        {
        }
    }
}