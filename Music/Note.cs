using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabySmash.Music
{
    public enum NoteDuration
    {
        ThirtySecond,
        Sixteenth,
        Eighth,
        Quarter,
        Whole,
        Double
    }
    public class Note
    {
        public Pitch Pitch { get; set; }
        public NoteDuration Duration { get; set; }
    }

    public static class SongProvider
    {
        public static TestSong song = new TestSong();
    }

    public class TestSong{
        public int CurrentNoteNum { get; set; }
        public List<string> Notes = new List<string>() { "C5", "C5", "G5", "G5", "A6", "A6","G5" };
    public Note GetNextNote()
    {
            var currentNote = new Note()
            {
                Pitch = Pitch.StringToPitch[Notes[CurrentNoteNum++]]
            };
            if (CurrentNoteNum == Notes.Count) CurrentNoteNum = 0;

        return currentNote;
    }
}

}
