using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

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

    [ValueConversion(typeof(Variable.CountType), typeof(Colors))]
    public class VariableCountTypeColorConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Variable.CountType))
                return new SolidColorBrush(Colors.LightCyan);
            return new SolidColorBrush((Variable.CountType)value == Variable.CountType.CT_SINGLE ? Colors.LightCyan : Colors.LightBlue);
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

    [ValueConversion(typeof(Variable.ReferencedType), typeof(Colors))]
    public class VariableReferencedTypeColorConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Variable.ReferencedType))
                return new SolidColorBrush(Colors.LightCyan);
            switch ((Variable.ReferencedType)value)
            {
                case Variable.ReferencedType.None:
                    return new SolidColorBrush(Color.FromRgb(225, 225, 225));
                case Variable.ReferencedType.Disactive:
                    return new SolidColorBrush(Colors.DimGray);
                case Variable.ReferencedType.Active:
                    return new SolidColorBrush(Colors.LightCoral);
                default:
                    return new SolidColorBrush(Colors.LightCyan);
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
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
                return false;
            Variable.VariableType type = (Variable.VariableType)value;
            return type == Variable.VariableType.VBT_Pointer;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return booleanValue ? Variable.VariableType.VBT_Pointer : Variable.VariableType.VBT_Const;
        }
    }
    [ValueConversion(typeof(Variable.EnableType), typeof(bool))]
    public class VariableEnableTypeConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Variable.EnableType))
                return false;
            Variable.EnableType type = (Variable.EnableType)value;
            return type == Variable.EnableType.ET_Disable;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return booleanValue ? Variable.EnableType.ET_Disable : Variable.EnableType.ET_Enable;
        }
    }
    public class BoolOpacityConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return b ? 0.5f : 1.0f;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
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

    [ValueConversion(typeof(string), typeof(bool))]
    public class StringBoolConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            return s == "T";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return b ? "T" : "F";
            //string s = (string)value;
            //return ValueTypeDic.GetKey(s, Variable.ValueType.VT_NONE);
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class ReturnTypeConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrEmpty(s))
                return "D";
            return s.ToUpper()[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    [ValueConversion(typeof(string), typeof(bool))]
    public class ReturnTypeVisibilityConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (string.IsNullOrEmpty(s))
                return false;
            if (s == "Default" || s == "Normal")
                return false;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }


    [ValueConversion(typeof(System.Windows.Vector), typeof(System.Windows.Vector))]
    public class FSMUIConnectionOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Point startPos = (System.Windows.Point)values[0];
            System.Windows.Point endPos = (System.Windows.Point)values[1];
            var dir = (endPos - startPos);
            dir.Normalize();
            var offset = new System.Windows.Vector(dir.Y, -dir.X) * 4;

            if ((string)parameter == "0")
                return startPos + offset;
            else
                return startPos - offset;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(bool), typeof(System.Windows.FrameworkElement))]
    public class FSMStateBackGroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isdefault = (bool)values[0];
            System.Windows.FrameworkElement o = values[1] as System.Windows.FrameworkElement;
            if ((bool)isdefault)
                return o.FindResource("defaultBackground");
            return o.FindResource("normalBackground");
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(object), typeof(System.Windows.FrameworkElement))]
    public class FSMStateBorderColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            FSMStateNode node = (values[0] as FSMStateRenderer).FSMStateOwner;
            System.Windows.FrameworkElement o = values[1] as System.Windows.FrameworkElement;
            if (node is FSMNormalStateNode)
                return o.FindResource("normalColor");
            if (node is FSMMetaStateNode)
                return o.FindResource("metaColor");
            if (node is FSMEntryStateNode)
                return o.FindResource("entryColor");
            if (node is FSMExitStateNode)
                return o.FindResource("exitColor");
            if (node is FSMAnyStateNode)
                return o.FindResource("anyColor");
            if (node is FSMUpperStateNode)
                return o.FindResource("upperColor");

            return o.FindResource("normalColor");
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class FSMStateBorderCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FSMStateNode node = (value as FSMStateRenderer).FSMStateOwner;
            if (node is FSMNormalStateNode)
                return 5;
            if (node is FSMMetaStateNode)
                return 20;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    [ValueConversion(typeof(int), typeof(bool))]
    public class IsTwoOrMoreConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value > 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
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
