using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BabySmash.Properties;

namespace BabySmash
{
    /// <summary>   
    /// Interaction logic for CoolImage.xaml
    /// Includes image colorization logic
    /// </summary>
    [Serializable]
    public partial class CoolImage
    {
        private static Random rnd = new Random();

        // store a copy of what was used to generate the image in case we want to ever backtrack for game/interaction purposes
        private VocabularyImage vocabularyImage = null;
        private System.Windows.Media.Color? imageColor = null;

        public CoolImage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Given a word, perform a vocabulary lookup and find/generated associated image then colorize and render to WPF image field
        /// </summary>
        /// <param name="x"></param>
        /// <param name="itemName"></param>
        /// <param name="templateColor"></param>
        public CoolImage(System.Windows.Media.Brush x, string itemName, System.Windows.Media.Color templateColor) : this()
        {
            // store copy of parameters used to generate this image
            vocabularyImage = Controller.ImageVocab.GetVocabularyImageForWord(itemName);
            if (vocabularyImage != null)
            {
                processImage(x, vocabularyImage, templateColor);
            }
            else
            {
                // if no bitmapFrame then fallback to just drawing a letter
                this.Character = itemName[0];
                this.letterPath.Fill = x;

                this.letterPath.Data = MakeCharacterGeometry(GetLetterCharacter(itemName[0]));
                this.Width = this.letterPath.Data.Bounds.Width + this.letterPath.Data.Bounds.X + this.letterPath.StrokeThickness / 2;
                this.Height = this.letterPath.Data.Bounds.Height + this.letterPath.Data.Bounds.Y + this.letterPath.StrokeThickness / 2;
            }
        }


        /// <summary>
        /// Colorize and setup image for final WPF rendering
        /// </summary>
        /// <param name="x"></param>
        /// <param name="vocabularyImage"></param>
        /// <param name="templateColor"></param>
        void processImage(System.Windows.Media.Brush x, VocabularyImage vocabularyImage, System.Windows.Media.Color templateColor)
        {
            // store copy of parameters used to generate this image
            imageColor = templateColor;

            BitmapFrame bitmapFrame = null; // used for colorizing and ultimately drawing to screen

            if (vocabularyImage.ImageBytes != null)
            {
                Stream imageStream = new MemoryStream(vocabularyImage.ImageBytes); // imagebytes expected to be in Pbgra32 or Rgba64
                PngBitmapDecoder decoder = new PngBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                bitmapFrame = decoder.Frames[0];
            }
            else if (vocabularyImage.EmojiChar != null)
            {
                bitmapFrame = GetBitmapFrameForEmoji(vocabularyImage.EmojiChar);
            }

            if (bitmapFrame != null)
            {
                // if everything worked correctly up to this point we should have a real bitmapFrame
                int iWidth = (int)bitmapFrame.PixelWidth;
                int iHeight = (int)bitmapFrame.PixelHeight;
                Int32 stride = (bitmapFrame.PixelWidth * bitmapFrame.Format.BitsPerPixel + 7) / 8;

                byte[] iPixel = ApplyColoring(templateColor, bitmapFrame, iWidth, iHeight);

                BitmapSource myBitmapSource = bitmapFrame;
                WriteableBitmap wBmp = new WriteableBitmap(myBitmapSource);
                wBmp.WritePixels(new Int32Rect(0, 0, iWidth, iHeight), iPixel, stride, 0);
                MyImage.Source = wBmp;
            }

        }

        /// <summary>
        /// Directly pass a vocabularyImage (emoji or png) to create and colorize
        /// </summary>
        /// <param name="x"></param>
        /// <param name="vocabularyImage"></param>
        /// <param name="templateColor"></param>
        public CoolImage(System.Windows.Media.Brush x, VocabularyImage vocabularyImage, System.Windows.Media.Color templateColor) : this()
        {
            if (vocabularyImage == null) throw new ArgumentNullException("null vocabulary image");
            this.processImage(x, vocabularyImage, templateColor);
        }

        /// <summary>
        /// Renders a given emoji string to a bitmap image and returns for further processing
        /// </summary>
        /// <param name="emojiString"></param>
        /// <returns></returns>
        private BitmapFrame GetBitmapFrameForEmoji(string emojiString)
        {
            BitmapFrame frame;
            string fontName = "Segoe UI Emoji"; // trying options
            fontName = "Segoe Color Emoji"; // can't seem to get the color emojis to render 

            FormattedText text = new FormattedText(emojiString,
                new CultureInfo("en-us"), FlowDirection.LeftToRight,
                new Typeface(new FontFamily(fontName), FontStyles.Normal, FontWeights.Normal, new FontStretch()),
                100, this.Foreground);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawText(text, new Point(2, 2));
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap(180, 180, 120, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bmp));
            frame = bitmapEncoder.Frames[0];
            return frame;
        }

        private static byte[] ApplyColoring(Color templateColor, BitmapFrame frame, int iWidth, int iHeight)
        {
            byte[] iPixel = new byte[iWidth * iHeight * 8];
            Int32 stride = (frame.PixelWidth * frame.Format.BitsPerPixel + 7) / 8;
            if (frame.Format == PixelFormats.Rgba64)
            {
                // PNG images for my vocabulary.zip file are typically in the RGBA64 format
                frame.CopyPixels(iPixel, stride, 0);

                for (int i = 0; i < iPixel.Length; i += 8)
                {
                    int weightThreshold = 1000;
                    uint pixelContent = (((uint)iPixel[i]) << 8) + iPixel[i + 1] + (((uint)iPixel[i + 2]) << 8) + iPixel[i + 3] + (((uint)iPixel[i + 4]) << 8) + iPixel[i + 5];

                    if (pixelContent > weightThreshold)
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
            }else if (frame.Format == PixelFormats.Pbgra32)
            {
                iPixel = new byte[iHeight * stride];
                frame.CopyPixels(iPixel, stride, 0);

                for (int i = 0; i < iPixel.Length; i += 4)
                {
                    int weightThreshold = 2; // if pixel RGB add up to more than 2 we treat it as a 'black' pixel that can be colorized based on templatecolor
                    uint pixelContent = (uint)iPixel[i] + (uint)iPixel[i + 1] + +(uint)iPixel[i + 2] + +(uint)iPixel[i + 3]; //+ (uint)iPixel[i + 2] + (uint)iPixel[i + 3];//(((uint)iPixel[i]) << 8) + iPixel[i + 1] + (((uint)iPixel[i + 2]) << 8) + iPixel[i + 3] + (((uint)iPixel[i + 4]) << 8) + iPixel[i + 5];

                    if (pixelContent > weightThreshold)
                    {
                        iPixel[i + 0] = (byte)(templateColor.ScB * 255);
                        iPixel[i + 1] = (byte)(templateColor.ScG * 255);
                        iPixel[i + 2] = (byte)(templateColor.ScR * 255);
                        iPixel[i + 3] = (byte)(templateColor.ScA * 255);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Unsupported format applying color to image: " + frame.Format);
                throw new ArgumentException("Unsupported format applying color to image: " + frame.Format);
            }
            return iPixel;
        }

        public char Character { get; private set; }

        private static Geometry MakeCharacterGeometry(char character)
        {

            var fontFamily = new System.Windows.Media.FontFamily(Settings.Default.FontFamily);
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