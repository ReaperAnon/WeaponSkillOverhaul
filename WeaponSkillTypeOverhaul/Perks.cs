using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Pex;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WeaponSkillTypeOverhaul.Perks;

namespace WeaponSkillTypeOverhaul
{
    internal class PerkNode
    {
        public bool Enable = false;
        public IFormLinkGetter<IPerkGetter> Perk = FormLink<IPerkGetter>.Null;
        public float X = 0;
        public float Y = 0;
        public uint GridX = 0;
        public uint GridY = 0;
        public List<int> Links = new();
    }

    internal class PerkFile
    {
        public string Name = "";
        public string Description = "";
        public string Skydome = "";
        public bool SkydomeNormalNif = false;
        public string LevelFile = "";
        public uint LevelID = 0;
        public string RatioFile = "";
        public uint RatioID = 0;
        public string ShowLevelupFile = "";
        public uint ShowLevelupID = 0;
        public string ShowMenuFile = "";
        public uint ShowMenuID = 0;
        public string PerkPointsFile = "";
        public uint PerkPointsID = 0;
        public string LegendaryFile = "";
        public uint LegendaryID = 0;
        public string ColorFile = "";
        public uint ColorID = 0;
        public string DebugReloadFile = "";
        public uint DebugReloadID = 0;
        public List<PerkNode> Nodes = new();
    }

    public class Perks
    {
        static Dictionary<IFormLinkGetter<IPerkGetter>, List<IFormLinkGetter<IPerkGetter>>> PerkLinkDict = new();

        public class ConditionReplacement
        {
            public int ShiftIndex { get; set; } = -1; // should be the index of j, shift everything above j with the shiftamount
            public int ShiftAmount { get; set; } = 0; // should be -1 each time for when there are conditions between j and i for the range check stuff
            public int InsertionIndex { get; set; } = -1;
            public List<int> DeletionIndexes { get; set; } = new();
            public List<Condition> Replacements { get; set; } = new();
        }

        public struct ProcessedCheck
        {
            public float ID { get; set; }
            public CompareOperator Operator { get; set; }

            public ProcessedCheck(float id, CompareOperator op) { ID = id; Operator = op; }
        }

        private static List<IFormLink<IKeywordGetter>> GetKeywordsFromItemType(float retVal)
        {
            List<IFormLink<IKeywordGetter>> keywords = new();
            switch (retVal)
            {
                case 1: keywords.Add(Skyrim.Keyword.WeapTypeSword); break;
                case 2: keywords.Add(Skyrim.Keyword.WeapTypeDagger); break;
                case 3: keywords.Add(Skyrim.Keyword.WeapTypeWarAxe); break;
                case 4: keywords.Add(Skyrim.Keyword.WeapTypeMace); break;
                case 5: keywords.Add(Skyrim.Keyword.WeapTypeGreatsword); break;
                case 6: keywords.Add(Skyrim.Keyword.WeapTypeBattleaxe); keywords.Add(Skyrim.Keyword.WeapTypeWarhammer); break;
                default: return new();
            }

            return keywords;
        }

        private static void AddKeywordConditions(ConditionFloat condFloat, HasKeywordConditionData condData, ConditionReplacement conditionReplacement, ref int numAdded)
        {
            foreach (var typeKey in Program.Settings.WeaponDefinitions.Keys)
            {
                if (!Program.Settings.WeaponDefinitions[typeKey].WeaponTypeDefinition.DesiredType.Equals(condData.Keyword.Link))
                    continue;
                
                ConditionFloat newCondition = condFloat.DeepCopy();
                var copyCondData = (newCondition.Data as HasKeywordConditionData)!;
                copyCondData.Keyword = new FormLinkOrIndex<IKeywordGetter>(copyCondData, Categories.KeywordMatches[typeKey].FormKey);

                if (++numAdded > 1)
                    newCondition.Flags |= Condition.Flag.OR;

                conditionReplacement.Replacements.Add(newCondition);
            }
        }

        private static void AddKeywordConditions(ConditionFloat condFloat, WornHasKeywordConditionData condData, ConditionReplacement conditionReplacement, ref int numAdded)
        {
            foreach (var typeKey in Program.Settings.WeaponDefinitions.Keys)
            {
                if (!Program.Settings.WeaponDefinitions[typeKey].WeaponTypeDefinition.DesiredType.Equals(condData.Keyword.Link))
                    continue;

                ConditionFloat newCondition = condFloat.DeepCopy();
                var copyCondData = (newCondition.Data as WornHasKeywordConditionData)!;
                copyCondData.Keyword = new FormLinkOrIndex<IKeywordGetter>(copyCondData, Categories.KeywordMatches[typeKey].FormKey);

                if (++numAdded > 1)
                    newCondition.Flags |= Condition.Flag.OR;

                conditionReplacement.Replacements.Add(newCondition);
            }
        }

