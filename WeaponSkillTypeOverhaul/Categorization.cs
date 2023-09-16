using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
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
    public class Categories
    {
        private static string WeapTypePrefix { get; } = "WST_WeapType";

        public static List<IFormLinkGetter<IPerkGetter>> DualWieldPerks { get; set; } = new();
        public static List<IFormLinkGetter<ISpellGetter>> DualWieldSpells { get; set; } = new();
        public static List<IFormLinkGetter<IMagicEffectGetter>> DualWieldMGEF { get; set; } = new();


        public static Dictionary<string, IFormLinkGetter<IKeywordGetter>> KeywordMatches { get; set; } = new();

        // Tuple: LeftHand RightHand
        public static Dictionary<IFormLinkGetter<IKeywordGetter>, Tuple<IFormLinkGetter<ISpellGetter>, IFormLinkGetter<ISpellGetter>>> AbilityMatches { get; set; } = new();

        private static readonly FormLink<IFormListGetter> KeywordList = FormKey.Factory("000D62:Keyword Tracker.esp");
        private static readonly FormLink<IFormListGetter> AbilityList = FormKey.Factory("001D8E:Keyword Tracker.esp");

        /// <summary>
        ///  Returns true if the editor ID of a weapon form contains a key that excludes it from the weapon definition.
        /// </summary>
        /// <param name="weaponDefinition"></param>
        /// <param name="weaponGetter"></param>
        /// <returns></returns>
        private static bool IsIllegalKey(WeaponDefinition weaponDefinition, IWeaponGetter weaponGetter)
        {
            return weaponDefinition.ForbiddenKeys.Any(key => weaponGetter.EditorID?.Replace(" ", "").Contains(key, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        /// <summary>
        /// Returns true if the weapon form contains a keyword that excludes it from the weapon definition.
        /// </summary>
        /// <param name="weaponDefinition"></param>
        /// <param name="weaponGetter"></param>
        /// <returns></returns>
        private static bool IsIllegalKeyword(WeaponDefinition weaponDefinition, IWeaponGetter weaponGetter)
        {
            return weaponDefinition.ForbiddenKeywords.Any(key => weaponGetter.HasKeyword(key));
        }

        /// <summary>
        /// Returns true if the weapon form is excluded from the weapon definition.
        /// </summary>
        /// <param name="weaponDefinition"></param>
        /// <param name="weaponGetter"></param>
        /// <returns></returns>
        private static bool IsIllegalForm(WeaponDefinition weaponDefinition, IWeaponGetter weaponGetter)
        {
            return weaponDefinition.ForbiddenForms.Any(form => weaponGetter.ToLink().Equals(form));
        }

        /// <summary>
        /// Returns true if the weapon form fits the weapon definition.
        /// </summary>
        /// <param name="weaponDefinition"></param>
        /// <param name="weaponGetter"></param>
        /// <returns></returns>
        private static int GetTypeMatch(WeaponDefinition weaponDefinition, IWeaponGetter weaponGetter)
        {
            if (IsIllegalForm(weaponDefinition, weaponGetter))
                return -1;

            if (IsIllegalKeyword(weaponDefinition, weaponGetter))
                return -1;

            if (IsIllegalKey(weaponDefinition, weaponGetter))
                return -1;

            if (weaponDefinition.Forms.Any(form => weaponGetter.ToLink().Equals(form)))
                return ushort.MaxValue + weaponDefinition.Priority; // Weapons assigned to a category by formlink receive priority over everything else.

            if (weaponDefinition.Keywords.Any(key => weaponGetter.HasKeyword(key)))
                return weaponDefinition.Priority;

            if (weaponDefinition.Keys.Any(key => weaponGetter.EditorID?.Replace(" ", "").Contains(key, StringComparison.OrdinalIgnoreCase) ?? false))
                return weaponDefinition.Priority;

            return -1;
        }

        private static Tuple<IFormLinkGetter<ISpellGetter>, IFormLinkGetter<ISpellGetter>> CreateSpellAbilities(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string typeKey)
        {
            var weaponAbilityLeft = state.PatchMod.Spells.AddNew($"WST{typeKey}AbilityLeft");
            weaponAbilityLeft.CastType = CastType.ConstantEffect;
            weaponAbilityLeft.TargetType = TargetType.Self;
            weaponAbilityLeft.Flags = SpellDataFlag.IgnoreResistance | SpellDataFlag.NoAbsorbOrReflect;

            var weaponAbilityRight = state.PatchMod.Spells.AddNew($"WST{typeKey}AbilityRight");
            weaponAbilityRight.CastType = CastType.ConstantEffect;
            weaponAbilityRight.TargetType = TargetType.Self;
            weaponAbilityRight.Flags = SpellDataFlag.IgnoreResistance | SpellDataFlag.NoAbsorbOrReflect;

            return new(weaponAbilityLeft.ToLink(), weaponAbilityRight.ToLink());
        }

        /// <summary>
        /// Generate all the new weapon type keywords based on the dictionary entries in WeaponKeywordDefinitions.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool GenerateWeaponData(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!KeywordList.TryResolve(state.LinkCache, out var keywordListGetter) || !AbilityList.TryResolve(state.LinkCache, out var abilityListGetter))
                throw new Exception("Couldn't find the keyword or ability formlist in \"Keyword Tracker.esp\"");

            ProcessDualWieldList(state);

            var keywordList = state.PatchMod.FormLists.GetOrAddAsOverride(keywordListGetter);
            var abilityList = state.PatchMod.FormLists.GetOrAddAsOverride(abilityListGetter);
            foreach (string typeKey in Program.Settings.WeaponDefinitions.Keys)
            {
                var formattedKey = typeKey.Replace(" ", "");
                var newEquipAbilities = CreateSpellAbilities(state, formattedKey);
                var newKeywordLink = state.PatchMod.Keywords.AddNew(WeapTypePrefix + formattedKey).ToLink();

                KeywordMatches.Add(typeKey, newKeywordLink);
                AbilityMatches.Add(newKeywordLink, newEquipAbilities);
                keywordList.Items.Add(newKeywordLink);
                abilityList.Items.Add(newEquipAbilities.Item1);
                abilityList.Items.Add(newEquipAbilities.Item2);
            }

            return KeywordMatches.Any();
        }

        /// <summary>
        /// Assign the newly generated keywords to the weapons they're supposed to match based on their settings.
        /// </summary>
        /// <param name="state"></param>
        public static void AssignWeaponTypes(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var weaponGetter in state.LoadOrder.PriorityOrder.Weapon().WinningOverrides())
            {
                List<Tuple<KeyValuePair<string, WeaponDefinition>, int>> typeMatches = new();
                foreach (var weaponDefinition in Program.Settings.WeaponDefinitions)
                {
                    int priority = GetTypeMatch(weaponDefinition.Value, weaponGetter);
                    if (priority >= 0)
                        typeMatches.Add(new(weaponDefinition, priority));
                }

                if (!typeMatches.Any())
                    continue;

                // Determine the weapon type that matched the current weapon based on priority.
                string typeKey = typeMatches.MaxBy(x => x.Item2)!.Item1.Key;
                WeaponDefinition typeMatch = Program.Settings.WeaponDefinitions[typeKey];
                Weapon weaponSetter = state.PatchMod.Weapons.GetOrAddAsOverride(weaponGetter);

                (weaponSetter.Keywords ??= new()).Add(KeywordMatches[typeKey]);
                (weaponSetter.Data ??= new()).Skill = typeMatch.WeaponTypeDefinition.IsTwoHanded ? Skill.TwoHanded : Skill.OneHanded;

                typeKey = typeKey.ToLower().Trim();
                typeKey = char.ToUpper(typeKey[0]) + typeKey[1..];
                var whitespaceIdx = Enumerable.Range(0, typeKey.Length).Where(idx => typeKey[idx] == ' ').ToList();
                foreach (var idx in whitespaceIdx)
                    typeKey = typeKey[..(idx + 1)] + char.ToUpper(typeKey[idx + 1]) + typeKey[(idx + 2)..];

                weaponSetter.Name = typeMatch.WeaponTypeDefinition.NameForm.Replace("$name", weaponSetter.Name).Replace("$type", typeKey);
            }

            Console.WriteLine();
        }

        private static void ProcessDualWieldList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var perkLink in Program.Settings.DualWieldPerks)
            {
                if (!perkLink.TryResolve(state.LinkCache, out var perkGetter))
                    continue;

                DualWieldPerks.Add(perkLink);
                foreach (var perkEffect in perkGetter.Effects)
                {
                    if (perkEffect is not IPerkAbilityEffectGetter abilityEffect)
                        continue;

                    if (!abilityEffect.Ability.TryResolve(state.LinkCache, out var abilityGetter))
                        continue;

                    DualWieldSpells.Add(abilityEffect.Ability);

                    foreach (var spellEffect in abilityGetter.Effects)
                    {
                        if (!spellEffect.BaseEffect.TryResolve(state.LinkCache, out var mgefGetter))
                            continue;

                        DualWieldMGEF.Add(spellEffect.BaseEffect);
                    }
                }
            }
        }

        private static bool IsDualWieldCondition<T>(T conditionArray) where T : IReadOnlyList<IConditionGetter>
        {
            if (!conditionArray.Any())
                return false;

            float lhRangeStart = 0, lhRangeEnd = -1;
            float rhRangeStart = 0, rhRangeEnd = -1;
            List<float> lhExclusion = new();
            List<float> rhExclusion = new();
            foreach (var cond in conditionArray)
            {
                if (cond is not IConditionFloatGetter condFloat)
                    continue;

                if (condFloat.Data is not IGetEquippedItemTypeConditionDataGetter condData)
                    continue;

                if (condFloat.CompareOperator == CompareOperator.GreaterThan || condFloat.CompareOperator == CompareOperator.GreaterThanOrEqualTo)
                {
                    if (condData.ItemSource == CastSource.Left)
                        lhRangeStart = condFloat.CompareOperator == CompareOperator.GreaterThan ? condFloat.ComparisonValue + 1 : condFloat.ComparisonValue;
                    else
                        rhRangeStart = condFloat.CompareOperator == CompareOperator.GreaterThan ? condFloat.ComparisonValue + 1 : condFloat.ComparisonValue;
                }
                else if (condFloat.CompareOperator == CompareOperator.LessThan || condFloat.CompareOperator == CompareOperator.LessThanOrEqualTo)
                {
                    if (condData.ItemSource == CastSource.Left)
                        lhRangeEnd = condFloat.CompareOperator == CompareOperator.LessThan ? condFloat.ComparisonValue - 1 : condFloat.ComparisonValue;
                    else
                        rhRangeEnd = condFloat.CompareOperator == CompareOperator.LessThan ? condFloat.ComparisonValue - 1 : condFloat.ComparisonValue;
                }
                else if (condFloat.CompareOperator == CompareOperator.NotEqualTo)
                {
                    if (condData.ItemSource == CastSource.Left)
                        lhExclusion.Add(condFloat.ComparisonValue);
                    else
                        rhExclusion.Add(condFloat.ComparisonValue);
                }
            }

            // If there isn't a start defined but 0 is excluded and numbers below 4 aren't, it still meets the criteria.
            if (lhRangeStart == 0 && lhExclusion.Any(entry => entry == 0) && lhExclusion.All(entry => entry == 0 || entry > 4))
                lhRangeStart = 1;

            if (rhRangeStart == 0 && rhExclusion.Any(entry => entry == 0) && rhExclusion.All(entry => entry == 0 || entry > 4))
                rhRangeStart = 1;


            return lhRangeStart == 1 && rhRangeStart == 1 && lhRangeEnd == 4 && rhRangeEnd == 4;
        }

        public static bool IsDualWieldPerk(IPerkGetter perk, ILinkCache linkCache)
        {
            if (DualWieldPerks.Contains(perk.ToLink()))
                return true;

            bool isDualWield = perk.Effects.Any();
            foreach (var perkEffect in perk.Effects)
            {
                bool effectHasDualWield = perkEffect is IPerkAbilityEffectGetter perkAbility && perkAbility.Ability.TryResolve(linkCache, out var spellGetter) && IsDualWieldSpell(spellGetter, linkCache);
                foreach (var condList in perkEffect.Conditions)
                    effectHasDualWield |= IsDualWieldCondition(condList.Conditions);

                isDualWield &= effectHasDualWield;
            }

            if (isDualWield)
                DualWieldPerks.Add(perk);

            return isDualWield;
        }

        public static bool IsDualWieldSpell(ISpellGetter spell, ILinkCache linkCache)
        {
            if (DualWieldSpells.Contains(spell.ToLink()))
                return true;

            bool isDualWield = spell.Effects.Any();
            foreach (var spellEffect in spell.Effects)
                isDualWield &= IsDualWieldCondition(spellEffect.Conditions) || (spellEffect.BaseEffect.TryResolve(linkCache, out var spellGetter) && IsDualWieldMGEF(spellGetter));

            if (isDualWield)
                DualWieldSpells.Add(spell);

            return isDualWield;
        }

        public static bool IsDualWieldMGEF(IMagicEffectGetter mgef)
        {
            if (DualWieldMGEF.Contains(mgef.ToLink()))
                return true;

            if (IsDualWieldCondition(mgef.Conditions))
            {
                DualWieldMGEF.Add(mgef);
                return true;
            }

            return false;
        }
    }
}
