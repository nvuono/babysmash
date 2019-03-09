using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BabySmash.Music
{
    public class Synth
    {
        public static readonly MixingSampleProvider msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100,2));
        public static readonly IWavePlayer OutputDevice = new WaveOutEvent();
        public enum WaveTypes
        {
            Sin,
            Triangle,
            Square,
            Sawtooth,
            RawWavetable,
            INVALID
        }

        public static double[] WaveTable = CreateWavetable();
        public static double[] AdsrTable = CreatedAdsrEnvelope();

        /// <summary>
        /// Creates AttackDecaySustainRelease envelope for shaping the final waveform generated for a note
        /// </summary>
        /// <returns></returns>
        public static double[] CreatedAdsrEnvelope()
        {
            // sample#, magnitude (0 to 1)
            List<Tuple<double, double>> inVals = ReadWaveTableFromFile("AdsrEnvelope");
            return LinearExpandSampleTable(inVals, 2048);
        }

        /// <summary>
        /// Given a list of critical points, will expand the set of samples using linear interpolation to fill the given window.
        /// Expect critical point sample indices to be in increasing order
        /// x and y coordinate inputs should be normalized to a 0.0 to 1.0 range
        /// </summary>
        /// <param name="criticalPointList"></param>
        /// <returns></returns>
        public static double[] LinearExpandSampleTable(List<Tuple<double, double>> criticalPointList, int totalSamples)
        {
            double[] wavetable = new double[totalSamples];
            int tupleNum = 0;

            double deltaSamples = totalSamples * (criticalPointList[tupleNum + 1].Item1 - criticalPointList[tupleNum].Item1);
            double deltaMagnitude = criticalPointList[tupleNum + 1].Item2 - criticalPointList[tupleNum].Item2;
            double startingMagnitude = criticalPointList[tupleNum].Item2;
            double dY = deltaMagnitude / deltaSamples;
            double magnitude = startingMagnitude;

            for (int i = 0; i < totalSamples; i++)
            {
                wavetable[i] = magnitude;
                if (i >= totalSamples * criticalPointList[tupleNum + 1].Item1)
                {
                    tupleNum++;
                    dY = (criticalPointList[tupleNum + 1].Item2 - criticalPointList[tupleNum].Item2) / (totalSamples * (criticalPointList[tupleNum + 1].Item1 - criticalPointList[tupleNum].Item1));
                    magnitude = criticalPointList[tupleNum].Item2;
                }
                else
                {
                    magnitude = magnitude + dY;
                }
            }
            return wavetable;
        }

        public static List<Tuple<double,double>> ReadWaveTableFromFile(string tableName)
        {
            List<Tuple<double, double>> retList = new List<Tuple<double, double>>();
            System.IO.StreamReader sr = new System.IO.StreamReader("SynthTables.txt");
            string currLine = "";
            bool readingCorrectTable = false;
            while((currLine = sr.ReadLine()) != null){
                if (currLine == "#" + tableName)
                {
                    readingCorrectTable = true;
                }
                else if (currLine.StartsWith("#"))
                {
                    readingCorrectTable = false;
                    if (retList.Count > 0)
                    {
                        // we've loaded the last element in the requested table and can return it
                        return retList;
                    }
                }
                else if (readingCorrectTable && !String.IsNullOrWhiteSpace(currLine))
                {
                    // if we're in the correct table and don't have a blank line of some kind
                    string[] strSplit = currLine.Split(',');
                    if (strSplit.Length == 2)
                    {
                        retList.Add(new Tuple<double, double>(double.Parse(strSplit[0].Trim()), double.Parse(strSplit[1].Trim())));
                    }
                }
            }
            return retList;
        }

        /// <summary>
        /// Creates a wavetable to either apply multiplicatively to a generated signal 
        /// or use directly with an oscillator at a given pitch
        /// </summary>
        /// <returns></returns>
        public static double[] CreateWavetable()
        {
            // manually drew a waveform and captured x/y for critical points
            // sample#, magnitude (0 to 1)
            List<Tuple<double, double>> inVals = ReadWaveTableFromFile("Wavetable1");
            return LinearExpandSampleTable(inVals, 2048);

        }

        public static void ThreadBeep(double amplitude, double Frequency, int Duration, WaveTypes waveType, bool applyWaveTable = true, bool applyAdsr = true)
        {
            new Task(
                new Action(() =>
            {
                BeepBeepWave(amplitude, Frequency, Duration, waveType, applyWaveTable, applyAdsr);
            })).Start();
        }
        /// <summary>
        /// Amplitude on scale from 0.0 to 1.0
        /// </summary>
        /// <param name="amp"></param>
        /// <param name="Frequency"></param>
        /// <param name="Duration">msec</param>
        /// <param name="waveType"></param>
        public static void BeepBeepWave(double amplitude, double Frequency, int Duration, WaveTypes waveType, bool applyWaveTable = true, bool applyAdsr = true)
        {
            double DeltaFT = 2 * Math.PI * Frequency / 44100.0; // used for sinusoid generation
            //double amplitude = amp;//((Amplitude * (System.Math.Pow(2, 15))) / 1000) - 1;
            int freqPeriod = (int)(44100 / Frequency);
            int totalSamples = 441 * Duration / 10;
            short Sample = 0;
            int Bytes = totalSamples * 8;
            //int[] Hdr = { 0X46464952, 36 + Bytes, 0X45564157, 0X20746D66, 16, 0X20001, 44100, 176400, 0X100004, 0X61746164, Bytes };
            using (MemoryStream MS = new MemoryStream(Bytes))
            {
                using (BinaryWriter BW = new BinaryWriter(MS))
                {
                    //for (int I = 0; I < Hdr.Length; I++)
                    //{
                    //    BW.Write(Hdr[I]);
                    //}
                    waveType = WaveTypes.Sin;
                    //A =2;
                    double amplFreqPeriod = amplitude;// (A / freqPeriod); // used when doing 16 bit pcm wav
                    double dblSample = 0;
                    for (int T = 0; T < totalSamples; T++)
                    {
                        if (waveType == WaveTypes.Triangle)
                        {
                            int tMod = T % freqPeriod; //triangle wave
                            if (tMod < freqPeriod / 2)
                            {
                                dblSample = (Math.Abs((T % freqPeriod) * (2*amplFreqPeriod))); // 
                            }
                            else
                            {
                                dblSample = amplitude -(Math.Abs((T % freqPeriod) * (amplFreqPeriod))); 
                            }
                        }
                        else if (waveType == WaveTypes.Square)
                        {
                            dblSample = ((T % freqPeriod) < (amplFreqPeriod / 2) ? amplitude : 0);
                        }
                        else if (waveType == WaveTypes.RawWavetable)
                        {
                            dblSample = amplitude; // let wavetable apply shaping
                        }
                        else if (waveType == WaveTypes.Sin)
                        {
                            dblSample = 50*DeltaFT + (amplitude * Math.Sin(DeltaFT * T)); //sine
                        }
                        else if (waveType == WaveTypes.Sawtooth)
                        {
                            dblSample = (Math.Abs((T % freqPeriod)) * (amplFreqPeriod)); //sawtooth wave
                        }
                        int waveTableIndex = (int)(((double)WaveTable.Length) / ((double)freqPeriod)) * (T % freqPeriod);
                        dblSample *= WaveTable[waveTableIndex];
                        // apply ADSR to overall waveform
                        waveTableIndex = (int)(T * (((double)AdsrTable.Length) / totalSamples)); // index into ADSR table = total # of samples / tableLength
                        dblSample *= AdsrTable[waveTableIndex];
                        //Sample = (short)dblSample; // might want System.Convert.ToInt16
                        Single sSamp = System.Convert.ToSingle(dblSample);
                        //if (T % 1 == 0) System.Diagnostics.Debug.WriteLine(T + ": " + sSamp);                                                            //apply wavetable synthesis

                        BW.Write(sSamp);
                        BW.Write(sSamp);
                        //Sample = System.Convert.ToInt16(dblSample);
                        //BW.Write(Sample);
                        //BW.Write(Sample);
                    }
                    BW.Flush();
                    MS.Seek(0, SeekOrigin.Begin);
                    var waveFileReader = new RawSourceWaveStream(MS, WaveFormat.CreateIeeeFloatWaveFormat(44100,2));
                    //var waveoutDevice = new WaveOutEvent();
                    //IWaveProvider provider = waveFileReader;// new RawSourceWaveStrem(new MemoryStream(sound), new WaveFormat();
                    //waveoutDevice.Init(waveFileReader);
                    //waveoutDevice.Play();
                    //var sc = new SampleChannel(waveFileReader, true);

                    //                    var sp = mew NAudio.Wave.SampleProviders.Pcm16BitToSampleProvider()
                    if(OutputDevice.PlaybackState!= PlaybackState.Playing)
                    {
                        //msp.ReadFully = true;
                        OutputDevice.Init(msp);
                    }

                    if (false)
                    {
                        //OutputDevice = new WaveOutEvent();
                        //msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                        msp.ReadFully = true;
                        msp.AddMixerInput(waveFileReader);
                        OutputDevice.Init(msp);
                        OutputDevice.Play();
                        //var _format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
                        //msp = new MixingSampleProvider(_format);
                    }
                    else
                    {
                        msp.AddMixerInput(waveFileReader);
                    }
                    msp.AddMixerInput(waveFileReader);
                    msp.ReadFully = true;
                    OutputDevice.Play();
                    System.Threading.Thread.Sleep(Duration);
                    //using (SoundPlayer SP = new SoundPlayer(MS))
                    {
                      //  SP.Play();
                    }
                }
            }//memorystream
        }//end function

        /// <summary>
        /// Generate a graph based on critical points for visualization
        /// </summary>
        /// <param name="iHeight"></param>
        /// <param name="iWidth"></param>
        /// <returns></returns>
        public static BitmapFrame generateGraph(int iHeight, int iWidth)
        {
            double scaledWidth = iWidth - 40; // should probably figure out why this doesn't work when i try to render to the full 180 width
            double scaledHeight = iHeight - 40; // scale height by the same amount even though I had no issues with Y axis
            
            //ADSR envelope graph
            List<Tuple<double, double>> inVals = new List<Tuple<double, double>>()
            {
                new Tuple<double,double>(0,0.0048667),
                new Tuple<double,double>(0.208496, 0.859164),
                new Tuple<double,double>(0.313134, 0.574462),
                new Tuple<double,double>(0.66992, 0.563392),
                new Tuple<double,double>(1, 0.0168068)
            };

            //WaveTable graph
            inVals = new List<Tuple<double, double>>()
            {
                new Tuple<double,double>(0, 0.556951), new Tuple<double,double>(0.02265, 0.605547),
                new Tuple<double,double>(0.0574, 0.663995), new Tuple<double,double>(0.087207, 0.710288),
                new Tuple<double,double>(0.11692, 0.742053), new Tuple<double,double>(0.163816, 0.761877),
                new Tuple<double,double>(0.20325, 0.767102), new Tuple<double,double>(0.240055, 0.738403),
                new Tuple<double,double>(0.257094, 0.697406), new Tuple<double,double>(0.293672, 0.622699),
                new Tuple<double,double>(0.327572, 0.504384), new Tuple<double,double>(0.332212, 0.446317),
                new Tuple<double,double>(0.371193, 0.359528), new Tuple<double,double>(0.405296, 0.282376),
                new Tuple<double,double>(0.444516, 0.244015), new Tuple<double,double>(0.510914, 0.222866),
                new Tuple<double,double>(0.555466, 0.266881), new Tuple<double,double>(0.609716 , 0.279513),
                new Tuple<double,double>(0.656587, 0.294495), new Tuple<double,double>(0.691513, 0.384422),
                new Tuple<double,double>(0.716682, 0.493624), new Tuple<double,double>(0.738993, 0.522896),
                new Tuple<double,double>(0.768384, 0.489283), new Tuple<double,double>(0.790314, 0.441069),
                new Tuple<double,double>(0.824298, 0.339703), new Tuple<double,double>(0.851118, 0.284273),
                new Tuple<double,double>(0.892933, 0.272571), new Tuple<double,double>(0.932546, 0.314117),
                new Tuple<double,double>(0.964854, 0.372541), new Tuple<double,double>(1, 0.469684)
            };

            BitmapFrame frame;
            
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawLine(new Pen(Brushes.Red,1), new System.Windows.Point(0, 0), new System.Windows.Point(scaledWidth, 0));
            drawingContext.DrawLine(new Pen(Brushes.Red, 1), new System.Windows.Point(0, 0), new System.Windows.Point(0, scaledHeight));
            drawingContext.DrawLine(new Pen(Brushes.Red, 1), new System.Windows.Point(scaledWidth, scaledHeight), new System.Windows.Point(scaledWidth, 0));
            drawingContext.DrawLine(new Pen(Brushes.Red, 1), new System.Windows.Point(scaledWidth, scaledHeight), new System.Windows.Point(0, scaledHeight));
            for (int i = 0; i < inVals.Count-1; i++)
            {
                double startX = inVals[i].Item1 * scaledWidth;
                double startY = scaledHeight - (inVals[i].Item2 * scaledHeight);
                double endX = inVals[i+1].Item1 * scaledWidth;
                double endY = scaledHeight - (inVals[i+1].Item2 * scaledHeight);

                drawingContext.DrawLine(new Pen(Brushes.Green, 1), new System.Windows.Point(startX, startY), new System.Windows.Point(endX, endY));
            }

            drawingContext.Close();
            // heigh width 180  by default
            RenderTargetBitmap bmp = new RenderTargetBitmap(iHeight, iWidth, 120, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bmp));
            frame = bitmapEncoder.Frames[0];
            return frame;

        }
    }
}
