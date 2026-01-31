# v0.2.2 - Major UI addition

- The end of scout report now shows even more details on how you gained the XP!
- Note that it may seem like the numbers are wrong with ascent multipliers, thats mainly because the winning amount isnt multiplied.

# v0.2.1 - Slight API Change for developers

- The sPEAKer dev has request the addition of gaining experience in the airport as some features are only obtained there.
- Mod developers can now add another bool to `AddExperience` that will alward the experience in the airport.
- No integrated XP gaining systems will give you XP in the airport, only external mods will.

### Contributors for v0.2.1

- @atomictyler ( Atomic ) - Made the changes to the API
- @.chofo (" ✰ onlystar ) - sPEAKer dev that requested the change

# v0.2.0 - New configs, bug fixed and UI changes

- The end of scout report should now be changes, the timeline title shows XP gained and your level now. It looks something like `TIMELINE (+1361.25XP) (LEVEL 42)`
- I'm currently unsure if other players levels still don't show up in the end of game scout report.
- 2 New configs!
    - Show Ascent Multiplier (True): When false, the text that says the ascent multiplier will disappear, good if localization messed up or you have a mod that breaks this feature
   - Show Leveling Users Only (False): When false, people without the mod will be shown as level [1]. If true, they will just have their default name.

# v0.1.9 - Bug fix & false anti-cheat accusations

- Fixed a bug where you would leak memory over time, this should be fixed. If levelling UI goes missing, ping @atomictyler
- Anti-Cheats like PEAKER would accuse others of name spoofing, this should be fixed, if others still get accused of name spoofing by an anti cheat, ping @atomictyler in the leveling thread.

# v0.1.8 - 2 Config values & Save fixes

- Includes two configs
    - Show Experience Gain
   - Show Level Gain
- The mod no longer saves to disk every time you gain XP, it instead saves on application close.
- If you experience save loss (not saving when you exit the app) go to the discord thread and ping @atomictyler
- An Autosave system may also come soon, however as long as it saves when you close the game this shouldn't be necessary.

### Contributors for v0.1.8

- @atomictyler ( Atomic ) - Made all code changes.
- @kirsho ( Kirsho ) - Alerted me about saving each time xp is gained and why its so bad, also gave the method to use `OnApplicationQuit`.

# v0.1.7 - Major bug fix

- The last update had a last minute change that I didn't test (oops).
- Save files should be perfectly intact, however ping @atomictyler if it was and I can send you a new save file with the desired level and XP.
- Backups should now work. Existing backups will still also work.

### Contributors for v0.1.7

- @atomictyler ( Atomic ) - I fixed my own error :3

# v0.1.6 - Major Update, Automatic Backups

- Now backup your data with ease!
- Launch the game at least once to generate the config
- Go the config section of your mod manager and you should see a dropdown to load a backup.
- A backup is created each time you load the game, not as you quit, as the current save is that data.

### Contributors for v0.1.6

- @atomictyler ( Atomic ) - One bluescreen and lots of contemplating later, I made the update!

# v0.1.5 - API Changes, XP Changes and more!

- This update changes adds 2 new methods into the public API: `AddOneUseItem` and `SetOneUseItem`.
- The BBNO$ bugle is now set as a one time use item, however the moral boost still gives XP.
- Moral boost changed from 20XP -> 10XP
- Internal API and saving changes for the new one time use system
- Added a guide to the readme/description for backing up/deleting your data in the case of exploiters (if people want to do that).
- Changed manifest description to include how the mod works (with an experience system).

### Contributors for v0.1.5

- @atomictyler ( Atomic ) - Made the whole update.

# v0.1.4 - Bug fixes, Ascent multiplier UI, Icon change

- This update contains a bug fix with the multiplier code giving the multiplied XP, but showing the user the unmultiplied code. This was an easy fix.
- The boarding pass now displays the XP multiplier next to the name.
- Another new icon!

### Contributors for v0.1.4

- @atomictyler ( Atomic ) - Made the code changes, additions and new icon.

# v0.1.3 - API changes, Ascent multipliers and Airport immunity

- If the current scene is the airport, you can no longer gain XP.
- Made changes internally about the API for the ascent multiplier.
- Tenderfoot: 0.8x XP multiplier
- Regular PEAK: 1x XP multiplier
- Ascent 1: 1.1x XP multiplier
- And so on, adding .1x each time.

### Contributors for v0.1.3

- @atomictyler ( Atomic ) - Made the code changes and additions.
- @hyaxn ( hyaxn ) - Supplied with the suggestion for Airport and Ascent multipliers

# v0.1.2 - An extra way to gain XP

- You now gain XP for lighting the campfire
- Base at 10XP, and then +5XP added for each segment

# v0.1.1 - Small Update

- Changed the icon to be nicer.

# v0.1.0 - Release

- Release
- This mod is in the very early stages of development.

### Contributors for v0.1.0

- @atomictyler ( Atomic )
- @kirsho ( Kirsho )
> - @.chofo (" ✰ onlystar ) - They didn't contribute directly, but helped!
> - @hamunii ( Hamunii ) - They didn't contribute directly, but helped!