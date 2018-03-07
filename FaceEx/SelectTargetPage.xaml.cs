using Windows.Storage.Pickers;
using Windows.UI.Xaml;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceEX
{
    /// <inheritdoc cref="Windows.UI.Xaml.Controls.Page" />
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectTargetPage
    {
        public SelectTargetPage()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick_Image(object sender, RoutedEventArgs e)
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
        }
    }
}