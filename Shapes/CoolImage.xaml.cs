using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BabySmash.Properties;

namespace BabySmash
{
    /// <summary>   
    /// Interaction logic for CoolImage.xaml
    /// </summary>
    [Serializable]
    public partial class CoolImage
    {
        public CoolImage()
        {
            this.InitializeComponent();
        }

        public CoolImage(System.Windows.Media.Brush x, char letter, string itemName, Color templateColor) : this()
        {
            string imgPath = "C:\\projects\\babysmash\\vocabulary\\" + itemName + ".png";
            if (System.IO.File.Exists(imgPath))
            {
                Stream imageStream = new FileStream(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                PngBitmapDecoder decoder = new PngBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                BitmapSource myBitmapSource = decoder.Frames[0];
                WriteableBitmap wBmp = new WriteableBitmap(myBitmapSource);

                int iWidth = (int)decoder.Frames[0].Width;
                int iHeight = (int)decoder.Frames[0].Height;
                byte[] iPixel = new byte[iWidth * iHeight * 8];
                decoder.Frames[0].CopyPixels(iPixel, iWidth * 8, 0);

                for (int i = 0; i < iPixel.Length; i += 8)
                {
                    uint pixelContent = (((uint)iPixel[i]) << 8) + iPixel[i + 1] + (((uint)iPixel[i + 2]) << 8) + iPixel[i + 3] + (((uint)iPixel[i + 4]) << 8) + iPixel[i + 5];

                    if (pixelContent > 1000)
                    {
                        iPixel[i] = (byte)((((UInt16)(templateColor.ScR * 65535)) & 0xFF00) >> 8);
                        iPixel[i + 1] = (byte)((((UInt16)(templateColor.ScR * 65535)) & 0xFF));

                        iPixel[i + 2] = (byte)((((UInt16)(templateColor.ScG * 65535)) & 0xFF00) >> 8);
                        iPixel[i + 3] = (byte)((((UInt16)(templateColor.ScG * 65535)) & 0xFF));

                        iPixel[i + 4] = (byte)((((UInt16)(templateColor.ScB * 65535)) & 0xFF00) >> 8);
                        iPixel[i + 5] = (byte)((((UInt16)(templateColor.ScB * 65535)) & 0xFF));

                        iPixel[i + 6] = (byte)((((UInt16)(templateColor.ScA * 65535)) & 0xFF00) >> 8); ;
                        iPixel[i + 7] = (byte)((((UInt16)(templateColor.ScA * 65535)) & 0xFF)); ;
                    }
                }
                wBmp.WritePixels(new Int32Rect(0, 0, iWidth, iHeight), iPixel, iWidth * 8, 0);
                MyImage.Source = wBmp;
            }
            else
            {
                this.Character = letter;
                this.letterPath.Fill = x;

                this.letterPath.Data = MakeCharacterGeometry(GetLetterCharacter(letter));
                this.Width = this.letterPath.Data.Bounds.Width + this.letterPath.Data.Bounds.X + this.letterPath.StrokeThickness / 2;
                this.Height = this.letterPath.Data.Bounds.Height + this.letterPath.Data.Bounds.Y + this.letterPath.StrokeThickness / 2;
            }
        }

        public char Character { get; private set; }

        private static Geometry MakeCharacterGeometry(char character)
        {

            var fontFamily = new FontFamily(Settings.Default.FontFamily);
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
            var formattedText = new FormattedText(
                character.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                300,
                Brushes.Black);
            return formattedText.BuildGeometry(new Point(0, 0)).GetAsFrozen() as Geometry;
        }

        private static char GetLetterCharacter(char name)
        {
            Debug.Assert(name == char.ToUpperInvariant(name), "Always provide uppercase character names to this method.");

            if (Settings.Default.ForceUppercase)
            {
                return name;
            }

            // Return a random uppercase or lowercase letter.
            return Utils.GetRandomBoolean() ? name : char.ToLowerInvariant(name);
        }
    }
}