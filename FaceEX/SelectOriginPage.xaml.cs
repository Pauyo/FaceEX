using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Microsoft.ProjectOxford.Face;
using Windows.Storage;
using System.Collections.Generic;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace FaceEX
{
    /// <inheritdoc cref="Windows.UI.Xaml.Controls.Page" />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectOriginPage
    {
        private IReadOnlyList<StorageFile> fileDatas;
        private Brush defaultBack;
        private static readonly FaceServiceClient FaceServiceClient = new FaceServiceClient(
            "3d7d23e210144e1ab01e5f7a335d0a1d",
            "https://westcentralus.api.cognitive.microsoft.com/face/v1.0"
        );

        public SelectOriginPage()
        {
            InitializeComponent();
            defaultBack = Image.Background;
        }

        private async void ButtonBase_OnClick_Image(object sender, RoutedEventArgs e)
        {
            // Define file dialog
            var fileOpenPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            // Filters
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".jpeg");
            fileOpenPicker.FileTypeFilter.Add(".png");

            // Open File picker dialog
            fileDatas = await fileOpenPicker.PickMultipleFilesAsync();

            var images = new List<BitmapImage>();
            if (fileDatas != null)
            {
                foreach (StorageFile file in fileDatas)
                {
                    string cExt = file.FileType;
                    Windows.Storage.Streams.IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    using (Windows.Storage.Streams.IRandomAccessStream filestream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                       BitmapImage bitmapImage = new BitmapImage();
                       await bitmapImage.SetSourceAsync(fileStream);
                       images.Add(bitmapImage);
                    }
                }
                if (images.Count > 0)
                {
                    Image.Content = null;
                    Image.PointerEntered += new PointerEventHandler(Target_PointerEntered);
                    Image.PointerExited += new PointerEventHandler(Target_PointerExited);
                    ImageBrush ib = new ImageBrush();
                    ib.ImageSource = images[0];
                    Image.Background = ib;
                }
                else
                {
                    Image.PointerEntered += new PointerEventHandler(defaultTarget_PointerEntered);
                    Image.PointerExited += new PointerEventHandler(defaultTarget_PointerExited);
                    Image.Background = defaultBack;
                }
            }
        }

        private void defaultTarget_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Image.Content = "Upload a File";
        }

        private void defaultTarget_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Image.Content = "Upload a File";
        }

        private void Target_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Image.Content = null;
        }

        private void Target_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Image.Content = "Upload another File";
        }

        private async void Search_person(object sender, RoutedEventArgs e)
        {
            if (fileDatas != null && PersonName.Text != "")
            {
                const string personGroupId = "default";
                const string personGroupName = "Default";
                // Group
                try
                {
                    await FaceServiceClient.CreatePersonGroupAsync(
                        personGroupId,
                        personGroupName
                    );
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }

                // Person
                string personId = PersonName.Text;
                var person = await FaceServiceClient.CreatePersonInPersonGroupAsync(
                    personGroupId,
                    personId
                );

                // Train person
                foreach (var fileData in fileDatas)
                    await FaceServiceClient.AddPersonFaceInPersonGroupAsync(
                        personGroupId,
                        person.PersonId,
                        await fileData.OpenStreamForReadAsync()
                );

                await FaceServiceClient.TrainPersonGroupAsync(personGroupId);

                // ReSharper disable once SuspiciousTypeConversion.Global
                while ((await FaceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId)).Status.Equals("running"))
                    await Task.Delay(1000);
                

                Frame.Navigate(typeof(SelectTargetPage), personId);
            } else {
                Validate.Content = "You must Upload a file and write the person's name";
            }
        }
    }
}