# What does the mod do?

This mod adds further progression to PEAK. This mod comes included with an extensive experience and leveling system
carefully made to make sure you get the best leveling experience!

This mod is in early release. Code may be subject to change and is being actively developed on.

> Currently a level takes the experience `level * 100`. This may change to a more complicated formula but right now is not subject to.

## Ways of gaining experience

- Winning a game: **+500xp**
- Dying, but someone else wins: **+50xp**
- Using an item: Dynamic, but ranges from **8xp-3xp**
- Opening Ancient Luggage: **+35xp**
- Opening Explorer's Luggage: **+25xp**
- Opening Big Luggage: **+20xp**
- Opening Regular Luggage: **+15xp**
- 1 in 20 chance whilst climbing every 15s: **+15xp**
- Using an ancient statue: **+50xp**
- Being revived: **+50xp**
- Reviving someone: **+100xp**
- Moral boost (with spam prevention): **+10xp**
- You die: **+5xp**
- Getting fed an item: **+10xp**
- Becoming a zombie (UNTESTED): **+100xp**
- Lightning the campfire: **10xp-25xp**

> Ascents give XP multipliers, these are also shown on the passport.
> Tenderfoot: 0.8x
> PEAK: 1x
> Ascent 1: 1.1x
> Ascent 2: 1.2x...

# How to reset or backup your data

### Using the config

This mod now comes with a config to atomically backup your data!
Launch the game at least once, then the config will generate.
A dropdown should appear with the data of the backup for you to load.

> It is recommended you still frequently manually backup your data.

### Manually

Because the data is persistent across profiles, maybe you want to reset your level and experience because some exploiter found away to give you a lot of experience.

Follow the steps below to restart your progress (Windows version):
1. Hold the windows key and press R (Or type in run in the search bar)
2. Copy this into the bar: `%appdata%\..\Local\LandCrab\PEAK\PEAKLeveling\`
3. Delete `player_stats.sav`
4. Re-open PEAK!

> This can also be used to backup your save file which is recommended when playing with randoms!

Follow the steps below to backup your progress (Windows version):
1. Hold the windows key and press R (Or type in run in the search bar)
2. Copy this into the bar: `%appdata%\..\Local\LandCrab\PEAK\PEAKLeveling\`
3. Copy `player_stats.sav`
5. Store the file wherever you want
6. When you need to replace the file, do the same steps but paste instead of copy the file.

# Contributors:

<details>
<summary> @atomictyler ( Atomic ) </summary>

- Made the API and Plugin side
- Created the saving system
- Created UI Code
- Did most PUN related networking
- Made changes to, or created some of the XP Gaining Systems

</details>

<details>
<summary> @kirsho ( Kirsho ) </summary>

- Created part of the following XP Gaining Systems:
    - Luggage opening (80%)
   - Item using primary & secondary (80%)
   - Receiving Badges
   - +50xp per Ascent ontop of the +500 from completion

</details>

<details>
<summary> @.chofo (" ✰ onlystar ) </summary>

- Provided part of the networking code publically for others to use

</details>

<details>
<summary> @hamunii ( Hamunii ) </summary>

- Made the PEAK Modding BepInEx template

</details>

Do you want to contribute? Go to the [Github](https://github.com/AtomicTyler1/PEAKLeveling/tree/master/Leveling/Leveling) or suggest in the [discord thread](https://discord.com/channels/1363179626435707082/1446608266661724423)

# Bugs, feedback and showing support

To show support, please like the mod on thunderstore!

To report bugs and give feedback please make an issue on the [Github](https://github.com/AtomicTyler1/PEAKLeveling/tree/master/Leveling/Leveling) or give me a ping in the [discord thread](https://discord.com/channels/1363179626435707082/1446608266661724423)!