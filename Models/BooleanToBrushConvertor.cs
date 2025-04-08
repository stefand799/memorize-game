using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MemorizeGame.Converters
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public IBrush? TrueBrush { get; set; }
        public IBrush? FalseBrush { get; set; }
        
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}