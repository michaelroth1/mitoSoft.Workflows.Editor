﻿using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using mitoSoft.Workflows.Editor.Helpers.Extensions;
using Splat;
using System;
using System.Reactive.Linq;
using System.Windows;

namespace mitoSoft.Workflows.Editor.Helpers.Transformations
{
    public class Scale : ReactiveObject
    {
        [Reactive] public Point Scales { get; set; } = new Point(1, 1);
        [Reactive] public Point Center { get; set; }

        [Reactive] public double Value { get; set; } = 1.0;

        public double ScaleX
        {
            get { return Scales.X; }
        }
        public double ScaleY
        {
            get { return Scales.Y; }
        }

        public double CenterX
        {
            get { return Center.X; }
        }
        public double CenterY
        {
            get { return Center.Y; }
        }

        public Scale()
        {
            this.WhenAnyValue(x => x.Value).Subscribe(value => Scales = PointExtensition.CreatePoint(value));
        }
    }
}
