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
        public static Dictionary<string, string> EmojiImageDict = GetEmojiImageDict();

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

        public KeyValuePair<string,string> GetKvpForEmojiBasedOnFirstLetter(char letter)
        {
            var emojisStartingWithC = ImageVocabulary.EmojiImageDict.Keys.Where(i => i.ToUpperInvariant().StartsWith(letter.ToString().ToUpper()));
            string word = "";
            string emoji="";

            if (emojisStartingWithC.Any())
            {
                string rndKey = emojisStartingWithC.ElementAt(rnd.Next(emojisStartingWithC.Count()));
                if (rndKey != null)
                {
                    word = rndKey;
                    if (word.IndexOf("_") > 0)
                    {
                        word = word.Substring(0, word.IndexOf("_"));
                        emoji = EmojiImageDict[word];
                    }
                }
            }
            return new KeyValuePair<string, string>(word, emoji);
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

        static string getUnicodeString(string ucStr)
        {
            return char.ConvertFromUtf32(int.Parse(ucStr, System.Globalization.NumberStyles.HexNumber)).ToString();
        }

        static KeyValuePair<string,string> createKvpFromLine(string line)
        {
            //"horse_1f40e.png" 
            string[] strSplit = line.Split('_');
            string key = strSplit[0];
            string val = strSplit[1];
            return new KeyValuePair<string, string>(key, getUnicodeString(val));
        }

        static Dictionary<string, string> GetEmojiImageDict()
        {
            Dictionary<string, string> emojiDict = new Dictionary<string, string>();
            List<string> includedEmojis = new List<string>()
            {
               "poo_1f4a9",
"ghost_1f47b",
"kiss_1f48b",
"heart_1f498",
"thumb_1f44d",
"ear_1f442",
"nose_1f443",
"eyes_1f440",
"tongue_1f445",
"mouth_1f444",
"baby_1f476",
"runner_1f3c3",
"dancer_1f483",
"horse_1f3c7",
"snowboarder_1f3c2",
"surfer_1f3c4",
"rowboat_1f6a3",
"swimmer_1f3ca",
"bicyclist_1f6b4",
"bath_1f6c0",
"family_1f46a",
"footprints_1f463",
"monkey-face_1f435",
"monkey_1f412",
"dog-face_1f436",
"dog_1f415",
"poodle_1f429",
"wolf-face_1f43a",
"cat-face_1f431",
"cat_1f408",
"tiger-face_1f42f",
"tiger_1f405",
"leopard_1f406",
"horse-face_1f434",
"horse_1f40e",
"cow-face_1f42e",
"ox_1f402",
"water-buffalo_1f403",
"cow_1f404",
"pig-face_1f437",
"pig_1f416",
"boar_1f417",
"pig-nose_1f43d",
"ram_1f40f",
"sheep_1f411",
"goat_1f410",
"camel_1f42a",
"elephant_1f418",
"mouse-face_1f42d",
"mouse_1f401",
"rat_1f400",
"hamster-face_1f439",
"rabbit-face_1f430",
"rabbit_1f407",
"bear-face_1f43b",
"koala_1f428",
"panda-face_1f43c",
"paw-prints_1f43e",
"chicken_1f414",
"rooster_1f413",
"chick_1f424",
"bird_1f426",
"penguin_1f427",
"frog-face_1f438",
"crocodile_1f40a",
"turtle_1f422",
"snake_1f40d",
"dragon-face_1f432",
"dragon_1f409",
"spouting-whale_1f433",
"whale_1f40b",
"dolphin_1f42c",
"fish_1f41f",
"tropical-fish_1f420",
"blowfish_1f421",
"octopus_1f419",
"spiral-shell_1f41a",
"snail_1f40c",
"bug_1f41b",
"ant_1f41c",
"honeybee_1f41d",
"lady-beetle_1f41e"

            };
            foreach(var str in includedEmojis)
            {
                var kvp = createKvpFromLine(str);
                if (!emojiDict.ContainsKey(kvp.Key))
                {
                    emojiDict.Add(kvp.Key, kvp.Value);
                }
            }
            return emojiDict;
        }
        static Dictionary<string, string> GetEmojiImageDictOld()
        {
            string[] strEmojis = "🤖,🐶,🐺,🐱,🦁,🐯,🦊,🐮,🐷,🐗,🐭,🐹,🐰,🐻,🐨,🐼,🐸,🐴,🦄,🐔,🐲".Split(',');
            int strCount = 0;
            Dictionary<string, string> emojiDict = new Dictionary<string, string>();
            emojiDict.Add("ghost", "👻");
            emojiDict.Add("robot", strEmojis[strCount++].ToString());
            emojiDict.Add("dog", strEmojis[strCount++].ToString());
            emojiDict.Add("wolf", strEmojis[strCount++].ToString());
            emojiDict.Add("cat", strEmojis[strCount++].ToString());
            emojiDict.Add("lion", strEmojis[strCount++].ToString());
            emojiDict.Add("tiger", strEmojis[strCount++].ToString());
            emojiDict.Add("giraffe", "🦒");
            emojiDict.Add("fox", strEmojis[strCount++].ToString());
            emojiDict.Add("cow", strEmojis[strCount++].ToString());
            emojiDict.Add("pig", strEmojis[strCount++].ToString());
            emojiDict.Add("panda", strEmojis[strCount++].ToString());
            emojiDict.Add("frog", strEmojis[strCount++].ToString());
            emojiDict.Add("zebra", "🦓");
            emojiDict.Add("hamster", strEmojis[strCount++].ToString());
            emojiDict.Add("rabbit_2", strEmojis[strCount++].ToString());
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
            strEmojis = "🐿,🦎,🐊,🐢,🐍,🐉,🦕,🦖,🦈,🐬,🦑,🐳,🐋,🐟,🐠,🦐,🐡,🐙,🐚,🦀,🦅,🦆,🦉,🦃,🐓".Split(',');
            strCount = 0;
            emojiDict.Add("chipmunk", strEmojis[strCount++].ToString());
            emojiDict.Add("lizard", strEmojis[strCount++].ToString());
            emojiDict.Add("alligator", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());
            emojiDict.Add("", strEmojis[strCount++].ToString());

            /*
            strEmojis = "🐣🐤🐥🐦🐧🕊🦇🦋🐌🐛🦗🐜🐝🐞🦂🕷🕸👄🧠👅👀👶🛀🏄‍♀️🏄‍♂️";
            strCount = 0;
            
            strEmojis = "🏌️‍♀️🏌️‍♂️🏂🎨🏆🥁🎷🎸🎺🎻🎧🎤🔨🔑🔒💣💰✏🖌🖋✂📌⏰🗑🥨🍩🍪";
            strCount = 0;
            //{"💣","💰","✏","🖌","🖋","✂","📌","⏰","🗑","🥨","🍩","🍪","🐷" };
             * 
             */
            return emojiDict;
        }
    }
}