        private static void AddItemTypeConditions(ConditionFloat condFloat, ConditionReplacement conditionReplacement, List<IFormLink<IKeywordGetter>> weapKeywords, CastSource itemSource, bool notEqual, ref int numAdded)
        {
            foreach (var typeKey in Program.Settings.WeaponDefinitions.Keys)
            {
                foreach (var weapKeyword in weapKeywords)
                {
                    if (!Program.Settings.WeaponDefinitions[typeKey].WeaponTypeDefinition.DesiredType.Equals(weapKeyword))
                        continue;

                    ConditionFloat newCondition = new()
                    {
                        ComparisonValue = 1f,
                        CompareOperator = notEqual ? condFloat.CompareOperator : CompareOperator.EqualTo,
                        Data = new HasSpellConditionData(),
                        Flags = condFloat.Flags
                    };
                    (newCondition.Data as HasSpellConditionData)!.Spell = new FormLinkOrIndex<ISpellGetter>(newCondition.Data, itemSource == CastSource.Left ? Categories.AbilityMatches[Categories.KeywordMatches[typeKey]].Item1.FormKey : Categories.AbilityMatches[Categories.KeywordMatches[typeKey]].Item2.FormKey);

                    if (++numAdded > 1)
                        newCondition.Flags |= Condition.Flag.OR;

                    conditionReplacement.Replacements.Add(newCondition);
                }
            }
        }

        private static bool CanSkipItemTypeCondition(ConditionFloat condFloat)
        {
            if (condFloat.CompareOperator == CompareOperator.LessThan && condFloat.ComparisonValue <= 1)
                return true;

            if(condFloat.CompareOperator == CompareOperator.LessThanOrEqualTo && condFloat.ComparisonValue < 1)
                return true;

            if (condFloat.CompareOperator == CompareOperator.GreaterThan && condFloat.ComparisonValue >= 6)
                return true;

            if(condFloat.CompareOperator == CompareOperator.GreaterThanOrEqualTo && condFloat.ComparisonValue > 6)
                return true;

            return false;
        }

