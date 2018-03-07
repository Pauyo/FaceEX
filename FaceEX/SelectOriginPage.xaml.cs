using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Microsoft.ProjectOxford.Face;

namespace FaceEX
{
    /// <inheritdoc cref="Windows.UI.Xaml.Controls.Page" />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectOriginPage
    {
        private static readonly IFaceServiceClient FaceServiceClient = new FaceServiceClient(
            "3d7d23e210144e1ab01e5f7a335d0a1d",
            "https://westcentralus.api.cognitive.microsoft.com/face/v1.0"
        );

        public SelectOriginPage()
        {
            InitializeComponent();
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
            var fileDatas = await fileOpenPicker.PickMultipleFilesAsync();

            // Group
            const string personGroupId = "default";
            const string personGroupName = "Default";
            if (await FaceServiceClient.GetPersonGroupAsync(personGroupId) == null)
                await FaceServiceClient.CreatePersonGroupAsync(
                    personGroupId,
                    personGroupName
                );

            // Person
            const string personId = "Default";
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

            Frame.Navigate(typeof(SelectTargetPage));
        }
    }
}