using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MemorizeGame.Models
{
    public partial class User : ObservableObject
    {
        private string _imagePath = string.Empty;
        
        public string Username { get; set; } = string.Empty;
        
        // Make ImagePath initialize the image when set
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                LoadImage();
                OnPropertyChanged();
            }
        }
        
        // Image property to bind to - not serialized
        [ObservableProperty]
        [property: JsonIgnore] // Don't serialize the bitmap
        private Bitmap? _userImage;
        
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        
        // Constructor
        public User(string username, string imagePath)
        {
            Username = username;
            GamesPlayed = 0;
            GamesWon = 0;
            
            // Set image path (which will load the image)
            ImagePath = imagePath;
        }
        
        // Default constructor for serialization
        public User()
        {
            Username = string.Empty;
            _imagePath = string.Empty;
            GamesPlayed = 0;
            GamesWon = 0;
        }
        
        // Helper method to load the image
        private void LoadImage()
        {
            try
            {
                if (string.IsNullOrEmpty(_imagePath))
                {
                    Debug.WriteLine($"User {Username}: Image path is empty, skipping image load.");
                    return;
                }
                
                // Log the full path for debugging
                Debug.WriteLine($"User {Username}: Loading image from path: {_imagePath}");
                
                // Load the image directly from the file system
                if (File.Exists(_imagePath))
                {
                    try
                    {
                        UserImage = new Bitmap(_imagePath);
                        Debug.WriteLine($"User {Username}: Successfully loaded image");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"User {Username}: Error creating bitmap: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"User {Username}: Image file not found: {_imagePath}");
                    // Try to find if the file exists in a different location
                    TryFindAndLoadImageAlternate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"User {Username}: Failed to load image from {_imagePath}: {ex.Message}");
            }
        }
        
        // Try to load from alternative locations
        private void TryFindAndLoadImageAlternate()
        {
            try
            {
                // If the path includes Assets, try to construct a path relative to the executable
                if (_imagePath.Contains("Assets"))
                {
                    // Extract the part of the path after "Assets"
                    int assetsIndex = _imagePath.IndexOf("Assets");
                    if (assetsIndex >= 0)
                    {
                        string relativePath = _imagePath.Substring(assetsIndex);
                        string alternatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                        
                        Debug.WriteLine($"User {Username}: Trying alternate path: {alternatePath}");
                        
                        if (File.Exists(alternatePath))
                        {
                            UserImage = new Bitmap(alternatePath);
                            Debug.WriteLine($"User {Username}: Successfully loaded image from alternate path");
                            // Update the path to the working one
                            _imagePath = alternatePath;
                        }
                        else
                        {
                            Debug.WriteLine($"User {Username}: Alternate path not found: {alternatePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"User {Username}: Error in alternate image loading: {ex.Message}");
            }
        }
        
        // Method to call after deserialization to ensure image is loaded
        public void ReloadImage()
        {
            Debug.WriteLine($"User {Username}: Reloading image after deserialization from path: {_imagePath}");
            LoadImage();
        }
    }
}