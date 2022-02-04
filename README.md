# AutoPape: The 4Chan Based Wallpaper Utility
## A utility to automatically download and apply wallpapers from the 4Chan /wg/ and /w/ boards
#### Not in any way affiliated with or endorsed by 4chan community support LLC

## Disclaimer:
#### The boards being pulled from will occasionally have NSFW content in the threads. Most threads are marked as such in either the subject line or initial post text and can generally be sorted out by a keyword blacklist. Posters tend to stay on topic, so NSFW content is usually only found on NSFW threads. However, not all threads are explicitly marked and not all poster stay on topic. As a result, this tool should be regarded as NSFW. 
### Users should be 18+ years of age.

#### In the future, more methods will be implemented to reduce how much NSFW content will make it onto a users desktop if they do not want it. For now, it is best to just filter out the keywords commonly associated with NSFW threads. The application will build a default list of terms that can later be expanded or reduced by the user as per their use case.

## Features:
- Automatically apply wallpapers from the 4chan /wg/ - Wallpapers/General and /w/ - Anime/Wallpapers boards.
- Import wallpapers from your system to be shuffled into the wallpaper queue.
- Blacklist and Whitelist thread subject terms to fine tune the types of wallapers that get applied to your desktop.
- Manually save and automatically archive wallpapers for later use.
- Limit disk space usage to not destroy your hard drive with wallpapers.
- Turn off disk space limiter to destroy your hard drive with wallpapers.

## Usage:
- A wallpaper will be chosen automatically from the board for each of your monitors on startup.
  - A thread in the side bar can be saved by pressing the save button. They will be saved to the directory specified in General Settings (Default: AppData)
- Another wallpaper will be chosen and applied every user defined time span starting at the top of the next hour.
- Bring your own wallpapers by either dragging and dropping the images onto the import tab or go to the custom folder in the save directory and drag them in there. Images in sub directories will be ignored.
- The automated archive will occur once per day at 1am. This will save everything from all enabled boards archive section.
  - This is off by default. Saving this much can get large fast, so be careful with this feature.
  - You can limit how much space can be used by the save directory to reduce archiving impact.
  - You can also limit how much is saved by only allowing images that properly fit your monitors and only taking from threads that pass the blacklist/whitelist check.
- If a wallpaper can not be found that fits all parameters for your monitor:
  - A default wallpaper will be generated. It will be a random solid color and white text in the center showing the hex representation of the color.
  - Maybe be less picky.
  - Maybe get a monitor that isn't a weird resolution that no one likes/uses.
## Modes explained for narrower/Wider wallpapers:
- ### Center
  - Centers the wallpaper on the screen. Narrower wallpapers will have a black boarder, wider wallpapers will be cut off on all sides
- ### Stretch
  - Warps the image to take up the entire screen without any cutoff or black boarders. (Not generally recommended)
- ### Fit
  - Forces the wallpaper to the same height as the monitor. Width will scale proportionally.
- ### Fill
  - Forces the wallpaper to the same width as the monitor. Height will scale proportionally.

## Contact me:
If you wish to get in contact with me, you can email me at AutoPape@not-studying.com. If you have a question, put QUESTION in your subject line. comments and suggestions should be COMMENT, and bugs should be BUG. In the case of bugs, please provide as much detail as possible. Include things like what you were doing, buttons you pressed, and what the end result was. It could also potentially be helpful to provide me a copy of your settings file. Look for Settings.XML in your AutoPape directory in AppData.
