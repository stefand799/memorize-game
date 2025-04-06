using Avalonia.Controls;
using MemorizeGame.ViewModels;

namespace MemorizeGame.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}