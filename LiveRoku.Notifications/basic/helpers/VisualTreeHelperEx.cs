using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace LiveRoku.Notifications {
    static class VisualTreeHelperEx {
        public static T GetVisualChild<T> (this DependencyObject parent) where T : DependencyObject {
            T child = default (T);
            int numVisuals = VisualTreeHelper.GetChildrenCount (parent);
            for (int i = 0; i < numVisuals; i++) {
                var v = VisualTreeHelper.GetChild (parent, i);
                child = v as T;
                if (child == null) {
                    child = GetVisualChild<T> (v);
                }

                if (child != null) {
                    break;
                }
            }

            return child;
        }

        public static IEnumerable<T> GetVisualChilds<T> (this DependencyObject parent) where T : DependencyObject {
            int numVisuals = VisualTreeHelper.GetChildrenCount (parent);
            for (int i = 0; i < numVisuals; i++) {
                var v = VisualTreeHelper.GetChild (parent, i);
                T child = v as T;
                if (child == null) {
                    foreach (var visualChild in GetVisualChilds<T> (v)) {
                        yield return visualChild;
                    }
                }

                if (child != null) {
                    yield return child;
                }
            }
        }

        public static T FindChild<T> (this DependencyObject parent, string childName) where T : DependencyObject {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount (parent);
            for (int i = 0; i < childrenCount; i++) {
                var child = VisualTreeHelper.GetChild (parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null) {
                    // recursively drill down the tree
                    foundChild = FindChild<T> (child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                } else if (!string.IsNullOrEmpty (childName)) {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName) {
                        // if the child's name is of the request name
                        foundChild = (T) child;
                        break;
                    }
                } else {
                    // child element found.
                    foundChild = (T) child;
                    break;
                }
            }

            return foundChild;
        }
    }
}