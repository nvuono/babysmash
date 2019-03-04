using BabySmash.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
    public enum DemandState
    {
        Startup, Pending, Presented,
        SucceededLast, FailedLast,
        FinalSuccess, FinalFailure,
        INVALID
    }

    class DemandController
    {
        private readonly List<MainWindow> windows = new List<MainWindow>();
        protected DemandState controllerState = DemandState.Startup;

        int ticksInCurrentState = 0;


        List<UserControl> DemandControls { get; set; }
        private Dictionary<string, List<UserControl>> figuresUserControlQueue = new Dictionary<string, List<UserControl>>();

        public DemandController()
        {

        }

        public void Tick()
        {
            Debug.WriteLine("Tick #" + ticksInCurrentState + " in demandController: " + controllerState);
            switch (controllerState)
            {
                case DemandState.Startup:
                    controllerState = DemandState.Pending;
                    ticksInCurrentState = 0;
                    break;
                case DemandState.Pending:
                    break;
                case DemandState.FinalSuccess:
                    controllerState = DemandState.Pending;
                    ticksInCurrentState = 0;
                    break;
                default:
                    break;
            }
            ticksInCurrentState++;
        }

        public void DrawDemand(FrameworkElement uie, char c)
        {
            FigureTemplate template = FigureGenerator.GenerateFigureTemplate(c);
            foreach (MainWindow window in this.windows)
            {
                UserControl f = FigureGenerator.NewUserControlFrom(template);
                window.AddFigure(f);

                var queue = figuresUserControlQueue[window.Name];
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

                if (queue.Count > Settings.Default.ClearAfter)
                {
                    window.RemoveFigure(queue[0]);
                    queue.RemoveAt(0);
                }
            }

            // Find the last word typed, if applicable.
            string lastWord = null;// this.wordFinder.LastWord(figuresUserControlQueue.Values.First());
            if (lastWord != null)
            {
                foreach (MainWindow window in this.windows)
                {
                    //this.wordFinder.AnimateLettersIntoWord(figuresUserControlQueue[window.Name], lastWord);
                }

                //SpeakString(lastWord);
            }
            else
            {
                //PlaySound(template);
            }
        }
    }
}