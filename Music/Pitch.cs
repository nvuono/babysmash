using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabySmash.Music
{
    public class Pitch
    {
        public static Dictionary<string,Pitch> StringToPitch = new PitchDictionary().StringToPitch;
        public double Freq { get; set; }
        public string Name { get; set; }

        public Pitch(string pitchName, double freq)
        {
            Freq = freq;
            Name = pitchName;
        }
    }

    public class PitchDictionary
    {
        public Dictionary<string, Pitch> StringToPitch = new Dictionary<string, Pitch>();
        List<Pitch> pitchList = new List<Pitch>();

        public PitchDictionary()
        {
            pitchList = new List<Pitch>() {
                 new Pitch("C0", 16.35),  new Pitch("Cs0/Db0", 17.32),  new Pitch("D0", 18.35),  new Pitch("Ds0/Eb0", 19.45),
 new Pitch("E0", 20.60), new Pitch("F0", 21.83),
 new Pitch("Fs0/Gb0", 23.12),  new Pitch("G0", 24.50),
 new Pitch("Gs0/Ab0", 25.96),  new Pitch("A0", 27.50),
 new Pitch("As0/Bb0", 29.14), new Pitch("B0", 30.87),
 new Pitch("C1", 32.70), new Pitch("Cs1/Db1", 34.65),
 new Pitch("D1", 36.71), new Pitch("Ds1/Eb1", 38.89),
 new Pitch("E1", 41.20), new Pitch("F1", 43.65),
 new Pitch("Fs1/Gb1", 46.25), new Pitch("G1", 49.00),
 new Pitch("Gs1/Ab1", 51.91), new Pitch("A1", 55.00),
 new Pitch("As1/Bb1", 58.27), new Pitch("B1", 61.74),
 new Pitch("C2", 65.41), new Pitch("Cs2/Db2", 69.30),
 new Pitch("D2", 73.42), new Pitch("Ds2/Eb2", 77.78),
 new Pitch("E2", 82.41), new Pitch("F2", 87.31),
 new Pitch("Fs2/Gb2", 92.50), new Pitch("G2", 98.00),
 new Pitch("Gs2/Ab2", 103.83), new Pitch("A2", 110.00),
 new Pitch("As2/Bb2", 116.54), new Pitch("B2", 123.47),
 new Pitch("C3", 130.81), new Pitch("Cs3/Db3", 138.59),
 new Pitch("D3", 146.83), new Pitch("Ds3/Eb3", 155.56),
 new Pitch("E3", 164.81), new Pitch("F3", 174.61),
 new Pitch("Fs3/Gb3", 185.00), new Pitch("G3", 196.00),
 new Pitch("Gs3/Ab3", 207.65), new Pitch("A3", 220.00),
 new Pitch("As3/Bb3", 233.08), new Pitch("B3", 246.94),
 new Pitch("C4", 261.63), new Pitch("Cs4/Db4", 277.18),
 new Pitch("D4", 293.66), new Pitch("Ds4/Eb4", 311.13),
 new Pitch("E4", 329.63), new Pitch("F4", 349.23),
 new Pitch("Fs4/Gb4", 369.99),
 new Pitch("G4", 392.00),
 new Pitch("Gs4/Ab4", 415.30),
 new Pitch("A4", 440.00),
 new Pitch("As4/Bb4", 466.16),
 new Pitch("B4", 493.88),
 new Pitch("C5", 523.25),
 new Pitch("Cs5/Db5", 554.37),
 new Pitch("D5", 587.33),
 new Pitch("Ds5/Eb5", 622.25),
 new Pitch("E5", 659.25),
 new Pitch("F5", 698.46),
 new Pitch("Fs5/Gb5", 739.99),
 new Pitch("G5", 783.99),
 new Pitch("Gs5/Ab5", 830.61),
 new Pitch("A5", 880.00), new Pitch("As5/Bb5", 932.33),
 new Pitch("B5", 987.77),
 new Pitch("C6", 1046.50),
 new Pitch("Cs6/Db6", 1108.73),
 new Pitch("D6", 1174.66),
 new Pitch("Ds6/Eb6", 1244.51),
 new Pitch("E6", 1318.51),
 new Pitch("F6", 1396.91),
 new Pitch("Fs6/Gb6", 1479.98),
 new Pitch("G6", 1567.98),
 new Pitch("Gs6/Ab6", 1661.22),
 new Pitch("A6", 1760.00),
 new Pitch("As6/Bb6", 1864.66),
 new Pitch("B6", 1975.53),
 new Pitch("C7", 2093.00),
 new Pitch("Cs7/Db7", 2217.46),
 new Pitch("D7", 2349.32),
 new Pitch("Ds7/Eb7", 2489.02),
 new Pitch("E7", 2637.02),
 new Pitch("F7", 2793.83),
 new Pitch("Fs7/Gb7", 2959.96),
 new Pitch("G7", 3135.96),
 new Pitch("Gs7/Ab7", 3322.44),
 new Pitch("A7", 3520.00),
 new Pitch("As7/Bb7", 3729.31),
 new Pitch("B7", 3951.07),
 new Pitch("C8", 4186.01),
 new Pitch("Cs8/Db8", 4434.92),
 new Pitch("D8", 4698.63),
 new Pitch("Ds8/Eb8", 4978.03),
 new Pitch("E8", 5274.04),
 new Pitch("F8", 5587.65),
 new Pitch("Fs8/Gb8", 5919.91),
 new Pitch("G8", 6271.93),
 new Pitch("Gs8/Ab8", 6644.88),
 new Pitch("A8", 7040.00),
 new Pitch("As8/Bb8", 7458.62),
 new Pitch("B8", 7902.13)
            };
            foreach(var pitch in pitchList)
            {
                if (pitch.Name.Contains("/")){
                    var split = pitch.Name.Split('/');
                    StringToPitch.Add(split[0], pitch);
                    StringToPitch.Add(split[1], pitch);
                }
                else
                {
                    StringToPitch.Add(pitch.Name, pitch);
                }
            }
        }
    }
}
