using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace IvyLock.Model
{
    public abstract class Model : INotifyPropertyChanged,
        INotifyPropertyChanging
    {
        private Dictionary<string, Object> properties = new Dictionary<string, object>();

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        private PropertyInfo GetPropertyInfo<TProperty>(
            Expression<Func<TProperty>> propertyLambda)
        {
            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            return propInfo;
        }

        public T Get<T>([CallerMemberName] string propertyName = "")
        {
            return properties.TryGetValue(propertyName, out object o) && o is T ? (T)o : default(T);
        }

        public void Set<T>(T value, [CallerMemberName] string propertyName = "")
        {
            properties[propertyName] = value;
        }

        public void Set<T>(T value, ref T variable, [CallerMemberName] string propertyName = "")
        {
            RaisePropertyChanging(propertyName);

            variable = value;

            RaisePropertyChanged(propertyName);
        }

        public void SetProperty<T>(T value, ref T variable, Expression<Func<T>> propertyLambda)
        {
            string propertyName = GetPropertyInfo(propertyLambda).Name;

            RaisePropertyChanging(propertyName);

            variable = value;

            RaisePropertyChanged(propertyName);
        }

        public void RaisePropertyChanging(string propertyName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            });
        }

        public void RaisePropertyChanging<T>(Expression<Func<T>> propertyLambda)
        {
            string propertyName = GetPropertyInfo(propertyLambda).Name;

            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            });
        }

        public void RaisePropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public void RaisePropertyChanged<T>(Expression<Func<T>> propertyLambda)
        {
            string propertyName = GetPropertyInfo(propertyLambda).Name;
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}