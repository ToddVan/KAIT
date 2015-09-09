//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.


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
