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
    public class VariableValidColorConvertor: IValueConverter
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

    public static class VariableHelper
    {
        public static Bimap<Variable.ValueType, string> ValueTypeDic = new Bimap<Variable.ValueType, string>
        {
            {Variable.ValueType.VT_INT, "INT" },
            {Variable.ValueType.VT_FLOAT, "FLOAT" },
            {Variable.ValueType.VT_BOOL, "BOOL" },
            {Variable.ValueType.VT_VECTOR3, "VECTOR3" },
            {Variable.ValueType.VT_STRING, "STRING" },
            {Variable.ValueType.VT_ENUM, "ENUM" },
            {Variable.ValueType.VT_AGENT, "AGENT" }
        };
    }

    [ValueConversion(typeof(Variable.ValueType), typeof(string))]
    public class VariableValueTypeConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Variable.ValueType))
                return string.Empty;
            Variable.ValueType type = (Variable.ValueType)value;
            return VariableHelper.ValueTypeDic.GetValue(type, string.Empty);
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

}
