using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PhotoCull {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private Photo[] photoList;

        private Photo leftPhoto;

        public Photo LeftPhoto {
            get {
                return leftPhoto;
            }
            set {
                if (value != leftPhoto && value != rightPhoto && leftPhoto != null) {
                    leftPhoto.UnloadImage();
                }

                leftPhoto = value;
            }
        }

        private Photo rightPhoto;

        public Photo RightPhoto {
            get {
                return rightPhoto;
            }
            set {
                if (value != leftPhoto && value != rightPhoto && rightPhoto != null) {
                    rightPhoto.UnloadImage();
                }

                rightPhoto = value;
            }
        }

        public bool Zoom {
            get; set;
        }

        private void chooseImages(object sender, RoutedEventArgs e) {
            const string jpegExtensions = "*.jpg;*.jpeg";
            const string rawExtensions = "*.dng;*.DNG;" +
                "*.crw;*.CR2;*.MRW;*.3fr;*.ari;*.arw;*.srf;*.sr2;*.bay;*.cri;" +
                "*.cap;*.iiq;*.eip;*.erf;*.fff;*.mef;*.mdc;*.mos;*.nef;*.nrw;" +
                "*.dcs;*.dcr;*.drf;*.k25;*.kdc;*.orf;*.pef;*.ptx;*.pxn;*.R3D;" +
                "*.raf;*.raw;*.rw2;*.rwl;*.rwz;*.srw;*.x3f";
            const string tiffExtensions = "*.tif;*.tiff";
            OpenFileDialog dialog = new OpenFileDialog {
                CheckFileExists = true,
                Filter = "All images|" +
                         jpegExtensions + ";" + rawExtensions + ";" + tiffExtensions +
                         "|Jpeg images|" + jpegExtensions + "|RAW images|" + rawExtensions +
                         "|TIFF images|" + tiffExtensions,
                Multiselect = true,
                Title = "Choose files to compare...",
                ShowReadOnly = false,
                ValidateNames = true
            };
            if ((dialog.ShowDialog(this) ?? false) && dialog.FileNames.Length > 1) {
                photoList = dialog.FileNames.Select(f => new Photo(f)).ToArray();
                searchForComparisons();
            }
        }

        private void searchForComparisons() {
            sortList();
            if (!searchOnce(false) && !searchOnce(true)) {
                foreach (Photo item in photoList) {
                    foreach (var comparison in item.Comparisons.ToArray()) {
                        if (comparison.Value == PhotoComparison.Different) {
                            item.Comparisons[comparison.Key] = PhotoComparison.NotCompared;
                            comparison.Key.Comparisons[item] = PhotoComparison.NotCompared;
                        }
                    }
                }
                if (!searchOnce(false)) {
                    searchOnce(true);
                }
            }
        }

        private bool searchOnce(bool allowRejected) {
            foreach (Photo lhs in photoList) {
                foreach (Photo rhs in photoList.Reverse()) {
                    if (isValidComparison(lhs, rhs, allowRejected)) {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool isValidComparison(Photo lhs, Photo rhs, bool allowRejected) {
            if (rhs == lhs) {
                return false;
            }

            if (!allowRejected && (rhs.IsRejected || lhs.IsRejected)) {
                return false;
            }

            if (!lhs.Comparisons.ContainsKey(rhs) ||
                lhs.Comparisons[rhs] == PhotoComparison.NotCompared) {
                LeftPhoto = lhs;
                RightPhoto = rhs;
                showPhotos();
                return true;
            }
            return false;
        }

        private void showPhotos() {
            if (LeftPhoto != null) {
                this.LeftImage.Source = LeftPhoto.Image;
                this.LeftImageNameText.Text = LeftPhoto.FilePath;
                this.LeftImageNameText.TextDecorations = LeftPhoto.TextDecorations;
            } else {
                this.LeftImage.Source = null;
            }
            if (RightPhoto != null) {
                this.RightImage.Source = RightPhoto.Image;
                this.RightImageNameText.Text = RightPhoto.FilePath;
                this.RightImageNameText.TextDecorations = RightPhoto.TextDecorations;
            } else {
                this.RightImage.Source = null;
            }
            if (RightPhoto != null && LeftPhoto != null) {
                LeftButton.IsEnabled = true;
                RightButton.IsEnabled = true;
                KeepBothButton.IsEnabled = true;
            }
        }

        private void sortList() {
            Array.Sort(photoList);
            this.FileListBox.ItemsSource = new Photo[0];
            this.FileListBox.ItemsSource = photoList;
            this.FileListBox.UpdateLayout();
        }

        private void keepBothClick(object sender, RoutedEventArgs e) {
            disableButtons();

            LeftPhoto.Comparisons[RightPhoto] = PhotoComparison.Different;
            RightPhoto.Comparisons[LeftPhoto] = PhotoComparison.Different;
            searchForComparisons();
        }

        private void disableButtons() {
            LeftButton.IsEnabled = false;
            RightButton.IsEnabled = false;
            KeepBothButton.IsEnabled = false;
        }

        private void rightImageClick(object sender, RoutedEventArgs e) {
            disableButtons();

            LeftPhoto.Comparisons[RightPhoto] = PhotoComparison.Worse;
            RightPhoto.Comparisons[LeftPhoto] = PhotoComparison.Better;
            LeftPhoto.IsRejected = true;
            RightPhoto.IsRejected = false;
            searchForComparisons();
        }

        private void leftImageClick(object sender, RoutedEventArgs e) {
            disableButtons();

            LeftPhoto.Comparisons[RightPhoto] = PhotoComparison.Better;
            RightPhoto.Comparisons[LeftPhoto] = PhotoComparison.Worse;
            LeftPhoto.IsRejected = false;
            RightPhoto.IsRejected = true;
            searchForComparisons();
        }

        private void listBoxSelection(object sender, SelectionChangedEventArgs e) {
            if (FileListBox.SelectedIndex < 0) {
                return;
            }

            if (RightPhoto != this.photoList[FileListBox.SelectedIndex]) {
                LeftPhoto = this.photoList[FileListBox.SelectedIndex];
            } else if (LeftPhoto != this.photoList[FileListBox.SelectedIndex]) {
                RightPhoto = this.photoList[FileListBox.SelectedIndex];
            }

            showPhotos();
        }

        private void moveZoom(object sender, MouseEventArgs e) {
            Image imageView = sender as Image;
            if (imageView == null) {
                return;
            }

            if (!Zoom) {
                return;
            }

            var mousePointer = e.GetPosition(imageView);
            mousePointer = zoomImage(LeftImage, mousePointer);
            mousePointer = zoomImage(RightImage, mousePointer);
        }

        private Point zoomImage(Image imageView, Point mousePointer) {
            var imageSource = imageView.Source;
            if (imageSource is CroppedBitmap) {
                imageSource = (imageSource as CroppedBitmap).Source;
            }
            var bmp = imageSource as BitmapImage;
            double height = LeftButton.ActualHeight;
            double width = LeftButton.ActualWidth;
            Point center = new Point(
                mousePointer.X * bmp.PixelWidth / width,
                mousePointer.Y * bmp.PixelHeight / height);
            Point origin = new Point(
                Math.Max(center.X - width / 2, 0),
                Math.Max(center.Y - height / 2, 0));
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

        private void mouseLeaving(object sender, MouseEventArgs e) {
            unzoom(LeftImage);
            unzoom(RightImage);
        }

        private static void unzoom(Image imageView) {
            if (imageView.Source is CroppedBitmap) {
                imageView.Source = (imageView.Source as CroppedBitmap).Source;
            }
        }

        private void toggleZooming(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Right) {
                this.Zoom = !this.Zoom;
                if (!Zoom) {
                    unzoom(LeftImage);
                    unzoom(RightImage);
                } else {
                    zoomImage(LeftImage, e.GetPosition(sender as Button));
                    zoomImage(RightImage, e.GetPosition(sender as Button));
                }
            }
        }

        private void rejectednessChanged(object sender, RoutedEventArgs e) {
            sortList();
        }

        private void deleteRejects(object sender, RoutedEventArgs e) {
            var rejects = photoList
                .Where(p => p.IsRejected && p.SourceUri.IsFile)
                .ToArray();
            if (rejects.Length > 0) {
                var result = MessageBox.Show(this, string.Join(", ", rejects.Select(p => p.SourceUri.LocalPath)),
                    "Confirm delete of " + rejects.Length + " files.",
                    MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.OK) {
                    disableButtons();
                    if (rejects.Contains(LeftPhoto)) {
                        LeftPhoto = null;
                    }
                    if (rejects.Contains(RightPhoto)) {
                        RightPhoto = null;
                    }
                    photoList = photoList.Except(rejects).ToArray();
                    searchForComparisons();
                    foreach (Photo item in rejects) {
                        item.UnloadImage();
                        item.UnloadThumb();
                        try {
                            File.Delete(item.SourceUri.LocalPath);
                        } catch (Exception ex) {
                            MessageBox.Show(this, "Error deleting " +
                                item.SourceUri.LocalPath + ": \n" + ex.ToString());
                        }
                    }
                }
            }
        }
    }
}
