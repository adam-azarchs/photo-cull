using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoCull {
    public enum PhotoComparison {
        NotCompared = 0,
        Better = 1,
        Worse = 2,

        /// <summary>
        /// Some images are not strictly better or worse.
        /// </summary>
        Different = 3
    }

    public class Photo : IComparable<Photo> {
        public const int ThumbSize = 92;

        public string FilePath {
            get; private set;
        }
        private BitmapImage image;
        public BitmapImage Image {
            get {
                if (image == null) {
                    LoadImage();
                }

                return image;
            }
            private set {
                image = value;
            }
        }
        public BitmapImage Thumb {
            get; private set;
        }
        public bool IsRejected {
            get; set;
        }
        public readonly Dictionary<Photo, PhotoComparison> Comparisons
            = new Dictionary<Photo, PhotoComparison>();

        static readonly Pen strikePen = new Pen(Brushes.Red, 5);
        private readonly Uri sourceUri;

        public Uri SourceUri {
            get {
                return sourceUri;
            }
        }


        public Photo(string fileName) {
            FilePath = System.IO.Path.GetFileName(fileName);
            this.sourceUri = new Uri("file://" + fileName);
            LoadImage();
            double thumbScale = (double)ThumbSize / (double)Math.Max(Image.PixelHeight, Image.PixelWidth);
            Thumb = new BitmapImage();
            Thumb.BeginInit();
            Thumb.CacheOption = BitmapCacheOption.OnLoad;
            Thumb.DecodePixelHeight = (int)Math.Round(Image.PixelHeight * thumbScale);
            Thumb.DecodePixelWidth = (int)Math.Round(Image.PixelWidth * thumbScale);
            Thumb.UriSource = this.sourceUri;
            Thumb.EndInit();
            UnloadImage();
            IsRejected = false;
        }

        public void UnloadImage() {
            Image = null;
        }

        public void LoadImage() {
            Image = new BitmapImage();
            Image.BeginInit();
            Image.CacheOption = BitmapCacheOption.OnLoad;
            Image.UriSource = this.sourceUri;
            Image.EndInit();
        }

        public TextDecorationCollection TextDecorations {
            get {
                TextDecorationCollection collection = new TextDecorationCollection();
                if (IsRejected) {
                    collection.Add(new TextDecoration(TextDecorationLocation.Strikethrough, strikePen,
                        0, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended));
                }
                return collection;
            }
        }

        public int CompareTo(Photo other) {
            if (Comparisons.ContainsKey(other)) {
                if (Comparisons[other] == PhotoComparison.Better) {
                    return -1;
                } else if (Comparisons[other] == PhotoComparison.Worse) {
                    return 1;
                }
            }
            if (IsRejected && !other.IsRejected) {
                return 1;
            }

            if (!IsRejected && other.IsRejected) {
                return -1;
            }

            return Math.Sign(worseness(this, other) - worseness(other, this));
        }

        private double worseness(Photo lhs, Photo rhs) {
            double worseness = 0.0;
            foreach (var comparison in lhs.Comparisons
                .Where(c => c.Value != PhotoComparison.NotCompared)) {
                if (comparison.Value == PhotoComparison.Better) {
                    worseness -= 0.5;
                } else if (comparison.Value == PhotoComparison.Worse) {
                    worseness += 0.5;
                } else if (comparison.Value == PhotoComparison.Different) {
                    worseness -= 0.25;
                }
                if (rhs.Comparisons.ContainsKey(comparison.Key) &&
                    rhs.Comparisons[comparison.Key] != PhotoComparison.NotCompared) {
                    if (rhs.Comparisons[comparison.Key] == PhotoComparison.Worse) {
                        worseness -= 0.5;
                    }
                    if (rhs.Comparisons[comparison.Key] == PhotoComparison.Better) {
                        worseness += 0.5;
                    }
                }
            }
            return worseness;
        }

        public override string ToString() {
            return this.FilePath;
        }

        internal void UnloadThumb() {
            this.Thumb = null;
        }
    }
}
