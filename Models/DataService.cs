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
// Method to load images from the file system instead of resources
private string[] GetImagePathsForCategory(GameCategory category, int count)
{
    string categoryFolder = category switch
    {
        GameCategory.Category1 => "albums",
        GameCategory.Category2 => "category2",
        GameCategory.Category3 => "category3",
        _ => "albums"
    };
    
    // Path to the cards directory
    string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "cards", categoryFolder);
    
    // Check if directory exists
    if (!Directory.Exists(basePath))
    {
        Console.WriteLine($"Category directory not found: {basePath}");
        // Fallback to Avalonia logo
        string[] fallbackPaths = new string[count * 2];
        for (int i = 0; i < fallbackPaths.Length; i++)
        {
            fallbackPaths[i] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "avalonia-logo.ico");
        }
        return fallbackPaths;
    }
    
    // Get all image files with common image extensions
    string[] imageFiles = Directory.GetFiles(basePath, "*.png")
                                  .Concat(Directory.GetFiles(basePath, "*.jpg"))
                                  .Concat(Directory.GetFiles(basePath, "*.jpeg"))
                                  .ToArray();
    
    if (imageFiles.Length == 0)
    {
        Console.WriteLine($"No images found in category directory: {basePath}");
        // Fallback to Avalonia logo
        string[] fallbackPaths = new string[count * 2];
        for (int i = 0; i < fallbackPaths.Length; i++)
        {
            fallbackPaths[i] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "avalonia-logo.ico");
        }
        return fallbackPaths;
    }
    
    // Make sure we have enough images for the cards
    // We'll use modulo arithmetic to cycle through available images if we don't have enough
    List<string> cardImagePaths = new List<string>();
    for (int i = 0; i < count; i++)
    {
        int imageIndex = i % imageFiles.Length;
        string imagePath = imageFiles[imageIndex];
        
        // Add each image twice (for pairs)
        cardImagePaths.Add(imagePath);
        cardImagePaths.Add(imagePath);
    }
    
    // Shuffle the paths
    Random rng = new Random();
    int n = cardImagePaths.Count;
    while (n > 1)
    {
        n--;
        int k = rng.Next(n + 1);
        string temp = cardImagePaths[k];
        cardImagePaths[k] = cardImagePaths[n];
        cardImagePaths[n] = temp;
    }
    
    return cardImagePaths.ToArray();
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
        // Add this method to your DataService class
        public string VerifyImagePaths()
        {
            List<string> diagnosticInfo = new List<string>();
    
            // Check base directories
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(baseDir, "Assets");
            string imagesDir = Path.Combine(assetsDir, "images");
            string cardsDir = Path.Combine(imagesDir, "cards");
            string albumsDir = Path.Combine(cardsDir, "albums");
    
            diagnosticInfo.Add($"Base directory exists: {Directory.Exists(baseDir)}");
            diagnosticInfo.Add($"Assets directory exists: {Directory.Exists(assetsDir)}");
            diagnosticInfo.Add($"Images directory exists: {Directory.Exists(imagesDir)}");
            diagnosticInfo.Add($"Cards directory exists: {Directory.Exists(cardsDir)}");
            diagnosticInfo.Add($"Albums directory exists: {Directory.Exists(albumsDir)}");
    
            // List all image files if albums directory exists
            if (Directory.Exists(albumsDir))
            {
                string[] allFiles = Directory.GetFiles(albumsDir);
                diagnosticInfo.Add($"Found {allFiles.Length} files in albums directory:");
                foreach (var file in allFiles)
                {
                    diagnosticInfo.Add($"- {Path.GetFileName(file)}");
                }
            }
    
            // Check Avalonia resource format
            // Note: This is a simple string check, not an actual resource validation
            string avaloniaPath = "avares://MemorizeGame/Assets/images/cards/albums";
            diagnosticInfo.Add($"Avalonia resource path format: {avaloniaPath}");
    
            return string.Join(Environment.NewLine, diagnosticInfo);
        }
    }
}