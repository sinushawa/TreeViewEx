using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Windows.Controls
{
    internal static class TreeViewElementFinder
    {
        internal static TreeViewExItem FindNext(TreeViewExItem treeViewItem, bool ignoreInvisible)
        {
            // find first child
            if (treeViewItem.IsExpanded)
            {
                TreeViewExItem item = treeViewItem.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewExItem;
                if (item != null)
                {
                    if (item.IsEnabled)
                    {
                        if (ignoreInvisible && item.Visibility != Windows.Visibility.Visible)
                        {
                            return FindNext(item, ignoreInvisible);
                        }

                        return item;
                    }
                    else
                    {
                        return FindNext(item, ignoreInvisible);
                    }
                }
            }

            // find next sibling
            TreeViewExItem sibling = FindNextSiblingRecursive(treeViewItem) as TreeViewExItem;
            if (sibling != null && !(ignoreInvisible && sibling.Visibility != Windows.Visibility.Visible))
            {
                return sibling;
            }

            return null;
        }

        internal static ItemsControl FindNextSibling(ItemsControl itemsControl)
        {
            ItemsControl parentIc = ItemsControl.ItemsControlFromItemContainer(itemsControl);
            if (parentIc == null) return null;
            int index = parentIc.ItemContainerGenerator.IndexFromContainer(itemsControl);
            return parentIc.ItemContainerGenerator.ContainerFromIndex(index + 1) as ItemsControl; // returns null if index to large or nothing found
        }

        internal static ItemsControl FindNextSiblingRecursive(ItemsControl itemsControl)
        {
            ItemsControl parentIc = ItemsControl.ItemsControlFromItemContainer(itemsControl);
            if (parentIc == null) return null;
            int index = parentIc.ItemContainerGenerator.IndexFromContainer(itemsControl);
            if (index < parentIc.Items.Count - 1)
            {
                return parentIc.ItemContainerGenerator.ContainerFromIndex(index + 1) as ItemsControl; // returns null if index to large or nothing found
            }

            return FindNextSiblingRecursive(parentIc);
        }

        /// <summary>
        /// Returns the first item. If tree is virtualized, it is the first realized item.
        /// </summary>
        /// <param name="treeView">The tree.</param>
        /// <returns>Returns a TreeViewExItem.</returns>
        internal static TreeViewExItem FindFirst(TreeViewEx treeView, bool ignoreInvisible)
        {
            for (int i = 0; i < treeView.Items.Count; i++)
            {
                var item = treeView.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewExItem;
                if (item != null && GetIsVisible(item)) return item;
            }

            return null;
        }

        /// <summary>
        /// Returns the last item. If tree is virtualized, it is the last realized item.
        /// </summary>
        /// <param name="treeView">The tree.</param>
        /// <returns>Returns a TreeViewExItem.</returns>
        internal static TreeViewExItem FindLast(TreeViewEx treeView, bool ignoreInvisible)
        {
            for (int i = treeView.Items.Count - 1; i >= 0; i--)
            {
                var item = treeView.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewExItem;
                if (item != null && GetIsVisible(item)) return item;
            }

            return null;
        }

        /// <summary>
        /// Returns all items in tree recursively. If virtualization is enabled, only realized items are returned.
        /// </summary>
        /// <param name="treeView">The tree.</param>
        /// <param name="ignoreInvisible">True if only visible items should be returned.</param>
        /// <returns>Returns an enumerable of items.</returns>
        internal static IEnumerable<TreeViewExItem> FindAll(TreeViewEx treeView, bool ignoreInvisible)
        {
            TreeViewExItem currentItem = FindFirst(treeView, ignoreInvisible);
            while (currentItem != null)
            {
                if (GetIsVisible(currentItem)) yield return currentItem;
                currentItem = FindNext(currentItem, ignoreInvisible);
            }
        }

        private static bool GetIsVisible(TreeViewExItem currentItem)
        {
            return currentItem.Visibility == Visibility.Visible;
        }
    }
}
