BabySmash
=========
## updates by nvuono
Expanded on usage of vocabulary words:
* When a letter is pressed, we do some vocabulary lookups to see if we have an image that starts with that letter
* pressing A brings up an acorn, D brings up a dog, etc

Added a couple methods of pulling in images to support vocabulary:
* Easiest method is using emoji based on the Windows 8 segoe UI emoji availability. This just renders the black and white emojis but that's fine for our purposes.
* Second method allows a png to be loaded from a Vocabulary.zip file where the filename is simply the name of the vocabulary word like "dog.png" 

Images have a color applied to them like how all other letters and shapes get a color:
* We do a basic RGB threshold calculation for every pixel to "leave it blank" or force it to the selected color

Started framework for adding some kind of basic game functions to match what you might find in little toy laptops, speak-n-spell, etc.

The thought is that there would be a "demand" which would encapsulate several types of requests:
 * asking to type out the numbers 1 to 10
 * repeating the letters of a given word (with or without image)
 * spelling a word based only on the image and speech

 Added some musical framework for possibly doing something like singing the A-B-C song but the text-to-speech engine is really picky about the prosody rate values for built-in voices so I'm not sure if that will go anywhere.

original doc continued below

## Overview
The BabySmash game for small kids.  

As babies or children smash on the keyboard, colored shapes, letters and numbers appear on the screen and are voiced to help breed familiarization.

Baby Smash will lock out the Windows Key, as well as Ctrl-Esc and Alt-Tab so your baby won't likely exit the application, rotate your monitor display, and so on. Pressing ALT-F4 will exit the application and Shift-Ctrl-Alt-O brings up the options dialog.

Originally developed by Scott Hanselman, based on AlphaBaby. The version here contains some enhancements, but the original version is also available: http://www.hanselman.com/babysmash/

## Enhancements
This version of BabySmash includes at least the following enhancements over the original:
* Keypad typing now register as numbers typed, just like the number row.
* Bug fixes, including cleaner shutdown.
* Improved sound handling.
* Ovals are added to the roster of shapes (including Circle, Heart, Hexagon, Rectangle, Square, Star, Trapezoid, Triangle), letters, and numbers.

## AutoHotkey
Used in conjunction with a tool like AutoHotkey, you can essentially create a "baby lock hotkey" so you can baby-proof your PC inputs at a moment's notice, with this immersive application instead of just the boring Windows Lock Screen.  To set up:
* Download and install, if you don't already have it. Available for free at: http://www.autohotkey.com/
* Run AutoHotkey; for the first time, it will prompt if you want to edit the script. You do.
* If the script is not open, right-click the AutoHotkey taskbar icon (an 'H' icon) and select 'Edit This Script'.
* Choose a hotkey. Avoid relying on the Windows key, as it will be held while BabySmash starts and may be buggy when you exit BabySmash due to the way the key is intercepted. I like to use Control+Shift+Z.
* Code the hotkey. If you're using Control+Shift+Z, you can add "^+z::Run D:\GIT\babysmash\bin\Release\BabySmash.exe" right after the line "#z::Run www.autohotkey.com" (without quotes); Obviously your path to BabySmash.exe will vary depending on where you installed or built the code.
* Save the file and close your text editor.
* Right-click the AutoHotkey taskbar, and select 'Reload This Script'.
* Try out your new hotkey to make sure it works.  If not, go back to 'Edit This Script' and try again.

For more advanced customization, see also: http://ahkscript.org/docs/Tutorial.htm
