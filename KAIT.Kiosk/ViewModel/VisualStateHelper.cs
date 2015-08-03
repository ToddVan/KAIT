using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KAIT.Kiosk.ViewModel
{
    public class VisualStateHelper : DependencyObject
    {

        public static readonly DependencyProperty VisualStateProperty =
            DependencyProperty.RegisterAttached("VisualState", typeof(string), typeof(VisualStateHelper), new PropertyMetadata(string.Empty,StateChanged));

        
        public static void SetVisualState(UIElement element, string value)
        {
            element.SetValue(VisualStateProperty, value);
        }
        public static string GetVisualState(UIElement element)
        {
            return (string)element.GetValue(VisualStateProperty);
        }

        internal static void StateChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                //Debug.WriteLine("VisualStatehelper: " + ((FrameworkElement)target).Name + " " + args.NewValue.ToString());
                bool result = VisualStateManager.GoToElementState((FrameworkElement)target, args.NewValue.ToString(), true);
            }
        }

    }
}
