using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace FaceEX
{
    /// <inheritdoc cref="Windows.UI.Xaml.Controls.Page" />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectTargetPage
    {
        // Style
        private const double LineThickness = 2.0;
        private readonly SolidColorBrush _fillBrush = new SolidColorBrush(Colors.Transparent);
        private readonly SolidColorBrush _lineBrush = new SolidColorBrush(Colors.Yellow);

        // Photo Processor
        private IList<DetectedFace> _detectedFaces;
        private FaceDetector _faceDetector;

        public SelectTargetPage()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick_Image(object sender, RoutedEventArgs e)
        {
            // Define file dialog
            var fileOpenPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            // Filters
            fileOpenPicker.FileTypeFilter.Add(".avi");
            fileOpenPicker.FileTypeFilter.Add(".mov");
            fileOpenPicker.FileTypeFilter.Add(".mp4");
            fileOpenPicker.FileTypeFilter.Add(".wmv");
            fileOpenPicker.FileTypeFilter.Add(".mkv");
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".png");

            // Open dialog
            var fileData = await fileOpenPicker.PickSingleFileAsync();

            if (fileData.ContentType.StartsWith("image"))
            {
                DetectFace_Image(fileData);
            }
            else if (fileData.ContentType.StartsWith("video"))
            {
                var frame = await GetFrameAsync(fileData);
                var bitmapImage = new BitmapImage();
                var randomAccessStream = new InMemoryRandomAccessStream();
                await RandomAccessStream.CopyAsync(frame, randomAccessStream);
                randomAccessStream.Seek(0);
                bitmapImage.SetSource(randomAccessStream);
            }
        }

        public async Task<IInputStream> GetFrameAsync(StorageFile file)
        {
            var mediaClip = await MediaClip.CreateFromFileAsync(file);
            var mediaComposition = new MediaComposition();
            mediaComposition.Clips.Add(mediaClip);
            return await mediaComposition.GetThumbnailAsync(
                TimeSpan.FromMilliseconds(5000), 0, 0, VideoFramePrecision.NearestFrame);
        }

        private async void DetectFace_Image(IStorageFile fileData)
        {
            // Open file
            var fileStream = await fileData.OpenAsync(FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(fileStream);

            // Resize to optimize machine learning complexity
            var transform = new BitmapTransform();
            const float sourceImageHeightLimit = 1280;

            if (decoder.PixelHeight > sourceImageHeightLimit)
            {
                var scalingFactor = sourceImageHeightLimit / decoder.PixelHeight;
                transform.ScaledWidth = (uint) Math.Floor(decoder.PixelWidth * scalingFactor);
                transform.ScaledHeight = (uint) Math.Floor(decoder.PixelHeight * scalingFactor);
            }

            // Output transformed file
            var sourceBitmap = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat,
                BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            const BitmapPixelFormat faceDetectionPixelFormat = BitmapPixelFormat.Gray8;

            var convertedBitmap = sourceBitmap.BitmapPixelFormat != faceDetectionPixelFormat
                ? SoftwareBitmap.Convert(sourceBitmap, faceDetectionPixelFormat)
                : sourceBitmap;

            // Init dectetor
            if (_faceDetector == null) _faceDetector = await FaceDetector.CreateAsync();

            _detectedFaces = await _faceDetector.DetectFacesAsync(convertedBitmap);

            ShowDetectedFaces(sourceBitmap);

            sourceBitmap.Dispose();
            fileStream.Dispose();
            convertedBitmap.Dispose();
        }

        private async void ShowDetectedFaces(SoftwareBitmap sourceBitmap)
        {
            var pixelWidth = sourceBitmap.PixelWidth;
            var pixelHeight = sourceBitmap.PixelHeight;

            // Treat bitmap image to software bitmap image
            var brush = new ImageBrush();
            var bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(sourceBitmap);
            brush.ImageSource = bitmapSource;
            brush.Stretch = Stretch.Fill;

            // Define Canvas of image
            VisualizationCanvas.Background = brush;

            if (_detectedFaces == null) return;

            // Fix rectangle scale 
            var widthScale = pixelWidth / VisualizationCanvas.ActualWidth;
            var heightScale = pixelHeight / VisualizationCanvas.ActualHeight;

            foreach (var face in _detectedFaces)
            {
                var box = new Rectangle
                {
                    Tag = face.FaceBox,
                    Width = (int) (face.FaceBox.Width / widthScale),
                    Height = (int) (face.FaceBox.Height / heightScale),
                    Fill = _fillBrush,
                    Stroke = _lineBrush,
                    StrokeThickness = LineThickness,
                    Margin = new Thickness((uint) (face.FaceBox.X / widthScale), (uint) (face.FaceBox.Y / heightScale),
                        0, 0)
                };

                VisualizationCanvas.Children.Add(box);
            }
        }
    }
}