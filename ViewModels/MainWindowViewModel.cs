using System;
using CommunityToolkit.Mvvm.ComponentModel;
using MemorizeGame.Models;

namespace MemorizeGame.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // This property will hold the current view/component to display
        [ObservableProperty]
        private ViewModelBase _currentView;
        
        // DataService for access to models
        private readonly DataService _dataService;
        
        // Currently logged in user
        [ObservableProperty]
        private User? _currentUser;
        
        public MainWindowViewModel()
        {
            _dataService = new DataService();
            
            // Set the initial view to be the login view
            _currentView = new LoginViewModel(this, _dataService);
        }
        
        // Method to navigate between different views
        public void NavigateTo(ViewModelBase viewModel)
        {
            CurrentView = viewModel;
        }
        
        // Navigate to the game view 
        public void NavigateToGame()
        {
            if (CurrentUser != null)
            {
                CurrentView = new GameViewModel(this, _dataService, CurrentUser);
            }
        }
        
        // Navigate to the login view
        public void NavigateToLogin()
        {
            CurrentUser = null;
            CurrentView = new LoginViewModel(this, _dataService);
        }
    }
}