        private static bool GetReplacementConditions<T>(T conditionArray, out List<ConditionReplacement> conditionReplacements) where T : IList<Condition>
        {
            List<int> processedIndices = new();

            conditionReplacements = new();
            for (int i = conditionArray.Count - 1; i >= 0; --i)
            {
                if (conditionArray[i] is ConditionFloat condFloat)
                {
                    if (processedIndices.Contains(i))
                        continue;

                    if (condFloat.Data is HasKeywordConditionData hasKeywordData)
                    {
                        int numAdded = 0;
                        ConditionReplacement conditionReplacement = new() { InsertionIndex = i + 1, DeletionIndexes = new() { i } };
                        AddKeywordConditions(condFloat, hasKeywordData, conditionReplacement, ref numAdded);
                        if (conditionReplacement.Replacements.Any())
                            conditionReplacements.Add(conditionReplacement);
                    }
                    else if (condFloat.Data is WornHasKeywordConditionData wornHasKeywordData)
                    {
                        int numAdded = 0;
                        ConditionReplacement conditionReplacement = new() { InsertionIndex = i + 1, DeletionIndexes = new() { i } };
                        AddKeywordConditions(condFloat, wornHasKeywordData, conditionReplacement, ref numAdded);
                        if (conditionReplacement.Replacements.Any())
                            conditionReplacements.Add(conditionReplacement);
                    }
                    else if (condFloat.Data is GetEquippedItemTypeConditionData itemTypeCondData)
                    {
                        int numAdded = 0;
                        if (CanSkipItemTypeCondition(condFloat))
                            continue;

                        // Find the next opposite compare operator condition in the array.
                        if (condFloat.CompareOperator == CompareOperator.LessThan || condFloat.CompareOperator == CompareOperator.LessThanOrEqualTo)
                        {
                            int lowCapIdx = -1;
                            float rangeCapLow = 0;
                            processedIndices.Add(i);
                            for (int j = i - 1; j >= 0; --j)
                            {
                                if (conditionArray[j] is ConditionFloat condLowerFloat && condLowerFloat.Data is GetEquippedItemTypeConditionData condLowerData)
                                {
                                    if (itemTypeCondData.ItemSource != condLowerData.ItemSource || condLowerFloat.CompareOperator != CompareOperator.GreaterThan && condLowerFloat.CompareOperator != CompareOperator.GreaterThanOrEqualTo)
                                        continue;

                                    rangeCapLow = condLowerFloat.CompareOperator == CompareOperator.GreaterThan ? condLowerFloat.ComparisonValue + 1 : condLowerFloat.ComparisonValue;
                                    processedIndices.Add(j);
                                    lowCapIdx = j;
                                }
                            }

                            // If the lower range cap is 0, we need to keep the condition at the top.
                            if (rangeCapLow == 0)
                            {
                                condFloat.CompareOperator = CompareOperator.EqualTo;
                                condFloat.ComparisonValue = 0;
                                condFloat.Flags |= Condition.Flag.OR;
                            }

                            // If the lower range cap is above 0 we can delete the condition, otherwise keep it as modified.
                            ConditionReplacement conditionReplacement = new() { InsertionIndex = i + 1, DeletionIndexes = rangeCapLow > 0 ? new() { i, lowCapIdx } : new() { i } };
                            Range condRange = new((int)rangeCapLow, condFloat.CompareOperator == CompareOperator.LessThan ? (int)condFloat.ComparisonValue - 1 : (int)(condFloat.ComparisonValue));
                            
                            // Already includes all weapon types, no need to overcomplicate the conditions.
                            if (condRange.End.Value >= 7 && condRange.Start.Value <= 0)
                                continue;

                            for (int j = condRange.End.Value; j >= condRange.Start.Value; --j)
                                AddItemTypeConditions(condFloat, conditionReplacement, GetKeywordsFromItemType(j), itemTypeCondData.ItemSource, false, ref numAdded);

                            if (conditionReplacement.Replacements.Any())
                                conditionReplacements.Add(conditionReplacement);

                        } // If there's an unprocessed condition with a greater compare operator, it means it has no higher cap.
                        else if (condFloat.CompareOperator == CompareOperator.GreaterThan || condFloat.CompareOperator == CompareOperator.GreaterThanOrEqualTo)
                        {
                            numAdded = 1; // Since we keep the original condition for values above 6, the original final compare type is still in place, this skips it being added.
                            processedIndices.Add(i);
                            ConditionReplacement conditionReplacement = new() { InsertionIndex = i };
                            Range condRange = new(condFloat.CompareOperator == CompareOperator.GreaterThan ? (int)(condFloat.ComparisonValue + 1) : (int)condFloat.ComparisonValue, 6);

                            // Already includes all weapon types, no need to overcomplicate the conditions.
                            if (condRange.End.Value >= 7 && condRange.Start.Value <= 0)
                                continue;

                            for (int j = condRange.End.Value; j <= condRange.Start.Value; --j)
                                AddItemTypeConditions(condFloat, conditionReplacement, GetKeywordsFromItemType(j), itemTypeCondData.ItemSource, false, ref numAdded);

                            if (conditionReplacement.Replacements.Any())
                            {
                                condFloat.ComparisonValue = condFloat.CompareOperator == CompareOperator.GreaterThan ? 6 : 7;
                                conditionReplacements.Add(conditionReplacement);
                            }
                        }
                        else if (condFloat.CompareOperator == CompareOperator.NotEqualTo)
                        {
                            numAdded = -1; // Negative conditions don't need ORs.
                            ConditionReplacement conditionReplacement = new() { InsertionIndex = i + 1, DeletionIndexes = new() { i } };
                            AddItemTypeConditions(condFloat, conditionReplacement, GetKeywordsFromItemType(condFloat.ComparisonValue), itemTypeCondData.ItemSource, true, ref numAdded);

                            if (conditionReplacement.Replacements.Any())
                                conditionReplacements.Add(conditionReplacement);
                        }
                        else if (condFloat.CompareOperator == CompareOperator.EqualTo)
                        {
                            ConditionReplacement conditionReplacement = new() { InsertionIndex = i + 1, DeletionIndexes = new() { i } };
                            AddItemTypeConditions(condFloat, conditionReplacement, GetKeywordsFromItemType(condFloat.ComparisonValue), itemTypeCondData.ItemSource, false, ref numAdded);

                            if (conditionReplacement.Replacements.Any())
                                conditionReplacements.Add(conditionReplacement);
                        }
                    }
                }
            }

            return conditionReplacements.Any();
        }

