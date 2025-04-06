using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemorizeGame.Models;

namespace MemorizeGame.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly DataService _dataService;

        // For user list
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUserSelected))]
        private User? _selectedUser;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        // For creating new user
        [ObservableProperty]
        private string _newUsername = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedImage))]
        private string _selectedImagePath = string.Empty;

        // Derived properties
        public bool IsUserSelected => SelectedUser != null;
        public bool HasSelectedImage => !string.IsNullOrEmpty(SelectedImagePath);

        // Default constructor for design-time
        public LoginViewModel()
        {
            // Design-time constructor
            _mainWindowViewModel = new MainWindowViewModel();
            _dataService = new DataService();
        }

        // Runtime constructor
        public LoginViewModel(MainWindowViewModel mainWindowViewModel, DataService dataService)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dataService = dataService;

            // Load users when view model is created
            _ = LoadUsersAsync();
        }

        // Command to select a profile image
        [RelayCommand]
        private async Task SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Profile Image",
                AllowMultiple = false,
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter
                    {
                        Name = "Image Files",
                        Extensions = new System.Collections.Generic.List<string> { "jpg", "jpeg", "png", "gif" }
                    }
                }
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var result = await dialog.ShowAsync(desktop.MainWindow);
                if (result?.Length > 0)
                {
                    SelectedImagePath = result[0];
                }
            }
        }

        // Command to create a new user account
        [RelayCommand]
        private async Task CreateAccount()
        {
            if (string.IsNullOrWhiteSpace(NewUsername))
            {
                await ShowErrorAsync("Please enter a username.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedImagePath))
            {
                await ShowErrorAsync("Please select a profile image.");
                return;
            }

            // Check if username already exists
            if (Users.Any(u => u.Username.Equals(NewUsername, StringComparison.OrdinalIgnoreCase)))
            {
                await ShowErrorAsync("Username already exists.");
                return;
            }

            // Create and save new user
            var newUser = new User(NewUsername, SelectedImagePath);
            await _dataService.SaveUserAsync(newUser);

            // Refresh user list
            await LoadUsersAsync();

            // Clear form
            NewUsername = string.Empty;
            SelectedImagePath = string.Empty;
        }

        // Command to play as selected user
        [RelayCommand]
        private void Play()
        {
            if (SelectedUser != null)
            {
                _mainWindowViewModel.CurrentUser = SelectedUser;
                _mainWindowViewModel.NavigateToGame();
            }
        }

        // Command to delete selected user
        [RelayCommand]
        private async Task DeleteUser()
        {
            if (SelectedUser == null)
                return;

            var userName = SelectedUser.Username;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var messageBox = new Window
                {
                    Title = "Confirm Deletion",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var panel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Spacing = 10
                };

                panel.Children.Add(new TextBlock
                {
                    Text = $"Are you sure you want to delete the user '{userName}'?\nThis will delete all saved games and statistics.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10
                };

                var cancelButton = new Button { Content = "Cancel" };
                var deleteButton = new Button { Content = "Delete" };

                cancelButton.Click += (s, e) => messageBox.Close();
                deleteButton.Click += async (s, e) =>
                {
                    await _dataService.DeleteUserAsync(userName);
                    await LoadUsersAsync();
                    messageBox.Close();
                };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(deleteButton);
                panel.Children.Add(buttonPanel);
                messageBox.Content = panel;

                await messageBox.ShowDialog(desktop.MainWindow);
            }
        }

        // Command to exit the application
        [RelayCommand]
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        // Load users from data service
        private async Task LoadUsersAsync()
        {
            var userList = await _dataService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(userList);
        }

        // Helper method to show error dialog
        private async Task ShowErrorAsync(string message)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var messageBox = new Window
                {
                    Title = "Error",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var panel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Spacing = 10
                };

                panel.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                };

                okButton.Click += (s, e) => messageBox.Close();
                panel.Children.Add(okButton);
                messageBox.Content = panel;

                await messageBox.ShowDialog(desktop.MainWindow);
            }
        }
    }
}