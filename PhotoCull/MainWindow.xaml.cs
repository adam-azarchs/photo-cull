using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PhotoCull
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Photo[] photoList;

        private Photo leftPhoto;

        public Photo LeftPhoto
        {
            get { return leftPhoto; }
            set {
                if (value != leftPhoto && value != rightPhoto && leftPhoto != null)
                    leftPhoto.UnloadImage();
                leftPhoto = value;
            }
        }

        private Photo rightPhoto;

        public Photo RightPhoto
        {
            get { return rightPhoto; }
            set
            {
                if (value != leftPhoto && value != rightPhoto && rightPhoto != null)
                    rightPhoto.UnloadImage();
                rightPhoto = value;
            }
        }

        public bool Zoom { get; set; }

        private void ChooseImages(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.Filter = "Jpeg images|*.jpg;*.jpeg";
            dialog.Multiselect = true;
            dialog.Title = "Choose files to compare...";
            dialog.ShowReadOnly = false;
            dialog.ValidateNames = true;
            if ((dialog.ShowDialog(this) ?? false) && dialog.FileNames.Length > 1)
            {
                photoList = dialog.FileNames.Select(f => new Photo(f)).ToArray();
                SearchForComparisons();
            }
        }

        private void SearchForComparisons()
        {
            SortList();
            if (!SearchOnce(false) && !SearchOnce(true))
            {
                foreach (Photo item in photoList)
                {
                    foreach (var comparison in item.Comparisons.ToArray())
                    {
                        if (comparison.Value == PhotoComparison.Different)
                        {
                            item.Comparisons[comparison.Key] = PhotoComparison.NotCompared;
                            comparison.Key.Comparisons[item] = PhotoComparison.NotCompared;
                        }
                    }
                }
                if (!SearchOnce(false))
                {
                    SearchOnce(true);
                }
            }
        }

        private bool SearchOnce(bool allowRejected)
        {
            foreach (Photo lhs in photoList)
            {
                foreach (Photo rhs in photoList.Reverse())
                {
                    if (IsValidComparison(lhs, rhs, allowRejected)) return true;
                }
            }
            return false;
        }

        private bool IsValidComparison(Photo lhs, Photo rhs, bool allowRejected)
        {
            if (rhs == lhs)
                return false;
            if (!allowRejected && (rhs.IsRejected || lhs.IsRejected))
                return false;
            if (!lhs.Comparisons.ContainsKey(rhs) ||
                lhs.Comparisons[rhs] == PhotoComparison.NotCompared)
            {
                LeftPhoto = lhs;
                RightPhoto = rhs;
                ShowPhotos();
                return true;
            }
            return false;
        }

        private void ShowPhotos()
        {
            if (LeftPhoto != null)
            {
                this.LeftImage.Source = LeftPhoto.Image;
                this.LeftImageNameText.Text = LeftPhoto.FilePath;
                this.LeftImageNameText.TextDecorations = LeftPhoto.TextDecorations;
            }
            else
            {
                this.LeftImage.Source = null;
            }
            if (RightPhoto != null)
            {
                this.RightImage.Source = RightPhoto.Image;
                this.RightImageNameText.Text = RightPhoto.FilePath;
                this.RightImageNameText.TextDecorations = RightPhoto.TextDecorations;
            }
            else
            {
                this.RightImage.Source = null;
            }
            if (RightPhoto != null && LeftPhoto != null)
            {
                LeftButton.IsEnabled = true;
                RightButton.IsEnabled = true;
                CantChooseButton.IsEnabled = true;
            }
        }

        private void SortList()
        {
            Array.Sort(photoList);
            this.FileListBox.ItemsSource = new Photo[0];
            this.FileListBox.ItemsSource = photoList;
            this.FileListBox.UpdateLayout();
        }

        private void CantChooseClick(object sender, RoutedEventArgs e)
        {
            DisableButtons();

            LeftPhoto.Comparisons[RightPhoto] = PhotoComparison.Different;
            RightPhoto.Comparisons[LeftPhoto] = PhotoComparison.Different;
            SearchForComparisons();
        }

        private void DisableButtons()
        {
            LeftButton.IsEnabled = false;
            RightButton.IsEnabled = false;
            CantChooseButton.IsEnabled = false;
        }

        private void RightImageClick(object sender, RoutedEventArgs e)
        {
            DisableButtons();

            LeftPhoto.Comparisons[RightPhoto] = PhotoComparison.Worse;
            RightPhoto.Comparisons[LeftPhoto] = PhotoComparison.Better;
            LeftPhoto.IsRejected = true;
            RightPhoto.IsRejected = false;
            SearchForComparisons();
        }

        private void LeftImageClick(object sender, RoutedEventArgs e)
        {
            DisableButtons();

            LeftPhoto.Comparisons[RightPhoto] = PhotoComparison.Better;
            RightPhoto.Comparisons[LeftPhoto] = PhotoComparison.Worse;
            LeftPhoto.IsRejected = false;
            RightPhoto.IsRejected = true;
            SearchForComparisons();
        }

        private void ListBoxSelection(object sender, SelectionChangedEventArgs e)
        {
            if (FileListBox.SelectedIndex < 0)
                return;
            if (RightPhoto != this.photoList[FileListBox.SelectedIndex])
                LeftPhoto = this.photoList[FileListBox.SelectedIndex];
            else if (LeftPhoto != this.photoList[FileListBox.SelectedIndex])
                RightPhoto = this.photoList[FileListBox.SelectedIndex];
            ShowPhotos();
        }

        private void MoveZoom(object sender, MouseEventArgs e)
        {
            Image imageView = sender as Image;
            if (imageView == null)
                return;
            if (!Zoom)
                return;
            var mousePointer = e.GetPosition(imageView);
            mousePointer = ZoomImage(LeftImage, mousePointer);
            mousePointer = ZoomImage(RightImage, mousePointer);
        }

        private Point ZoomImage(Image imageView, Point mousePointer)
        {
            var imageSource = imageView.Source;
            if (imageSource is CroppedBitmap)
            {
                imageSource = (imageSource as CroppedBitmap).Source;
            }
            var bmp = imageSource as BitmapImage;
            imageView.Stretch = Stretch.None;
            double height = LeftButton.ActualHeight;
            double width = LeftButton.ActualWidth;
            Point center = new Point(
                mousePointer.X * bmp.PixelWidth / width,
                mousePointer.Y * bmp.PixelHeight / height);
            Point origin = new Point(
                Math.Max(center.X - width / 2, 0),
                Math.Max(center.Y - height / 2, 0));
            height *= bmp.PixelHeight / bmp.Height;
            width *= bmp.PixelWidth / bmp.Width;
            Point extent = new Point(
                Math.Min(origin.X + width, bmp.PixelWidth),
                Math.Min(origin.Y + height, bmp.PixelHeight));
            origin = new Point(
                Math.Max(extent.X - width, 0),
                Math.Max(extent.Y - height, 0));
            var rect = new Int32Rect(
                    (int)Math.Floor(origin.X),
                    (int)Math.Floor(origin.Y),
                    (int)Math.Ceiling(extent.X - origin.X),
                    (int)Math.Ceiling(extent.Y - origin.Y));
            imageView.Source = new CroppedBitmap(bmp, rect);
            imageView.UpdateLayout();
            return mousePointer;
        }

        private void MouseLeaving(object sender, MouseEventArgs e)
        {
            Unzoom(LeftImage);
            Unzoom(RightImage);
        }

        private static void Unzoom(Image imageView)
        {
            imageView.Stretch = Stretch.Uniform;
            if (imageView.Source is CroppedBitmap)
            {
                imageView.Source = (imageView.Source as CroppedBitmap).Source;
            }
        }

        private void ToggleZooming(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                this.Zoom = !this.Zoom;
                if (!Zoom)
                {
                    Unzoom(LeftImage);
                    Unzoom(RightImage);
                }
            }
        }

        private void RejectednessChanged(object sender, RoutedEventArgs e)
        {
            SortList();
        }

        private void DeleteRejects(object sender, RoutedEventArgs e)
        {
            var rejects = photoList
                .Where(p => p.IsRejected && p.SourceUri.IsFile)
                .ToArray();
            if (rejects.Length > 0)
            {
                var result = MessageBox.Show(this, string.Join(", ", rejects.Select(p => p.SourceUri.LocalPath)),
                    "Confirm delete of " + rejects.Length + " files.",
                    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.OK)
                {
                    DisableButtons();
                    if (rejects.Contains(LeftPhoto))
                    {
                        LeftPhoto = null;
                    }
                    if (rejects.Contains(RightPhoto))
                    {
                        RightPhoto = null;
                    }
                    photoList = photoList.Except(rejects).ToArray();
                    SearchForComparisons();
                    foreach (Photo item in rejects)
                    {
                        item.UnloadImage();
                        item.UnloadThumb();
                        try
                        {
                            File.Delete(item.SourceUri.LocalPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, "Error deleting " +
                                item.SourceUri.LocalPath + ": \n" + ex.ToString());
                        }
                    }
                }
            }
        }
    }
}
