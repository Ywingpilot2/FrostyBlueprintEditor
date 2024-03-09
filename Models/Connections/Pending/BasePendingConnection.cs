using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BlueprintEditorPlugin.Editors.NodeWrangler;
using BlueprintEditorPlugin.Models.Nodes.Ports;
using BlueprintEditorPlugin.Options;
using Prism.Commands;

namespace BlueprintEditorPlugin.Models.Connections.Pending
{
    public class BasePendingConnection : IPendingConnection
    {
        private readonly INodeWrangler _nodeWrangler;
        public IPort Source { get; set; }


        #region Anchors

        private Point _sAnchor;
        public Point SourceAnchor
        {
            get => _sAnchor;
            set
            {
                _sAnchor = value;
                NotifyPropertyChanged(nameof(SourceAnchor));
                NotifyPropertyChanged(nameof(CurvePoint1));
            }
        }

        private Point _tAnchor;
        public Point TargetAnchor
        {
            get => _tAnchor;
            set
            {
                _tAnchor = value;
                NotifyPropertyChanged(nameof(TargetAnchor));
                NotifyPropertyChanged(nameof(CurvePoint2));
            }
        }

        #endregion

        #region Curve Points

        public Point CurvePoint1
        {
            get
            {
                //The curve point is just the average of the 2 points
                if (Source != null && Source.Direction == PortDirection.Out)
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(SourceAnchor.X + 85,
                            Source.Anchor.Y);
                    }
                    else
                    {
                        return new Point(SourceAnchor.X + 25,
                            Source.Anchor.Y);
                    }
                }
                else
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(TargetAnchor.X - 85,
                            TargetAnchor.Y);
                    }
                    else
                    {
                        return new Point(TargetAnchor.X - 25,
                            TargetAnchor.Y);
                    }
                }
            }
        }

        public Point CurvePoint2
        {
            get
            {
                //The curve point is just the average of the 2 points
                if (Source != null && Source.Direction == PortDirection.Out)
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(TargetAnchor.X - 85,
                            TargetAnchor.Y);
                    }
                    else
                    {
                        return new Point(TargetAnchor.X - 25,
                            TargetAnchor.Y);
                    }
                }
                else
                {
                    if (EditorOptions.WireStyle == ConnectionStyle.Curvy)
                    {
                        //The curve point is just the average of the 2 points
                        return new Point(SourceAnchor.X + 85,
                            SourceAnchor.Y);
                    }
                    else
                    {
                        return new Point(SourceAnchor.X + 25,
                            SourceAnchor.Y);
                    }
                }
            }
        }

        #endregion

        public ICommand Start { get; }
        public ICommand Finish { get; }

        public BasePendingConnection(INodeWrangler wrangler)
        {
            _nodeWrangler = wrangler;
            
            Start = new DelegateCommand<IPort>(StartPending);
            Finish = new DelegateCommand<IPort>(StopPending);
        }

        public virtual void StartPending(IPort source)
        {
            Source = source;
            NotifyPropertyChanged(nameof(CurvePoint1));
            NotifyPropertyChanged(nameof(CurvePoint2));
        }

        public virtual void StopPending(IPort target)
        {
            if (target == null)
                return;
                
            if (Source.Direction == PortDirection.Out && target.Direction == PortDirection.In)
            {
                _nodeWrangler.AddConnection(new BaseConnection((BaseOutput)Source, (BaseInput)target));
            }
            else if (Source.Direction == PortDirection.In && target.Direction == PortDirection.Out)
            {
                _nodeWrangler.AddConnection(new BaseConnection((BaseOutput)target, (BaseInput)Source));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}