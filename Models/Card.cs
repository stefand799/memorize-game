using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MemorizeGame.Models
{
    public partial class Card : ObservableObject
    {
        private string _imagePath = string.Empty;
        
        public int Id { get; set; }
        
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
        [property: JsonIgnore] // Correctly applying JsonIgnore to the generated property
        private Bitmap? _cardImage;
        
        public int PairId { get; set; }
        
        [ObservableProperty]
        private bool _isFlipped;
        
        [ObservableProperty]
        private bool _isMatched;
        
        public Card(int id, string imagePath, int pairId)
        {
            Id = id;
            PairId = pairId;
            _isFlipped = false;
            _isMatched = false;
            
            // Set image path (which will load the image)
            ImagePath = imagePath;
        }
        
        // Default constructor for serialization
        public Card()
        {
            Id = 0;
            PairId = 0;
            _isFlipped = false;
            _isMatched = false;
            _imagePath = string.Empty;
        }
        
        // Helper method to load the image
        private void LoadImage()
        {
            try
            {
                if (string.IsNullOrEmpty(_imagePath))
                {
                    Debug.WriteLine($"Card {Id}: Image path is empty, skipping image load.");
                    return;
                }
                
                // Log the full path for debugging
                Debug.WriteLine($"Card {Id}: Loading image from path: {_imagePath}");
                
                // Load the image directly from the file system
                if (File.Exists(_imagePath))
                {
                    try
                    {
                        CardImage = new Bitmap(_imagePath);
                        Debug.WriteLine($"Card {Id}: Successfully loaded image");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Card {Id}: Error creating bitmap: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Card {Id}: Image file not found: {_imagePath}");
                    // Try to find if the file exists in a different location
                    TryFindAndLoadImageAlternate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Card {Id}: Failed to load image from {_imagePath}: {ex.Message}");
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
                        
                        Debug.WriteLine($"Card {Id}: Trying alternate path: {alternatePath}");
                        
                        if (File.Exists(alternatePath))
                        {
                            CardImage = new Bitmap(alternatePath);
                            Debug.WriteLine($"Card {Id}: Successfully loaded image from alternate path");
                            // Update the path to the working one
                            _imagePath = alternatePath;
                        }
                        else
                        {
                            Debug.WriteLine($"Card {Id}: Alternate path not found: {alternatePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Card {Id}: Error in alternate image loading: {ex.Message}");
            }
        }
        
        // Method to call after deserialization to ensure image is loaded
        public void ReloadImage()
        {
            Debug.WriteLine($"Card {Id}: Reloading image after deserialization from path: {_imagePath}");
            LoadImage();
        }
    }
}