﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Options;
using BlueprintEditorPlugin.Models.Entities;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;

//using System.IO;

namespace BlueprintEditorPlugin.Editors.PropertyGrid.Items
{
    internal class RefTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AssetEntry entry)
                return entry.Type;
            return value as string;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BlueprintPointerRefEditor : BlueprintTypeEditor<BlueprintPointerRefControl>
    {
        public BlueprintPointerRefEditor()
        {
            ValueProperty = BlueprintPointerRefControl.ValueProperty;
            NotifyOnTargetUpdated = true;
        }

        protected override void CustomizeEditor(BlueprintPointerRefControl editor, FrostyPropertyGridItemData item)
        {
            base.CustomizeEditor(editor, item);
            if (editor is BlueprintPointerRefControl pointerRefEditor)
            {
                pointerRefEditor.NodeWrangler = _graphEditor;
            }
        }
    }

    [TemplatePart(Name = PART_AssignButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_MoreOptionsButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_Popup, Type = typeof(ComboBox))]
    public class BlueprintPointerRefControl : Control
    {
        private const string PART_AssignButton = "PART_AssignButton";
        private const string PART_MoreOptionsButton = "PART_MoreOptionsButton";
        private const string PART_Popup = "PART_Popup";

        private static ImageSource refSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Reference.png") as ImageSource;
        private static ImageSource classSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/ClassRef.png") as ImageSource;

        #region -- Properties --

        #region -- Value --
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(BlueprintPointerRefControl), new FrameworkPropertyMetadata(null));
        public object Value
        {
            get => GetValue(ValueProperty);
            set { SetValue(ValueProperty, value); Refresh(); }
        }
        #endregion

        #region -- RefValue --
        public static readonly DependencyProperty RefValueProperty = DependencyProperty.Register("RefValue", typeof(string), typeof(BlueprintPointerRefControl), new FrameworkPropertyMetadata(""));
        public string RefValue
        {
            get => (string)GetValue(RefValueProperty);
            set => SetValue(RefValueProperty, value);
        }
        public static readonly DependencyProperty RefValueNameProperty = DependencyProperty.Register("RefValueName", typeof(string), typeof(BlueprintPointerRefControl), new FrameworkPropertyMetadata(""));
        public string RefValueName
        {
            get => (string)GetValue(RefValueNameProperty);
            set => SetValue(RefValueNameProperty, value);
        }
        public static readonly DependencyProperty RefValuePathProperty = DependencyProperty.Register("RefValuePath", typeof(string), typeof(BlueprintPointerRefControl), new FrameworkPropertyMetadata(""));
        public string RefValuePath
        {
            get => (string)GetValue(RefValuePathProperty);
            set => SetValue(RefValuePathProperty, value);
        }
        #endregion

        #region -- RefType --
        public static readonly DependencyProperty RefTypeProperty = DependencyProperty.Register("RefType", typeof(object), typeof(BlueprintPointerRefControl), new FrameworkPropertyMetadata(""));
        public object RefType
        {
            get => GetValue(RefTypeProperty);
            set => SetValue(RefTypeProperty, value);
        }
        #endregion

        public string RefTooltip
        {
            get
            {
                PointerRef pointerRef = (PointerRef)Value;
                if (pointerRef.Type == PointerRefType.Null)
                    return "";
                else if (pointerRef.Type == PointerRefType.External)
                    return pointerRef.External.ClassGuid.ToString();

                dynamic obj = pointerRef.Internal;
                if (obj == null)
                    return "";
                
                return obj.GetInstanceGuid().ToString();
            }
        }

        #endregion

        protected Button assignButton;
        protected Button moreOptionsButton;
        protected ComboBox popup;
        protected Type baseType;

        private List<object> assignObjs = new List<object>();
        private Guid assignFileGuid;
        private bool isInternal = false;
        private bool canCreate = false;
        private bool targetUpdatedBound = false;

        public INodeWrangler NodeWrangler;

        static BlueprintPointerRefControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BlueprintPointerRefControl), new FrameworkPropertyMetadata(typeof(BlueprintPointerRefControl)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            FrostyPropertyGridItemData p = GetPropertyItem();
            baseType = p.GetCustomAttribute<EbxFieldMetaAttribute>().BaseType;

            Focusable = false;

            assignButton = GetTemplateChild(PART_AssignButton) as Button;
            moreOptionsButton = GetTemplateChild(PART_MoreOptionsButton) as Button;
            popup = GetTemplateChild(PART_Popup) as ComboBox;

            assignButton.Click += AssignButton_Click;
            moreOptionsButton.Click += OptionsButton_Click;
            popup.SelectionChanged += Popup_SelectionChanged;
            popup.DropDownOpened += Popup_DropDownOpened;

            if (baseType == null)
            {
                // can't edit
                IsEnabled = false;
            }
            else
            {
                if ((p.Flags & FrostyPropertyGridItemFlags.IsReference) == 0)
                {
                    if (!TypeLibrary.IsSubClassOf(baseType, "Asset"))
                        canCreate = true;
                    else
                    {
                        Type[] types = TypeLibrary.GetTypes(baseType);
                        foreach (Type type in types)
                        {
                            if (type.GetCustomAttribute<IsInlineAttribute>() != null)
                            {
                                canCreate = true;
                                break;
                            }
                        }
                    }
                }
            }

            TargetUpdated += FrostyPointerRefControl_TargetUpdated;
            targetUpdatedBound = true;

            Loaded += (o, e) =>
            {
                if (!targetUpdatedBound)
                    TargetUpdated += FrostyPointerRefControl_TargetUpdated;
            };
            Unloaded += (o, e) =>
            {
                TargetUpdated -= FrostyPointerRefControl_TargetUpdated;
                targetUpdatedBound = false;
            };

            RefreshUI();
        }
        private void Filter_TextChanged(object sender, RoutedEventArgs e)
        {
            TextBox filter = sender as TextBox;
            if (filter.Text == "")
                popup.Items.Filter = null;
            else
            {
                string filterText = filter.Text.ToLower();

                // search name by default
                popup.Items.Filter = (object a) => { return ((PointerRefClassType)a).Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0; };

                // search id instead if valid hex
                if (uint.TryParse(filterText, NumberStyles.HexNumber, null, out uint uintResult))
                {
                    popup.Items.Filter = (object a) => { return ((PointerRefClassType)a).Id.Equals(uintResult); };

                    // if filter was given a valid hex but no results found, assume user was searching for name. ex. "eff" would be valid hex but is likely a name search.
                    if (popup.Items.Count == 0)
                    {
                        popup.Items.Filter = (object a) => { return ((PointerRefClassType)a).Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0; };
                    }
                }

                // search guid instead if valid guid
                if (Guid.TryParse(filterText, out Guid guidResult))
                {
                    uint.TryParse(filterText.Split('-').Last(), NumberStyles.HexNumber, null, out uint id);
                    popup.Items.Filter = (object a) => { return ((PointerRefClassType)a).Guid.Equals(guidResult) || ((PointerRefClassType)a).Id.Equals(id); };
                }
            }
            popup.IsDropDownOpen = true;
        }
        private void FrostyPointerRefControl_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            RefreshName();
        }

        /// <summary>
        /// Updates the popup UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Popup_DropDownOpened(object sender, EventArgs e)
        {
            Popup popupMenu = (popup.Template.FindName("PART_PopupMenu", popup) as Popup);
            Button clearButton = (popupMenu.FindName("PART_ClearButton") as Button);
            Button openButton = (popupMenu.FindName("PART_OpenButton") as Button);
            Button findButton = (popupMenu.FindName("PART_FindButton") as Button);
            Button graphButton = (popupMenu.FindName("PART_OpenGraphButton") as Button);
            Button createButton = (popupMenu.FindName("PART_CreateButton") as Button);
            TextBlock textBlock = (popupMenu.FindName("PART_TextBlock") as TextBlock);
            Border separator = (popupMenu.FindName("PART_Separator") as Border);
            Border tbborder = (popupMenu.FindName("PART_TBBorder") as Border);
            TextBox filter = (popupMenu.FindName("PART_FilterTextBox") as TextBox);

            clearButton.Click -= ClearButton_Click;
            findButton.Click -= FindButton_Click;
            graphButton.Click -= GraphButton_Click;
            openButton.Click -= OpenButton_Click;
            createButton.Click -= CreateButton_Click;

            clearButton.Click += ClearButton_Click;
            findButton.Click += FindButton_Click;
            graphButton.Click += GraphButton_Click;
            openButton.Click += OpenButton_Click;
            createButton.Click += CreateButton_Click;
            filter.TextChanged += Filter_TextChanged;

            PointerRef ptrRef = (PointerRef)Value;
            clearButton.IsEnabled = ptrRef.Type != PointerRefType.Null;
            findButton.IsEnabled = !(ptrRef.Type == PointerRefType.Internal || ptrRef.Type == PointerRefType.Null);
            graphButton.IsEnabled = !(ptrRef.Type == PointerRefType.Internal || ptrRef.Type == PointerRefType.Null);
            openButton.IsEnabled = !(ptrRef.Type == PointerRefType.Internal || ptrRef.Type == PointerRefType.Null);
            createButton.IsEnabled = canCreate;
            textBlock.Text = "Assign from " + ((isInternal) ? "self" : App.AssetManager.GetEbxEntry(assignFileGuid).Name);
            separator.Visibility = (assignObjs.Count != 0 && isInternal) ? Visibility.Visible : Visibility.Collapsed;
            tbborder.Visibility = (assignObjs.Count != 0) ? Visibility.Visible : Visibility.Collapsed;
            filter.Visibility = (assignObjs.Count != 0) ? Visibility.Visible : Visibility.Collapsed;
            clearButton.Visibility = (isInternal) ? Visibility.Visible : Visibility.Collapsed;
            findButton.Visibility = (isInternal) ? Visibility.Visible : Visibility.Collapsed;
            createButton.Visibility = (isInternal) ? Visibility.Visible : Visibility.Collapsed;

            popup.SelectedItem = null;
        }

        /// <summary>
        /// Creates a new class reference
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ClassSelector win = new ClassSelector(TypeLibrary.GetTypes(baseType));
            if (win.ShowDialog() == true)
            {
                Type selectedType = win.SelectedClass;
                object newObj = TypeLibrary.CreateObject(selectedType.Name);

                if (newObj != null)
                {
                    AssetClassGuid guid = new AssetClassGuid();
                    if (((PointerRef)Value).Type == PointerRefType.Internal)
                    {
                        // use existing guid if replacing an object
                        PointerRef existingValue = (PointerRef)Value;
                        dynamic obj = existingValue.Internal;
                        
                        if (obj != null)
                        {
                            guid = obj.GetInstanceGuid();
                        }
                    }

                    // otherwise a new guid
                    if (!guid.IsExported)
                    {
                        if (NodeWrangler is IEbxNodeWrangler)
                        {
                            EbxAssetEntry asset;
                
                            if (!(NodeWrangler.SelectedVertices[0] is IEntityObject))
                                return;
                        
                            //I hope I someday meet gman so I can tell him how much I fucking hate him for making me do this
                            if (((IEntityObject)NodeWrangler.SelectedVertices[0]).Type == PointerRefType.Internal)
                            {
                                asset = App.AssetManager.GetEbxEntry(((IEbxNodeWrangler)NodeWrangler).Asset.FileGuid);
                            }
                            else
                            {
                                IEntityObject node = (IEntityObject)NodeWrangler.SelectedVertices[0];
                                asset = App.AssetManager.GetEbxEntry(node.FileGuid);
                            }

                            // set internal id to -1 so it will be set on adding to asset
                            guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(((IEbxNodeWrangler)NodeWrangler).Asset.Objects, selectedType, asset.Guid), -1);
                        }
                        else
                        {
                            guid = new AssetClassGuid(Guid.NewGuid(), -1);
                        }
                    }

                    PointerRef newValue = new PointerRef(newObj);
                    ((dynamic)newValue.Internal).SetInstanceGuid(guid);

                    if (TypeLibrary.IsSubClassOf(newValue.Internal, "DataBusPeer"))
                    {
                        byte[] b = guid.ExportedGuid.ToByteArray();
                        uint value = (uint)((b[2] << 16) | (b[1] << 8) | b[0]);
                        newValue.Internal.GetType().GetProperty("Flags", BindingFlags.Public | BindingFlags.Instance).SetValue(newValue.Internal, value);
                    }

                    if (NodeWrangler is IEbxNodeWrangler wrangler)
                    {
                        if (!(NodeWrangler.SelectedVertices.LastOrDefault() is IEntityObject))
                            return;

                        IEntityObject node = (IEntityObject)NodeWrangler.SelectedVertices.LastOrDefault();
                        
                        // add it to the list of objects so it can be assigned places
                        if (node.Type == PointerRefType.Internal)
                        {
                            wrangler.Asset.AddObject(newValue.Internal);
                            // TODO: Implement method for adding this object as a new node
                        }
                        else
                        {
                            
                            if (node != null)
                            {
                                EbxAssetEntry assetEntry = App.AssetManager.GetEbxEntry(node.FileGuid);
                                EbxAsset asset = App.AssetManager.GetEbx(assetEntry);
                        
                                asset.AddObject(newValue.Internal);
                            }
                        }
                    }
                    Value = newValue;
                }
            }
            popup.IsDropDownOpen = false;
            e.Handled = true;
        }

        /// <summary>
        /// Opens the referenced asset
        /// </summary>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            PointerRef ptrRef = (PointerRef)Value;
            if (ptrRef.Type == PointerRefType.External)
            {
                App.EditorWindow.OpenAsset(App.AssetManager.GetEbxEntry(ptrRef.External.FileGuid));
            }
            popup.IsDropDownOpen = false;
        }

        /// <summary>
        /// Finds the referenced asset in the data explorer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            PointerRef ptrRef = (PointerRef)Value;
            if (ptrRef.Type == PointerRefType.External)
            {
                App.EditorWindow.DataExplorer.SelectAsset(App.AssetManager.GetEbxEntry(ptrRef.External.FileGuid));
            }
            popup.IsDropDownOpen = false;
        }

        private void GraphButton_Click(object sender, RoutedEventArgs e)
        {
            PointerRef ptrRef = (PointerRef)Value;
            if (ptrRef.Type == PointerRefType.External)
            {
                EbxAssetEntry selectedAsset = App.AssetManager.GetEbxEntry(ptrRef.External.FileGuid);
                if (selectedAsset == null)
                {
                    popup.IsDropDownOpen = false;
                    return;
                }
            
                IEbxGraphEditor graphEditor = ExtensionsManager.GetValidGraphEditor(selectedAsset);
                if (graphEditor == null)
                {
                    App.Logger.LogError("No valid graph editor exists for this file");
                    popup.IsDropDownOpen = false;
                    return;
                }

                BlueprintEditorPlugin.BlueprintEditor editor;
                if (EditorOptions.LoadBeforeOpen)
                {
                    editor = new BlueprintEditorPlugin.BlueprintEditor();
                    editor.LoadBlueprint(selectedAsset, graphEditor);
                }
                else
                {
                    editor = new BlueprintEditorPlugin.BlueprintEditor(selectedAsset, graphEditor);
                }
                
                App.EditorWindow.OpenEditor($"{selectedAsset.Filename} (Ebx Graph)", editor);
                popup.IsDropDownOpen = false;
            }
        }

        /// <summary>
        /// Performs the actual assinging for either internal or external selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Popup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (popup.SelectedItem == null)
                return;

            int index = (popup.SelectedItem as PointerRefClassType).Index;
            object obj = assignObjs[index];

            if (isInternal)
            {
                Value = new PointerRef(obj);
            }
            else
            {
                AssetClassGuid guid = ((dynamic)assignObjs[index]).GetInstanceGuid();

                EbxImportReference reference = new EbxImportReference()
                {
                    FileGuid = assignFileGuid,
                    ClassGuid = guid.ExportedGuid
                };

                if (NodeWrangler is IEbxNodeWrangler wrangler)
                {
                    if (!(NodeWrangler.SelectedVertices.LastOrDefault() is IEntityObject))
                        return;
                
                    //I hope I someday meet gman so I can tell him how much I fucking hate him for making me do this
                    if (((IEntityObject)NodeWrangler.SelectedVertices.LastOrDefault()).Type == PointerRefType.Internal)
                    {
                        wrangler.Asset.AddDependency(reference.FileGuid);
                    }
                    else
                    {
                        IEntityObject node = ((IEntityObject)NodeWrangler.SelectedVertices.LastOrDefault());
                        EbxAssetEntry asset = App.AssetManager.GetEbxEntry(node.FileGuid);
                        EbxAsset ebx = App.AssetManager.GetEbx(asset);

                        ebx.AddDependency(reference.FileGuid);
                    }
                }
                Value = new PointerRef(reference);
            }
        }

        /// <summary>
        /// Brings up the options menu, where a user can clear the reference, find a reference in the data explorer,
        /// create a new reference, or assign from a class within the currently edited ebx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            isInternal = true;
            assignObjs.Clear();

            List<PointerRefClassType> types = new List<PointerRefClassType>();

            if (NodeWrangler is IEbxNodeWrangler wrangler)
            {
                foreach (dynamic obj in wrangler.Asset.Objects)
                {
                    if (TypeLibrary.IsSubClassOf((object)obj, baseType.Name))
                    {
                        assignObjs.Add(obj);
                        AssetClassGuid guid = obj.GetInstanceGuid();

                        types.Add(new PointerRefClassType()
                        {
                            Name = obj.__Id,
                            Type = obj.GetType(),
                            Guid = guid.ExportedGuid,
                            Id = (uint)guid.InternalId,
                            Index = assignObjs.Count - 1,
                            HasCustomTransientId = obj.__Id != obj.GetType().Name
                        });
                    }
                }
            }

            types = types.OrderBy(t => !t.HasCustomTransientId).ThenBy(t => t.Type.Name).ThenBy(t => t.Guid).ThenBy(t => t.Id).ToList();

            //types.Sort((PointerRefClassType a, PointerRefClassType b) => 
            //{
            //    int i = a.Type.Name.CompareTo(b.Type.Name);
            //    return (i == 0) ? (a.Guid.CompareTo(b.Guid) + a.Id.CompareTo(b.Id)) : i;
            //});

            popup.ItemsSource = types;
            popup.IsDropDownOpen = true;
        }

        /// <summary>
        /// Clears the reference
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Value = new PointerRef();
            popup.IsDropDownOpen = false;
        }

        /// <summary>
        /// Assigns an ebx class from an external ebx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            isInternal = false;

            // no selected asset, no menu
            EbxAssetEntry selectedAsset = App.SelectedAsset;
            if (selectedAsset == null)
                return;

            // selected asset is the same as the editing one
            if (NodeWrangler is IEbxNodeWrangler wrangler)
            {
                EbxAssetEntry currentAsset;
            
                if (!(NodeWrangler.SelectedVertices[0] is IEntityObject))
                    return;
            
                if (((IEntityObject)NodeWrangler.SelectedVertices[0]).Type == PointerRefType.Internal)
                {
                    currentAsset = App.AssetManager.GetEbxEntry(wrangler.Asset.FileGuid);
                }
                else
                {
                    IEntityObject node = ((IEntityObject)NodeWrangler.SelectedVertices[0]);
                    currentAsset = App.AssetManager.GetEbxEntry(node.FileGuid);
                }
            
                isInternal = selectedAsset == currentAsset;
                if (isInternal)
                    return;
            }

            EbxAsset asset = App.AssetManager.GetEbx(selectedAsset);
            assignFileGuid = asset.FileGuid;

            int count = 0;
            assignObjs.Clear();
            List<PointerRefClassType> types = new List<PointerRefClassType>();

            foreach (dynamic obj in asset.ExportedObjects)
            {
                // only exported objects of the base or sub types are allowed
                if (TypeLibrary.IsSubClassOf((object)obj, baseType.Name))
                {
                    assignObjs.Add(obj);
                    types.Add(new PointerRefClassType()
                    {
                        Name = obj.__Id,
                        Type = obj.GetType(),
                        Guid = obj.GetInstanceGuid().ExportedGuid,
                        Id = 0,
                        Index = assignObjs.Count - 1,
                        HasCustomTransientId = obj.__Id != obj.GetType().Name
                    });

                    count++;
                }
            }

            if (count == 0)
            {
                // display error on no types found
                FrostyMessageBox.Show("No valid types found in asset, must be a subclass of '" + baseType.Name + "'", "Frosty Editor");
            }
            else if (count == 1)
            {
                AssetClassGuid guid = ((dynamic)assignObjs[0]).GetInstanceGuid();

                // automatically assign since only one type was found
                EbxImportReference reference = new EbxImportReference()
                {
                    FileGuid = assignFileGuid,
                    ClassGuid = guid.ExportedGuid
                };

                if (NodeWrangler is IEbxNodeWrangler)
                {
                    if (!(NodeWrangler.SelectedVertices[0] is IEntityObject))
                        return;
                
                    if (((IEntityObject)NodeWrangler.SelectedVertices[0]).Type == PointerRefType.Internal)
                    {
                        ((IEbxNodeWrangler)NodeWrangler).Asset.AddDependency(reference.FileGuid);
                    }
                    else
                    {
                        IEntityObject node = ((IEntityObject)NodeWrangler.SelectedVertices[0]);
                        EbxAssetEntry nodeAsset = App.AssetManager.GetEbxEntry(node.FileGuid);
                        EbxAsset ebx = App.AssetManager.GetEbx(nodeAsset);

                        ebx.AddDependency(reference.FileGuid);
                    }
                }
                Value = new PointerRef(reference);
            }
            else
            {
                // display list
                types = types.OrderBy(t => !t.HasCustomTransientId).ThenBy(t => t.Type.Name).ThenBy(t => t.Guid).ThenBy(t => t.Id).ToList();

                types.Sort((PointerRefClassType a, PointerRefClassType b) =>
                {
                    int i = a.Id.CompareTo(b.Id);
                    return (i == 0) ? a.Guid.CompareTo(b.Guid) : i;
                });

                popup.ItemsSource = types;
                popup.IsDropDownOpen = true;
            }
        }

        /// <summary>
        /// Updates the UI and bindings
        /// </summary>
        protected virtual void Refresh()
        {
            RefreshUI();
            RefreshName();

            TextBlock tb = GetTemplateChild("PART_RefName") as TextBlock;
            Image img = GetTemplateChild("PART_TypeImage") as Image;

            tb.GetBindingExpression(TextBlock.ToolTipProperty).UpdateTarget();
            BindingOperations.GetBindingExpression(img, Image.SourceProperty).UpdateTarget();
        }

        protected virtual void RefreshName()
        {
            PointerRef pointerRef = (PointerRef)Value;

            dynamic value = null;
            string path = "";
            string type = "";

            if (pointerRef.Type == PointerRefType.External)
            {
                EbxAssetEntry entry = App.AssetManager.GetEbxEntry(pointerRef.External.FileGuid);

                if (entry != null)
                {
                    EbxAsset asset = App.AssetManager.GetEbx(entry);
                    if (asset == null || pointerRef.External.ClassGuid == Guid.Empty || pointerRef.External.ClassGuid == asset.RootInstanceGuid)
                    {
                        // fallback or external root asset
                        RefValue = entry.Filename + ((entry.Path != "") ? " (" + entry.Name + ")" : "");
                        RefValueName = entry.Filename;
                        RefValuePath = entry.Name;
                        RefType = entry;
                        return;
                    }
                    else
                    {
                        // external object referencing non root class
                        dynamic obj = asset.GetObject(pointerRef.External.ClassGuid);
                        if (obj == null)
                        {
                            obj = asset.GetObject(pointerRef.External.ClassGuid);
                        }

                        if (obj != null)
                        {
                            value = obj;
                            path = entry.Name;
                            type = obj.GetType().Name;
                        }
                        else
                        {
                            RefValue = "(invalid)";
                            RefValueName = RefValue;
                            RefValuePath = "";
                            RefType = (baseType != null) ? baseType.Name : "";
                            return;
                        }
                    }
                }
                else
                {
                    RefValue = "(invalid)";
                    RefValueName = RefValue;
                    RefValuePath = "";
                    RefType = (baseType != null) ? baseType.Name : "";
                    return;
                }
            }
            else if (pointerRef.Type == PointerRefType.Internal && NodeWrangler is IEbxNodeWrangler wrangler)
            {
                EbxAssetEntry currentAsset = App.AssetManager.GetEbxEntry(wrangler.Asset.FileGuid);
                value = pointerRef.Internal;
                if (value == null)
                {
                    RefValue = "(invalid)";
                    RefValueName = RefValue;
                    RefValuePath = "";
                    RefType = (baseType != null) ? baseType.Name : "";
                    return;
                }
                
                path = currentAsset.Name;
                type = pointerRef.Internal.GetType().Name;
            }

            if (value == null)
            {
                RefValue = "(null)";
                RefValueName = RefValue;
                RefValuePath = "";
                RefType = (baseType != null) ? baseType.Name : "";
                return;
            }

            RefType = type;
            RefValue = value.__Id + " (" + path + ")";
            RefValueName = value.__Id;
            RefValuePath = path;
        }

        /// <summary>
        /// Updates the UI
        /// </summary>
        private void RefreshUI()
        {
            //PointerRef pointerRef = (PointerRef)Value;
            //Border b = GetTemplateChild("PART_InternalIcon") as Border;
            //Image i = GetTemplateChild("PART_TypeImage") as Image;

            //if (pointerRef.Type == PointerRefType.Internal)
            //{
            //    b.Visibility = Visibility.Visible;
            //    i.Visibility = Visibility.Collapsed;
            //}
            //else
            //{
            //    b.Visibility = Visibility.Collapsed;
            //    i.Visibility = Visibility.Visible;
            //}
        }

        /// <summary>
        /// Returns the property grid item that this editor belongs to
        /// </summary>
        /// <returns></returns>
        private FrostyPropertyGridItemData GetPropertyItem()
        {
            Binding b = BindingOperations.GetBinding(this, ValueProperty);
            return b.Source as FrostyPropertyGridItemData;
        }
    }
}
