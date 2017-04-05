using IvyLock.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IvyLock.View.Control
{
    public class SettingTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate
            SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is Setting)
            {
                Setting setting = item as Setting;

                return element.FindResource(setting.Type.ToString())
                            as DataTemplate;
            }

            return null;
        }
    }
}