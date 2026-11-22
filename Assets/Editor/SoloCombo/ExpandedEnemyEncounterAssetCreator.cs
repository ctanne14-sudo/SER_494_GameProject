using System;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor.CombatIntegration
{
    public static class ExpandedEnemyEncounterAssetCreator
    {
        private const string EncounterFolder =
            "Assets/GameData/Resources/SoloCombo/Encounters";

        [MenuItem("Tools/SER494/Create or Update Expanded Enemy Encounters")]
        public static void CreateOrUpdateAssets()
        {
            CreateThornSlime();
            CreateOreMonster(isElite: false);
            CreateOreMonster(isElite: true);
            CreateDungeonThief(isSecondVariant: false);
            CreateDungeonThief(isSecondVariant: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Created or updated five expanded reusable Enemy Encounter assets.");
        }

        private static void CreateThornSlime()
        {
            const string enemyId = "thorn-slime";
            EnemyActionAuthoringData action1 = Action(enemyId, 1,
                Damage(4, 0.40f, DamageType.Piercing),
                Damage(7, 0.40f, DamageType.Piercing));
            EnemyActionAuthoringData action2 = Action(enemyId, 2,
                Damage(6, 0.70f, DamageType.Blunt),
                Damage(7, 0.10f, DamageType.Piercing),
                Damage(8, 0.10f, DamageType.Piercing));
            EnemyActionAuthoringData action3 = Action(enemyId, 3,
                Damage(3, 0.30f, DamageType.Blunt),
                Evade(4),
                Damage(8, 0.40f, DamageType.Blunt));
            EnemyActionAuthoringData action4 = ActionWithWindows(
                enemyId,
                4,
                new[] { Damage(7, 0.30f, DamageType.Piercing) },
                new[]
                {
                    new EnemyActiveWindowAuthoringData(
                        "enemy.thorn-slime.action-4.damage-reduction",
                        "Damage Taken -50%",
                        1,
                        10,
                        ActiveWindowEffect.DefensiveState,
                        damageReduction: 0.50f)
                });
            EnemyActionAuthoringData action5 = Action(enemyId, 5,
                Status(
                    4,
                    "status.thorn-slime.slashing-resistance",
                    "Slashing Damage Taken -20%",
                    CombatStatusDurationType.TimeSteps,
                    30,
                    CombatStatusModifierDirection.DamageTaken,
                    DamageModifier.ForType(DamageType.Slashing, -0.20d)),
                Status(
                    5,
                    "status.thorn-slime.piercing-power",
                    "Piercing Damage Dealt +20%",
                    CombatStatusDurationType.TimeSteps,
                    30,
                    CombatStatusModifierDirection.DamageDealt,
                    DamageModifier.ForType(DamageType.Piercing, 0.20d)));
            EnemyActionAuthoringData action6 = Action(enemyId, 6,
                Damage(1, 0.20f, DamageType.Blunt),
                Evade(2),
                Evade(3),
                Evade(9));
            EnemyActionAuthoringData action7 = Action(enemyId, 7,
                Damage(3, 0.10f, DamageType.Piercing),
                Damage(4, 0.10f, DamageType.Piercing),
                Damage(5, 0.10f, DamageType.Piercing),
                Damage(6, 0.10f, DamageType.Piercing),
                Damage(7, 0.10f, DamageType.Piercing),
                Damage(8, 1.00f, DamageType.Piercing));

            CreateOrUpdateEncounter(
                "ThornSlimeEncounter.asset",
                "encounter.thorn-slime",
                "Thorn Slime Encounter",
                "A four-round Thorn Slime combat cycle.",
                "Thorn Slime",
                maximumHealth: 450,
                attack: 40,
                defense: 15,
                rewardGold: 150,
                materialRolls: 3,
                materialDrops: new[]
                {
                    Drop(BattleRewardSummary.ThornMaterialId, 50),
                    Drop(BattleRewardSummary.SlimeChunkMaterialId, 50)
                },
                goldDrops: null,
                sequences: new[]
                {
                    Sequence(enemyId, 1, action1, action5, action3),
                    Sequence(enemyId, 2, action2, action6, action4),
                    Sequence(enemyId, 3, action1, action4, action2),
                    Sequence(enemyId, 4, action5, action7, action3)
                });
        }

        private static void CreateOreMonster(bool isElite)
        {
            string enemyId = isElite ? "elite-ore-monster" : "ore-monster";
            float action1FinalDamage = isElite ? 0.60f : 0.40f;
            float action2FirstDamage = isElite ? 0.30f : 0.20f;
            float resistanceAttackDamage = isElite ? 0.70f : 0.60f;
            float action5FirstDamage = isElite ? 0.40f : 0.20f;
            float action5SecondDamage = isElite ? 0.30f : 0.20f;
            float action6Damage = isElite ? 0.40f : 0.20f;
            float overcommitDamage = isElite ? 1.50f : 1.30f;

            EnemyActionAuthoringData action1 = Action(enemyId, 1,
                Damage(3, 0.30f, DamageType.Blunt),
                Damage(5, 0.30f, DamageType.Blunt),
                Damage(8, action1FinalDamage, DamageType.Blunt));
            EnemyActionAuthoringData action2 = Action(enemyId, 2,
                Guard(2),
                Guard(4),
                Damage(6, action2FirstDamage, DamageType.Blunt),
                Damage(7, 0.30f, DamageType.Blunt));
            EnemyActionAuthoringData action3 = Action(enemyId, 3,
                Status(
                    3,
                    $"status.{enemyId}.slashing-resistance",
                    "Slashing Damage Taken -30%",
                    CombatStatusDurationType.TimeSteps,
                    30,
                    CombatStatusModifierDirection.DamageTaken,
                    DamageModifier.ForType(DamageType.Slashing, -0.30d)),
                Damage(6, resistanceAttackDamage, DamageType.Blunt));
            EnemyActionAuthoringData action4 = Action(enemyId, 4,
                Status(
                    3,
                    $"status.{enemyId}.piercing-resistance",
                    "Piercing Damage Taken -30%",
                    CombatStatusDurationType.TimeSteps,
                    30,
                    CombatStatusModifierDirection.DamageTaken,
                    DamageModifier.ForType(DamageType.Piercing, -0.30d)),
                Damage(6, resistanceAttackDamage, DamageType.Blunt));
            EnemyActionAuthoringData action5 = Action(enemyId, 5,
                Status(
                    1,
                    $"status.{enemyId}.slashing-power",
                    "Slashing Damage Dealt +20%",
                    CombatStatusDurationType.Rounds,
                    1,
                    CombatStatusModifierDirection.DamageDealt,
                    DamageModifier.ForType(DamageType.Slashing, 0.20d)),
                Damage(5, action5FirstDamage, DamageType.Slashing),
                Damage(9, action5SecondDamage, DamageType.Slashing));
            EnemyActionAuthoringData action6 = Action(enemyId, 6,
                Damage(2, action6Damage, DamageType.Slashing),
                Guard(4),
                Damage(7, action6Damage, DamageType.Slashing),
                Guard(9));
            EnemyActionAuthoringData action7 = Action(enemyId, 7,
                Status(
                    4,
                    $"status.{enemyId}.blunt-power",
                    "Blunt Damage Dealt +20%",
                    CombatStatusDurationType.TimeSteps,
                    60,
                    CombatStatusModifierDirection.DamageDealt,
                    DamageModifier.ForType(DamageType.Blunt, 0.20d)),
                Damage(
                    8,
                    overcommitDamage,
                    DamageType.Blunt,
                    DamageResolutionTag.Overcommit,
                    new ActionDisruptionDefinition(ActionDisruptionType.Stagger, 2)));

            CreateOrUpdateEncounter(
                isElite ? "EliteOreMonsterEncounter.asset" : "OreMonsterEncounter.asset",
                $"encounter.{enemyId}",
                isElite ? "Elite Ore Monster Encounter" : "Ore Monster Encounter",
                isElite
                    ? "A four-round elite Ore Monster combat cycle."
                    : "A four-round Ore Monster combat cycle.",
                isElite ? "Elite Ore Monster" : "Ore Monster",
                maximumHealth: isElite ? 800 : 700,
                attack: 50,
                defense: isElite ? 50 : 40,
                rewardGold: isElite ? 500 : 400,
                materialRolls: 5,
                materialDrops: isElite
                    ? new[]
                    {
                        Drop(BattleRewardSummary.CrystalMaterialId, 33),
                        Drop(BattleRewardSummary.PureGoldMaterialId, 34),
                        Drop(BattleRewardSummary.MithrilMaterialId, 33)
                    }
                    : new[]
                    {
                        Drop(BattleRewardSummary.CrystalMaterialId, 17),
                        Drop(BattleRewardSummary.PureGoldMaterialId, 16),
                        Drop(BattleRewardSummary.MithrilMaterialId, 17),
                        Drop(BattleRewardSummary.WhiteCopperMaterialId, 18),
                        Drop(BattleRewardSummary.BronzeMaterialId, 16),
                        Drop(BattleRewardSummary.BrassMaterialId, 16)
                    },
                goldDrops: null,
                sequences: new[]
                {
                    Sequence(enemyId, 1, action1, action4, action5),
                    Sequence(enemyId, 2, action2, action3, action1),
                    Sequence(enemyId, 3, action2, action7, action1),
                    Sequence(enemyId, 4, action3, action5, action6)
                });
        }

        private static void CreateDungeonThief(bool isSecondVariant)
        {
            string enemyId = isSecondVariant ? "dungeon-thief-2" : "dungeon-thief-1";
            EnemyActionAuthoringData[] actions = isSecondVariant
                ? CreateDungeonThiefTwoActions(enemyId)
                : CreateDungeonThiefOneActions(enemyId);

            CreateOrUpdateEncounter(
                isSecondVariant
                    ? "DungeonThiefIIEncounter.asset"
                    : "DungeonThiefIEncounter.asset",
                $"encounter.{enemyId}",
                isSecondVariant
                    ? "Dungeon Thief II Encounter"
                    : "Dungeon Thief I Encounter",
                isSecondVariant
                    ? "A three-round defensive Dungeon Thief combat cycle."
                    : "A three-round evasive Dungeon Thief combat cycle.",
                isSecondVariant ? "Dungeon Thief II" : "Dungeon Thief I",
                maximumHealth: 250,
                attack: isSecondVariant ? 35 : 30,
                defense: isSecondVariant ? 10 : 0,
                rewardGold: 0,
                materialRolls: 3,
                materialDrops: new[]
                {
                    Drop(BattleRewardSummary.YellowSilkMaterialId, 41),
                    Drop(BattleRewardSummary.RedSilkMaterialId, 41),
                    Drop(BattleRewardSummary.CrystalMaterialId, 6),
                    Drop(BattleRewardSummary.PureGoldMaterialId, 6),
                    Drop(BattleRewardSummary.MithrilMaterialId, 6)
                },
                goldDrops: new[]
                {
                    new EncounterGoldRewardAuthoringData(50, 35),
                    new EncounterGoldRewardAuthoringData(100, 31),
                    new EncounterGoldRewardAuthoringData(150, 21),
                    new EncounterGoldRewardAuthoringData(200, 10),
                    new EncounterGoldRewardAuthoringData(1000, 3)
                },
                sequences: isSecondVariant
                    ? new[]
                    {
                        Sequence(enemyId, 1, actions[5], actions[0], actions[1]),
                        Sequence(enemyId, 2, actions[1], actions[4], actions[3]),
                        Sequence(enemyId, 3, actions[2], actions[3], actions[0])
                    }
                    : new[]
                    {
                        Sequence(enemyId, 1, actions[0], actions[3], actions[4]),
                        Sequence(enemyId, 2, actions[2], actions[5], actions[4]),
                        Sequence(enemyId, 3, actions[0], actions[1], actions[2])
                    });
        }

        private static EnemyActionAuthoringData[] CreateDungeonThiefOneActions(string enemyId)
        {
            return new[]
            {
                Action(enemyId, 1, Damage(5, 0.80f, DamageType.Slashing)),
                Action(enemyId, 2, Evade(3), Damage(8, 0.60f, DamageType.Slashing)),
                Action(enemyId, 3,
                    Damage(2, 0.20f, DamageType.Slashing), Evade(6), Evade(10)),
                Action(enemyId, 4, Evade(1), Evade(5), Evade(7)),
                Action(enemyId, 5,
                    Damage(5, 0.40f, DamageType.Slashing),
                    Damage(9, 0.40f, DamageType.Slashing)),
                Action(enemyId, 6, Evade(2), Evade(5), Evade(8))
            };
        }

        private static EnemyActionAuthoringData[] CreateDungeonThiefTwoActions(string enemyId)
        {
            return new[]
            {
                Action(enemyId, 1, Damage(5, 0.70f, DamageType.Blunt)),
                ActionWithWindows(
                    enemyId,
                    2,
                    new[]
                    {
                        Damage(6, 0.30f, DamageType.Blunt),
                        Damage(8, 0.40f, DamageType.Blunt)
                    },
                    new[]
                    {
                        new EnemyActiveWindowAuthoringData(
                            "enemy.dungeon-thief-2.action-2.damage-reduction",
                            "Damage Taken -10%",
                            1,
                            10,
                            ActiveWindowEffect.DefensiveState,
                            damageReduction: 0.10f)
                    }),
                Action(enemyId, 3,
                    Damage(2, 0.30f, DamageType.Blunt),
                    Evade(5),
                    Damage(8, 0.40f, DamageType.Blunt)),
                Action(enemyId, 4,
                    Evade(1),
                    Damage(6, 0.30f, DamageType.Blunt),
                    Damage(8, 0.40f, DamageType.Blunt)),
                Action(enemyId, 5,
                    Guard(1),
                    Damage(4, 0.20f, DamageType.Blunt),
                    Damage(5, 0.20f, DamageType.Blunt),
                    Guard(6),
                    Damage(9, 0.70f, DamageType.Blunt)),
                Action(enemyId, 6, Evade(2), Evade(5), Evade(8))
            };
        }

        private static void CreateOrUpdateEncounter(
            string assetName,
            string encounterId,
            string displayName,
            string description,
            string enemyName,
            int maximumHealth,
            int attack,
            int defense,
            int rewardGold,
            int materialRolls,
            EncounterRewardDropAuthoringData[] materialDrops,
            EncounterGoldRewardAuthoringData[] goldDrops,
            EnemySequenceAuthoringData[] sequences)
        {
            string assetPath = $"{EncounterFolder}/{assetName}";
            SoloComboEncounterDefinitionAsset encounter =
                AssetDatabase.LoadAssetAtPath<SoloComboEncounterDefinitionAsset>(assetPath);
            bool isNew = encounter == null;
            if (isNew)
            {
                encounter = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
                encounter.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
            else
            {
                Undo.RecordObject(encounter, $"Update {displayName}");
            }

            encounter.Configure(
                encounterId,
                displayName,
                description,
                enemyName,
                maximumHealth,
                attack,
                defense,
                newManaRecoveryPerTurn: 1,
                newStaminaRecoveryPerTurn: 10,
                newStartingItems: Array.Empty<EncounterItemAuthoringData>(),
                newScriptedSequences: sequences,
                newRewardGold: rewardGold,
                newRewardMaterialRollCount: materialRolls,
                newRewardMaterialDrops: materialDrops,
                newRewardGoldDrops: goldDrops);

            if (!encounter.TryCreateDefinition(out _, out string validationError))
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(encounter);
                }

                throw new InvalidOperationException(
                    $"Encounter '{displayName}' is invalid: {validationError}");
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(encounter, assetPath);
                Undo.RegisterCreatedObjectUndo(encounter, $"Create {displayName}");
            }

            EditorUtility.SetDirty(encounter);
        }

        private static EnemyActionAuthoringData Action(
            string enemyId,
            int actionNumber,
            params EnemyResolutionEventAuthoringData[] events)
        {
            return ActionWithWindows(enemyId, actionNumber, events, null);
        }

        private static EnemyActionAuthoringData ActionWithWindows(
            string enemyId,
            int actionNumber,
            EnemyResolutionEventAuthoringData[] events,
            EnemyActiveWindowAuthoringData[] windows)
        {
            return new EnemyActionAuthoringData(
                $"enemy.{enemyId}.action-{actionNumber}",
                $"Action {actionNumber}",
                10,
                events,
                windows);
        }

        private static EnemySequenceAuthoringData Sequence(
            string enemyId,
            int roundNumber,
            params EnemyActionAuthoringData[] actions)
        {
            return new EnemySequenceAuthoringData(
                $"enemy.{enemyId}.round-{roundNumber}",
                $"Round {roundNumber}",
                actions);
        }

        private static EncounterRewardDropAuthoringData Drop(string itemId, int weight)
        {
            return new EncounterRewardDropAuthoringData(itemId, weight);
        }

        private static EnemyResolutionEventAuthoringData Damage(
            int offset,
            float multiplier,
            DamageType damageType,
            DamageResolutionTag tags = DamageResolutionTag.None,
            ActionDisruptionDefinition onEvadedDisruption = null)
        {
            return new EnemyResolutionEventAuthoringData(
                offset,
                ActionResolutionEffect.Damage,
                multiplier,
                damageType,
                tags,
                onEvadedDisruption: onEvadedDisruption);
        }

        private static EnemyResolutionEventAuthoringData Guard(int offset)
        {
            return new EnemyResolutionEventAuthoringData(offset, ActionResolutionEffect.Guard);
        }

        private static EnemyResolutionEventAuthoringData Evade(int offset)
        {
            return new EnemyResolutionEventAuthoringData(offset, ActionResolutionEffect.Evade);
        }

        private static EnemyResolutionEventAuthoringData Status(
            int offset,
            string statusId,
            string displayName,
            CombatStatusDurationType durationType,
            int duration,
            CombatStatusModifierDirection direction,
            DamageModifier modifier)
        {
            return new EnemyResolutionEventAuthoringData(
                offset,
                ActionResolutionEffect.ApplyBuff,
                statusEffect: new CombatStatusDefinition(
                    statusId,
                    displayName,
                    durationType,
                    duration,
                    direction,
                    modifier,
                    CombatStatusStackingPolicy.IndependentInstances),
                statusTarget: ActionStatusTarget.Self);
        }
    }
}
