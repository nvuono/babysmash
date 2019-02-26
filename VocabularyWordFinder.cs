using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabySmash
{
    public class VocabularyWordFinder
    {
        private Random rand = new Random();
        List<string> pictureWords = new List<string>();

        public VocabularyWordFinder(string wordsFilePath)
        {
            pictureWords.Clear();
            var allFiles = System.IO.Directory.GetFileSystemEntries("C:\\projects\\babysmash\\vocabulary\\", "*.png");
            foreach (var file in allFiles)
            {
                var fileInfo = new System.IO.FileInfo(file);
                pictureWords.Add(fileInfo.Name.ToUpper());
            }
        }

        public string LastWord(string wordSoFar)
        {
            string retString = null;
            if (wordSoFar != null)
            {
                if (pictureWords.Any(i => i[0] == wordSoFar[0]))
                {
                    var letterMatches = pictureWords.Where(i => i[0] == wordSoFar[0]);
                    int selectedMatch = rand.Next(letterMatches.Count());
                    string strMatch = letterMatches.ElementAt(selectedMatch);
                    strMatch = strMatch.Substring(0, strMatch.Length - 4);
                    retString = strMatch;
                }
            }
            return retString;
        }

    }


}
