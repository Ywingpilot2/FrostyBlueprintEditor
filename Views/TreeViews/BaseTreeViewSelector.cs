using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Frosty.Controls;
using Frosty.Core.Controls;

namespace BlueprintEditorPlugin.Views.TreeViews
{
    public class TreeExplorerFolderItem : BaseTreeExplorerItem
    {
        public List<BaseTreeExplorerItem> SubItems { get; set; }
        public override bool IsExpandable => true;
        public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/CloseFolder.png") as ImageSource;

        public TreeExplorerFolderItem()
        {
            SubItems = new List<BaseTreeExplorerItem>();
        }
    }

    public class BaseTreeExplorerItem
    {
        public virtual string Name { get; set; }
        public virtual ImageSource Icon { get; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public virtual bool IsExpandable => false;
    }
    
    public enum TreeViewSelectionMode
    {
        Click,
        Drag,
        Both
    }
    
    [TemplatePart(Name = PART_FilterTextBox, Type = typeof(FrostyWatermarkTextBox))]
    [TemplatePart(Name = PART_ModuleClassView, Type = typeof(TreeView))]
    public class BaseTreeViewSelector : Control
    {
        private const string PART_FilterTextBox = "FilterBox_Element";
        private const string PART_ModuleClassView = "TreeExplorer_Element";
        
        static BaseTreeViewSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BaseTreeViewSelector), new FrameworkPropertyMetadata(typeof(BaseTreeViewSelector)));
        }
        
        public static readonly DependencyProperty TreeViewsProperty = DependencyProperty.Register("TreeViews", typeof(IList<BaseTreeExplorerItem>), typeof(BaseTreeViewSelector), new UIPropertyMetadata(null));
        public IList<BaseTreeExplorerItem> TreeViews
        {
            get => (IList<BaseTreeExplorerItem>)GetValue(TreeViewsProperty);
            set => SetValue(TreeViewsProperty, value);
        }
        
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register("Mode", typeof(TreeViewSelectionMode), typeof(BaseTreeViewSelector), new UIPropertyMetadata(TreeViewSelectionMode.Click));
        public TreeViewSelectionMode Mode
        {
            get => (TreeViewSelectionMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        
        public BaseTreeExplorerItem SelectedItem => _treeExplorer.SelectedItem as BaseTreeExplorerItem;

        protected FrostyWatermarkTextBox filterTextBox;
        protected TreeView _treeExplorer;
        
        public event RoutedPropertyChangedEventHandler<object> SelectedItemChanged;
        public event MouseButtonEventHandler ItemDoubleClicked;
        
        /// <summary>
        /// When <see cref="Mode"/> is set to <see cref="TreeViewSelectionMode"/>.Drag, this event is fired when the user begins dragging an element in the treeview
        /// </summary>
        public event RoutedEventHandler ItemBeginDrag;
        
        /// <summary>
        /// When <see cref="Mode"/> is set to <see cref="TreeViewSelectionMode"/>.Drag, this event is fired when the user cancels dragging an element
        /// </summary>
        public event RoutedEventHandler ItemCancelDrag;
        
        /// <summary>
        /// When <see cref="Mode"/> is set to <see cref="TreeViewSelectionMode"/>.Drag, this event is fired when the user finishes dragging an element
        /// </summary>
        public event RoutedEventHandler ItemEndDrag;

        private bool _isDragging;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            filterTextBox = GetTemplateChild(PART_FilterTextBox) as FrostyWatermarkTextBox;
            _treeExplorer = GetTemplateChild(PART_ModuleClassView) as TreeView;

            _treeExplorer.SelectedItemChanged += SelectedItemChanged;
            if (Mode is TreeViewSelectionMode.Click or TreeViewSelectionMode.Both)
            {
                _treeExplorer.MouseDoubleClick += ItemDoubleClicked;
            }
            if (Mode is TreeViewSelectionMode.Drag or TreeViewSelectionMode.Both)
            {
                _treeExplorer.PreviewMouseLeftButtonDown += BeginDragHandling;
                _treeExplorer.PreviewMouseLeftButtonUp += EndDragHandling;
            }
            
            filterTextBox.LostFocus += filterTextBox_LostFocus;
            filterTextBox.KeyUp += filterTextBox_KeyUp;
            
            UpdateTreeView();
        }
        
        private void filterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (filterTextBox.Text == "")
                SetFilter(null);
            else
            {
                SetFilter(item => ((BaseTreeExplorerItem)item).Name.IndexOf(filterTextBox.Text.ToLower(), StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void filterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                filterTextBox_LostFocus(this, new RoutedEventArgs());
        }

        /// <summary>
        /// TODO: This isn't a good way to filter a tree view. This limits only 1 level of filtering down a folder chain
        /// How deep down folders go shouldn't matter to begin with
        /// Though I also suppose if someone needed it to do more, they would override this and <see cref="UpdateTreeView"/> to remake the tree
        /// </summary>
        /// <param name="filter"></param>
        public virtual void SetFilter(Predicate<object> filter)
        {
            if (filter == null)
            {
                _treeExplorer.Items.Filter = null;
                foreach (BaseTreeExplorerItem item in _treeExplorer.Items)
                {
                    if (item is TreeExplorerFolderItem)
                    {
                        TreeViewItem folderItem = _treeExplorer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                        folderItem.Items.Filter = null;
                    }
                }
                
                return;
            }
            
            _treeExplorer.Items.Filter = item =>
            {
                if (item is TreeExplorerFolderItem folder)
                {
                    if (folder.SubItems.Any(i => filter.Invoke(i)))
                    {
                        TreeViewItem folderItem = _treeExplorer.ItemContainerGenerator.ContainerFromItem(folder) as TreeViewItem;
                        folderItem.Items.Filter = filter;
                        return true;
                    }

                    return false;
                }
                else
                {
                    return filter.Invoke((BaseTreeExplorerItem)item);
                }
            };
        }

        protected virtual void UpdateTreeView()
        {
            _treeExplorer.Items.Refresh();
        }

        private void BeginDragHandling(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            object item = GetDataFromMousePosition(_treeExplorer);
            if (item == null || item is TreeExplorerFolderItem)
                return;

            _isDragging = true;
            ItemBeginDrag?.Invoke(sender, new RoutedEventArgs());
        }
        
        private void EndDragHandling(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (!_isDragging)
                return;
            
            object item = GetDataFromMousePosition(_treeExplorer);
            _isDragging = false;
            
            if (item != null)
            {
                ItemCancelDrag?.Invoke(sender, new RoutedEventArgs());
                return;
            }
            
            ItemEndDrag?.Invoke(sender, new RoutedEventArgs());
        }
        
        protected object GetDataFromMousePosition(ItemsControl source)
        {
            if (source.InputHitTest(Mouse.GetPosition(source)) is UIElement element)
            {
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    data = source.ItemContainerGenerator.ItemFromContainer(element);

                    if (data == DependencyProperty.UnsetValue)
                    {
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    }

                    if (element == source)
                    {
                        return null;
                    }
                }

                if (data != DependencyProperty.UnsetValue)
                {
                    (element as TreeViewItem).IsSelected = true;
                    return data is ClassSelectorModuleItem ? GetDataFromMousePosition(element as TreeViewItem) : data;
                }
            }

            return null;
        }
    }
}