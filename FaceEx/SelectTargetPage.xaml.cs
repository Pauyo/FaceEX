using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.ProjectOxford.Face;

namespace FaceEX
{
    /// <inheritdoc cref="Windows.UI.Xaml.Controls.Page" />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectTargetPage
    {
        private static readonly IFaceServiceClient FaceServiceClient = new FaceServiceClient(
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
                DetectFace_Image(await fileData.OpenStreamForReadAsync());
            }
            else if (fileData.ContentType.StartsWith("video"))
            {
                DetectFace_Video(fileData);
            }
        }

        private static async void DetectFace_Video(StorageFile fileData)
        {
            const int frameJumpSize = 15;
            var videoProperties = await fileData.Properties.GetVideoPropertiesAsync();
            for (
                var time = TimeSpan.FromSeconds(0);
                time < videoProperties.Duration;
                time += TimeSpan.FromSeconds(frameJumpSize)
            ) {
                var mediaClip = await MediaClip.CreateFromFileAsync(fileData);
                var mediaComposition = new MediaComposition();
                mediaComposition.Clips.Add(mediaClip);
                var frame = await mediaComposition.GetThumbnailAsync(
                    time,
                    0,
                    0,
                    VideoFramePrecision.NearestFrame
                );
                var randomAccessStream = new InMemoryRandomAccessStream();
                await RandomAccessStream.CopyAsync(frame, randomAccessStream);

                DetectFace_Image(randomAccessStream.AsStreamForRead());

                await Task.Delay(20_000);
            }
        }

        private static async void DetectFace_Image(Stream fileData)
        {
            var faces = await FaceServiceClient.DetectAsync(fileData);
            var faceIds = faces.Select(face => face.FaceId).ToArray();

            var results = await FaceServiceClient.IdentifyAsync("default", faceIds);
            foreach (var identifyResult in results)
            {
                if (identifyResult.Candidates.Length == 0)
                {
                    //TODO no match handler
                }
                else
                {
                    var candidateId = identifyResult.Candidates[0].PersonId;
                    var person = await FaceServiceClient.GetPersonInLargePersonGroupAsync("default", candidateId);
                    //TODO success handler
                }
            }

        }
    }
}