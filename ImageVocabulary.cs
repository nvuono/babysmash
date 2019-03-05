using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BabySmash
{
    public class ImageVocabulary
    {
        private static Random rnd = new Random(); // not threadsafe but I think we'll be ok

        string _resourceFileName = "vocabulary.zip";
        Dictionary<string, byte[]> FileNameToByteDict = new Dictionary<string, byte[]>();
        Dictionary<string, List<string>> WordToFileNameDict = new Dictionary<string, List<string>>();
        /// <summary>
        /// Keeps track of all vocabulary items and loads them ALL into memory
        /// I've only got a sample corpus of 1-2MB so this shouldn't be an issue for
        /// any reasonable 2010+ PC but you'd probably want to implement some LRU caching
        /// if you wind up using a significantly larger corpus of images
        /// </summary>
        public ImageVocabulary()
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
                        FileNameToByteDict.Add(entry.Name, buff);
                        if (!WordToFileNameDict.ContainsKey(simpleName))
                        {
                            WordToFileNameDict.Add(simpleName, new List<string>());
                        }
                        WordToFileNameDict[simpleName].Add(entry.Name);
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
            if (filename.IndexOf(".png")>0)
            {
                returnString = returnString.Replace(".png", "")
;            }
            return returnString;
        }

        public  byte[] GetImageForWord(string word)
        {
            byte[] retBytes = null;
            if (WordToFileNameDict.ContainsKey(word) && FileNameToByteDict.ContainsKey(WordToFileNameDict[word].FirstOrDefault())){
                var fileName = WordToFileNameDict[word].FirstOrDefault();
                retBytes = FileNameToByteDict[fileName];
            }
            return retBytes;
        }

        public string GetWordBasedOnFirstLetter(char letter)
        {
            string retWord = null;
            var allWordsForLetter = WordToFileNameDict.Keys.Where(i => i.StartsWith(letter.ToString()));
            if (allWordsForLetter.Any())
            {
                retWord = allWordsForLetter.ElementAt(rnd.Next(allWordsForLetter.Count()));               
            }
            return retWord;
        }


        public static Dictionary<string, string> GetEmojiImageDict()
        {
            string[] strEmojis = "🤖,🐶,🐺,🐱,🦁,🐯,🦒,🦊,🐮,🐷,🐗,🐭,🐹,🐰,🐻,🐨,🐼,🐸,🦓,🐴,🦄,🐔,🐲".Split(',');
            int strCount = 0;
            Dictionary<string, string> emojiDict = new Dictionary<string, string>();
            emojiDict.Add("ghost", "👻");
            emojiDict.Add("robot", strEmojis[strCount++].ToString());
            emojiDict.Add("dog", strEmojis[strCount++].ToString());
            emojiDict.Add("wolf", strEmojis[strCount++].ToString());
            emojiDict.Add("cat", strEmojis[strCount++].ToString());
            emojiDict.Add("lion", strEmojis[strCount++].ToString());
            emojiDict.Add("tiger", strEmojis[strCount++].ToString());
            emojiDict.Add("giraffe", strEmojis[strCount++].ToString());
            emojiDict.Add("fox", strEmojis[strCount++].ToString());
            emojiDict.Add("cow", strEmojis[strCount++].ToString());
            emojiDict.Add("pig", strEmojis[strCount++].ToString());
            emojiDict.Add("panda", strEmojis[strCount++].ToString());
            emojiDict.Add("frog", strEmojis[strCount++].ToString());
            emojiDict.Add("zebra", strEmojis[strCount++].ToString());
            emojiDict.Add("horse", strEmojis[strCount++].ToString());
            emojiDict.Add("unicorn", strEmojis[strCount++].ToString());
            emojiDict.Add("rooster", strEmojis[strCount++].ToString());
            emojiDict.Add("dragon", strEmojis[strCount++].ToString());

            strEmojis = "🐩,🦌,🦍,🦏,🐒,🐄,🐖,🐏,🐑,🐐,🐪,🐘,🐁,🐀,🦔,🐇".Split(',');
            strCount = 0;
            emojiDict.Add("poodle", strEmojis[strCount++].ToString());
            emojiDict.Add("reindeer", strEmojis[strCount++].ToString());
            emojiDict.Add("gorilla", strEmojis[strCount++].ToString());
            emojiDict.Add("rhino", strEmojis[strCount++].ToString());
            emojiDict.Add("monkey_2", strEmojis[strCount++].ToString());
            emojiDict.Add("cow_2", strEmojis[strCount++].ToString());
            emojiDict.Add("pig_2", strEmojis[strCount++].ToString());
            emojiDict.Add("ram", strEmojis[strCount++].ToString());
            emojiDict.Add("sheep", strEmojis[strCount++].ToString());
            emojiDict.Add("goat", strEmojis[strCount++].ToString());
            emojiDict.Add("camel", strEmojis[strCount++].ToString());
            emojiDict.Add("elephant", strEmojis[strCount++].ToString());
            emojiDict.Add("mouse", strEmojis[strCount++].ToString());
            emojiDict.Add("rat", strEmojis[strCount++].ToString());
            emojiDict.Add("hedgehog", strEmojis[strCount++].ToString());
            emojiDict.Add("rabbit", strEmojis[strCount++].ToString());

            /*
            strEmojis = "🐿🦎🐊🐢🐍🐉🦕🦖🦈🐬🦑🐳🐋🐟🐠🦐🐡🐙🐚🦀🦅🦆🦉🦃🐓";
            strCount = 0;

            strEmojis = "🐣🐤🐥🐦🐧🕊🦇🦋🐌🐛🦗🐜🐝🐞🦂🕷🕸👄🧠👅👀👶🛀🏄‍♀️🏄‍♂️";
            strCount = 0;
            
            strEmojis = "🏌️‍♀️🏌️‍♂️🏂🎨🏆🥁🎷🎸🎺🎻🎧🎤🔨🔑🔒💣💰✏🖌🖋✂📌⏰🗑🥨🍩🍪";
            strCount = 0;
            */
            return emojiDict;
        }
    }
}
