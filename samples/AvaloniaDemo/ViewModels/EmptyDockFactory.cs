﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Factories;

namespace AvaloniaDemo.ViewModels
{
    /// <inheritdoc/>
    public class EmptyDockFactory : BaseDockFactory
    {
        /// <inheritdoc/>
        public override IDock CreateLayout()
        {
            var view = new ViewDock
            {
                Id = nameof(ViewDock),
                Dock = "",
                Width = double.NaN,
                Height = double.NaN,
                Title = nameof(ViewDock)
            };

            return new RootDock
            {
                Id = nameof(RootDock),
                Dock = "",
                Width = double.NaN,
                Height = double.NaN,
                Title = nameof(RootDock),
                CurrentView = view,
                DefaultView = view,
                Views = new ObservableCollection<IDock>
                {
                    view
                }
            };
        }

        /// <inheritdoc/>
        public override void InitLayout(IDock layout, object context)
        {
            this.ContextLocator = new Dictionary<string, Func<object>>
            {
                [nameof(RootDock)] = () => context,
                [nameof(LayoutDock)] = () => context,
                [nameof(DocumentDock)] = () => context,
                [nameof(ToolDock)] = () => context,
                [nameof(SplitterDock)] = () => context,
                [nameof(DockWindow)] = () => context
            };

            this.HostLocator = new Dictionary<string, Func<IDockHost>>
            {
                [nameof(DockWindow)] = () => new HostWindow()
            };

            this.Update(layout, context, null);

            layout.ShowWindows();

            layout.CurrentView = layout.DefaultView;
            layout.CurrentView?.ShowWindows();
        }
    }
}
