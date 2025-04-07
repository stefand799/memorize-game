using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MemorizeGame.Styles
{
    public class DarkTheme : Avalonia.Styling.Styles
    {
        public DarkTheme()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}