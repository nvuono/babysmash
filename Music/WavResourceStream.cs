using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BabySmash.Music
{
    /// <summary>
    /// From Mark Heath mods in 2008 to babysmash that never made it onto Github 
    /// http://mark-dot-net.blogspot.com/2008/11/using-naudio-to-replace-babysmash-audio.html
    /// </summary>
    class WavResourceStream : WaveStream
    {
        WaveStream sourceStream;

        public WavResourceStream(string resourceName)
        {
            // get the namespace 
            string strNameSpace = Assembly.GetExecutingAssembly().GetName().Name;

            // get the resource into a stream
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(strNameSpace + resourceName);
            sourceStream = new WaveFileReader(stream);
            var format = new WaveFormat(44100, 16, sourceStream.WaveFormat.Channels);

            if (sourceStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                sourceStream = WaveFormatConversionStream.CreatePcmStream(sourceStream);
                sourceStream = new BlockAlignReductionStream(sourceStream);
            }
            if (sourceStream.WaveFormat.SampleRate != 44100 ||
                sourceStream.WaveFormat.BitsPerSample != 16)
            {
                sourceStream = new WaveFormatConversionStream(format, sourceStream);
                sourceStream = new BlockAlignReductionStream(sourceStream);
            }

            sourceStream = new WaveChannel32(sourceStream);
        }

        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        public override long Length
        {
            get { return sourceStream.Length; }
        }

        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return sourceStream.Read(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (sourceStream != null)
            {
                sourceStream.Dispose();
                sourceStream = null;
            }
            base.Dispose(disposing);
        }
    }
}