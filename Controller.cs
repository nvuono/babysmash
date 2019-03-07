﻿using BabySmash.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WinForms = System.Windows.Forms;

namespace BabySmash
{
    using Newtonsoft.Json;
    using System.Globalization;
    using System.IO;
    using System.Media;
    using System.Speech.Synthesis;

    public enum ControlModes
    {
        BasicLetter,
        LetterToWord,
        Piano,
        INVALID
    }

    public class Controller
    {
        ControlModes ControlMode = ControlModes.Piano;
        static Random rnd = new Random(); // not thread safe but our babies will be ok
        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static Controller instance = new Controller();
        private static DemandController demandController = new DemandController();
        public static ImageVocabulary ImageVocab = null;

        public bool isOptionsDialogShown { get; set; }
        private bool isDrawing = false;
        private readonly SpeechSynthesizer objSpeech = new SpeechSynthesizer();
        private readonly List<MainWindow> windows = new List<MainWindow>();

        private DispatcherTimer timer = new DispatcherTimer();
        private Queue<Shape> ellipsesQueue = new Queue<Shape>();
        private Dictionary<string, List<UserControl>> figuresUserControlQueue = new Dictionary<string, List<UserControl>>();
        private ApplicationDeployment deployment = null;
        private WordFinder wordFinder = new WordFinder("Words.txt");

        /// <summary>Prevents a default instance of the Controller class from being created.</summary>
        private Controller() { }

        public static Controller Instance
        {
            get { return instance; }
        }

        void deployment_CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            if (e.Error == null && e.UpdateAvailable)
            {
                try
                {
                    MainWindow w = this.windows[0];
                    w.updateProgress.Value = 0;
                    w.UpdateAvailableLabel.Visibility = Visibility.Visible;

                    deployment.UpdateAsync();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(ex.ToString());
                    MainWindow w = this.windows[0];
                    w.UpdateAvailableLabel.Visibility = Visibility.Hidden;
                }
            }
        }

