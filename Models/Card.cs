using System;
using System.IO;
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
        
        // Image property to bind to
        [ObservableProperty]
        private Bitmap _cardImage;
        
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
                    return;
                
                // Load the image directly from the file system
                if (File.Exists(_imagePath))
                {
                    CardImage = new Bitmap(_imagePath);
                }
                else
                {
                    Console.WriteLine($"Image file not found: {_imagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from {_imagePath}: {ex.Message}");
            }
        }
    }
}