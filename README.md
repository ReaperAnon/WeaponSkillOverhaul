![](https://i.imgur.com/WNHaQGf.png)
### [Synthesis Install](https://github.com/Mutagen-Modding/Synthesis/wiki/Installation)
### [How to Use](https://github.com/Mutagen-Modding/Synthesis/wiki/Typical-Usage#adding-patchers)

The mod can be found either by searching for WeaponSkillOverhaul on the patcher list or added via the .synth file found [here](https://github.com/ReaperAnon/WeaponSkillOverhaul/releases/tag/meta).

### Consider donating if you enjoy my work:
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/A0A6P3CRK)

# Details
A dynamically generated mod to rework how the one and two handed weapon skills, their respective perks and related spell and magic effects work, with the ability to create and define new weapon categories and assign them to perk trees on the fly. Also includes the ability to generate custom skill trees. By default meant to recreate a more Oblivion-like skill distribution but it can be modified to largely any configuration you like.

This is achieved by the mod changing the conditions of perks, spells and magic effects to work according to the settings created by the user, while distributing new keywords (without messing up the old ones or creating incompatibilities) and thus creating new weapon categories as well.

It's lightly similar to DanielUA's old True Equipment Overhaul (we have similarly bland naming schemes) which you could say it draws inspiration from.

## YouTube Demonstration
[![Vid1](https://img.youtube.com/vi/b-d-V9fGn5U/sddefault.jpg)](https://www.youtube.com/watch?v=b-d-V9fGn5U)

## Rationale
I always liked the sort of "roleplaying" idea of having a longsword for general combat and a smaller arming sword for tighter quarters or maybe just to use a shield in hairy situations. With the release of mods like Precision and Immersive Equipment Displays, where now you could have them on your person and where using a shorter weapon in tight spaces actually MAKES SENSE so you don't get stuck hitting walls, the mere roleplaying aspect became more of a practical reality.

Problem is, it didn't alleviate the issue of having to level two different skills just to use two different lengths of what is essentially the same weapon type. Now you don't have to!

By default it's set up to recreate the skills into Hafted and Bladed weapon skills respectively, but it can be kept mostly vanilla or used to just shift perk lines around (say you like the war axe perk tree in a perk mod and you would like curved swords in your game to use them as well). I'll explain how to use it further down.

## Installation
First, **it requires** the [Spell Perk Item Distributor](https://www.nexusmods.com/skyrimspecialedition/mods/36869) mod (and its requirements). It will not work properly otherwise.

Second, **it requires its conventional mod counterpart** from the [releases section here](https://github.com/ReaperAnon/WeaponSkillOverhaul/releases/tag/mod). It contains scripts (lighthweight and event-based) that help keep track of the new weapon types.

Third, and **optional**, it requires [Custom Skills Framework](https://www.nexusmods.com/skyrimspecialedition/mods/41780) and [Custom Skills Menu](https://www.nexusmods.com/skyrimspecialedition/mods/62423) (and their requirements) if you want the mod to transfer dual wielding perks or other stance perks (like perks for only having a single weapon equipped) into their own custom perk tree, which would otherwise be left looking out of place (but universally functional and differentiated!) in whatever your new one handed skill tree will be.

## Compatibility
Should be compatible with absolutely everything. The only compatibility changes it really needs are when some dual wielding perks in some perk mods are more complex and aren't recognized. I tested it with Adamant, Ordinator and Vokrii and it worked just fine.

There are also some pre-included compatibility entries there for a perk from Ordinator and for the ones in the Ordinator - Combat Styles mod. 

## Presets
**By default the mod comes with the settings set up for the vanilla perks (so without perk mods or perk mods that don't add any new perk branches, like for daggers). More presets will be uploaded shortly and linked to here.**
- **Vanilla Preset:** Rather basic, no new weapon types. Assigns daggers to also use the perks for swords.
- **Perk Mod Preset:** Meant for users of perk mods like Adamant, Vokrii, etc. Adds a more specific dagger category that can be expanded and mixes up the perk distribution a bit.
- **Perk Mod + New Weapon Types:** Same as the previous one but defines several new weapon types like spears, curved swords, straight swords, etc. Recommended if you use mods like Animated Armory or similar.
- **Vokriinator + New Weapon Types + Stances:** A true showcase of what the mod can really do. Generates a more in-depth Stances perk tree by manually defining the perks from Vokrii and Ordinator as Stance perks and also includes the previous weapon types. Meant to be used with the regular version of Vokriinator, not Vokriinator Black.

**Installing presets** is rather easy. Once you have the patcher added to Synthesis, it should generate a folder with a settings file inside of it. If the folder isn't there, try either opening the settings menu or running the patcher "dry" first.

Simply navigate to your **Synthesis** install folder and then **Data\Skyrim Special Edition (or whatever profile you're using)\WeaponSkillOverhaul (or whatever name you gave the patcher)** and **replace the settings.json file** with any of the ones provided.

All profiles can be downloaded [here in the releases section](https://github.com/ReaperAnon/WeaponSkillOverhaul/releases/tag/profiles).
   
## Function and Usage
The mod renames the skill trees, modifies perk, spell and magic effect conditions, creates and applies new weapon types, unlinks dual wield perks from the one handed skill tree (if you want it to), removes all non-dual wielding perk prerequisites and makes both the one and two handed skill satisfy the skill requirement.

This means that even if you use maces or axes, the dual wielding perks will function as expected, even if you don't have the option to move them into a custom perk tree enabled (although I highly recommend it).

I'll describe the options and what they do now, highly recommend to read this section (similar, but less verbose explanations can be found in the tooltips when mousing over any option).
- **Two Handed Skill Name:** The name you want the two handed skill to have. Can be left empty to leave unchanged.
  
- **Two Handed Abbreviation:** What the name looks like abbreviated (two handed - 2h, that kind of thing). Can be left empty.
  
- **One Handed Skill Name:** The name the one handed skill should have. Can be left empty.
  
- **One Handed Abbreviation:** Same as the other one. Can be left empty.

- **Change Perk Names:** Changes perk names if enabled. This usually results in kinda crappy changes as it's a straight text replacement and perk names usually have a sort of artistic flair to them.

- **Change Spell Names:** Same as above, but enabled by default. Can be disabled if you don't want the names to be changed.
 
- **Change Magic Effect Names:** Same as above.
  
- **Modify Skill Types:** The first of the more involved options. If enabled, the patcher takes it as fact that you will mix and match one and two handed weapons between the skills. It will remove the perk requirements on the basic sprinting power attacks (there's no nice way to keep it) and will process dual wielding perks to separate them. If you intend to keep the actual skills unchanged (as in the one handed skill only containing one handed weapons and the two handed skill only containing two handed weapons), then you should disable this.
  
- **Create Stances Perk Tree:** Only works if you have the optionally required custom skill mods enabled, otherwise it'll tell you you're missing them. If enabled, it will generate a new skill tree for all the detected dual wielding perks and move them there, so they doesn't clutter your unrelated replacement of the one handed skill tree. Highly recommended.
  
- **Stance Perks:** A list of perks that should be counted as perks related to weapon stances (one and two handed, dual wielding, etc). The mod has automatic detection for dual wielding perks, but some perks are more unique and some might have unique effects if you have either two weapons or just one weapon with an empty off-hand. The perks added to this list will not have their functionality changed and will be detached from the perk trees or moved to a new one. Extremely fun to add the general two or one handed perks to this list (so basically almost every perk that is not directly related to a weapon type).
  
- **Weapon Type Assignments:** A dictionary of all the weapon types you want your game to have. You simply need to type a name into the textbox and press the + sign to create a new weapon type.
  
- **Priority:** The priority of a weapon category determines if it can win over others. It's possible that a weapon might meet the conditions for belonging into multiple categories, in which case the priority number will decide which one it belongs to.
  
- **Desired Type:** The original weapon type whose perks you want this category of weapons to use. To put it simply, if you set it to WeapTypeSword then this category of weapons will use sword perks and will replace whatever text snippets you entered for WeapTypeSword. If you set it to WeapTypeWarAxe then this weapon category will use war axe perks.
  
- **Naming Scheme:** The way you want the weapon names to be formatted. You can leave it empty if you don't want them changed. The $name part gets replaced with the weapon's name and the $type part with its newly assigned type. To put it simply "$type - $name" would look like "Straight Sword - Iron Sword" in-game, while "$name ($type)" would look like "Iron Sword (Straight Sword)".
  
- **Is Two Handed Skill:** Determines which skill this weapon category will belong to. To make it easy, if you set the "Desired Type" to a weapon type which has its perks in the two handed tree, this should be enabled. If you set it to one which has its perks in the one handed tree, it should be disabled.
  
- **Keys:** A list of text snippets by which this weapon type can be identified. If you make a "curved sword" category and add "katana" as a key, then every weapon that has "katana" in its editor ID will become a curved sword.
  
- **Keywords:** Every weapon that has one of the keywords on the list will be part of the selected category. If you make a category called "straight sword" and add the WeapTypeSword keyword here, then every regular sword in the game will be a straight sword.
  
- **Weapon Forms:** You can browse and select actual weapons by their form ID here, they will be made part of the selected weapon type.
  
- **Forbidden Keys, Keywords and Weapon Forms:** Same as above but the opposite way. Things that match any of these will NEVER be part of the selected weapon category.

- **Text Replacements:** A list of identifiers for the weapon keywords the original perks (or your installed perk mods) use. This is for the mod to be able to recognize what phrases it should be replacing for the above-defined weapon types. For example, you could have 3 categories, "Dagger", "Claw" and "Short Sword", assigned to WeapTypeDagger. An entry in this list defined with WeapTypeDagger basically tells the mod which text snippets (defined in the Name List) should be replaced with that enumeration of "daggers, claws and short swords". To keep with this example, if the name list has an entry for "dagger" then every mention of the word "daggers" in perk descriptions will be replaced with "daggers, claws and short swords".
  
- **Type to Replace:** The weapon keyword the entry is associated with.
  
- **Category Name:** Here you can add a general category name by which you want the associated weapon categories to be referred to in perk names. It would look ugly if a perk called "Dagger Specialization" would be renamed to "Dagger, Claw and Short Sword Specialization". If you set the category name to "Short Blade" in this example, then the perk would be renamed "Short Blade Specialization" instead.
  
- **Name List:** The above-mentioned list of identifiers that should each be replaced with the names of the new weapon categories belonging to the keyword used.
  

I know it's pretty confusing when reading through it, but you likely don't have to change the options much. If you want to, they become pretty self-explanatory when looking through the preset options there already.

## Credits
### Mutagen

### Darenii - Custom perk menu background

### Koveich - Sovngarde font
