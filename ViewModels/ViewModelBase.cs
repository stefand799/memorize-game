using System;
using System.Globalization;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MemorizeGame.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
    }
    
    // Add the converter directly in the same namespace
    public class EnumEqualsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // If the parameter is a string and value is an enum, try to parse the string
            if (parameter is string paramString && value.GetType().IsEnum)
            {
                if (Enum.TryParse(value.GetType(), paramString, out var enumValue))
                {
                    return value.Equals(enumValue);
                }
            }

            // Otherwise just do direct comparison
            return value.Equals(parameter);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}