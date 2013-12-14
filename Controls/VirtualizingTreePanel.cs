using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace System.Windows.Controls
{
    class VirtualizingTreePanel : VirtualizingPanel, IScrollInfo
    {
        private SizesCache cachedSizes;
        private Size extent = new Size(0, 0);
        private Size viewport = new Size(0, 0);

        public VirtualizingTreePanel()
        {
            cachedSizes = new SizesCache();
            CanHorizontallyScroll = true;
            CanVerticallyScroll = true;
        }

        /// <summary>
        /// Measure the children
        /// </summary>
        /// <param name="availableSize">Size available</param>
        /// <returns>Size desired</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (ScrollOwner != null)
            {
                if (ScrollOwner.ScrollableWidth < HorizontalOffset) SetHorizontalOffset(ScrollOwner.ScrollableWidth);
                if (ScrollOwner.ScrollableHeight < VerticalOffset) SetVerticalOffset(ScrollOwner.ScrollableHeight);
            }

            // We need to access InternalChildren before the generator to work around a bug
            UIElementCollection children = InternalChildren;
            IItemContainerGenerator generator = ItemContainerGenerator;
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            TreeViewExItem treeViewItem = itemsControl as TreeViewExItem;
            TreeViewEx treeView = itemsControl as TreeViewEx ?? treeViewItem.ParentTreeView;

            double currentY = 0;
            double maxWidth = 0;
            int firstVisibleItemIndex = 0;
            int lastVisibleItemIndex = 0;

            if (itemsControl.HasItems)
            {
                if (treeView.IsVirtualizing)
                {
                    double bottomY = VerticalOffset + availableSize.Height;

                    //add sizes of not visible items before visible ones to currentX
                    for (int i = 0; i < itemsControl.Items.Count; i++)
                    {
                        // get height, maybe it is estimated
                        double height = GetContainerHeightForItem(itemsControl, i);

                        if (currentY + height >= VerticalOffset && currentY <= bottomY)
                        {
                            firstVisibleItemIndex = i;
                            lastVisibleItemIndex = i;

                            // we found the first visible item, lets realize it and all other visible items. while we 
                            // do so, we take care of counting i up
                            GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);
                            int itemGeneratorIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

                            using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                            {
                                // create items until current y is bigger than bottomy => we reached the last visible item
                                while (currentY <= bottomY && i < itemsControl.Items.Count)
                                {
                                    // Get or create the child
                                    bool newlyRealized;
                                    TreeViewExItem child = generator.GenerateNext(out newlyRealized) as TreeViewExItem;
                                    if (newlyRealized)
                                    {
                                        // Figure out if we need to insert the child at the end or somewhere in the middle
                                        AddOrInsertItemToInternalChildren(itemGeneratorIndex, child);
                                        child.ParentTreeView = treeView;
                                        generator.PrepareItemContainer(child);
                                    }
                                    else
                                    {
                                        // The child has already been created, let's be sure it's in the right spot
                                        if (child != children[itemGeneratorIndex]) throw new InvalidOperationException("Wrong child was generated");
                                    }

                                    child.Measure(new Size(double.MaxValue, double.MaxValue));

                                    // now get the real height
                                    height = child.DesiredSize.Height;
                                    // add real height to cache
                                    cachedSizes.AddOrChange(i, height);
                                    // add real height to current position
                                    currentY += height;
                                    // save the maximum needed width
                                    maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);

                                    lastVisibleItemIndex++;
                                    i++;
                                    itemGeneratorIndex++;

                                    //break realization if we reach the bottom of control
                                    if (currentY > bottomY) break;
                                }
                            }
                        }
                        else
                        {
                            currentY += height;
                        }
                    }

                    // Note: this could be deferred to idle time for efficiency
                    CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);
                }
                else
                {
                    GeneratorPosition startPos = generator.GeneratorPositionFromIndex(0);
                    using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                    {
                        for (int i = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1; i < itemsControl.Items.Count; i++)
                        {
                            // Get or create the child
                            bool newlyRealized;
                            TreeViewExItem child = generator.GenerateNext(out newlyRealized) as TreeViewExItem;
                            if (newlyRealized)
                            {
                                // Figure out if we need to insert the child at the end or somewhere in the middle
                                AddOrInsertItemToInternalChildren(i, child);
                                child.ParentTreeView = treeView ?? treeViewItem.ParentTreeView;
                                generator.PrepareItemContainer(child);
                            }

                            child.Measure(new Size(double.MaxValue, double.MaxValue));
                            // now get the real height
                            double height = child.DesiredSize.Height;
                            // add real height to current position
                            currentY += height;
                            // save the maximum needed width
                            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                        }
                    }
                }
            }

            Extent = new Size(maxWidth, currentY);
            Viewport = availableSize;

            return Extent;
        }

        /// <summary>
        /// Arrange the children
        /// </summary>
        /// <param name="finalSize">Size available</param>
        /// <returns>Size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            TreeViewExItem treeViewItem = itemsControl as TreeViewExItem;
            TreeViewEx treeView = itemsControl as TreeViewEx ?? treeViewItem.ParentTreeView;
            IItemContainerGenerator generator = this.ItemContainerGenerator;

            //Extent = finalSize;
            bool foundVisibleItem = false; ;
            double currentY = 0;
            if (treeView.IsVirtualizing)
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    FrameworkElement child = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;

                    if (foundVisibleItem)
                    {
                        if (child == null)
                        {
                            // other items are not visible / virtualized
                            break;
                        }
                    }
                    else
                    {
                        if (child != null)
                        {
                            // found first visible item
                            foundVisibleItem = true;
                        }
                    }

                    if (child != null)
                    {
                        child.Arrange(new Rect(-HorizontalOffset, currentY - VerticalOffset, finalSize.Width, child.DesiredSize.Height));
                        currentY += child.ActualHeight;
                    }
                    else
                    {
                        currentY += GetContainerHeightForItem(itemsControl, i);
                    }
                }

                // update average after arrange, because we have to use the same average as in measure for our calculation of currentY.
                cachedSizes.Update();
            }
            else
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    UIElement child = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;

                    if (child != null) child.Arrange(new Rect(-HorizontalOffset, currentY - VerticalOffset, finalSize.Width, child.DesiredSize.Height));
                    currentY += child.DesiredSize.Height;
                }
            }

            return finalSize;
        }

        private void AddOrInsertItemToInternalChildren(int itemGeneratorIndex, TreeViewExItem child)
        {
            if (itemGeneratorIndex >= InternalChildren.Count)
            {
                base.AddInternalChild(child);
            }
            else
            {
                base.InsertInternalChild(itemGeneratorIndex, child);
            }
        }

        /// <summary>
        /// Revirtualize items that are no longer visible
        /// </summary>
        /// <param name="minDesiredGenerated">first item index that should be visible</param>
        /// <param name="maxDesiredGenerated">last item index that should be visible</param>
        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            UIElementCollection children = this.InternalChildren;
            IItemContainerGenerator generator = this.ItemContainerGenerator;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                GeneratorPosition childGeneratorPos = new GeneratorPosition(i, 0);
                int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
                if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
                {
                    generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }

            cachedSizes.CleanUp(maxDesiredGenerated);
        }

        /// <summary>
        /// When items are removed, remove the corresponding UI if necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
            }
        }

        #region Layout specific code

        /// <summary>
        /// Returns the size of the container for a given item.  The size can come from the container, a lookup, or a guess depending
        /// on the virtualization state of the item.
        /// </summary>
        /// <param name="itemsControl">
        /// <param name="item">
        /// <param name="index">
        /// <param name="container">returns the container for the item; null if the container wasn't found
        /// <returns></returns>
        private double GetContainerHeightForItem(ItemsControl itemsControl, int index)
        {
            double height;

            if (cachedSizes.ContainsSize(index))
            {
                height = cachedSizes[index];
            }
            else
            {
                height = cachedSizes.GetMax();
            }

            return height;
        }

        #endregion

        public Size Extent
        {
            get
            {
                return extent;
            }
            set
            {
                if (extent == value) return;
                extent = value;

                if (ScrollOwner == null) return;
                ScrollOwner.InvalidateScrollInfo();
            }
        }

        public Size Viewport
        {
            get
            {
                return viewport;
            }
            set
            {
                if (viewport == value) return;
                viewport = value;

                if (ScrollOwner == null) return;
                ScrollOwner.InvalidateScrollInfo();
            }
        }

        private double GetScrollLineHeightY()
        {
            return 15;
        }

        private double GetScrollLineHeightX()
        {
            return 15;
        }

        #region IScrollInfo implementation

        public ScrollViewer ScrollOwner { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public double HorizontalOffset { get; private set; }

        public double VerticalOffset { get; private set; }

        public double ExtentHeight
        {
            get { return Extent.Height; }
        }

        public double ExtentWidth
        {
            get { return Extent.Width; }
        }

        public double ViewportHeight
        {
            get { return Viewport.Height; }
        }

        public double ViewportWidth
        {
            get { return Viewport.Width; }
        }

        public void LineUp()
        {
            SetVerticalOffset(this.VerticalOffset - GetScrollLineHeightY());
        }

        public void LineDown()
        {
            SetVerticalOffset(this.VerticalOffset + GetScrollLineHeightY());
        }

        public void PageUp()
        {
            SetVerticalOffset(this.VerticalOffset - viewport.Height + 10);
        }

        public void PageDown()
        {
            SetVerticalOffset(this.VerticalOffset + viewport.Height - 10);
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(this.VerticalOffset - GetScrollLineHeightY());
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(this.VerticalOffset + GetScrollLineHeightY());
        }

        public void LineLeft()
        {
            SetHorizontalOffset(this.HorizontalOffset - GetScrollLineHeightX());
        }

        public void LineRight()
        {
            SetHorizontalOffset(this.HorizontalOffset + GetScrollLineHeightX());
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (rectangle.IsEmpty || visual == null || visual == this || !base.IsAncestorOf(visual))
            {
                return Rect.Empty;
            }

            TreeViewExItem treeViewExItem = visual as TreeViewExItem;
            FrameworkElement element;
            if (treeViewExItem != null)
            {
                element = treeViewExItem.Template.FindName("border", treeViewExItem) as FrameworkElement;
            }
            else
            {
                element = visual as FrameworkElement;
            }

            var transform = visual.TransformToAncestor(this);
            Point p = transform.Transform(new Point(0, 0));
            Rect rect = new Rect(p, element.RenderSize);

            if (rect.X < 0)
            {
                SetHorizontalOffset(HorizontalOffset + rect.X);
            }
            else if (treeViewExItem != null && treeViewExItem.ParentTreeView.ActualWidth < rect.X)
            {
                SetHorizontalOffset(HorizontalOffset + rect.X);
            }

            if (rect.Y < 0)
            {
                SetVerticalOffset(VerticalOffset + rect.Y);
            }
            else if (treeViewExItem != null && treeViewExItem.ParentTreeView.ActualHeight < rect.Y + rect.Height)
            {
                // set 5 more, so the next item is realized for sure.
                double verticalOffset = rect.Y + rect.Height + VerticalOffset - treeViewExItem.ParentTreeView.ActualHeight + 5;
                SetVerticalOffset(verticalOffset);
            }

            return new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(this.HorizontalOffset - GetScrollLineHeightX());
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(this.HorizontalOffset + GetScrollLineHeightX());
        }

        public void PageLeft()
        {
            SetHorizontalOffset(this.HorizontalOffset - viewport.Width + 10);
        }

        public void PageRight()
        {
            SetHorizontalOffset(this.HorizontalOffset + viewport.Width - 10);
        }

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || viewport.Width >= extent.Width)
            {
                offset = 0;
            }
            else
            {
                if (offset + viewport.Width >= extent.Width)
                {
                    offset = extent.Width - viewport.Width;
                }
            }

            HorizontalOffset = offset;

            if (ScrollOwner != null)
                ScrollOwner.InvalidateScrollInfo();

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || viewport.Height >= extent.Height)
            {
                offset = 0;
            }
            else
            {
                if (offset + viewport.Height >= extent.Height)
                {
                    offset = extent.Height - viewport.Height;
                }
            }

            VerticalOffset = offset;

            if (ScrollOwner != null)
                ScrollOwner.InvalidateScrollInfo();

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        #endregion
    }
}
