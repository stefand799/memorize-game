using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MemorizeGame.Models
{
    public class DataService
    {
        private const string UsersFileName = "users.json";
        private const string StatsFileName = "statistics.json";
        private const string SavedGamesFolder = "saved_games";
        
        // Use consistent serialization options for both save and load
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        
        // Returns the application data directory path
        private string GetAppDataPath()
        {
            // Get the directory where the executable is located
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string dataPath = Path.Combine(appPath, "GameData");
            
            Debug.WriteLine($"App data directory: {dataPath}");
    
            if (!Directory.Exists(dataPath))
            {
                Debug.WriteLine($"Creating data directory: {dataPath}");
                Directory.CreateDirectory(dataPath);
            }
        
            string savedGamesPath = Path.Combine(dataPath, SavedGamesFolder);
            if (!Directory.Exists(savedGamesPath))
            {
                Debug.WriteLine($"Creating saved games directory: {savedGamesPath}");
                Directory.CreateDirectory(savedGamesPath);
            }
        
            return dataPath;
        }
        
        // User management methods
        public async Task<List<User>> GetAllUsersAsync()
        {
            string filePath = Path.Combine(GetAppDataPath(), UsersFileName);
            Debug.WriteLine($"Loading users from: {filePath}");
            
            if (!File.Exists(filePath))
            {
                Debug.WriteLine("Users file does not exist, returning empty list.");
                return new List<User>();
            }
                
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
                Debug.WriteLine($"Loaded {users.Count} users");
                return users;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading users: {ex.Message}");
                return new List<User>();
            }
        }
        
        public async Task SaveUserAsync(User user)
        {
            var users = await GetAllUsersAsync();
            
            // Update existing user or add new one
            int index = users.FindIndex(u => u.Username == user.Username);
            if (index >= 0)
                users[index] = user;
            else
                users.Add(user);
                
            await SaveAllUsersAsync(users);
        }
        
        public async Task SaveAllUsersAsync(List<User> users)
        {
            string filePath = Path.Combine(GetAppDataPath(), UsersFileName);
            Debug.WriteLine($"Saving {users.Count} users to: {filePath}");
            
            try
            {
                string json = JsonSerializer.Serialize(users, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                Debug.WriteLine("Users saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving users: {ex.Message}");
                throw; // Re-throw to inform caller
            }
        }
        
        public async Task DeleteUserAsync(string username)
        {
            // Remove user from list
            var users = await GetAllUsersAsync();
            int initialCount = users.Count;
            users.RemoveAll(u => u.Username == username);
            Debug.WriteLine($"Removed {initialCount - users.Count} users with username: {username}");
            
            await SaveAllUsersAsync(users);
            
            // Delete saved games
            string savedGamesPath = Path.Combine(GetAppDataPath(), SavedGamesFolder);
            if (Directory.Exists(savedGamesPath))
            {
                string searchPattern = $"{username}_*.json";
                Debug.WriteLine($"Searching for saved games with pattern: {searchPattern} in {savedGamesPath}");
                
                int filesDeleted = 0;
                foreach (var file in Directory.GetFiles(savedGamesPath, searchPattern))
                {
                    Debug.WriteLine($"Deleting file: {file}");
                    File.Delete(file);
                    filesDeleted++;
                }
                
                Debug.WriteLine($"Deleted {filesDeleted} saved game files");
            }
        }
        
        // Game methods
        public async Task SaveGameAsync(Game game)
        {
            // Sanitize username for filename
            string safeUsername = string.Join("_", game.Username.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"{safeUsername}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(GetAppDataPath(), SavedGamesFolder, fileName);
            
            Debug.WriteLine($"Saving game for user '{game.Username}' to: {filePath}");
            
            game.SavedAt = DateTime.Now;
            
            try
            {
                // Make sure we're using consistent JsonSerializerOptions
                string json = JsonSerializer.Serialize(game, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                Debug.WriteLine("Game saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving game: {ex.Message}");
                throw; // Re-throw to inform caller
            }
        }
        
        public async Task<List<Game>> GetSavedGamesForUserAsync(string username)
        {
            List<Game> savedGames = new List<Game>();
            string savedGamesPath = Path.Combine(GetAppDataPath(), SavedGamesFolder);
            
            // Sanitize username for filename search
            string safeUsername = string.Join("_", username.Split(Path.GetInvalidFileNameChars()));
            string searchPattern = $"{safeUsername}_*.json";
            
            Debug.WriteLine($"Looking for saved games in: {savedGamesPath}");
            Debug.WriteLine($"Using search pattern: {searchPattern}");
            
            if (!Directory.Exists(savedGamesPath))
            {
                Debug.WriteLine($"Saved games directory does not exist: {savedGamesPath}");
                return savedGames;
            }

            // List all files in the directory to help with debugging
            Debug.WriteLine("All files in saved games directory:");
            foreach (var file in Directory.GetFiles(savedGamesPath))
            {
                Debug.WriteLine($" - {Path.GetFileName(file)}");
            }
                
            // Try to find matching files
            var matchingFiles = Directory.GetFiles(savedGamesPath, searchPattern);
            Debug.WriteLine($"Found {matchingFiles.Length} matching saved game files");
            
            foreach (var file in matchingFiles)
            {
                try
                {
                    Debug.WriteLine($"Loading saved game from: {file}");
                    string json = await File.ReadAllTextAsync(file);
                    
                    // Use consistent serialization options
                    var game = JsonSerializer.Deserialize<Game>(json, _jsonOptions);
                    
                    if (game != null)
                    {
                        Debug.WriteLine($"Successfully loaded game saved at: {game.SavedAt}");
                        
                        // Make sure to reload all card images
                        Debug.WriteLine($"Reloading images for {game.Cards.Count} cards");
                        foreach (var card in game.Cards)
                        {
                            // Make sure the card's image is reloaded
                            if (card != null)
                            {
                                Debug.WriteLine($"Reloading image for card {card.Id}: {card.ImagePath}");
                                try
                                {
                                    // Manually reload the image since the JsonIgnore will prevent it from being serialized
                                    if (!string.IsNullOrEmpty(card.ImagePath) && File.Exists(card.ImagePath))
                                    {
                                        card.ReloadImage();
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"Card image path not found: {card.ImagePath}");
                                    }
                                }
                                catch (Exception imgEx)
                                {
                                    Debug.WriteLine($"Error reloading card image: {imgEx.Message}");
                                }
                            }
                        }
                        
                        savedGames.Add(game);
                    }
                    else
                    {
                        Debug.WriteLine("Deserialized game was null");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue with other files
                    Debug.WriteLine($"Error loading saved game {file}: {ex.Message}");
                    Debug.WriteLine($"Exception details: {ex}");
                }
            }
            
            Debug.WriteLine($"Loaded {savedGames.Count} saved games");
            return savedGames;
        }
        
        // Helper method for generating card pairs
        public List<Card> GenerateCardDeck(GameConfiguration config, GameCategory category)
        {
            // Calculate number of pairs (half of total cards)
            int totalCards = config.Rows * config.Columns;
            int pairsCount = totalCards / 2;
            
            Debug.WriteLine($"Generating deck with {totalCards} cards ({pairsCount} pairs) for category: {category}");
            
            // Get images based on category
            string[] imagePaths = GetImagePathsForCategory(category, pairsCount);
            
            // Create card list with pairs
            List<Card> cards = new List<Card>();
            for (int i = 0; i < pairsCount; i++)
            {
                int pairId = i; // Use as identifier for pair
                Debug.WriteLine($"Creating card pair {i} with image: {imagePaths[i]}");
                
                // Create two cards with the same image but different IDs
                cards.Add(new Card(i * 2, imagePaths[i], pairId));
                cards.Add(new Card(i * 2 + 1, imagePaths[i], pairId));
            }
            
            // Shuffle the cards
            Random rng = new Random();
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card temp = cards[k];
                cards[k] = cards[n];
                cards[n] = temp;
            }
            
            Debug.WriteLine($"Generated and shuffled {cards.Count} cards");
            return cards;
        }
        
        // Get image paths for a specific category
        private string[] GetImagePathsForCategory(GameCategory category, int pairsCount)
        {
            string categoryFolder = category switch
            {
                GameCategory.Category1 => "albums",
                GameCategory.Category2 => "monsters",
                GameCategory.Category3 => "category3",
                _ => "albums"
            };
            
            Debug.WriteLine($"Getting images for category: {category} (folder: {categoryFolder})");
            
            // Path to the cards directory
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "cards", categoryFolder);
            Debug.WriteLine($"Image directory path: {basePath}");
            
            // Check if directory exists
            if (!Directory.Exists(basePath))
            {
                Debug.WriteLine($"Category directory not found: {basePath}");
                // Fallback to Avalonia logo
                string[] fallbackPaths = new string[pairsCount];
                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "avalonia-logo.ico");
                Debug.WriteLine($"Using fallback image path: {fallbackPath}");
                
                for (int i = 0; i < fallbackPaths.Length; i++)
                {
                    fallbackPaths[i] = fallbackPath;
                }
                return fallbackPaths;
            }
            
            // Get all image files with common image extensions
            string[] imageFiles = Directory.GetFiles(basePath, "*.png")
                                          .Concat(Directory.GetFiles(basePath, "*.jpg"))
                                          .Concat(Directory.GetFiles(basePath, "*.jpeg"))
                                          .ToArray();
            
            Debug.WriteLine($"Found {imageFiles.Length} image files in category directory");
            
            if (imageFiles.Length == 0)
            {
                Debug.WriteLine($"No images found in category directory: {basePath}");
                // Fallback to Avalonia logo
                string[] fallbackPaths = new string[pairsCount];
                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "avalonia-logo.ico");
                Debug.WriteLine($"Using fallback image path: {fallbackPath}");
                
                for (int i = 0; i < fallbackPaths.Length; i++)
                {
                    fallbackPaths[i] = fallbackPath;
                }
                return fallbackPaths;
            }
            
            // Verify we have enough unique images for each pair
            if (imageFiles.Length < pairsCount)
            {
                Debug.WriteLine($"Warning: Not enough unique images in category. Need {pairsCount} but found {imageFiles.Length}.");
                // Use the images we have and cycle through them if necessary
                string[] selectedPaths = new string[pairsCount];
                for (int i = 0; i < pairsCount; i++)
                {
                    selectedPaths[i] = imageFiles[i % imageFiles.Length];
                }
                return selectedPaths;
            }
            
            // Shuffle the available images
            Random rng = new Random();
            string[] shuffledImages = imageFiles.OrderBy(x => rng.Next()).ToArray();
            
            // Take just the number of unique images we need for pairs
            return shuffledImages.Take(pairsCount).ToArray();
        }
        
        // Update user statistics after game completion
        public async Task UpdateStatisticsAsync(string username, bool isWin)
        {
            Debug.WriteLine($"Updating statistics for user: {username}, win: {isWin}");
            
            var users = await GetAllUsersAsync();
            var user = users.Find(u => u.Username == username);

            if (user != null)
            {
                Debug.WriteLine($"Found user {username}. Current stats: Played={user.GamesPlayed}, Won={user.GamesWon}");
                user.GamesPlayed++;
                if (isWin)
                    user.GamesWon++;
                
                Debug.WriteLine($"Updated stats: Played={user.GamesPlayed}, Won={user.GamesWon}");
                await SaveAllUsersAsync(users);
            }
            else
            {
                Debug.WriteLine($"User {username} not found for statistics update");
            }
        }
    }
}