using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MemorizeGame.Models;

namespace MemorizeGame.ViewModels
{
    public partial class GameViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly DataService _dataService;
        private readonly User _currentUser;
        private Timer? _gameTimer;
        private Game? _currentGame;
        private List<int> _flippedCardIds = new();
        private bool _timerRunning = false;

        // Game configuration properties
        [ObservableProperty]
        private GameCategory _currentCategory = GameCategory.Category1;

        [ObservableProperty]
        private GameMode _currentMode = GameMode.Standard;

        [ObservableProperty]
        private int _customRows = 3;

        [ObservableProperty]
        private int _customColumns = 4;

        [ObservableProperty]
        private int _gameTimeSeconds = 120;

        [ObservableProperty]
        private int _gameRows = 4;

        [ObservableProperty]
        private int _gameColumns = 4;

        // Game state properties
        [ObservableProperty]
        private bool _isGameActive = false;

        [ObservableProperty]
        private ObservableCollection<Card> _cards = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TimeRemainingText))]
        private int _timeRemaining = 120; // Initialize with default time

        [ObservableProperty]
        private string _gameStatusText = "Ready to play";

        [ObservableProperty]
        private IBrush _timeRemainingColor = Brushes.Black;

        // Computed properties
        public string WelcomeMessage => $"Welcome, {_currentUser.Username}!";
        public string TimeRemainingText => $"Time remaining: {TimeRemaining} seconds";

        // Default constructor for design-time
        public GameViewModel()
        {
            // Design-time constructor
            _mainWindowViewModel = new MainWindowViewModel();
            _dataService = new DataService();
            _currentUser = new User("Design-time User", "");
        }

        // Runtime constructor
        public GameViewModel(MainWindowViewModel mainWindowViewModel, DataService dataService, User currentUser)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dataService = dataService;
            _currentUser = currentUser;
            
            // Initialize with default time
            TimeRemaining = GameTimeSeconds;
        }

        #region Commands

        [RelayCommand]
        private void SetCategory(string categoryName)
        {
            CurrentCategory = categoryName switch
            {
                "Category1" => GameCategory.Category1,
                "Category2" => GameCategory.Category2,
                "Category3" => GameCategory.Category3,
                _ => GameCategory.Category1
            };
        }

        [RelayCommand]
        private void SetGameMode(string modeName)
        {
            if (modeName == "Standard")
            {
                CurrentMode = GameMode.Standard;
                GameRows = 4;
                GameColumns = 4;
            }
        }

        [RelayCommand]
        private void SetCustomGameMode()
        {
            // Validate dimensions (must be at least 2x2 and product must be even)
            if (CustomRows < 2 || CustomRows > 6 || CustomColumns < 2 || CustomColumns > 6)
            {
                ShowErrorAsync("Rows and columns must be between 2 and 6.");
                return;
            }

            if ((CustomRows * CustomColumns) % 2 != 0)
            {
                ShowErrorAsync("The number of cards (rows × columns) must be even.");
                return;
            }

            CurrentMode = GameMode.Custom;
            GameRows = CustomRows;
            GameColumns = CustomColumns;
        }

        [RelayCommand]
        private void NewGame()
        {
            StartNewGame();
        }

        [RelayCommand]
        private async Task OpenGame()
        {
            var savedGames = await _dataService.GetSavedGamesForUserAsync(_currentUser.Username);
            if (savedGames.Count == 0)
            {
                await ShowErrorAsync("No saved games found.");
                return;
            }

            // Ideally, we'd show a proper dialog to select a saved game
            // For simplicity, we'll just load the most recent saved game
            var mostRecentGame = savedGames.OrderByDescending(g => g.SavedAt).First();
            LoadGame(mostRecentGame);
        }

        [RelayCommand]
        private async Task SaveGame()
        {
            if (!IsGameActive || _currentGame == null)
            {
                await ShowErrorAsync("No active game to save.");
                return;
            }

            // Update elapsed time
            _currentGame.Configuration.ElapsedTime = _currentGame.Configuration.TotalTime - TimeRemaining;
            
            // Update the cards state in the game
            _currentGame.Cards = Cards.ToList();
            
            // Save the game
            await _dataService.SaveGameAsync(_currentGame);
            
            GameStatusText = "Game saved successfully!";
        }

        [RelayCommand]
        private async Task ShowStatistics()
        {
            var users = await _dataService.GetAllUsersAsync();
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var statsWindow = new Window
                {
                    Title = "Game Statistics",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                var grid = new Grid
                {
                    Margin = new Thickness(20)
                };
                
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.Children.Add(new TextBlock
                {
                    Text = "Player Statistics",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                });
                
                // Create stats list
                var listBox = new ListBox
                {
                    [Grid.RowProperty] = 1
                };
                
                foreach (var user in users)
                {
                    listBox.Items?.Add(new TextBlock
                    {
                        Text = $"{user.Username} - Played: {user.GamesPlayed} - Won: {user.GamesWon}",
                        Margin = new Thickness(5)
                    });
                }
                
                grid.Children.Add(listBox);
                
                // Add OK button
                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                    [Grid.RowProperty] = 2
                };
                
                okButton.Click += (s, e) => statsWindow.Close();
                grid.Children.Add(okButton);
                
                statsWindow.Content = grid;
                
                await statsWindow.ShowDialog(desktop.MainWindow);
            }
        }

        [RelayCommand]
        private void Exit()
        {
            // Stop any active game timer
            StopGameTimer();
            
            // Navigate back to login screen
            _mainWindowViewModel.NavigateToLogin();
        }

        [RelayCommand]
        private async Task ShowAbout()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var aboutWindow = new Window
                {
                    Title = "About",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                var panel = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 10
                };
                
                panel.Children.Add(new TextBlock
                {
                    Text = "Memory Game",
                    FontSize = 24,
                    FontWeight = FontWeight.Bold
                });
                
                panel.Children.Add(new TextBlock
                {
                    Text = "Developed by: Your Name Here",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });
                
                panel.Children.Add(new TextBlock
                {
                    Text = "Email: your.email@university.edu",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });
                
                panel.Children.Add(new TextBlock
                {
                    Text = "Group: Your Group Number",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });
                
                panel.Children.Add(new TextBlock
                {
                    Text = "Specialization: Your Specialization",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });
                
                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                
                okButton.Click += (s, e) => aboutWindow.Close();
                panel.Children.Add(okButton);
                
                aboutWindow.Content = panel;
                
                await aboutWindow.ShowDialog(desktop.MainWindow);
            }
        }

        [RelayCommand]
        private void FlipCard(int cardId)
        {
            if (!IsGameActive)
                return;
                
            // Debug line - useful to check if command is being triggered
            Debug.WriteLine($"Flip card command triggered for card ID: {cardId}");
            
            var cardToFlip = Cards.FirstOrDefault(c => c.Id == cardId);
            if (cardToFlip == null || cardToFlip.IsFlipped || cardToFlip.IsMatched)
                return;
                
            // Don't allow selecting the same card twice
            if (_flippedCardIds.Contains(cardId))
                return;
                
            // Flip the card - this should work now that Card implements INotifyPropertyChanged
            cardToFlip.IsFlipped = true;
            _flippedCardIds.Add(cardId);
            
            // Debug line
            Debug.WriteLine($"Card {cardId} flipped. Current flipped cards: {string.Join(", ", _flippedCardIds)}");
            
            // Check if we need to process a pair
            if (_flippedCardIds.Count == 2)
            {
                ProcessPair();
            }
        }

        #endregion

        #region Game Logic Methods

        private void StartNewGame()
        {
            // Stop any existing timer
            StopGameTimer();
            
            // Configure new game
            var config = new GameConfiguration
            {
                Category = CurrentCategory,
                Mode = CurrentMode,
                Rows = GameRows,
                Columns = GameColumns,
                TotalTime = GameTimeSeconds,
                ElapsedTime = 0
            };
            
            // Create new game instance
            _currentGame = new Game(_currentUser.Username, config);
            
            // Generate card deck
            var newCards = _dataService.GenerateCardDeck(config, CurrentCategory);
            Cards = new ObservableCollection<Card>(newCards);
            _currentGame.Cards = newCards;
            
            // Set up game state
            _flippedCardIds = new List<int>();
            
            // Explicitly ensure time is set correctly
            TimeRemaining = GameTimeSeconds;
            IsGameActive = true;
            GameStatusText = "Game started! Find matching pairs.";
            TimeRemainingColor = Brushes.Black;
            
            // Debug information
            Debug.WriteLine($"Starting new game with {Cards.Count} cards. Time: {TimeRemaining} seconds");
            
            // Start the timer after UI updates
            Dispatcher.UIThread.Post(() => StartGameTimer());
        }

        private void LoadGame(Game game)
        {
            // Stop any existing timer
            StopGameTimer();
            
            // Load game configuration
            _currentGame = game;
            CurrentCategory = game.Configuration.Category;
            CurrentMode = game.Configuration.Mode;
            GameRows = game.Configuration.Rows;
            GameColumns = game.Configuration.Columns;
            GameTimeSeconds = game.Configuration.TotalTime;
            
            // Set up game state
            Cards = new ObservableCollection<Card>(game.Cards);
            _flippedCardIds = new List<int>();
            
            // Ensure time is set correctly from the saved game
            TimeRemaining = game.RemainingTime;
            IsGameActive = true;
            GameStatusText = "Saved game loaded! Continue finding matching pairs.";
            
            // Update time color if needed
            UpdateTimeRemainingColor();
            
            // Start the timer
            Dispatcher.UIThread.Post(() => StartGameTimer());
        }

        private void StartGameTimer()
        {
            // Only start if we don't already have a running timer
            if (_timerRunning)
                return;
                
            _gameTimer = new Timer(1000);
            _gameTimer.Elapsed += OnGameTimerTick;
            _gameTimer.AutoReset = true;
            _timerRunning = true;
            
            // Debug line
            Debug.WriteLine("Timer started");
            
            _gameTimer.Start();
        }

        private void StopGameTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Elapsed -= OnGameTimerTick;
                _gameTimer.Dispose();
                _gameTimer = null;
                _timerRunning = false;
                
                // Debug line
                Debug.WriteLine("Timer stopped");
            }
        }

        private void OnGameTimerTick(object? sender, ElapsedEventArgs e)
        {
            // Update on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                // Debug line - useful to see if timer is firing
                Debug.WriteLine($"Timer tick - Current time: {TimeRemaining}");
                
                if (TimeRemaining > 0)
                {
                    TimeRemaining--;
                    UpdateTimeRemainingColor();
                    
                    // Check if time is up
                    if (TimeRemaining == 0)
                    {
                        EndGame(false);
                    }
                }
            });
        }

        private void UpdateTimeRemainingColor()
        {
            // Change color based on time left
            TimeRemainingColor = TimeRemaining switch
            {
                <= 10 => Brushes.Red,
                <= 30 => Brushes.Orange,
                _ => Brushes.Black
            };
        }

        private void ProcessPair()
        {
            if (_flippedCardIds.Count != 2)
                return;
                
            // Get the two flipped cards
            var card1 = Cards.First(c => c.Id == _flippedCardIds[0]);
            var card2 = Cards.First(c => c.Id == _flippedCardIds[1]);
            
            // Check if they match by using PairId (much more reliable than comparing paths)
            bool isMatch = card1.PairId == card2.PairId;
            
            Debug.WriteLine($"Checking pair: Card {card1.Id} (PairId: {card1.PairId}) and Card {card2.Id} (PairId: {card2.PairId}). Match: {isMatch}");
            
            if (isMatch)
            {
                // Mark as matched
                card1.IsMatched = true;
                card2.IsMatched = true;
                
                // Clear flipped cards
                _flippedCardIds.Clear();
                
                // Check if all cards are matched
                if (Cards.All(c => c.IsMatched))
                {
                    EndGame(true);
                }
            }
            else
            {
                // Schedule unflip after a delay
                ScheduleUnflip();
            }
        }

        private void ScheduleUnflip()
        {
            var cardsToUnflip = new List<int>(_flippedCardIds);
            
            // In a real implementation, we'd use a proper timer with UI dispatch
            // For simplicity, we'll just create a task with a delay
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                
                // Update on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Unflip the cards
                    foreach (var id in cardsToUnflip)
                    {
                        var card = Cards.FirstOrDefault(c => c.Id == id);
                        if (card != null)
                        {
                            card.IsFlipped = false;
                            Debug.WriteLine($"Unflipping card {id}");
                        }
                    }
                    
                    // Clear flipped cards
                    _flippedCardIds.Clear();
                });
            });
        }

        private async void EndGame(bool isWin)
        {
            // Stop the timer
            StopGameTimer();
            
            // Update game state
            IsGameActive = false;
            if (_currentGame != null)
            {
                _currentGame.IsCompleted = true;
            }
            
            // Update status
            GameStatusText = isWin ? "Congratulations! You won!" : "Game over! You ran out of time.";
            
            // Update statistics
            await _dataService.UpdateStatisticsAsync(_currentUser.Username, isWin);
            
            // Show game over dialog
            await ShowGameOverDialogAsync(isWin);
        }

        private async Task ShowGameOverDialogAsync(bool isWin)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var messageBox = new Window
                {
                    Title = isWin ? "Victory!" : "Game Over",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                var panel = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15
                };
                
                panel.Children.Add(new TextBlock
                {
                    Text = isWin 
                        ? "Congratulations! You matched all the cards!" 
                        : "Time's up! Better luck next time.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 16
                });
                
                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                
                var playAgainButton = new Button { Content = "Play Again" };
                var closeButton = new Button { Content = "Close" };
                
                playAgainButton.Click += (s, e) =>
                {
                    messageBox.Close();
                    StartNewGame();
                };
                
                closeButton.Click += (s, e) => messageBox.Close();
                
                buttonPanel.Children.Add(playAgainButton);
                buttonPanel.Children.Add(closeButton);
                panel.Children.Add(buttonPanel);
                
                messageBox.Content = panel;
                
                await messageBox.ShowDialog(desktop.MainWindow);
            }
        }

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
                    Margin = new Thickness(20),
                    Spacing = 15
                };
                
                panel.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                okButton.Click += (s, e) => messageBox.Close();
                panel.Children.Add(okButton);

                messageBox.Content = panel;

                await messageBox.ShowDialog(desktop.MainWindow);
            }
        }

        #endregion
    }
}