        private static bool ReplaceConditions<T>(T conditionArray) where T : IList<Condition>
        {
            if (!GetReplacementConditions(conditionArray, out var conditionReplacements))
                return false;

            List<Tuple<int, int>> indexOffsets = new();
            foreach (var condReplacement in conditionReplacements)
            {
                if (condReplacement.InsertionIndex != -1)
                    foreach (var replacement in condReplacement.Replacements)
                    {
                        int offset = 0;
                        foreach (var pair in indexOffsets)
                        {
                            if (condReplacement.InsertionIndex > pair.Item1)
                                offset += pair.Item2;
                        }

                        conditionArray.Insert(condReplacement.InsertionIndex + offset, replacement);
                    }

                foreach (var deletionIdx in condReplacement.DeletionIndexes)
                {
                    int offset = 0;
                    foreach (var pair in indexOffsets)
                    {
                        if (deletionIdx > pair.Item1)
                            offset += pair.Item2;
                    }

                    conditionArray.RemoveAt(deletionIdx + offset);
                }

                if (condReplacement.ShiftIndex > -1)
                    indexOffsets.Add(new(condReplacement.ShiftIndex, condReplacement.ShiftAmount));
            }

            return true;
        }

        public static bool ReplacePerkConditions(IPerk perk)
        {
            bool wasChanged = false;
            foreach (var perkEffect in perk.Effects)
            {
                foreach (var perkCondition in perkEffect.Conditions)
                    wasChanged |= ReplaceConditions(perkCondition.Conditions);
            }

            return wasChanged;
        }

        public static bool ReplaceSpellConditions(ISpell spell)
        {
            bool wasChanged = false;
            foreach (var spellEffect in spell.Effects)
            {
                for (int i = spellEffect.Conditions.Count - 1; i >= 0; --i)
                    wasChanged |= ReplaceConditions(spellEffect.Conditions);
            }

            return wasChanged;
        }

        public static bool ReplaceMGEFConditions(IMagicEffect mgef)
        {
            bool wasChanged = false;
            for (int i = mgef.Conditions.Count - 1; i >= 0; --i)
                wasChanged |= ReplaceConditions(mgef.Conditions);

            return wasChanged;
        }

        public static void ReplaceAnimationConditions(ISkyrimMod patchMod, ILinkCache linkCache)
        {
            List<FormLink<IIdleAnimationGetter>> animations = new()
            {
                Skyrim.IdleAnimation.AttackLeftHandForwardSprinting,
                Skyrim.IdleAnimation.AttackPowerLeftHandForwardSprinting,
                Skyrim.IdleAnimation.AttackRightForwardSprinting_1hand,
                Skyrim.IdleAnimation.AttackRightForwardSprinting_2hand,
                Skyrim.IdleAnimation.AttackRightPower2HMForwardSprinting,
                Skyrim.IdleAnimation.AttackRightPower2HWForwardSprinting,
                Skyrim.IdleAnimation.AttackRightPowerForwardSprinting
            };

            foreach (var animation in animations)
            {
                if (!animation.TryResolve(linkCache, out var resolvedAnimGetter))
                    continue;

                IdleAnimation resolvedAnim = patchMod.IdleAnimations.GetOrAddAsOverride(resolvedAnimGetter);
                resolvedAnim.Conditions.RemoveAll(cond => cond.Data is HasPerkConditionData);
            }
        }

        public static void ReplaceDualWieldConditions(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach(var perkLink in Categories.DualWieldPerks)
            {
                if (!state.LinkCache.TryResolve(perkLink, out var perkGetter))
                    continue;

                var perk = state.PatchMod.Perks.GetOrAddAsOverride(perkGetter);
                for (int i = perk.Conditions.Count - 1; i >= 0; i--)
                {
                    if (perk.Conditions[i] is not ConditionFloat condFloat)
                        continue;

                    if (condFloat.Data is GetBaseActorValueConditionData itemTypeData)
                    {
                        if (itemTypeData.ActorValue != ActorValue.OneHanded && itemTypeData.ActorValue != ActorValue.TwoHanded)
                            continue;

                        perk.Conditions.Insert(i + 1, condFloat.DeepCopy());
                        condFloat.Flags |= Condition.Flag.OR;
                        itemTypeData.ActorValue = itemTypeData.ActorValue == ActorValue.OneHanded ? ActorValue.TwoHanded : ActorValue.OneHanded;
                    }
                    else if (condFloat.Data is HasPerkConditionData hasPerkData)
                    {
                        if (!Categories.DualWieldPerks.Any(perk => perk.Equals(hasPerkData.Perk.Link)))
                            perk.Conditions.RemoveAt(i);
                    }
                }
            }
        }