        void deployment_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            MainWindow w = this.windows[0];
            w.updateProgress.Value = e.ProgressPercentage;
        }

        void deployment_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Debug.WriteLine(e.ToString());
                return;
            }
            MainWindow w = this.windows[0];
            w.UpdateAvailableLabel.Visibility = Visibility.Hidden;
        }

        public void Launch()
        {
            ImageVocab = new ImageVocabulary();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1); // 1 second timer tick default
            int windowNumber = 0;

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                deployment = ApplicationDeployment.CurrentDeployment;
                deployment.UpdateCompleted += new System.ComponentModel.AsyncCompletedEventHandler(deployment_UpdateCompleted);
                deployment.UpdateProgressChanged += deployment_UpdateProgressChanged;
                deployment.CheckForUpdateCompleted += deployment_CheckForUpdateCompleted;
                try
                {
                    deployment.CheckForUpdateAsync();
                }
                catch (InvalidOperationException e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }

            foreach (WinForms.Screen s in WinForms.Screen.AllScreens)
            {
                MainWindow m = null;

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    m = new MainWindow(this)
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = 800,
                        Top = 600,
                        Width = 800,
                        Height = 600,
                        WindowStyle = WindowStyle.SingleBorderWindow,
                        ResizeMode = ResizeMode.CanResize,
                        Topmost = true,
                        AllowsTransparency = Settings.Default.TransparentBackground,
                        Background = (Settings.Default.TransparentBackground ? new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)) : Brushes.WhiteSmoke),
                        Name = "Window" + windowNumber++.ToString()
                    };
                }
                else
                {
                    m = new MainWindow(this)
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = s.WorkingArea.Left,
                        Top = s.WorkingArea.Top,
                        Width = s.WorkingArea.Width,
                        Height = s.WorkingArea.Height,
                        WindowStyle = WindowStyle.None,
                        ResizeMode = ResizeMode.NoResize,
                        Topmost = true,
                        AllowsTransparency = Settings.Default.TransparentBackground,
                        Background = (Settings.Default.TransparentBackground ? new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)) : Brushes.WhiteSmoke),
                        Name = "Window" + windowNumber++.ToString()
                    };
                }

                figuresUserControlQueue[m.Name] = new List<UserControl>();

                m.Show();
                m.MouseLeftButtonDown += HandleMouseLeftButtonDown;
                m.MouseWheel += HandleMouseWheel;
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    m.WindowState = WindowState.Normal;
                }
                else
                {
                    m.WindowState = WindowState.Maximized;
                }

                windows.Add(m);
            }

            //Only show the info label on the FIRST monitor.
            windows[0].infoLabel.Visibility = Visibility.Visible;

            //Startup sound
            if (Properties.Settings.Default.PlayStartupSound)
            {
                Win32Audio.PlayWavResourceYield("EditedJackPlaysBabySmash.wav");
            }

            string[] args = Environment.GetCommandLineArgs();
            string ext = System.IO.Path.GetExtension(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);

            if (ApplicationDeployment.IsNetworkDeployed && (ApplicationDeployment.CurrentDeployment.IsFirstRun || ApplicationDeployment.CurrentDeployment.UpdatedVersion != ApplicationDeployment.CurrentDeployment.CurrentVersion))
            {
                //if someone made us a screensaver, then don't show the options dialog.
                if ((args != null && args[0] != "/s") && String.CompareOrdinal(ext, ".SCR") != 0)
                {
                    ShowOptionsDialog();
                }
            }
            timer.Start();
        }

        /// <summary>
        /// single place to add reasons why we shouldn't grab full focus
        /// </summary>
        /// <returns></returns>
        bool shouldGrabFocus()
        {
            if (System.Diagnostics.Debugger.IsAttached || isOptionsDialogShown)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Ticks every second according to interval config, grabs focus back to foreground if necessary and used for processing demand/game state changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timer_Tick(object sender, EventArgs e)
        {
            //Debug.WriteLine("timer_Tick");
            if (shouldGrabFocus())
            {
                try
                {
                    IntPtr windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                    SetForegroundWindow(windowHandle);
                    SetFocus(windowHandle);
                }
                catch (Exception)
                {
                    //Wish me luck!
                }
            }
            // process demands here
            demandController.Tick();

        }

        public void ProcessKey(FrameworkElement uie, KeyEventArgs e)
        {
            if (uie.IsMouseCaptured)
            {
                uie.ReleaseMouseCapture();
            }

            char displayChar = KeyControl.GetDisplayChar(e.Key);
            AddFigure(uie, displayChar);
        }



        private void AddFigure(FrameworkElement uie, char c)
        {
            string word = c.ToString();
            FigureTemplate template = null;
            
            if(ControlMode == ControlModes.Piano)
            {
                PianoControl(c);
            }
            if (ControlMode == ControlModes.LetterToWord)
            {
                string imgWord = ImageVocab.GetWordBasedOnFirstLetter(c);
                word = imgWord;

                if (String.IsNullOrWhiteSpace(word))
                {
                    word = c.ToString();
                }
            }

            template = FigureGenerator.GenerateFigureTemplate(word);

            foreach (MainWindow window in this.windows)
            {
                UserControl f = FigureGenerator.NewUserControlFrom(template);
                var queue = figuresUserControlQueue[window.Name];

                if (f != null)
                {
                    window.AddFigure(f);
                    queue.Add(f);

                    // Letters should already have accurate width and height, but others may them assigned.
                    if (double.IsNaN(f.Width) || double.IsNaN(f.Height))
                    {
                        f.Width = 300;
                        f.Height = 300;
                    }

                    Canvas.SetLeft(f, Utils.RandomBetweenTwoNumbers(0, Convert.ToInt32(window.ActualWidth - f.Width)));
                    Canvas.SetTop(f, Utils.RandomBetweenTwoNumbers(0, Convert.ToInt32(window.ActualHeight - f.Height)));

                    Storyboard storyboard = Animation.CreateDPAnimation(uie, f,
                                    UIElement.OpacityProperty,
                                    new Duration(TimeSpan.FromSeconds(Settings.Default.FadeAfter)), 1, 0);
                    if (Settings.Default.FadeAway) storyboard.Begin(uie);

                    IHasFace face = f as IHasFace;
                    if (face != null)
                    {
                        face.FaceVisible = Settings.Default.FacesOnShapes ? Visibility.Visible : Visibility.Hidden;
                    }
                }
                if (queue.Count > Settings.Default.ClearAfter)
                {
                    window.RemoveFigure(queue[0]);
                    queue.RemoveAt(0);
                }
            }

            // Find the last word typed, if applicable.
            string lastWord = this.wordFinder.LastWord(figuresUserControlQueue.Values.First());
            if (lastWord != null)
            {
                foreach (MainWindow window in this.windows)
                {
                    this.wordFinder.AnimateLettersIntoWord(figuresUserControlQueue[window.Name], lastWord);
                }

                SpeakString(lastWord);
            }
            else
            {
                PlaySound(template);
            }
        }

        private static void PianoControl(char c)
        {
            string pitchString = "";
            switch (c)
            {
                case 'A':
                    pitchString = "A3";
                    break;
                case 'S':
                    pitchString = "B3";
                    break;
                case 'D':
                    pitchString = "C4";
                    break;
                case 'F':
                    pitchString = "D4";
                    break;
                case 'G':
                    pitchString = "E4";
                    break;
                case 'H':
                    pitchString = "F4";
                    break;
                case 'J':
                    pitchString = "G4";
                    break;
                case 'K':
                    pitchString = "A4";
                    break;
                case 'L':
                    pitchString = "B4";
                    break;
                case ';':
                    pitchString = "C5";
                    break;
                case '\'':
                    pitchString = "D5";
                    break;
                case 'Q':
                    pitchString = "Gs3";
                    break;
                case 'W':
                    pitchString = "As3";
                    break;
                case 'R':
                    pitchString = "Cs4";
                    break;
                case 'T':
                    pitchString = "Ds4";
                    break;
                case 'U':
                    pitchString = "Fs4";
                    break;
                case 'I':
                    pitchString = "Gs4";
                    break;
                case 'O':
                    pitchString = "As4";
                    break;
                case '[':
                    pitchString = "Cs5";
                    break;
                case ']':
                    pitchString = "Ds5";
                    break;
            }
            if (!String.IsNullOrWhiteSpace(pitchString))
            {
                BeepBeepWave(300, (Music.Pitch.StringToPitch[pitchString].Freq), 500,0);
            }
        }

        void HandleMouseWheel(object sender, MouseWheelEventArgs e)
        {
            UserControl foo = sender as UserControl; //expected this on Sender!
            if (foo != null)
            {
                if (e.Delta < 0)
                {
                    Animation.ApplyZoom(foo, new Duration(TimeSpan.FromSeconds(0.5)), 2.5);
                }
                else
                {
                    Animation.ApplyZoom(foo, new Duration(TimeSpan.FromSeconds(0.5)), 0.5);
                }
            }
        }

        void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UserControl f = e.Source as UserControl;
            if (f != null && f.Opacity > 0.1) //can it be seen? 
            {
                isDrawing = true; //HACK: This is a cheat to stop the mouse draw action.
                Animation.ApplyRandomAnimationEffect(f, Duration.Automatic);
                PlayLaughter(); //Might be better to re-speak the name, color, etc.
            }
        }

        public void PlaySound(FigureTemplate template)
        {
            if (Settings.Default.Sounds == "Laughter")
            {
                PlayLaughter();
            }
            if (objSpeech != null && Settings.Default.Sounds == "Speech")
            {
                if (ControlMode != ControlModes.Piano)
                {
                    if (template.Letter != null && template.Letter.Length == 1 && Char.IsLetterOrDigit(template.Letter[0]))
                    {

                        SpeakString(template.Letter);

                    }
                    else
                    {
                        SpeakString(GetLocalizedString(Utils.ColorToString(template.Color)) + " " + template.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Returns <param name="key"></param> if value or culture is not found.
        /// </summary>
        public static string GetLocalizedString(string key)
        {
            CultureInfo keyboardLanguage = System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture;
            string culture = keyboardLanguage.Name;
            string path = $@"Resources\Strings\{culture}.json";
            string path2 = @"Resources\Strings\en-EN.json";
            string jsonConfig = null;
            if (File.Exists(path))
            {
                jsonConfig = File.ReadAllText(path);
            }
            else if (File.Exists(path2))
            {
                jsonConfig = File.ReadAllText(path2);
            }

            if (jsonConfig != null)
            {
                Dictionary<string, object> config = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonConfig);
                if (config.ContainsKey(key))
                {
                    return config[key].ToString();
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "No file");
            }

            return key;
        }

        private void PlayLaughter()
        {
            Win32Audio.PlayWavResource(Utils.GetRandomSoundFile());
        }

        private void SpeakString(string s)
        {
            ThreadedSpeak ts = new ThreadedSpeak(s);
            ts.Speak();
        }



        public void ShowOptionsDialog()
        {
            bool foo = Settings.Default.TransparentBackground;
            isOptionsDialogShown = true;
            var o = new Options();
            Mouse.Capture(null);
            foreach (MainWindow m in this.windows)
            {
                m.Topmost = false;
            }
            o.Topmost = true;
            o.Focus();
            o.ShowDialog();
            Debug.Write("test");
            foreach (MainWindow m in this.windows)
            {
                m.Topmost = true;
                //m.ResetCanvas();
            }
            isOptionsDialogShown = false;

            if (foo != Settings.Default.TransparentBackground)
            {
                MessageBoxResult result = MessageBox.Show(
                        "You've changed the Window Transparency Option. We'll need to restart BabySmash! for you to see the change. Pressing YES will restart BabySmash!. Is that OK?",
                        "Need to Restart", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                    System.Windows.Forms.Application.Restart();
                }
            }
        }

        public void MouseDown(MainWindow main, MouseButtonEventArgs e)
        {
            if (isDrawing || Settings.Default.MouseDraw) return;

            // Create a new Ellipse object and add it to canvas.
            Point ptCenter = e.GetPosition(main.mouseCursorCanvas);
            MouseDraw(main, ptCenter);
            isDrawing = true;
            main.CaptureMouse();

            Win32Audio.PlayWavResource("smallbumblebee.wav");
        }

        public void MouseWheel(MainWindow main, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                Win32Audio.PlayWavResourceYield("rising.wav");
            }
            else
            {
                Win32Audio.PlayWavResourceYield("falling.wav");
            }
        }

        public void MouseUp(MainWindow main, MouseButtonEventArgs e)
        {
            isDrawing = false;
            if (Settings.Default.MouseDraw) return;
            main.ReleaseMouseCapture();
        }

        public void MouseMove(MainWindow main, MouseEventArgs e)
        {
            if (isOptionsDialogShown)
            {
                main.ReleaseMouseCapture();
                return;
            }
            if (Settings.Default.MouseDraw && main.IsMouseCaptured == false)
                main.CaptureMouse();

            if (isDrawing || Settings.Default.MouseDraw)
            {
                MouseDraw(main, e.GetPosition(main));
            }

            // Cheesy, but hotkeys are ignored when the mouse is captured.
            // However, if we don't capture and release, the shapes will draw forever.
            if (Settings.Default.MouseDraw && main.IsMouseCaptured)
                main.ReleaseMouseCapture();
        }

        private void MouseDraw(MainWindow main, Point p)
        {
            //randomize at some point?
            Shape shape = new Ellipse
            {
                Stroke = SystemColors.WindowTextBrush,
                StrokeThickness = 0,
                Fill = Utils.GetGradientBrush(Utils.GetRandomColor()),
                Width = 50,
                Height = 50
            };

            ellipsesQueue.Enqueue(shape);
            main.mouseDragCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, p.X - 25);
            Canvas.SetTop(shape, p.Y - 25);

            if (Settings.Default.MouseDraw)
                Win32Audio.PlayWavResourceYield("smallbumblebee.wav");

            if (ellipsesQueue.Count > 30) //this is arbitrary
            {
                Shape shapeToRemove = ellipsesQueue.Dequeue();
                main.mouseDragCanvas.Children.Remove(shapeToRemove);
            }
        }

        //private static void ResetCanvas(MainWindow main)
        //{
        //   main.ResetCanvas();
        //}

        public void LostMouseCapture(MainWindow main, MouseEventArgs e)
        {
            if (Settings.Default.MouseDraw) return;
            if (isDrawing) isDrawing = false;
        }

        public static double[] WaveTable = CreateWavetable();
        public static double[] AsdrTable = CreateAsdrEnvelope();

        public static double[] CreateAsdrEnvelope()
        {
            // sample#, magnitude (0 to 1)
            List<Tuple<double, double>> inVals = new List<Tuple<double, double>>()
            {
                new Tuple<double,double>(0,0.0048667),
                new Tuple<double,double>(427.274, 0.859164),
                new Tuple<double,double>(641.308, 0.574462),
                new Tuple<double,double>(1372.63, 0.563392),
                new Tuple<double,double>(2048, 0.0168068)
            };

            return LinearExpandSampleTable(inVals);
        }

        public static double[] LinearExpandSampleTable(List<Tuple<double, double>> criticalPointList)
        {
            int totalSamples = 2048;
            double[] wavetable = new double[totalSamples];
            int tupleNum = 0;

            double deltaSamples = criticalPointList[tupleNum + 1].Item1 - criticalPointList[tupleNum].Item1;
            double deltaMagnitude = criticalPointList[tupleNum + 1].Item2 - criticalPointList[tupleNum].Item2;
            double startingMagnitude = criticalPointList[tupleNum].Item2;
            double dY = deltaMagnitude / deltaSamples;
            double magnitude = startingMagnitude;

            for (int i = 0; i < totalSamples; i++)
            {
                wavetable[i] = magnitude;
                if (i >= criticalPointList[tupleNum + 1].Item1)
                {
                    tupleNum++;
                    dY = (criticalPointList[tupleNum + 1].Item2 - criticalPointList[tupleNum].Item2) / (criticalPointList[tupleNum + 1].Item1 - criticalPointList[tupleNum].Item1);
                    magnitude = criticalPointList[tupleNum].Item2;
                }
                else
                {
                    magnitude = magnitude + dY;
                }
            }
            return wavetable;
        }

        public static double[] CreateWavetable()
        {
            // sample#, magnitude (0 to 1)
            List<Tuple<double, double>> inVals = new List<Tuple<double, double>>()
            {
                new Tuple<double,double>(0, 0.556951),
                new Tuple<double,double>(46.3906, 0.605547),
                new Tuple<double,double>(117.601, 0.663995),
                new Tuple<double,double>(178.6, 0.710288),
                new Tuple<double,double>(239.452, 0.742053),
                new Tuple<double,double>(335.495, 0.761877),
                new Tuple<double,double>(416.257, 0.767102),
                new Tuple<double,double>(491.632, 0.738403),
                new Tuple<double,double>(526.529, 0.697406),
                new Tuple<double,double>(601.44, 0.622699),
                new Tuple<double,double>(670.867, 0.504384),
                new Tuple<double,double>(680.37, 0.446317),
                new Tuple<double,double>(760.204, 0.359528),
                new Tuple<double,double>(830.046, 0.282376),
                new Tuple<double,double>(910.369, 0.244015),
                new Tuple<double,double>(1046.35, 0.222866),
                new Tuple<double,double>(1137.59, 0.266881),
                new Tuple<double,double>(1248.7, 0.279513),
                new Tuple<double,double>(1344.69, 0.294495),
                new Tuple<double,double>(1416.22, 0.384422),
                new Tuple<double,double>(1467.76, 0.493624),
                new Tuple<double,double>(1513.46, 0.522896),
                new Tuple<double,double>(1573.65, 0.489283),
                new Tuple<double,double>(1618.56, 0.441069),
                new Tuple<double,double>(1688.16, 0.339703),
                new Tuple<double,double>(1743.09, 0.284273),
                new Tuple<double,double>(1828.73, 0.272571),
                new Tuple<double,double>(1909.85, 0.314117),
                new Tuple<double,double>(1976.02, 0.372541),
                new Tuple<double,double>(2047, 0.469684),
            };
            return LinearExpandSampleTable(inVals);
        }

        public static void BeepBeepWave(int Amplitude, double Frequency, int Duration, int Square=0)
        { 
            double A = ((Amplitude * (System.Math.Pow(2, 15))) / 1000) - 1;
            int freqPeriod = (int)(44100 / Frequency);
            int totalSamples = 441 * Duration / 10;
            short Sample = 0;
            int Bytes = totalSamples * 4;
            int[] Hdr = { 0X46464952, 36 + Bytes, 0X45564157, 0X20746D66, 16, 0X20001, 44100, 176400, 0X100004, 0X61746164, Bytes };
            using (MemoryStream MS = new MemoryStream(44 + Bytes))
            {
                using (BinaryWriter BW = new BinaryWriter(MS))
                {
                    for (int I = 0; I < Hdr.Length; I++)
                    {
                        BW.Write(Hdr[I]);
                    }
                    double aFreqPeriod = (A / freqPeriod);
                    double dblSample = 0;
                    for (int T = 0; T < totalSamples; T++)
                    {
                        if (Square == 0)
                        {
                            dblSample =(Math.Abs((T % freqPeriod)) * (aFreqPeriod)); //triangle wave
                        }
                        else
                        {
                            dblSample =((T % freqPeriod) < (aFreqPeriod /2)? A : 0); //square wave
                        }
                        //apply wavetable synthesis
                        int waveTableIndex =(int) (((double)WaveTable.Length) / ((double)freqPeriod)) * (T % freqPeriod);
                        dblSample *= WaveTable[waveTableIndex];
                        // apply ASDR to overall waveform
                        // index into ASDR table = total # of samples / tableLength
                        waveTableIndex = (int) (T * (((double)AsdrTable.Length) / totalSamples));
                        dblSample *= AsdrTable[waveTableIndex];
                        Sample = (short)dblSample;
                        BW.Write(Sample);
                        BW.Write(Sample);
                    }
                    BW.Flush();
                    MS.Seek(0, SeekOrigin.Begin);
                    using (SoundPlayer SP = new SoundPlayer(MS))
                    {
                        SP.Play();
                    }
                }
            }
        }

        public static void BeepBeep(int Amplitude, double Frequency, int Duration)
        {
            double A = ((Amplitude * (System.Math.Pow(2, 15))) / 1000) - 1;
            double DeltaFT = 2 * Math.PI * Frequency / 44100.0;
            int Samples = 441 * Duration / 10;
            int Bytes = Samples * 4;
            int[] Hdr = { 0X46464952, 36 + Bytes, 0X45564157, 0X20746D66, 16, 0X20001, 44100, 176400, 0X100004, 0X61746164, Bytes };
            using (MemoryStream MS = new MemoryStream(44 + Bytes))
            {
                using (BinaryWriter BW = new BinaryWriter(MS))
                {
                    for (int I = 0; I < Hdr.Length; I++)
                    {
                        BW.Write(Hdr[I]);
                    }
                    for (int T = 0; T < Samples; T++)
                    {
                        short Sample = System.Convert.ToInt16(A * Math.Sin(DeltaFT * T)); //sine
                        BW.Write(Sample);
                        BW.Write(Sample);
                    }
                    BW.Flush();
                    MS.Seek(0, SeekOrigin.Begin);
                    using (SoundPlayer SP = new SoundPlayer(MS))
                    {
                        SP.Play();
                    }
                }
            }
        }

            public void testWaveOutput(double freq)
        {
            //https://stackoverflow.com/a/30939615
            //your wav streams
            MemoryStream wavNoHeader1 = new MemoryStream();
            BinaryWriter bw1 = new BinaryWriter(wavNoHeader1);

            ushort numsamples = 64000;
            double Vpp = 10000;

            short[] xvals;
            for (int i = 0; i < numsamples; i++)
            {
                double time = (double)(i / 44100.0);
                short y1 = (short)(Vpp * (Math.Sin(freq * 2.0 * Math.PI * time)));
                bw1.Write(y1);
            }
            wavNoHeader1.Position = 0;
            //result WAV stream
            MemoryStream wav = new MemoryStream();
            //create & write header
            ushort numchannels = 1;
            ushort samplelength = 2; // in bytes
            uint samplerate = 44100;
            int wavsize = (int)((wavNoHeader1.Length) / (numchannels * samplelength));
            BinaryWriter wr = new BinaryWriter(wav);
            wr.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            wr.Write(36 + wavsize);
            wr.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            wr.Write(16);
            wr.Write((ushort)1);
            wr.Write(numchannels);
            wr.Write(samplerate);
            wr.Write(samplerate * samplelength * numchannels); //byterate per second
            wr.Write(samplelength * numchannels); //blockalign
            wr.Write((ushort)(8 * samplelength)); //bitsPerSample
            wr.Write(System.Text.Encoding.ASCII.GetBytes("data")); //subchunk2ID
            wr.Write(numchannels * numsamples * samplelength); //subchunk2size = NumSamples * NumChannels * BitsPerSample/8 number of bytes in the data
            //append data from raw streams
            wavNoHeader1.CopyTo(wav);
            //play
            wav.Position = 0;
            SoundPlayer sp = new SoundPlayer(wav);
            sp.PlaySync();
        }
    }
}