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
                string type = setting.Type.ToString();

                if (setting.ReadOnly)
                    type = "Info";

                return element.FindResource(type) as DataTemplate;
            }

            return null;
        }
    }
}