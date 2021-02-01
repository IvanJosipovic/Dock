﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace Dock.Model
{
    /// <summary>
    /// Factory base class.
    /// </summary>
    public abstract class FactoryBase : IFactory
    {
        /// <inheritdoc/>
        public virtual IDictionary<string, Func<object>>? ContextLocator { get; set; }

        /// <inheritdoc/>
        public virtual IDictionary<string, Func<IHostWindow>>? HostWindowLocator { get; set; }

        /// <inheritdoc/>
        public virtual IDictionary<string, Func<IDockable>>? DockableLocator { get; set; }

        /// <inheritdoc/>
        public abstract IList<T> CreateList<T>(params T[] items);

        /// <inheritdoc/>
        public abstract IRootDock CreateRootDock();

        /// <inheritdoc/>
        public abstract IProportionalDock CreateProportionalDock();

        /// <inheritdoc/>
        public abstract ISplitterDock CreateSplitterDock();

        /// <inheritdoc/>
        public abstract IToolDock CreateToolDock();

        /// <inheritdoc/>
        public abstract IDocumentDock CreateDocumentDock();

        /// <inheritdoc/>
        public abstract IDockWindow CreateDockWindow();

        /// <inheritdoc/>
        public abstract IDock? CreateLayout();

        /// <inheritdoc/>
        public virtual void CreateDocument(IDocumentDock dock)
        {
        }

        /// <inheritdoc/>
        public virtual object? GetContext(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Func<object>? locator = null;
                if (ContextLocator?.TryGetValue(id, out locator) == true)
                {
                    return locator?.Invoke();
                }
            }
            Debug.WriteLine($"Context with provided id={id} is not registered.");
            return null;
        }

        /// <inheritdoc/>
        public virtual IHostWindow? GetHostWindow(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Func<IHostWindow>? locator = null;
                if (HostWindowLocator?.TryGetValue(id, out locator) == true)
                {
                    return locator?.Invoke();
                }
            }
            Debug.WriteLine($"Host window with provided id={id} is not registered.");
            return null;
        }

        /// <inheritdoc/>
        public virtual IDockable? GetDockable(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Func<IDockable>? locator = null;
                if (DockableLocator?.TryGetValue(id, out locator) == true)
                {
                    return locator?.Invoke();
                }
            }
            Debug.WriteLine($"Dockable with provided id={id} is not registered.");
            return null;
        }

        /// <inheritdoc/>
        public virtual void InitLayout(IDockable layout)
        {
            UpdateDockable(layout, null);

            if (layout is IDock dock)
            {
                if (dock.DefaultDockable is not null)
                {
                    dock.ActiveDockable = dock.DefaultDockable;
                }
            }

            if (layout is IRootDock rootDock)
            {
                if (rootDock.ShowWindows.CanExecute(null))
                {
                    rootDock.ShowWindows.Execute(null);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void UpdateDockWindow(IDockWindow window, IDockable? owner)
        {
            window.Host = GetHostWindow(window.Id);
            if (window.Host is not null)
            {
                window.Host.Window = window;
            }

            window.Owner = owner;
            window.Factory = this;

            if (window.Layout is not null)
            {
                UpdateDockable(window.Layout, window.Layout.Owner);
            }
        }

        /// <inheritdoc/>
        public virtual void UpdateDockable(IDockable dockable, IDockable? owner)
        {
            if (GetContext(dockable.Id) is { } context)
            {
                dockable.Context = context;
            }

            dockable.Owner = owner;

            if (dockable is IDock dock)
            {
                dock.Factory = this;

                if (dock.VisibleDockables is not null)
                {
                    foreach (var child in dock.VisibleDockables)
                    {
                        UpdateDockable(child, dockable);
                    }
                }
            }

            if (dockable is IRootDock rootDock)
            {
                if (rootDock.Windows is not null)
                {
                    foreach (var child in rootDock.Windows)
                    {
                        UpdateDockWindow(child, dockable);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public virtual void AddDockable(IDock dock, IDockable dockable)
        {
            UpdateDockable(dockable, dock);
            dock.VisibleDockables ??= CreateList<IDockable>();
            dock.VisibleDockables.Add(dockable);
        }

        /// <inheritdoc/>
        public virtual void InsertDockable(IDock dock, IDockable dockable, int index)
        {
            if (index >= 0)
            {
                UpdateDockable(dockable, dock);
                dock.VisibleDockables ??= CreateList<IDockable>();
                dock.VisibleDockables.Insert(index, dockable);
            }
        }

        /// <inheritdoc/>
        public virtual void AddWindow(IRootDock rootDock, IDockWindow window)
        {
            rootDock.Windows ??= CreateList<IDockWindow>();
            rootDock.Windows.Add(window);
            UpdateDockWindow(window, rootDock);
        }

        /// <inheritdoc/>
        public virtual void RemoveWindow(IDockWindow window)
        {
            if (window.Owner is IRootDock rootDock)
            {
                window.Exit();
                rootDock.Windows?.Remove(window);
            }
        }

        /// <inheritdoc/>
        public virtual void SetActiveDockable(IDockable dockable)
        {
            if (dockable.Owner is IDock dock)
            {
                dock.ActiveDockable = dockable;
            }
        }

        private void SetIsActive(IDockable dockable, bool active)
        {
            if (dockable is IDock dock)
            {
                dock.IsActive = active;
            }
        }

        /// <inheritdoc />
        public void SetFocusedDockable(IDock dock, IDockable? dockable)
        {
            if (dock.ActiveDockable is not null && FindRoot(dock.ActiveDockable, x => x.IsFocusableRoot) is { } root)
            {
                if (root.FocusedDockable?.Owner is not null)
                {
                    SetIsActive(root.FocusedDockable.Owner, false);
                }

                if (dockable is not null)
                {
                    root.FocusedDockable = dockable;
                }

                if (root.FocusedDockable?.Owner is not null)
                {
                    SetIsActive(root.FocusedDockable.Owner, true);
                }
            }
        }

        /// <inheritdoc/>
        public virtual IRootDock? FindRoot(IDockable dockable, Func<IRootDock, bool> predicate)
        {
            if (dockable.Owner is null)
            {
                return null;
            }
            if (dockable.Owner is IRootDock rootDock && predicate(rootDock))
            {
                return rootDock;
            }
            return FindRoot(dockable.Owner, predicate);
        }

        /// <inheritdoc/>
        public virtual IDockable? FindDockable(IDock dock, Func<IDockable, bool> predicate)
        {
            if (predicate(dock))
            {
                return dock;
            }

            if (dock.VisibleDockables is not null)
            {
                foreach (var dockable in dock.VisibleDockables)
                {
                    if (predicate(dockable))
                    {
                        return dockable;
                    }

                    if (dockable is IDock childDock)
                    {
                        var result = FindDockable(childDock, predicate);
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }
            }

            if (dock is IRootDock rootDock)
            {
                if (rootDock.Windows is not null)
                {
                    foreach (var window in rootDock.Windows)
                    {
                        if (window.Layout is not null)
                        {
                            if (predicate(window.Layout))
                            {
                                return window.Layout;
                            }

                            var result = FindDockable(window.Layout, predicate);
                            if (result is not null)
                            {
                                return result;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public virtual void PinDockable(IDockable dockable)
        {
            if (dockable.Owner is IToolDock toolDock)
            {
                var isVisible = false;
                var isPinned = false;

                if (toolDock.VisibleDockables is not null)
                {
                    isVisible = toolDock.VisibleDockables.Contains(dockable);
                }

                if (toolDock.PinnedDockables is not null)
                {
                    isPinned = toolDock.PinnedDockables.Contains(dockable);
                }

                if (isVisible && !isPinned)
                {
                    // Pin dockable.

                    toolDock.PinnedDockables ??= CreateList<IDockable>();

                    if (toolDock.VisibleDockables is not null)
                    {
                        toolDock.VisibleDockables.Remove(dockable);
                        toolDock.PinnedDockables.Add(dockable);
                    }

                    // TODO: Handle ActiveDockable state.
                    // TODO: Handle IsExpanded property of IToolDock.
                    // TODO: Handle AutoHide property of IToolDock.
                }
                else if (!isVisible && isPinned)
                {
                    // Unpin dockable.

                    toolDock.VisibleDockables ??= CreateList<IDockable>();

                    if (toolDock.PinnedDockables is not null)
                    {
                        toolDock.PinnedDockables.Remove(dockable);
                        toolDock.VisibleDockables.Add(dockable);
                    }

                    // TODO: Handle ActiveDockable state.
                    // TODO: Handle IsExpanded property of IToolDock.
                    // TODO: Handle AutoHide property of IToolDock.
                }
                else
                {
                    // TODO: Handle invalid state.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void FloatDockable(IDockable dockable)
        {
            if (dockable.Owner is IDock dock)
            {
                SplitToWindow(dock, dockable, 0, 0, 300, 400);
            }
        }

        private void Collapse(IDock dock)
        {
            if (dock.IsCollapsable && dock.VisibleDockables is not null && dock.VisibleDockables.Count == 0)
            {
                if (dock.Owner is IDock ownerDock && ownerDock.VisibleDockables is { })
                {
                    var toRemove = new List<IDockable>();
                    var dockIndex = ownerDock.VisibleDockables.IndexOf(dock);

                    if (dockIndex >= 0)
                    {
                        var indexSplitterPrevious = dockIndex - 1;
                        if (dockIndex > 0 && indexSplitterPrevious >= 0)
                        {
                            var previousVisible = ownerDock.VisibleDockables[indexSplitterPrevious];
                            if (previousVisible is ISplitterDock splitterPrevious)
                            {
                                toRemove.Add(splitterPrevious);
                            }
                        }

                        var indexSplitterNext = dockIndex + 1;
                        if (dockIndex < ownerDock.VisibleDockables.Count - 1 && indexSplitterNext >= 0)
                        {
                            var nextVisible = ownerDock.VisibleDockables[indexSplitterNext];
                            if (nextVisible is ISplitterDock splitterNext)
                            {
                                toRemove.Add(splitterNext);
                            }
                        }

                        foreach (var removeVisible in toRemove)
                        {
                            RemoveDockable(removeVisible, true);
                        }
                    }
                    else
                    {
                        // TODO:
                    }
                }

                if (dock is IRootDock rootDock && rootDock.Window is { })
                {
                    RemoveWindow(rootDock.Window);
                }
                else
                {
                    RemoveDockable(dock, true);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void RemoveDockable(IDockable dockable, bool collapse)
        {
            if (dockable.Owner is IDock dock && dock.VisibleDockables is { })
            {
                var index = dock.VisibleDockables.IndexOf(dockable);
                if (index < 0)
                {
                    return;
                }
                dock.VisibleDockables.Remove(dockable);
                var indexActiveDockable = index > 0 ? index - 1 : 0;
                if (dock.VisibleDockables.Count > 0)
                {
                    var nextActiveDockable = dock.VisibleDockables[indexActiveDockable];
                    dock.ActiveDockable = nextActiveDockable is not ISplitterDock ? nextActiveDockable : null;
                }
                else
                {
                    dock.ActiveDockable = null;
                }
                if (dock.VisibleDockables.Count == 1)
                {
                    var dockable0 = dock.VisibleDockables[0];
                    if (dockable0 is ISplitterDock splitter0)
                    {
                        RemoveDockable(splitter0, false);
                    }
                }
                if (dock.VisibleDockables.Count == 2)
                {
                    var dockable0 = dock.VisibleDockables[0];
                    var dockable1 = dock.VisibleDockables[1];
                    if (dockable0 is ISplitterDock splitter0)
                    {
                        RemoveDockable(splitter0, false);
                    }
                    if (dockable1 is ISplitterDock splitter1)
                    {
                        RemoveDockable(splitter1, false);
                    }
                }
                if (collapse)
                {
                    Collapse(dock);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void CloseDockable(IDockable dockable)
        {
            if (dockable.OnClose())
            {
                RemoveDockable(dockable, true);
            }
        }

        /// <inheritdoc/>
        public virtual void MoveDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable)
        {
            if (dock.VisibleDockables is null)
            {
                return;
            }

            var sourceIndex = dock.VisibleDockables.IndexOf(sourceDockable);
            var targetIndex = dock.VisibleDockables.IndexOf(targetDockable);

            if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
            {
                dock.VisibleDockables.RemoveAt(sourceIndex);
                dock.VisibleDockables.Insert(targetIndex, sourceDockable);
                dock.ActiveDockable = sourceDockable;
            }
        }

        /// <inheritdoc/>
        public virtual void MoveDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable? targetDockable)
        {
            if (targetDock.VisibleDockables is null)
            {
                targetDock.VisibleDockables = CreateList<IDockable>();
                if (targetDock.VisibleDockables is null)
                {
                    return;
                }
            }

            var isSameOwner = sourceDock == targetDock;

            var targetIndex = 0;

            if (sourceDock.VisibleDockables is not null && targetDock.VisibleDockables is not null && targetDock.VisibleDockables.Count > 0)
            {
                if (isSameOwner)
                {
                    var sourceIndex = sourceDock.VisibleDockables.IndexOf(sourceDockable);

                    if (targetDockable is not null)
                    {
                        targetIndex = targetDock.VisibleDockables.IndexOf(targetDockable);
                    }
                    else
                    {
                        targetIndex = targetDock.VisibleDockables.Count - 1;
                    }

                    if (sourceIndex == targetIndex)
                    {
                        return;
                    }
                }
                else
                {
                    if (targetDockable is not null)
                    {
                        targetIndex = targetDock.VisibleDockables.IndexOf(targetDockable);
                        if (targetIndex >= 0)
                        {
                            targetIndex += 1;
                        }
                        else
                        {
                            targetIndex = targetDock.VisibleDockables.Count - 1;
                        }
                    }
                    else
                    {
                        targetIndex = targetDock.VisibleDockables.Count - 1;
                    }
                }
            }

            if (sourceDock.VisibleDockables is not null && targetDock.VisibleDockables is not null)
            {
                if (isSameOwner)
                {
                    var sourceIndex = sourceDock.VisibleDockables.IndexOf(sourceDockable);
                    if (sourceIndex < targetIndex)
                    {
                        targetDock.VisibleDockables.Insert(targetIndex + 1, sourceDockable);
                        targetDock.VisibleDockables.RemoveAt(sourceIndex);
                    }
                    else
                    {
                        var removeIndex = sourceIndex + 1;
                        if (targetDock.VisibleDockables.Count + 1 > removeIndex)
                        {
                            targetDock.VisibleDockables.Insert(targetIndex, sourceDockable);
                            targetDock.VisibleDockables.RemoveAt(removeIndex);
                        }
                    }
                }
                else
                {
                    RemoveDockable(sourceDockable, true);
                    targetDock.VisibleDockables.Insert(targetIndex, sourceDockable);
                    UpdateDockable(sourceDockable, targetDock);
                    targetDock.ActiveDockable = sourceDockable;
                }
            }
        }

        /// <inheritdoc/>
        public virtual void SwapDockable(IDock dock, IDockable sourceDockable, IDockable targetDockable)
        {
            if (dock.VisibleDockables is null)
            {
                return;
            }

            var sourceIndex = dock.VisibleDockables.IndexOf(sourceDockable);
            var targetIndex = dock.VisibleDockables.IndexOf(targetDockable);

            if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
            {
                var originalSourceDockable = dock.VisibleDockables[sourceIndex];
                var originalTargetDockable = dock.VisibleDockables[targetIndex];

                dock.VisibleDockables[targetIndex] = originalSourceDockable;
                dock.VisibleDockables[sourceIndex] = originalTargetDockable;
                dock.ActiveDockable = originalTargetDockable;
            }
        }

        /// <inheritdoc/>
        public virtual void SwapDockable(IDock sourceDock, IDock targetDock, IDockable sourceDockable, IDockable targetDockable)
        {
            if (sourceDock.VisibleDockables is null || targetDock.VisibleDockables is null)
            {
                return;
            }

            var sourceIndex = sourceDock.VisibleDockables.IndexOf(sourceDockable);
            var targetIndex = targetDock.VisibleDockables.IndexOf(targetDockable);

            if (sourceIndex >= 0 && targetIndex >= 0)
            {
                var originalSourceDockable = sourceDock.VisibleDockables[sourceIndex];
                var originalTargetDockable = targetDock.VisibleDockables[targetIndex];
                sourceDock.VisibleDockables[sourceIndex] = originalTargetDockable;
                targetDock.VisibleDockables[targetIndex] = originalSourceDockable;

                UpdateDockable(originalSourceDockable, targetDock);
                UpdateDockable(originalTargetDockable, sourceDock);

                sourceDock.ActiveDockable = originalTargetDockable;
                targetDock.ActiveDockable = originalSourceDockable;
            }
        }

        /// <inheritdoc/>
        public virtual IDock CreateSplitLayout(IDock dock, IDockable dockable, DockOperation operation)
        {
            IDock? split;

            if (dockable is IDock dockableDock)
            {
                split = dockableDock;
            }
            else
            {
                split = CreateProportionalDock();
                split.Id = nameof(IProportionalDock);
                split.Title = nameof(IProportionalDock);
                split.VisibleDockables = CreateList<IDockable>();
                if (split.VisibleDockables is not  null)
                {
                    split.VisibleDockables.Add(dockable);
                    split.ActiveDockable = dockable;
                }
            }

            var containerProportion = dock.Proportion;
            dock.Proportion = double.NaN;

            var layout = CreateProportionalDock();
            layout.Id = nameof(IProportionalDock);
            layout.Title = nameof(IProportionalDock);
            layout.VisibleDockables = CreateList<IDockable>();
            layout.Proportion = containerProportion;

            var splitter = CreateSplitterDock();
            splitter.Id = nameof(ISplitterDock);
            splitter.Title = nameof(ISplitterDock);

            switch (operation)
            {
                case DockOperation.Left:
                case DockOperation.Right:
                    layout.Orientation = Orientation.Horizontal;
                    break;
                case DockOperation.Top:
                case DockOperation.Bottom:
                    layout.Orientation = Orientation.Vertical;
                    break;
            }

            switch (operation)
            {
                case DockOperation.Left:
                case DockOperation.Top:
                    if (layout.VisibleDockables is not null)
                    {
                        layout.VisibleDockables.Add(split);
                        layout.ActiveDockable = split;
                    }
                    break;
                case DockOperation.Right:
                case DockOperation.Bottom:
                    if (layout.VisibleDockables is not null)
                    {
                        layout.VisibleDockables.Add(dock);
                        layout.ActiveDockable = dock;
                    }
                    break;
            }

            layout.VisibleDockables?.Add(splitter);

            switch (operation)
            {
                case DockOperation.Left:
                case DockOperation.Top:
                    if (layout.VisibleDockables is not null)
                    {
                        layout.VisibleDockables.Add(dock);
                        layout.ActiveDockable = dock;
                    }
                    break;
                case DockOperation.Right:
                case DockOperation.Bottom:
                    if (layout.VisibleDockables is not null)
                    {
                        layout.VisibleDockables.Add(split);
                        layout.ActiveDockable = split;
                    }
                    break;
            }

            return layout;
        }

        /// <inheritdoc/>
        public virtual void SplitToDock(IDock dock, IDockable dockable, DockOperation operation)
        {
            switch (operation)
            {
                case DockOperation.Left:
                case DockOperation.Right:
                case DockOperation.Top:
                case DockOperation.Bottom:
                    {
                        if (dock.Owner is IDock ownerDock && ownerDock.VisibleDockables is { })
                        {
                            var index = ownerDock.VisibleDockables.IndexOf(dock);
                            if (index >= 0)
                            {
                                var layout = CreateSplitLayout(dock, dockable, operation);
                                ownerDock.VisibleDockables.RemoveAt(index);
                                ownerDock.VisibleDockables.Insert(index, layout);
                                UpdateDockable(layout, ownerDock);
                                ownerDock.ActiveDockable = layout;
                            }
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Not supported split operation: {operation}.");
            }
        }

        /// <inheritdoc/>
        public virtual IDockWindow? CreateWindowFrom(IDockable dockable)
        {
            IDockable? target;
            bool topmost;

            switch (dockable)
            {
                case ITool:
                    {
                        target = CreateToolDock();
                        target.Id = nameof(IToolDock);
                        target.Title = nameof(IToolDock);
                        if (target is IDock dock)
                        {
                            dock.VisibleDockables = CreateList<IDockable>();
                            if (dock.VisibleDockables is not null)
                            {
                                dock.VisibleDockables.Add(dockable);
                                dock.ActiveDockable = dockable;
                            }
                        }
                        topmost = true;
                    }
                    break;
                case IDocument:
                    {
                        target = CreateDocumentDock();
                        target.Id = nameof(IDocumentDock);
                        target.Title = nameof(IDocumentDock);
                        if (target is IDock dock)
                        {
                            dock.VisibleDockables = CreateList<IDockable>();
                            if (dock.VisibleDockables is not null)
                            {
                                dock.VisibleDockables.Add(dockable);
                                dock.ActiveDockable = dockable;
                            }
                        }
                        topmost = false;
                    }
                    break;
                case IToolDock:
                    {
                        target = dockable;
                        topmost = true;
                    }
                    break;
                case IDocumentDock:
                    {
                        target = dockable;
                        topmost = false;
                    }
                    break;
                case IProportionalDock proportionalDock:
                    {
                        target = proportionalDock;
                        topmost = false;
                    }
                    break;
                case IRootDock rootDock:
                    {
                        target = rootDock.ActiveDockable;
                        topmost = false;
                    }
                    break;
                default:
                    {
                        return null;
                    }
            }

            var root = CreateRootDock();
            root.Id = nameof(IRootDock);
            root.Title = nameof(IRootDock);
            root.VisibleDockables = CreateList<IDockable>();
            if (root.VisibleDockables is not null && target is not null)
            {
                root.VisibleDockables.Add(target);
                root.ActiveDockable = target;
                root.DefaultDockable = target;
            }
            root.Owner = null;

            var window = CreateDockWindow();
            window.Id = nameof(IDockWindow);
            window.Title = "";
            window.Width = double.NaN;
            window.Height = double.NaN;
            window.Topmost = topmost;
            window.Layout = root;

            root.Window = window;

            return window;
        }

        /// <inheritdoc/>
        public virtual void SplitToWindow(IDock dock, IDockable dockable, double x, double y, double width, double height)
        {
            var rootDock = FindRoot(dock, _ => true);
            if (rootDock is null)
            {
                return;
            }
            RemoveDockable(dockable, true);
            var window = CreateWindowFrom(dockable);
            if (window is not null)
            {
                AddWindow(rootDock, window);
                window.X = x;
                window.Y = y;
                window.Width = width;
                window.Height = height;
                window.Present(false);
            }
        }
    }
}
