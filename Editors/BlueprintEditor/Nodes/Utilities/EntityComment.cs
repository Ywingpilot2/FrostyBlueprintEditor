using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlueprintEditorPlugin.Models.Nodes;
using BlueprintEditorPlugin.Models.Nodes.Utilities;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace BlueprintEditorPlugin.Editors.BlueprintEditor.Nodes.Utilities
{
    public class EntityComment : BaseComment, IObjectContainer
    {
        private SolidColorBrush _headerColor;
        public SolidColorBrush HeaderColor
        {
            get => _headerColor;
            set
            {
                _headerColor = value;
                NotifyPropertyChanged(nameof(HeaderColor));
            }
        }

        public object Object { get; }

        public EntityComment(string header, Color color)
        {
            Header = header;
            HeaderColor = new SolidColorBrush(color);
            Object = new EditCommentArgs(this);
        }

        public EntityComment(string header)
        {
            Header = header;
            HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E7F4B"));
            Object = new EditCommentArgs(this);
        }
        
        public EntityComment()
        {
            Header = "Header";
            HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E7F4B"));
            Size = new System.Windows.Size(8, 8);
            Object = new EditCommentArgs(this);
        }

        public override ITransient Load(NativeReader reader)
        {
            throw new System.NotImplementedException();
        }

        public override void Save(NativeWriter writer)
        {
            throw new System.NotImplementedException();
        }

        
        public void OnObjectModified(object sender, ItemModifiedEventArgs args)
        {
            EditCommentArgs editComment = (EditCommentArgs)Object;
            Header = editComment.Header;
            HeaderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(editComment.Color));
        }
    }

    public class EditCommentArgs
    {
        public string Header { get; set; }
        public string Color { get; set; }

        public EditCommentArgs(EntityComment comment)
        {
            Header = comment.Header;
            Color = comment.HeaderColor.Color.ToString();
        }

        public EditCommentArgs()
        {
        }
    }
}