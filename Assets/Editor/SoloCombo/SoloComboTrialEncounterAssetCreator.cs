using System;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor.SoloCombo
{
    public static class SoloComboTrialEncounterAssetCreator
    {
        private const string GameDataFolder = "Assets/GameData";
        private const string ResourcesFolder = GameDataFolder + "/Resources";
        private const string SoloComboFolder = ResourcesFolder + "/SoloCombo";
        private const string EncounterFolder = SoloComboFolder + "/Encounters";

        [MenuItem("Tools/SER494/Create or Update Solo Combo Trial Encounters")]
        public static void CreateOrUpdate()
        {
            EnsureFolderPath(EncounterFolder);
            SoloComboEncounterDefinitionAsset greatsword = Synchronize(
                "GreatswordTrialEncounter",
                "training.greatsword",
                "Greatsword Trial",
                "Four scripted rounds for Charge, Clash, Super Armor, Buffs, and split damage. Stamina refills between rounds.",
                "Greatsword Training Knight",
                enemyDefense: 55,
                GreatswordSequences());
            Synchronize(
                "SwordAndShieldTrialEncounter",
                "training.sword-and-shield",
                "Sword & Shield Trial",
                "Four scripted rounds for Guard, Evade, airborne setup, multi-hit, and split healing. Stamina refills between rounds.",
                "Sword & Shield Training Knight",
                enemyDefense: 50,
                SwordAndShieldSequences());
            Synchronize(
                "GlaiveAndShieldTrialEncounter",
                "training.glaive-and-shield",
                "Glaive & Shield Trial",
                "Four scripted rounds for Clash, Guard windows, defense, Buffs, and split recovery. Stamina refills between rounds.",
                "Glaive & Shield Training Knight",
                enemyDefense: 55,
                GlaiveAndShieldSequences());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = greatsword;
            EditorGUIUtility.PingObject(greatsword);
            Debug.Log($"Created or synchronized 3 Solo Combo Encounter assets in {EncounterFolder}.");
        }

        private static SoloComboEncounterDefinitionAsset Synchronize(
            string fileName,
            string encounterId,
            string displayName,
            string description,
            string enemyDisplayName,
            int enemyDefense,
            EnemySequenceAuthoringData[] sequences)
        {
            string path = $"{EncounterFolder}/{fileName}.asset";
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && !(existing is SoloComboEncounterDefinitionAsset))
            {
                throw new InvalidOperationException(
                    $"Cannot create Encounter because {path} contains another asset type.");
            }

            SoloComboEncounterDefinitionAsset asset =
                existing as SoloComboEncounterDefinitionAsset;
            bool isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
                asset.name = fileName;
            }
            else
            {
                Undo.RecordObject(asset, "Synchronize Solo Combo Encounter");
            }

            asset.Configure(
                encounterId,
                displayName,
                description,
                enemyDisplayName,
                newEnemyMaximumHealth: 450,
                newEnemyAttack: 45,
                newEnemyDefense: enemyDefense,
                newManaRecoveryPerTurn: 1,
                newStaminaRecoveryPerTurn: 30,
                newStartingItems: DefaultItems(),
                newScriptedSequences: sequences);

            if (!asset.TryCreateDefinition(out _, out string validationError))
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }

                throw new InvalidOperationException(
                    $"Encounter '{displayName}' is invalid: {validationError}");
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, path);
                Undo.RegisterCreatedObjectUndo(asset, "Create Solo Combo Encounter");
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static EncounterItemAuthoringData[] DefaultItems()
        {
            return new[]
            {
                new EncounterItemAuthoringData("healing-potion", 12),
                new EncounterItemAuthoringData("greater-healing-potion", 4),
                new EncounterItemAuthoringData("vitality-tonic", 6)
            };
        }

        private static EnemySequenceAuthoringData[] GreatswordSequences()
        {
            return new[]
            {
                Sequence("training.greatsword.round-1", "R1 / Combo 1 — Charge Timing",
                    Guard("greatsword.r1.guard", "Charge Check Guard", 12),
                    Damage("greatsword.r1.pressure", "Shoulder Pressure", 6, 0.35f)),
                Sequence("training.greatsword.round-2", "R2 / Combo 2 — Clash then Evade",
                    Damage("greatsword.r2.clash", "Clash Strike", 13, 0.55f),
                    Damage("greatsword.r2.overcommit", "Overcommitted Finish", 10, 0.45f,
                        DamageResolutionTag.Overcommit,
                        onEvaded: Disruption(ActionDisruptionType.Stagger, 2))),
                Sequence("training.greatsword.round-3", "R3 / Combo 3 — Armor and Unguardable",
                    Damage("greatsword.r3.interrupt", "Charge Breaker", 10, 0.25f,
                        onHit: Disruption(ActionDisruptionType.Stagger, 2)),
                    Guard("greatsword.r3.guard", "Unguardable Check", 3),
                    Damage("greatsword.r3.buff-check", "Steadfast Check", 7, 0.40f)),
                Sequence("training.greatsword.round-4", "R4 / Combo 4 — Buff and Split Damage",
                    Damage("greatsword.r4.opening", "Opening Pressure", 6, 0.30f),
                    Guard("greatsword.r4.split-guard", "First Sweep Guard", 17),
                    Damage("greatsword.r4.finish", "Closing Check", 2, 0.25f))
            };
        }

        private static EnemySequenceAuthoringData[] SwordAndShieldSequences()
        {
            return new[]
            {
                Sequence("training.sword-shield.round-1", "R1 / Combo 1 — Evade and Guard Chain",
                    Damage("sword-shield.r1.evade-1", "Evade Check A", 10, 0.25f),
                    Damage("sword-shield.r1.guard", "Guard Check", 2, 0.30f),
                    Damage("sword-shield.r1.evade-2", "Evade Check B", 5, 0.25f),
                    Damage("sword-shield.r1.evade-3", "Evade Check C", 5, 0.25f)),
                Sequence("training.sword-shield.round-2", "R2 / Combo 2 — Three Guard Points",
                    Damage("sword-shield.r2.guard-1", "Opening Guard Check", 4, 0.30f),
                    Damage("sword-shield.r2.guard-2", "Main Guard Check", 9, 0.35f),
                    Damage("sword-shield.r2.guard-3", "Closing Guard Check", 10, 0.40f)),
                Sequence("training.sword-shield.round-3", "R3 / Combo 3 — Airborne Split Attack",
                    Damage("sword-shield.r3.setup", "Airborne Setup Check", 10, 0.25f),
                    Guard("sword-shield.r3.guard", "First Aerial Hit Guard", 3),
                    Damage("sword-shield.r3.second-hit", "Second Aerial Hit Check", 2, 0.30f),
                    Damage("sword-shield.r3.recovery", "Breather Setup", 8, 0.25f)),
                Sequence("training.sword-shield.round-4", "R4 / Combo 4 — Guard QTE and Split Healing",
                    Damage("sword-shield.r4.qte-1", "Guard Window A", 14, 0.20f),
                    Damage("sword-shield.r4.qte-2", "Guard Window B", 1, 0.20f),
                    Damage("sword-shield.r4.qte-3", "Guard Window C", 1, 0.20f),
                    Damage("sword-shield.r4.wound-1", "Potion Setup A", 2, 0.35f),
                    Damage("sword-shield.r4.wound-2", "Potion Setup B", 1, 0.25f))
            };
        }

        private static EnemySequenceAuthoringData[] GlaiveAndShieldSequences()
        {
            return new[]
            {
                Sequence("training.glaive-shield.round-1", "R1 / Combo 1 — Guard, Clash, Counter",
                    Damage("glaive-shield.r1.guard-1", "Opening Guard Check", 4, 0.30f),
                    Damage("glaive-shield.r1.clash", "Clash Strike", 9, 0.55f),
                    Damage("glaive-shield.r1.guard-2", "Counter Guard Check", 3, 0.30f),
                    Damage("glaive-shield.r1.evade", "Closing Evade Check", 7, 0.35f)),
                Sequence("training.glaive-shield.round-2", "R2 / Combo 2 — Defensive Chain and Buff",
                    Damage("glaive-shield.r2.defense-1", "Opening Defense Check", 2, 0.40f),
                    Damage("glaive-shield.r2.defense-2", "Main Defense Check", 8, 0.45f),
                    Damage("glaive-shield.r2.buff", "Empower Check", 11, 0.30f)),
                Sequence("training.glaive-shield.round-3", "R3 / Combo 3 — Armor, Guard, Finish",
                    Damage("glaive-shield.r3.armor-1", "Opening Armor Check", 3, 0.25f,
                        onHit: Disruption(ActionDisruptionType.Stagger, 2)),
                    Damage("glaive-shield.r3.guard", "Main Guard Check", 5, 0.35f),
                    Damage("glaive-shield.r3.armor-2", "Finisher Armor Check", 13, 0.30f,
                        onHit: Disruption(ActionDisruptionType.Stagger, 2))),
                Sequence("training.glaive-shield.round-4", "R4 / Combo 4 — Guard QTE and Recovery",
                    Damage("glaive-shield.r4.qte-1", "Guard Window A", 6, 0.20f),
                    Damage("glaive-shield.r4.qte-2", "Guard Window B", 1, 0.20f),
                    Damage("glaive-shield.r4.qte-3", "Guard Window C", 1, 0.20f),
                    Damage("glaive-shield.r4.linked-guard", "Linked Guard Check", 10, 0.35f),
                    Damage("glaive-shield.r4.recovery", "Recovery Setup", 4, 0.25f))
            };
        }

        private static EnemySequenceAuthoringData Sequence(
            string id,
            string name,
            params EnemyActionAuthoringData[] actions)
        {
            return new EnemySequenceAuthoringData(id, name, actions);
        }

        private static EnemyActionAuthoringData Damage(
            string id,
            string name,
            int cost,
            float multiplier,
            DamageResolutionTag tags = DamageResolutionTag.None,
            ActionDisruptionDefinition onHit = null,
            ActionDisruptionDefinition onEvaded = null)
        {
            return new EnemyActionAuthoringData(
                id,
                name,
                cost,
                ActionResolutionEffect.Damage,
                multiplier,
                damageTags: tags,
                onHitDisruption: onHit,
                onEvadedDisruption: onEvaded);
        }

        private static EnemyActionAuthoringData Guard(string id, string name, int cost)
        {
            return new EnemyActionAuthoringData(
                id,
                name,
                cost,
                ActionResolutionEffect.Guard);
        }

        private static ActionDisruptionDefinition Disruption(
            ActionDisruptionType type,
            int duration)
        {
            return new ActionDisruptionDefinition(type, duration);
        }

        private static void EnsureFolderPath(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
