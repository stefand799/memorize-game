using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MemorizeGame.Models
{
    // Class to represent a profile image item
    public partial class ProfileImageItem : ObservableObject
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        
        [ObservableProperty]
        private string _displayName = string.Empty;
        
        [ObservableProperty]
        private Bitmap? _imageData;
        
        [ObservableProperty]
        private bool _isSelected = false;
        
        public void LoadImage()
        {
            try
            {
                if (File.Exists(ImagePath))
                {
                    ImageData = new Bitmap(ImagePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image {ImagePath}: {ex.Message}");
            }
        }
    }
}