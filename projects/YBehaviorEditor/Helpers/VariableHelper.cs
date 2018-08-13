using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using YBehavior.Editor.Core;

namespace YBehavior.Editor
{
    [ValueConversion(typeof(bool), typeof(Colors))]
    public class VariableValidColorConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return new SolidColorBrush(Colors.White);
            return new SolidColorBrush((bool)value ? Colors.LightGreen : Colors.Red);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }

    [ValueConversion(typeof(bool), typeof(Colors))]
    public class VariableRefreshColorConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return new SolidColorBrush(Color.FromRgb(0xAC, 0xAC, 0xAC));
            return new SolidColorBrush((bool)value ? Colors.DarkGreen : Color.FromRgb(0xAC, 0xAC, 0xAC));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }

    [ValueConversion(typeof(Variable.ValueType), typeof(string))]
    public class VariableValueTypeConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Variable.ValueType))
                return string.Empty;
            Variable.ValueType type = (Variable.ValueType)value;
            return Variable.ValueTypeDic2.GetValue(type, string.Empty);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
            //string s = (string)value;
            //return ValueTypeDic.GetKey(s, Variable.ValueType.VT_NONE);
        }
    }

    [ValueConversion(typeof(List<Variable.ValueType>), typeof(bool))]
    public class VariableTypeCountConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is List<Variable.ValueType>))
                return false;
            List<Variable.ValueType> list = (List<Variable.ValueType>)value;
            return list.Count > 1;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
            //string s = (string)value;
            //return ValueTypeDic.GetKey(s, Variable.ValueType.VT_NONE);
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return !booleanValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return !booleanValue;
        }
    }
    [ValueConversion(typeof(Variable.VariableType), typeof(bool))]
    public class VariableVariableTypeConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Variable.VariableType))
                return string.Empty;
            Variable.VariableType type = (Variable.VariableType)value;
            return type == Variable.VariableType.VBT_Pointer;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return booleanValue ? Variable.VariableType.VBT_Pointer : Variable.VariableType.VBT_Const;
        }
    }

    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNullOrEmptyConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            return string.IsNullOrEmpty(s);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
            //string s = (string)value;
            //return ValueTypeDic.GetKey(s, Variable.ValueType.VT_NONE);
        }
    }


    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