        public static void CreatePerkTree(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            PerkFile perkFile = new()
            {
                Name = "Stances",
                Description = "Perks pertaining to stances, like dual wielding perks or any other available ones.",
                Skydome = "dareni\\interface\\intdestinyperkskydome.nif",
                RatioFile = state.OutputPath.Name,
                Nodes = new() { new() {  Enable = true, Links = new() { 1 } } },
            };

            MoveDualWieldPerks(state, perkFile);

            // Connect the nodes.
            // Enumerable.Range(2, perkFile.Nodes.Count - 2).ForEach(idx => perkFile.Nodes[0].Links.Add(idx));
            for (int i = 1; i < perkFile.Nodes.Count; i++)
            {
                for (int j = 1; j < perkFile.Nodes.Count; j++)
                {
                    if (PerkLinkDict[perkFile.Nodes[i].Perk].Any(entry => entry.Equals(perkFile.Nodes[j].Perk)))
                        perkFile.Nodes[i].Links.Add(j);
                }
            }

            // Attach orphaned links to the first node.
            for (int i = 2; i < perkFile.Nodes.Count; i++)
            {
                if (!perkFile.Nodes.Any(node => node.Links.Contains(i)))
                    perkFile.Nodes[1].Links.Add(i);
            }

            WritePerkFile(state, perkFile);
        }

        private static void WritePerkFile(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, PerkFile perkFile)
        {
            var treeWriter = File.CreateText(Path.Combine(state.DataFolderPath, "NetScriptFramework\\Plugins\\CustomSkill.WSOStancePerks.config.txt"));
            treeWriter.Write(
                $"Name = \"{perkFile.Name}\"" +
                $"\nDescription = \"{perkFile.Description}\"" +
                $"\nSkydome = \"{perkFile.Skydome}\"" +
                $"\nSkydomeNormalNif = {perkFile.SkydomeNormalNif}" +
                $"\nLevelFile = \"{perkFile.LevelFile}\"" +
                $"\nLevelId = {perkFile.LevelID}" +
                $"\nRatioFile = \"{perkFile.RatioFile}\"" +
                $"\nRatioId = {perkFile.RatioID}" +
                $"\nShowLevelupFile = \"{perkFile.ShowLevelupFile}\"" +
                $"\nShowLevelupId = {perkFile.ShowLevelupID}" +
                $"\nShowMenuFile = \"{perkFile.ShowMenuFile}\"" +
                $"\nShowMenuId = {perkFile.ShowMenuID}" +
                $"\nPerkPointsFile = \"{perkFile.PerkPointsFile}\"" +
                $"\nPerkPointsId = {perkFile.PerkPointsID}" +
                $"\nLegendaryFile = \"{perkFile.LegendaryFile}\"" +
                $"\nLegendaryId = {perkFile.LegendaryID}" +
                $"\nColorFile = \"{perkFile.ColorFile}\"" +
                $"\nColorId = {perkFile.ColorID}" +
                $"\nDebugReloadFile = \"{perkFile.DebugReloadFile}\"" +
                $"\nDebugReloadId = {perkFile.DebugReloadID}"
                );

            for (int i = 0; i < perkFile.Nodes.Count; i++)
            {
                if (i == 0)
                {
                    treeWriter.Write($"\nNode{i}.Enable = {perkFile.Nodes[i].Enable}" + $"\nNode{i}.Links = \"");
                }
                else
                {
                    treeWriter.Write(
                        $"\nNode{i}.Enable = {perkFile.Nodes[i].Enable}" +
                        $"\nNode{i}.PerkFile = \"{perkFile.Nodes[i].Perk.FormKey.ModKey}\"" +
                        $"\nNode{i}.PerkId = 0x{perkFile.Nodes[i].Perk.FormKey.ID.ToString("x16").TrimStart('0')}" +
                        $"\nNode{i}.X = {perkFile.Nodes[i].X}" +
                        $"\nNode{i}.Y = {perkFile.Nodes[i].Y}" +
                        $"\nNode{i}.GridX = {perkFile.Nodes[i].GridX}" +
                        $"\nNode{i}.GridY = {perkFile.Nodes[i].GridY}" +
                        $"\nNode{i}.Links = \""
                        );
                }

                for (int j = 0; j < perkFile.Nodes[i].Links.Count; j++)
                {
                    treeWriter.Write($"{perkFile.Nodes[i].Links[j]}" + (j < perkFile.Nodes[i].Links.Count - 1 ? ", " : ""));
                }

                treeWriter.Write("\"");
            }

            treeWriter.Close();
        }

