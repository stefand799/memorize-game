using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MemorizeGame.Models
{
    public class DataService
    {
        private const string UsersFileName = "users.json";
        private const string StatsFileName = "statistics.json";
        private const string SavedGamesFolder = "saved_games";
        
        // Returns the application data directory path
        private string GetAppDataPath()
        {
            // Get the directory where the executable is located
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string dataPath = Path.Combine(appPath, "GameData");
    
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
        
            if (!Directory.Exists(Path.Combine(dataPath, SavedGamesFolder)))
                Directory.CreateDirectory(Path.Combine(dataPath, SavedGamesFolder));
        
            return dataPath;
        }
        
        // User management methods
        public async Task<List<User>> GetAllUsersAsync()
        {
            string filePath = Path.Combine(GetAppDataPath(), UsersFileName);
            
            if (!File.Exists(filePath))
                return new List<User>();
                
            string json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
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
            string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }
        
        public async Task DeleteUserAsync(string username)
        {
            // Remove user from list
            var users = await GetAllUsersAsync();
            users.RemoveAll(u => u.Username == username);
            await SaveAllUsersAsync(users);
            
            // Delete saved games
            string savedGamesPath = Path.Combine(GetAppDataPath(), SavedGamesFolder);
            if (Directory.Exists(savedGamesPath))
            {
                foreach (var file in Directory.GetFiles(savedGamesPath, $"{username}_*.json"))
                {
                    File.Delete(file);
                }
            }
        }
        
        // Game methods
        public async Task SaveGameAsync(Game game)
        {
            string fileName = $"{game.Username}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(GetAppDataPath(), SavedGamesFolder, fileName);
            
            game.SavedAt = DateTime.Now;
            
            string json = JsonSerializer.Serialize(game, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }
        
        public async Task<List<Game>> GetSavedGamesForUserAsync(string username)
        {
            List<Game> savedGames = new List<Game>();
            string savedGamesPath = Path.Combine(GetAppDataPath(), SavedGamesFolder);
            
            if (!Directory.Exists(savedGamesPath))
                return savedGames;
                
            foreach (var file in Directory.GetFiles(savedGamesPath, $"{username}_*.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var game = JsonSerializer.Deserialize<Game>(json);
                    if (game != null)
                        savedGames.Add(game);
                }
                catch (Exception)
                {
                    // Skip corrupt files
                    continue;
                }
            }
            
            return savedGames;
        }
        
        // Helper method for generating card pairs
        public List<Card> GenerateCardDeck(GameConfiguration config, GameCategory category)
        {
            // Calculate number of pairs (half of total cards)
            int totalCards = config.Rows * config.Columns;
            int pairsCount = totalCards / 2;
            
            // Get images based on category
            string[] imagePaths = GetImagePathsForCategory(category, pairsCount);
            
            // Create card list with pairs
            List<Card> cards = new List<Card>();
            for (int i = 0; i < pairsCount; i++)
            {
                int pairId = i; // Use as identifier for pair
                
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
            
            return cards;
        }
        
        // Get image paths for a specific category
        private string[] GetImagePathsForCategory(GameCategory category, int count)
        {
            string categoryFolder;
            string baseFolder;
            
            switch (category)
            {
                case GameCategory.Category1:
                    categoryFolder = "albums";
                    break;
                case GameCategory.Category2:
                    categoryFolder = "category2";
                    break;
                case GameCategory.Category3:
                    categoryFolder = "category3";
                    break;
                default:
                    categoryFolder = "albums";
                    break;
            }
            
            // Get paths to images
            baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "cards", categoryFolder);
            
            List<string> availableImages = new List<string>();
            
            // Check if folder exists
            if (Directory.Exists(baseFolder))
            {
                // Look for both jpg and png files
                string[] jpgFiles = Directory.GetFiles(baseFolder, "album_*.jpg");
                string[] pngFiles = Directory.GetFiles(baseFolder, "album_*.png");
                
                // Add all found images to our list
                availableImages.AddRange(jpgFiles.Select(f => $"/Assets/images/cards/{categoryFolder}/{Path.GetFileName(f)}"));
                availableImages.AddRange(pngFiles.Select(f => $"/Assets/images/cards/{categoryFolder}/{Path.GetFileName(f)}"));
            }
            
            // If we didn't find any images, use placeholders
            if (availableImages.Count == 0)
            {
                for (int i = 1; i <= 18; i++)
                {
                    availableImages.Add($"/Assets/avalonia-logo.ico");
                }
            }
            
            // Make sure we have enough images
            List<string> result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                result.Add(availableImages[i % availableImages.Count]);
            }
            
            return result.ToArray();
        }
        
        // Update user statistics after game completion
        public async Task UpdateStatisticsAsync(string username, bool isWin)
        {
            var users = await GetAllUsersAsync();
            var user = users.Find(u => u.Username == username);
            
            if (user != null)
            {
                user.GamesPlayed++;
                if (isWin)
                    user.GamesWon++;
                    
                await SaveAllUsersAsync(users);
            }
        }
    }
}