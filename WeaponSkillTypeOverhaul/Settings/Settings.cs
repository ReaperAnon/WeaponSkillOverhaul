using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda.WPF.Reflection.Attributes;
using Newtonsoft.Json;
using System.IO;

namespace WeaponSkillTypeOverhaul
{
    public class WeapTypeDef
    {
        [SettingName("Desired Type")]
        [Tooltip("This should be a weapon keyword which has perks tied to it. For example, if you want this weapon category to use battleaxe perks you would set this to WeapTypeBattleaxe. If you have a perk mod which has keywords for other weapon types you can select those keywords too.")]
        public IFormLinkGetter<IKeywordGetter> DesiredType { get; set; } = FormLink<IKeywordGetter>.Null;


        [SettingName("Naming Scheme")]
        [Tooltip("The form the name of the weapon should be in, $name is replaced by the weapon name and $type by the weapon type. Leaving it empty makes no changes to weapon names.")]
        public string NameForm { get; set; } = "$name ($type)";


        [SettingName("Raises Two Handed Skill")]
        [Tooltip("By default weapons will contribute to raising the one handed skill. If you're assigning this category to a weapon type that is in the vanilla two handed perk tree you'll want to tick this so using this category of weapon will raise the correct skill.")]
        public bool IsTwoHanded { get; set; } = false;
    }

    public class WeaponDefinition
    {
        [SettingName("Priority")]
        [Tooltip("The priority of the category will determine if it can overwrite other categories. If a weapon matches the conditions for multiple categories it will belong to the one with the higher priority.")]
        public ushort Priority { get; set; } = 0;


        [SettingName("Weapon Type Definition")]
        [Tooltip("Assign this new weapon type to one of the vanilla weapon types here. Every weapon in the current category will use the selected weapon type's perks.")]
        public WeapTypeDef WeaponTypeDefinition { get; set; } = new() { NameForm = "$name ($type)" };


        [SettingName("Keys")]
        [Tooltip("Weapon names and/or editor IDs containing any of these keys will be assigned to this weapon type.")]
        public List<string> Keys { get; set; } = new();


        [SettingName("Keywords")]
        [Tooltip("Weapons using any of these keywords will be assigned to this weapon type.")]
        public List<IFormLinkGetter<IKeywordGetter>> Keywords { get; set; } = new();


        [SettingName("Weapon Forms")]
        [Tooltip("The items on this list will be assigned to this weapon type, ignoring every other option.")]
        public List<IFormLinkGetter<IWeaponGetter>> Forms { get; set; } = new();


        [SettingName("Forbidden Keys")]
        [Tooltip("Weapon names and/or editor IDs containing any of these keys will never be assigned to this weapon type, even if they have a match.")]
        public List<string> ForbiddenKeys { get; set; } = new();


        [SettingName("Forbidden Keywords")]
        [Tooltip("Weapons using any of these keywords will not be assigned to this weapon type.")]
        public List<IFormLinkGetter<IKeywordGetter>> ForbiddenKeywords { get; set; } = new();


        [SettingName("Forbidden Weapon Forms")]
        [Tooltip("The items on this list will never be assigned to this weapon type, ignoring every other option.")]
        public List<IFormLinkGetter<IWeaponGetter>> ForbiddenForms { get; set; } = new();
    }

    public class TextReplacement
    {
        [SettingName("Type To Replace")]
        [Tooltip("The original weapon type keyword this text entry is for.")]
        public IFormLinkGetter<IKeywordGetter> WeaponType { get; set; } = FormLink<IKeywordGetter>.Null;


        [SettingName("Category Name")]
        [Tooltip("By default the mentions of the original weapon types in perk or ability names get replaced with the names of all the new categories which use that perk. This option overrides that and you can give that collection of weapon categories a more concise name (for example, you might want to display \"short blade\" instead of \"daggers and claws\" or something similar).")]
        public string CategoryName { get; set; } = "";


        [SettingName("Name List")]
        [Tooltip("The list of names used by the perks for this weapon type that should get replaced with the new weapon categories.")]
        public List<string> Keys { get; set; } = new();
    }

    public class Settings
    {
        [SettingName("Two Handed Skill Name")]
        [Tooltip("The name the two handed skill should get. Can be left empty to not make any changes.")]
        public string SkillNameTwoHanded { get; set; } = "Hafted";


        [SettingName("Two Handed Abbreviation")]
        [Tooltip("The abbreviation of the two handed skill name. Can be left empty to not make any changes.")]
        public string SkillAbbrvTwoHanded { get; set; } = "HFT";


        [SettingName("One Handed Skill Name")]
        [Tooltip("The name the one handed skill should get. Can be left empty to not make any changes.")]
        public string SkillNameOneHanded { get; set; } = "Bladed";


        [SettingName("One Handed Abbreviation")]
        [Tooltip("The abbreviation of the one handed skill name. Can be left empty to not make any changes.")]
        public string SkillAbbrvOneHanded { get; set; } = "BLD";


        [SettingName("Change Perk Names")]
        [Tooltip("If you want the mod to change the names of perks (when needed) turn this on. Might result in weird naming as the application doesn't have much of a sense of artistic decorum.")]
        public bool ChangePerkNames { get; set; } = false;


        [SettingName("Change Spell Names")]
        [Tooltip("If you want the mod to change the names of spells (when needed) turn this on. Might result in weird naming as the application doesn't have much of a sense of artistic decorum.")]
        public bool ChangeSpellNames { get; set; } = true;


        [SettingName("Change Magic Effect Names")]
        [Tooltip("If you want the mod to change the names of magic effects (when needed) turn this on. Might result in weird naming as the application doesn't have much of a sense of artistic decorum.")]
        public bool ChangeMGEFNames { get; set; } = true;


        [SettingName("Modify Skill Types")]
        [Tooltip("If you don't want to change the skill types from one handed and two handed, just add new weapon types or mix and match perk trees, you should turn this option off to disable some of the functionality that shouldn't run.")]
        public bool ModifySkillTypes { get; set; } = false;


        [SettingName("Create Stances Perk Tree")]
        [Tooltip("Moves the dual wielding perks to a custom skill tree, which requires Custom Skills Framework and Custom Skills Menu.")]
        public bool MoveDualWieldPerks { get; set; } = false;


        [SettingName("Stance Perks")]
        [Tooltip("By default the mod can determine dual wielding perks on its own, but some more creative ones (like ones that have effects for both single hand and dual wield) will not be picked up. They can be added manually to this list to take care of them.")]
        public List<IFormLink<IPerkGetter>> DualWieldPerks { get; set; } = new();


        [SettingName("Weapon Type Assignments")]
        [Tooltip("Used for assigning different categories to weapons.")]
        public Dictionary<string, WeaponDefinition> WeaponDefinitions { get; set; } = new();


        [SettingName("Text Replacements")]
        [Tooltip("A list of entries that defines by what \"keys\" or text snippets an original weapon type can be identified by in the original perk descriptions or names.")]
        public List<TextReplacement> TextReplacements { get; set; } = new();
    }
}
