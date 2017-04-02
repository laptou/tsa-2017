using IvyLock.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IvyLock.UI.View
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
