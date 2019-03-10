using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace BabySmash
{

    public enum GameTypes
    {
        AlphabetSequence, //Repeats A B C to Z and user presses corresponding letter
        NumberSequence, // Repeats counting 1 to 10 and user presses corresponding key
        SpellWholeWord, // can ask to spell whole word given 3 blanks like CAT presents _ _ _ 
        PressFirstLetterForPicture, // GIven picture of CAT, says "cat", presents "_ A T" and asks for user to press missing letter
        MathCount, // show X number of cats and user has to press number
        MathAddNumbers, // arithmetic presents "1 + 2" and user expected to press 3
        MousePointAtPicture // says a word "Dog" and asks user to point mouse cursor/click on the dog emoji rendered
    }

    public enum DemandState
    {
        Startup, Pending, Presented,
        SucceededLast, FailedLast,
        FinalSuccess, FinalFailure,
        INVALID
    }

    public class DemandCharEntry
    {
        public Control VisualElement { get; set; }
        public char DemandedChar { get; set; }
        public bool DemandSuccessful { get; set; }
        public int DemandFailures { get; set; }

    }

    class DemandController
    {
        private int PENDING_WAIT_TICKS = 5;
        private int FINALSUCCESS_WAIT_TICKS = 5;

        private Controller mainController = null;
        protected DemandState controllerState = DemandState.Startup;

        /// <summary>
        /// Just using this to test out proof of concept for demands, expect something more elegant later
        /// </summary>
        char tempDemandedChar = 'A';

        int ticksInCurrentState = 0;

        List<DemandCharEntry> demandedChars = new List<DemandCharEntry>();
        int demandedCharIndex = 0;
        int totalDemandFailures = 0;

        List<UserControl> DemandControls { get; set; }
        private Dictionary<string, List<Control>> figureControls = new Dictionary<string, List<Control>>();

        public DemandController(Controller mainController)
        {
            this.mainController = mainController;
        }

        /// <summary>
        /// Returns true if mainController speech should be suspended
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool ProcessInput(char c)
        {
            bool retValue = false;
            if (controllerState == DemandState.Presented)
            {
                if(demandedChars!=null && demandedChars.Count >= demandedCharIndex)
                {
                    var dchar = demandedChars[demandedCharIndex];
                    if (dchar.DemandedChar == c)
                    {
                        dchar.DemandSuccessful = true;
                        demandedCharIndex++;
                        if (demandedCharIndex >= demandedChars.Count)
                        {
                            processDemandFinalSuccess(c);
                            retValue = true;
                        }
                    }
                    else
                    {
                        dchar.DemandFailures++;
                        totalDemandFailures++;
                        if (dchar.DemandFailures % 5 == 4)
                        {
                            mainController.SpeakString("Press " + dchar.DemandedChar);
                            retValue = true;
                        }
                    }
                }
            }
            return retValue;
        }

        void processDemandFinalSuccess(char c )
        {
            mainController.SpeakString("Great job, you pressed " + c);
            controllerState = DemandState.FinalSuccess;
        }

        public void Tick()
        {
            //Debug.WriteLine("Tick #" + ticksInCurrentState + " in demandController: " + controllerState);
            switch (controllerState)
            {
                case DemandState.Startup:
                    controllerState = DemandState.Pending;
                    ticksInCurrentState = 0;
                    totalDemandFailures = 0;
                    break;
                case DemandState.Pending:
                    if (ticksInCurrentState > 3) { 
                        DrawDemand();
                        controllerState = DemandState.Presented;
                    }
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

        public void DrawDemand()
        {
            demandedCharIndex = 0;
            TextBox uie = null;

            if (!figureControls.ContainsKey("txtInstruction"))
            {
                uie = new System.Windows.Controls.TextBox();
                figureControls.Add("txtInstruction", new List<Control>() { uie });
                uie.Name = "DemandText";
                uie.Text = "Press " + tempDemandedChar;
                foreach (MainWindow window in mainController.windows)
                {
                    window.AddFigure(uie);

                    Canvas.SetLeft(uie, 10);
                    Canvas.SetTop(uie, 10);
                }
            }
            else
            {
                uie = figureControls["txtInstruction"].FirstOrDefault() as TextBox;
            }

            if (uie != null)
            {
                uie.Text = "Press " + tempDemandedChar;
                demandedChars.Clear();
                demandedChars.Add(new DemandCharEntry() { DemandedChar = tempDemandedChar, VisualElement = uie, DemandSuccessful = false });
                tempDemandedChar++;
                mainController.SpeakString(uie.Text);


            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Lost uie for demand");
            }
        }
    }

}