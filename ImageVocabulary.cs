using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BabySmash
{
    public enum VocabularyImageTypes
    {
        NONE,
        VectorPNG,
        Emoji,
        INVALID
    }

    public class VocabularyImage
    {
        public VocabularyImageTypes ImageType { get; set; }
        public string Name { get; set; }
        public string Fullname { get; set; }
        public string EmojiChar { get; set; }
        public byte[] ImageBytes { get; set; }
    }

    public class ImageVocabulary
    {
        private static Random rnd = new Random(); // not threadsafe but I think we'll be ok

        string _resourceFileName = "vocabulary.zip";
        public Dictionary<string, List<VocabularyImage>> ImageVocabularyDictionary { get; set; }

        /// <summary>
        /// Keeps track of all vocabulary items and loads them ALL into memory
        /// I've only got a sample corpus of 1-2MB so this shouldn't be an issue for
        /// any reasonable 2010+ PC but you'd probably want to implement some LRU caching
        /// if you wind up using a significantly larger corpus of images
        /// </summary>
        public ImageVocabulary()
        {
            ImageVocabularyDictionary = new Dictionary<string, List<VocabularyImage>>();
            AddResourcePngsToDict();
            LoadEmojiImageDict();

        }

        /// <summary>
        /// Looks through resource vocbulary.zip file for PNGs and adds them to the general VocabularyImageDictionary
        /// </summary>
        private void AddResourcePngsToDict()
        {
            using (ZipArchive za = new ZipArchive(new MemoryStream(Properties.Resources.vocabulary)))
            {
                foreach (var entry in za.Entries)
                {
                    // we only want to read PNG files and we only want to read entries less than
                    // around 256kB. My example files are typically around 20k and max of 55k
                    if (entry.Name.ToLowerInvariant().EndsWith(".png") && entry.Length < 256000)
                    {
                        var stream = entry.Open();
                        byte[] buff = new byte[entry.Length];
                        stream.Read(buff, 0, (int)entry.Length);
                        string simpleName = SimplifyFileName(entry.Name);
                        if (!ImageVocabularyDictionary.ContainsKey(simpleName))
                        {
                            ImageVocabularyDictionary.Add(simpleName, new List<VocabularyImage>());
                        }
                        ImageVocabularyDictionary[simpleName].Add(new VocabularyImage()
                        {
                            ImageType = VocabularyImageTypes.VectorPNG,
                            Name = simpleName,
                            Fullname = entry.Name,
                            ImageBytes = buff
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Strips any extra junk off to get base word from a filename with extended
        /// descriptions
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string SimplifyFileName(string filename)
        {
            string returnString = filename;
            if (filename.IndexOf('_') > 0)
            {
                returnString = filename.Split('_')[0];
            }
            if (filename.IndexOf(".png") > 0)
            {
                returnString = returnString.Replace(".png", "")
;
            }
            return returnString;
        }

        /// <summary>
        /// Returns a random VocabularyImage item for a given word
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public VocabularyImage GetVocabularyImageForWord(string word)
        {
            if (ImageVocabularyDictionary.ContainsKey(word))
            {
                var wordEntries = ImageVocabularyDictionary[word];
                if (wordEntries.Any())
                {
                    return wordEntries.ElementAt(rnd.Next(wordEntries.Count()));
                }
            }

            return null;
        }

        public byte[] GetImageForWord(string word)
        {
            byte[] retBytes = null;
            if (ImageVocabularyDictionary.ContainsKey(word))
            {
                var wordEntries = ImageVocabularyDictionary[word].Where(i => i.ImageBytes != null);
                if (wordEntries.Any())
                {
                    retBytes = wordEntries.ElementAt(rnd.Next(wordEntries.Count())).ImageBytes;
                }
            }
            return retBytes;
        }

        public string GetWordBasedOnFirstLetter(char letter)
        {
            string retWord = null;
            var allWordsForLetter = ImageVocabularyDictionary.Keys.Where(i => i.StartsWith(letter.ToString()));
            if (allWordsForLetter.Any())
            {
                retWord = allWordsForLetter.ElementAt(rnd.Next(allWordsForLetter.Count()));
            }
            return retWord;
        }

        static string getUnicodeString(string ucStr)
        {
            return char.ConvertFromUtf32(int.Parse(ucStr, System.Globalization.NumberStyles.HexNumber)).ToString();
        }

        static KeyValuePair<string, string> createKvpFromLine(string line)
        {
            //"horse_1f40e.png" 
            string[] strSplit = line.Split('_');
            string key = strSplit[0];
            string val = strSplit[1];
            return new KeyValuePair<string, string>(key, getUnicodeString(val));
        }
        public void LoadEmojiImageDict()
        {
            Dictionary<string, string> emojiDict = new Dictionary<string, string>();
            var allLines = System.IO.File.ReadAllLines("emojivocabulary.csv");
            for (int i = 1; i < allLines.Count(); i++)
            {
                try
                {
                    string[] strSplit = allLines[i].Split(',');
                    string name = strSplit[0];
                    string simplename = name.ToUpperInvariant();
                    string hexCode = strSplit[1];
                    string emoji = getUnicodeString(hexCode);
                    bool include = strSplit[3] == "Y" ? true : false;
                    if (include)
                    {
                        if (!ImageVocabularyDictionary.ContainsKey(simplename))
                        {
                            ImageVocabularyDictionary.Add(simplename, new List<VocabularyImage>());
                        }

                        ImageVocabularyDictionary[simplename].Add(new VocabularyImage()
                        {
                            ImageType = VocabularyImageTypes.Emoji,
                            Name = simplename,
                            EmojiChar = emoji,
                            Fullname = hexCode,
                            ImageBytes = null
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Error loading emoji line[{0}]:{1}", i, allLines[i]));
                }
            }
        }
    }
}
