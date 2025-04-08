using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
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

        // Profile images in Assets/images/profile
        [ObservableProperty]
        private ObservableCollection<Models.ProfileImageItem> _profileImages = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelectedImage))]
        private Models.ProfileImageItem? _selectedProfileImage;

        // Derived properties
        public bool IsUserSelected => SelectedUser != null;
        public bool HasSelectedImage => SelectedProfileImage != null;

        // Default constructor for design-time
        public LoginViewModel()
        {
            // Design-time constructor
            _mainWindowViewModel = new MainWindowViewModel();
            _dataService = new DataService();
            
            // Load profile images for design time
            LoadProfileImagesAsync().ConfigureAwait(false);
        }

        // Runtime constructor
        public LoginViewModel(MainWindowViewModel mainWindowViewModel, DataService dataService)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dataService = dataService;

            // Load users and profile images when view model is created
            _ = LoadUsersAsync();
            _ = LoadProfileImagesAsync();
        }

        // Load profile images from Assets/images/profile directory
        private async Task LoadProfileImagesAsync()
        {
            try
            {
                // Path to profile images directory
                string profileImagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "profile");
                
                // Check if directory exists
                if (!Directory.Exists(profileImagesPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Profile images directory not found: {profileImagesPath}");
                    return;
                }
                
                // Get all image files
                var imageFiles = Directory.GetFiles(profileImagesPath, "*.png")
                                         .Concat(Directory.GetFiles(profileImagesPath, "*.jpg"))
                                         .Concat(Directory.GetFiles(profileImagesPath, "*.jpeg"))
                                         .ToArray();
                
                System.Diagnostics.Debug.WriteLine($"Found {imageFiles.Length} profile images");
                
                // Create profile image items
                var profileImagesList = new ObservableCollection<Models.ProfileImageItem>();
                
                foreach (var imagePath in imageFiles)
                {
                    try
                    {
                        var filename = Path.GetFileName(imagePath);
                        var item = new Models.ProfileImageItem
                        {
                            ImagePath = imagePath,
                            DisplayName = filename
                        };
                        
                        // Load the bitmap
                        item.LoadImage();
                        
                        profileImagesList.Add(item);
                        System.Diagnostics.Debug.WriteLine($"Added profile image: {imagePath}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading profile image {imagePath}: {ex.Message}");
                    }
                }
                
                // Update the collection on UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProfileImages = profileImagesList;
                    
                    // Select first image by default if available
                    if (ProfileImages.Count > 0)
                        SelectedProfileImage = ProfileImages[0];
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profile images: {ex.Message}");
            }
        }

        // Add this method to LoginViewModel.cs to clear the form after creating a user

        private void ClearForm()
        {
            NewUsername = string.Empty;
    
            // Don't clear SelectedProfileImage to keep the current selection
            // This provides a better UX as users often create multiple accounts with the same avatar
        }

// Then update the CreateAccount method to use this:

        [RelayCommand]
        private async Task CreateAccount()
        {
            if (string.IsNullOrWhiteSpace(NewUsername))
            {
                await ShowErrorAsync("Please enter a username.");
                return;
            }

            if (SelectedProfileImage == null)
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
            var newUser = new User(NewUsername, SelectedProfileImage.ImagePath);
            await _dataService.SaveUserAsync(newUser);

            // Refresh user list
            await LoadUsersAsync();

            // Clear form
            ClearForm();
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

        // Command to select a profile image
        [RelayCommand]
        private void SelectProfileImage(Models.ProfileImageItem imageItem)
        {
            SelectedProfileImage = imageItem;
            SelectedImagePath = imageItem.ImagePath;
        }

        // Load users from data service
        private async Task LoadUsersAsync()
        {
            var userList = await _dataService.GetAllUsersAsync();
            
            // Reload images for all users
            foreach (var user in userList)
            {
                user.ReloadImage();
            }
            
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

    // ProfileImageItem is now moved to Models/ProfileImageItem.cs
}