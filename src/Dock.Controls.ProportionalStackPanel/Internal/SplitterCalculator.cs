// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using Avalonia.Controls;

namespace Dock.Controls.ProportionalStackPanel;

/// <summary>
/// Internal utility class for calculating splitter-related metrics.
/// </summary>
internal static class SplitterCalculator
{
    /// <summary>
    /// Calculates the total thickness of all visible splitters in the children collection.
    /// </summary>
    /// <param name="children">The collection of child controls.</param>
    /// <param name="getIsCollapsed">Function to determine if a control is collapsed.</param>
    /// <returns>The total thickness of visible splitters.</returns>
    public static double GetTotalSplitterThickness(Avalonia.Controls.Controls children, System.Func<Control, bool> getIsCollapsed)
    {
        var totalThickness = 0.0;
        var previousWasCollapsed = false;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var isSplitter = ProportionalStackPanelSplitter.IsSplitter(child, out var splitter);

            if (isSplitter && splitter is not null)
            {
                // Skip splitters adjacent to collapsed elements
                if (ShouldSkipSplitter(i, children, previousWasCollapsed, getIsCollapsed))
                {
                    continue;
                }

                totalThickness += splitter.Thickness;
            }
            else
            {
                previousWasCollapsed = getIsCollapsed(child);
            }
        }

        return double.IsNaN(totalThickness) ? 0 : totalThickness;
    }

    /// <summary>
    /// Determines whether a splitter should be skipped based on adjacent collapsed elements.
    /// </summary>
    /// <param name="splitterIndex">The index of the splitter in the children collection.</param>
    /// <param name="children">The collection of child controls.</param>
    /// <param name="previousWasCollapsed">Whether the previous element was collapsed.</param>
    /// <param name="getIsCollapsed">Function to determine if a control is collapsed.</param>
    /// <returns>True if the splitter should be skipped, false otherwise.</returns>
    public static bool ShouldSkipSplitter(int splitterIndex, Avalonia.Controls.Controls children, bool previousWasCollapsed, System.Func<Control, bool> getIsCollapsed)
    {
        // Skip if previous element was collapsed
        if (previousWasCollapsed)
        {
            return true;
        }

        // Skip if next element is collapsed
        if (splitterIndex + 1 < children.Count)
        {
            var nextChild = children[splitterIndex + 1];
            if (getIsCollapsed(nextChild))
            {
                return true;
            }
        }

        return false;
    }
}
