using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;

namespace FaceEX
{
    /// <inheritdoc cref="Windows.UI.Xaml.Controls.Page" />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectTargetPage
    {
        private static readonly FaceServiceClient FaceServiceClient = new FaceServiceClient(
            "3d7d23e210144e1ab01e5f7a335d0a1d",
            "https://westcentralus.api.cognitive.microsoft.com/face/v1.0"
        );

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
                var detectedFace = await DetectFace_Image(await fileData.OpenStreamForReadAsync());

                // Condition Print image
                if (detectedFace != null)
                {
                    Image.Content = null;
                    Image.PointerEntered += new PointerEventHandler(Target_PointerEntered);
                    Image.PointerExited += new PointerEventHandler(Target_PointerExited);
                    var bitImage = new BitmapImage();
                    IRandomAccessStream fileStream = await fileData.OpenReadAsync();
                    await bitImage.SetSourceAsync(fileStream);
                    ImageBrush ib = new ImageBrush();
                    ib.ImageSource = bitImage;
                    Image.Background = ib;
                } else
                    Frame.Navigate(typeof(SelectNotFound));
            }
            else if (fileData.ContentType.StartsWith("video"))
            {
                var detectFaces = await DetectFace_Video(fileData);

                // Condition Print (detactFaces = tableau asssociatif : cle = temps(MM::SS) => valeur : Face)
                if (detectFaces.Count > 0)
                {
                    Image.Content = "La personne apparait à ces moments de videos : \n";
                    foreach (var time in detectFaces.Keys)
                    {
                        Image.Content = Image.Content + "  - " + time + "\n";
                    }
                }
                else
                    Frame.Navigate(typeof(SelectNotFound));
            }
        }

        private void Target_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Image.Content = null;
        }

        private void Target_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Image.Content = "Upload another File";
        }

        private void Change_content_button()
        {
            Image.Content = "Upload Another File";
        }

        private async Task<Dictionary<TimeSpan, Face[]>> DetectFace_Video(StorageFile fileData)
        {
            // Process configs
            const int frameJumpSize = 15;
            const int frameQuota = 20;
            const int frameTimeout = 60_000;

            // Video
            var videoProperties = await fileData.Properties.GetVideoPropertiesAsync();
            var videoTime = TimeSpan.FromSeconds(0);
            var videoFrame = 0;

            // DetectedFaces
            var detectedFaces = new Dictionary<TimeSpan, Face[]>();

            // Process every frame
            while (videoTime < videoProperties.Duration)
            {
                // Frame cut
                var mediaClip = await MediaClip.CreateFromFileAsync(fileData);
                var mediaComposition = new MediaComposition();
                mediaComposition.Clips.Add(mediaClip);
                var frame = await mediaComposition.GetThumbnailAsync(
                    videoTime,
                    0,
                    0,
                    VideoFramePrecision.NearestFrame
                );

                // Stream conversion
                var randomAccessStream = new InMemoryRandomAccessStream();
                await RandomAccessStream.CopyAsync(frame, randomAccessStream);
                randomAccessStream.Seek(0);

                // Process and add image
                var detectedFace = await DetectFace_Image(randomAccessStream.AsStreamForRead());
                if (detectedFace != null)
                {
                    detectedFaces.Add(videoTime, detectedFace);
                }

                // Quota overflow
                if (videoFrame != 0 && videoFrame % frameQuota == 0) await Task.Delay(frameTimeout);

                // Frame and time incrementation
                videoTime += TimeSpan.FromSeconds(frameJumpSize);
                videoFrame += 1;
            }

            return detectedFaces;
        }

        private async Task<Face[]> DetectFace_Image(Stream fileData)
        {
            // Detect faces in image
            var imageFaces = await FaceServiceClient.DetectAsync(fileData);
            if (imageFaces.Length <= 0)
            {
                return null;
            }

            // Process to recognition
            var recognizedFaces = await FaceServiceClient.IdentifyAsync(
                "default2",
                imageFaces.Select(face => face.FaceId).ToArray()
            );

            // Repack faces
            var confirmedFaces = (
                from recognizedFace in recognizedFaces
                from imageFace in imageFaces
                where recognizedFace.FaceId == imageFace.FaceId
                where recognizedFace.Candidates.Length >= 0
                where recognizedFace.Candidates.Any(face => face.Confidence >= 0.9)
                select imageFace
            ).ToArray();

            return confirmedFaces;
        }
    }
}