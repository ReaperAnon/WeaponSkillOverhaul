using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponSkillTypeOverhaul
{
    public class Text
    {
        private static Dictionary<IFormLinkGetter<IKeywordGetter>, List<string>> TextMatches { get; set; } = new();


        /// <summary>
        /// Generates a dictionary which includes all the new weapon type names the original ones should be replaced by for a given vanilla weapon type keyword.
        /// </summary>
        /// <param name="textMatchDict"></param>
        public static void BuildTextDictionary(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            TextReplacement oneHandedReplacement = new() { Keys = new() { "one handed", "onehanded" }, WeaponType = FormLinkGetter<IKeywordGetter>.Null }; Program.Settings.TextReplacements.Add(oneHandedReplacement);
            TextReplacement twoHandedReplacement = new() { Keys = new() { "two handed", "twohanded" }, WeaponType = FormLinkGetter<IKeywordGetter>.Null }; Program.Settings.TextReplacements.Add(twoHandedReplacement);

            foreach (var textReplacement in Program.Settings.TextReplacements)
            {
                var matchingDefs = Program.Settings.WeaponDefinitions.Where(def => def.Value.WeaponTypeDefinition.DesiredType.Equals(textReplacement.WeaponType)).Select(def => def.Key.ToLower().Trim()).ToList();

                // Sort the keys into descending order by length to avoid false positive partial text replacements.
                textReplacement.Keys.Sort((x, y) => x.Length < y.Length ? 1 : -1);

                TextMatches[textReplacement.WeaponType] = new(matchingDefs);
            }

            foreach (var textMatch in TextMatches)
            {
                if (textMatch.Key.IsNull) // Skip the skill entries.
                    continue;

                if (!textMatch.Value.Any() || textMatch.Value.All(str => str.IsNullOrEmpty()))
                    throw new Exception($"Missing weapon definitions for keyword {textMatch.Key.Resolve(state.LinkCache)}");
            }
        }

        static bool IsWholeWord(string text, int idx)
        {
            return idx == 0 || !char.IsLetter(text[idx - 1]);
        }

        static string GetFullWord(string text, string word, int startIdx)
        {
            int i = startIdx + word.Length;
            while (i < text.Length && char.IsLetter(text[i]))
                ++i;

            return text[startIdx..i];
        }

        static void GetWordInfo(string text, out bool isSplit, out bool isMajor, out bool isDoubleMajor, out bool isPlural)
        {
            isSplit = text.Contains(' ') || text.Contains('-');
            isDoubleMajor = isSplit && char.IsUpper(text[text.Replace('-', ' ').IndexOf(' ') + 1]);
            isMajor = char.IsUpper(text.First());
            isPlural = text.Last() == 's';
        }

        static bool WasSegmentProcessed(string text, List<string> addedPhrases, int idx)
        {
            foreach (var newPhrase in addedPhrases)
            {
                int searchIdx = text.IndexOf(newPhrase);
                if (idx >= searchIdx && idx <= searchIdx + newPhrase.Length)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns all starting indexes for a given search term in a string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="searchTerm"></param>
        /// <param name="allIndexes"></param>
        /// <returns></returns>
        static bool GetAllIndexesOf(string text, string searchTerm, out List<int> allIndexes)
        {
            allIndexes = new();
            int idx = text.LastIndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
            while (idx > -1)
            {
                allIndexes.Add(idx);
                text = text.Remove(idx);
                idx = text.LastIndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
            }

            return allIndexes.Any();
        }

        static void ModifyText(ref string origText, string textKey, int startIdx, List<string> replacements, List<string> addedPhrases, bool skillDescription)
        {
            string newText = "";
            string fullWord = GetFullWord(origText, textKey, startIdx);

            // Go through every replacement string in the list.
            GetWordInfo(fullWord, out var isSplit, out var isMajor, out var isDoubleMajor, out var isPlural);
            for (int i = 0; i < replacements.Count; ++i)
            {
                string formattedText = replacements[i];
                if (isMajor)
                {
                    formattedText = char.ToUpper(replacements[i][0]) + replacements[i][1..];
                    isMajor = false;
                }

                /*if (isMajor || isDoubleMajor)
                {
                    var idx = formattedText.Replace('-', ' ').IndexOf(' ');
                    if (idx > -1)
                        formattedText = formattedText[..(idx + 1)] + char.ToUpper(formattedText[idx + 1]) + formattedText[(idx + 2)..];
                }*/

                if (isPlural)
                    formattedText += 's';

                if (i < replacements.Count - 2)
                    formattedText += ", ";
                else if (i == replacements.Count - 2)
                    formattedText += skillDescription ? " or " : " and ";

                newText += formattedText;
            }

            origText = origText.Remove(startIdx, fullWord.Length).Insert(startIdx, newText);
            addedPhrases.Add(newText);
        }

        public static bool ReplaceText(string? origText, Action<string> ChangeText, bool isNameChange = false, bool isSkillDesc = false)
        {
            if (origText.IsNullOrEmpty() || origText.IsNullOrWhitespace())
                return false;

            bool foundReplacement = false;
            List<string> addedPhrases = new();
            foreach (var textGroup in Program.Settings.TextReplacements)
            {
                foreach (var textKey in textGroup.Keys)
                {
                    // Skip if there are no instances of the current key.
                    string formattedText = origText.Replace('-', ' ');
                    if (!GetAllIndexesOf(formattedText, textKey, out var allIndexes))
                        continue;

                    // Get the type list strings for the current weapon type's text group.
                    if (!TextMatches.TryGetValue(textGroup.WeaponType, out var replacements) && !textGroup.WeaponType.IsNull)
                        continue;

                    // Replace skill names.
                    if (textGroup.WeaponType.IsNull)
                    {
                        replacements = new();
                        if (textKey.Contains("one", StringComparison.OrdinalIgnoreCase))
                            replacements.Add(Program.Settings.SkillNameOneHanded.ToLower());
                        else if (textKey.Contains("two", StringComparison.OrdinalIgnoreCase))
                            replacements.Add(Program.Settings.SkillNameTwoHanded.ToLower());
                    }

                    // Entries in perk, spell or effect names get replaced by item category names instead of listing off the weapon types.
                    if (isNameChange && !textGroup.CategoryName.IsNullOrEmpty() && !textGroup.CategoryName.IsNullOrWhitespace())
                        replacements = new() { textGroup.CategoryName.ToLower() };

                    foreach (var idx in allIndexes)
                    {
                        if (IsWholeWord(origText, idx) && !WasSegmentProcessed(origText, addedPhrases, idx))
                        {
                            ModifyText(ref origText, textKey, idx, replacements!, addedPhrases, isSkillDesc);
                            foundReplacement = true;
                            ChangeText(origText);
                        }
                    }
                }
            }

            return foundReplacement;
        }
    }
}