        private static void MoveDualWieldPerks(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, PerkFile perkFile)
        {
            if (!state.LinkCache.TryResolve(Skyrim.ActorValueInformation.AVOneHanded, out var oneHandedGetter))
                return;

            var oneHandedSetter = state.PatchMod.ActorValueInformation.GetOrAddAsOverride(oneHandedGetter);
            foreach (var perkEntry in oneHandedSetter.PerkTree)
            {
                if (!Categories.DualWieldPerks.Any(perk => perk.Equals(perkEntry.Perk)))
                    continue;

                List<IFormLinkGetter<IPerkGetter>> links = new();
                perkEntry.ConnectionLineToIndices.ForEach(link => links.Add(oneHandedSetter.PerkTree.First(entry => entry.Index == link).Perk));
                PerkLinkDict[perkEntry.Perk] = links;

                perkFile.Nodes.Add(new()
                {
                    Enable = true,
                    Perk = perkEntry.Perk,
                    X = perkEntry.HorizontalPosition ?? 0,
                    Y = perkEntry.VerticalPosition ?? 0,
                    GridX = perkEntry.PerkGridX ?? 0,
                    GridY = perkEntry.PerkGridY ?? 0,
                    Links = new()
                });
            }
        }

        public static void RemoveDualWieldConnections(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LinkCache.TryResolve(Skyrim.ActorValueInformation.AVOneHanded, out var oneHandedGetter))
                return;

            var oneHandedSetter = state.PatchMod.ActorValueInformation.GetOrAddAsOverride(oneHandedGetter);
            foreach (var perkEntry in oneHandedSetter.PerkTree)
            {
                // Remove dual wield perk connections to regular perks.
                if (Categories.DualWieldPerks.Any(perk => perk.Equals(perkEntry.Perk)))
                {
                    for (int i = perkEntry.ConnectionLineToIndices.Count - 1; i >= 0; i--)
                    {
                        // If the perk index links to a regular perk.
                        if (!Categories.DualWieldPerks.Any(perk => perk.Equals(oneHandedSetter.PerkTree.Where(entry => entry.Index == perkEntry.ConnectionLineToIndices[i]).First().Perk)))
                        {
                            // Find the first link in the chain that is not a dual wield perk.
                            var previousLink = oneHandedSetter.PerkTree.First(entry => entry.ConnectionLineToIndices.Contains(perkEntry.Index ?? 9999));
                            while(Categories.DualWieldPerks.Any(perk => perk.Equals(previousLink.Perk)))
                                previousLink = oneHandedSetter.PerkTree.First(entry => entry.ConnectionLineToIndices.Contains(previousLink.Index ?? 9999));

                            if (previousLink is not null) // If a link is found that is not a dual wield perk, add the removed index to reconnect it.
                                previousLink.ConnectionLineToIndices.Add(perkEntry.ConnectionLineToIndices[i]);

                            // Remove the link.
                            perkEntry.ConnectionLineToIndices.RemoveAt(i);
                        }
                    }
                }
            }

            foreach (var perkEntry in oneHandedSetter.PerkTree)
            {
                // Remove normal perk connections to dual wield perks.
                if (!Categories.DualWieldPerks.Any(perk => perk.Equals(perkEntry.Perk)))
                {
                    for (int i = perkEntry.ConnectionLineToIndices.Count - 1; i >= 0; i--)
                    {
                        // If the perk index links to a dual wield perk.
                        if (Categories.DualWieldPerks.Any(perk => perk.Equals(oneHandedSetter.PerkTree.Where(entry => entry.Index == perkEntry.ConnectionLineToIndices[i]).First().Perk)))
                        {
                            // Add it to the root so it stays in the tree.
                            if(!Program.Settings.MoveDualWieldPerks)
                                oneHandedSetter.PerkTree.First(entry => entry.Index == 0).ConnectionLineToIndices.Add(perkEntry.ConnectionLineToIndices[i]);

                            // Remove the original.
                            perkEntry.ConnectionLineToIndices.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }
}
