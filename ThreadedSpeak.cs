using BabySmash.Music;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BabySmash
{
     class ThreadedSpeak
    {
        private static InstalledVoice neededVoice = null;

        private string Word = null;
        SpeechSynthesizer SpeechSynth = new SpeechSynthesizer();
        public ThreadedSpeak(string Word)
        {
            this.Word = Word;
            if (neededVoice == null)
            {
                CultureInfo keyboardLanguage = System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture;
                neededVoice = null;
                neededVoice = this.SpeechSynth.GetInstalledVoices(keyboardLanguage).Where(i => i.VoiceInfo.Name == Properties.Settings.Default.KeyboardName).FirstOrDefault();
                if (neededVoice == null)
                {
                    neededVoice = this.SpeechSynth.GetInstalledVoices(keyboardLanguage).LastOrDefault();
                }
            }

            if (neededVoice == null)
            {
                //http://superuser.com/questions/590779/how-to-install-more-voices-to-windows-speech
                //https://msdn.microsoft.com/en-us/library/windows.media.speechsynthesis.speechsynthesizer.voice.aspx
                //http://stackoverflow.com/questions/34776593/speechsynthesizer-selectvoice-fails-with-no-matching-voice-is-installed-or-th
                this.Word = "Unsupported Language";
            }
            else if (!neededVoice.Enabled)
            {
                this.Word = "Voice Disabled";
            }
            else
            {
                try
                {
                    this.SpeechSynth.SelectVoice(neededVoice.VoiceInfo.Name);
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.ToString());
                }
            }

            SpeechSynth.Rate = -1;
            SpeechSynth.Volume = 100;
        }
        public void Speak()
        {
            Thread oThread = new Thread(new ThreadStart(this.Start));
            oThread.Start();
        }
        private void Start()
        {
            try
            {
                var pb1 = new PromptBuilder();
                var pitch = SongProvider.song.GetNextNote().Pitch;
                string freqHz = pitch.Freq.ToString("0") + "Hz";
                pb1.AppendSsmlMarkup("<prosody pitch=\""+freqHz+"\" rate=\"slow\">"+Word+"</prosody >");                
                SpeechSynth.Speak(pb1);
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e.ToString());
            }
        }

    }
}
