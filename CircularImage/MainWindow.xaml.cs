using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Code to create the Circular view of an image. 
// Written by Amarnath S, April 2019.
// amarnaths.codeproject@gmail.com

namespace CircularImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapSource originalImage;      // Bitmap for the original image.
        BitmapSource transformedImage;           // Bitmap for the new image.

        byte[] originalPixels;
        int origStride;
        byte[] newPixels;

        // Lists of red, green and blue pixels in original image.
        List<byte> pixels8Red;
        List<byte> pixels8Green;
        List<byte> pixels8Blue;

        // Lists of red, green and blue pixels in new image.
        List<byte> pixels8RedNew;
        List<byte> pixels8GreenNew;
        List<byte> pixels8BlueNew;

        string fileName;                
        int originalWidth, originalHeight;
        int newWidth = 600;
        int newHeight = 600;
        int newStride;

        byte redBack, greenBack, blueBack;
        Color colour;

        public MainWindow()
        {
            pixels8Red = new List<byte>();
            pixels8Green = new List<byte>();
            pixels8Blue = new List<byte>();

            pixels8RedNew = new List<byte>();
            pixels8GreenNew = new List<byte>();
            pixels8BlueNew = new List<byte>();

            redBack = 66;
            greenBack = 66;
            blueBack = 66;
            colour = new Color();
            colour = Color.FromRgb(redBack, greenBack, blueBack);
        }

        /// <summary>
        /// Method to read in an image.
        /// </summary>
        private bool ReadImage(string fn, string fileNameOnly)
        {
            pixels8Red.Clear();
            pixels8Green.Clear();
            pixels8Blue.Clear();
            if (originalPixels != null)
            {
                Array.Clear(originalPixels, 0, originalPixels.Length);
            }

            bool retVal = false;
            // Open the image
            Uri imageUri = new Uri(fn, UriKind.RelativeOrAbsolute);
            originalImage = new BitmapImage(imageUri);
            origStride = (originalImage.PixelWidth * originalImage.Format.BitsPerPixel + 7) / 8;
            originalWidth = originalImage.PixelWidth;
            originalHeight = originalImage.PixelHeight;

            if ((originalImage.Format == PixelFormats.Bgra32) ||
                (originalImage.Format == PixelFormats.Bgr32))
            {
                originalPixels = new byte[origStride * originalHeight];
                // Read in pixel values from the image
                originalImage.CopyPixels(Int32Rect.Empty, originalPixels, origStride, 0);
                Title = "Circular View of: " + fileNameOnly;
                PopulatePixels(originalImage.Format.BitsPerPixel);
                retVal = true;
            }
            else
            {
                MessageBox.Show("Sorry, I don't support this image format.");
            }

            return retVal;
        }

        void PopulatePixels(int bitsPerPixel)
        {
            byte red, green, blue;

            if (bitsPerPixel == 24) // 24 bits per pixel 
            {
                for (int i = 0; i < originalPixels.Count(); i += 3)
                {
                    // In a 24-bit per pixel image, the bytes are stored in the order 
                    // BGR - Blue Green Red order.
                    blue = (byte)(originalPixels[i]);
                    green = (byte)(originalPixels[i + 1]);
                    red = (byte)(originalPixels[i + 2]);

                    pixels8Red.Add(red);
                    pixels8Green.Add(green);
                    pixels8Blue.Add(blue);
                }
            }

            if (bitsPerPixel == 32) // 32 bits per pixel
            {
                for (int i = 0; i < originalPixels.Count(); i += 4)
                {
                    // In a 32-bit per pixel image, the bytes are stored in the order 
                    // BGR - Blue Green Red Alpha order.
                    blue = (byte)(originalPixels[i]);
                    green = (byte)(originalPixels[i + 1]);
                    red = (byte)(originalPixels[i + 2]);

                    pixels8Red.Add(red);
                    pixels8Green.Add(green);
                    pixels8Blue.Add(blue);
                }
            }
        }

        private void bnOpen_Click(object sender, RoutedEventArgs e)
        {
            // Read in the image
            OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter =
                "All Image Files(*.bmp;*.png;*.tif;*.jpg)|*.bmp;*.png;*.tif;*.jpg|24-Bit Bitmap(*.bmp)|*.bmp|PNG(*.png)|*.png|TIFF(*.tif)|*.tif|JPEG(*.jpg)|*.jpg";
            Nullable<bool> result = ofd.ShowDialog();

            try
            {
                if (result == true)
                {
                    fileName = ofd.FileName;
                    Mouse.OverrideCursor = Cursors.Wait;
                    if (ReadImage(fileName, ofd.SafeFileName) == true)
                    {
                        bnSaveImage.IsEnabled = true;
                        ComputeCircularImage();
                        UpdateImage();
                    }
                    else
                    {
                        MessageBox.Show("Sorry, I'm unable to open this image!");
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Sorry, this does not seem to be an image. Please open an image!");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Computes the new circular image.
        /// </summary>
        private void ComputeCircularImage()
        {
            int bitsPerPixel = 24;
            newStride = (newWidth * bitsPerPixel + 7) / 8;
            if (newPixels != null)
            {
                Array.Clear(newPixels, 0, newPixels.Length);
            }
            newPixels = new byte[newStride * newHeight];
            
            int r1 = (int)sliderR1.Value;
            int r2 = (int)sliderR2.Value;
            int thetai1 = (int)sliderTheta1.Value;
            int thetai2 = (int)sliderTheta2.Value;

            pixels8RedNew.Clear();
            pixels8GreenNew.Clear();
            pixels8BlueNew.Clear();

            // Fill the pixels of the new image with the background colour. 
            for (int i = 0; i < newWidth * newHeight; ++i)
            {
                pixels8RedNew.Add(redBack);
                pixels8GreenNew.Add(greenBack);
                pixels8BlueNew.Add(blueBack);
            }

            double theta1 = thetai1 * Math.PI / 180.0; // radians
            double theta2 = thetai2 * Math.PI / 180.0; // radians

            double r, theta; // theta in radians
            int r21 = r2 - r1;
            double theta21 = theta2 - theta1; // radians
            double origX, origY;

            int x1, y1, x2, y2, y3, sourceIndex, targetIndex;
            int xOrigInt, yOrigInt;
            byte redSource, greenSource, blueSource;
            int noOrigPixels = originalHeight * originalWidth;

            for (y1 = 0; y1 < newHeight; ++y1)
            {
                for (x1 = 0; x1 < newWidth; ++x1)
                {
                    x2 = x1 - newWidth / 2;
                    y2 = y1 - newHeight / 2;
                    y3 = -y2;

                    r = Math.Sqrt(x2 * x2 + y2 * y2);
                    theta = Math.Atan2(y3, x2); // radians
                    if (theta < 0) theta = 2.0 * Math.PI + theta; // To convert theta to the range 0 to 2 * PI radians

                    if ((r1 <= r) && (r <= r2) && (theta1 <= theta) && (theta <= theta2))
                    {
                        // Formulas to calculate the original pixel location
                        origX = originalWidth * (theta2 - theta) / theta21;
                        origY = originalHeight * (r2 - r) / r21;

                        // Nearest Neighbour Assignment of Pixels
                        xOrigInt = (int)(Math.Round(origX));
                        yOrigInt = (int)(Math.Round(origY));

                        sourceIndex = yOrigInt * originalWidth + xOrigInt;

                        if ((sourceIndex >= 0) && (sourceIndex < noOrigPixels))
                        {
                            redSource = pixels8Red[sourceIndex];
                            greenSource = pixels8Green[sourceIndex];
                            blueSource = pixels8Blue[sourceIndex];

                            targetIndex = y1 * newWidth + x1;
                            if (targetIndex < newWidth * newHeight)
                            {
                                pixels8RedNew[targetIndex] = redSource;
                                pixels8GreenNew[targetIndex] = greenSource;
                                pixels8BlueNew[targetIndex] = blueSource;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to create a new bitmap with the new pixel values.
        /// </summary>
        private void UpdateImage()
        {
            int bitsPerPixel = 24;
            int stride = (newWidth * bitsPerPixel + 7) / 8;
            byte[] pixelsToWrite = new byte[stride * newHeight];
            int i1;

            for (int i = 0; i < pixelsToWrite.Count(); i += 3)
            {
                i1 = i / 3;
                pixelsToWrite[i] = pixels8RedNew[i1];
                pixelsToWrite[i + 1] = pixels8GreenNew[i1];
                pixelsToWrite[i + 2] = pixels8BlueNew[i1];
            }

            transformedImage = BitmapSource.Create(newWidth, newHeight, 96, 96, PixelFormats.Rgb24,
                null, pixelsToWrite, stride);
            img.Source = transformedImage;
        }

        private void sliderR2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (newPixels != null)
            {
                ComputeCircularImage();
                UpdateImage();
            }
        }

        private void sliderR1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (newPixels != null)
            {
                ComputeCircularImage();
                UpdateImage();
            }
        }

        private void sliderTheta1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (newPixels != null)
            {
                ComputeCircularImage();
                UpdateImage();
            }
        }

        private void sliderTheta2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (newPixels != null)
            {
                ComputeCircularImage();
                UpdateImage();
            }
        }

        private void bnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (newPixels == null)
            {
                MessageBox.Show("Please open an image first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG Images (.png)|*.png|JPG Images (.jpg)|*.jpg|BMP Images (.bmp)|*.bmp";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            try
            {
                // Process save file dialog box results
                if (result == true)
                {
                    ComputeCircularImage();
                    UpdateImage();
                    string fileToSave = dlg.FileName;
                    // I don't want the original file to be overwritten.
                    // Therefore, if the user inadvertently selects the original filename for saving,
                    // I create the new file name with an underscore _ appended to the filename.
                    if (fileToSave == fileName)
                    {
                        fileToSave = GetNewFileName(fileToSave);
                    }

                    // Save the image
                    string extn = Path.GetExtension(fileToSave);
                    FileStream fs = new FileStream(fileToSave, FileMode.Create);
                    if (extn == ".png")
                    {
                        // Save as PNG
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(transformedImage));
                        encoder.Save(fs);
                    }
                    else if (extn == ".jpg")
                    {
                        // Save as JPG
                        BitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(transformedImage));
                        encoder.Save(fs);
                    }
                    else // if (extn == "bmp")
                    {
                        // Save as BMP
                        BitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(transformedImage));
                        encoder.Save(fs);
                    }
                    fs.Close();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Function to compute the new filename with _ appended to the original filename.
        /// </summary>
        /// <param name="fileForSaving">Old file name</param>
        /// <returns>New file name</returns>
        private string GetNewFileName(string fileForSaving)
        {
            string folderName = Path.GetDirectoryName(fileForSaving);
            string fileOnly = Path.GetFileNameWithoutExtension(fileForSaving);
            string extension = Path.GetExtension(fileForSaving);
            string newFileName = folderName + "\\" + fileOnly + "_";
            newFileName += extension;
            return newFileName;
        }
    }
}