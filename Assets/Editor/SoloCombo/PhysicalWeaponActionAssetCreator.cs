using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor.SoloCombo
{
    public static class PhysicalWeaponActionAssetCreator
    {
        private const string GameDataFolder = "Assets/GameData";
        private const string SoloComboFolder = GameDataFolder + "/SoloCombo";
        private const string WeaponActionsFolder = SoloComboFolder + "/WeaponActions";
        private const string GreatswordFolder = WeaponActionsFolder + "/Greatsword";
        private const string SwordAndShieldFolder = WeaponActionsFolder + "/SwordAndShield";
        private const string GlaiveAndShieldFolder = WeaponActionsFolder + "/GlaiveAndShield";
        private const string AirborneSetupStatusId = "sequence.airborne-setup";
        private const string AirborneSetupDisplayName = "Airborne Setup";

        [MenuItem("Tools/SER494/Create Physical Weapon Actions/Greatsword")]
        public static void CreateGreatswordActions()
        {
            CreateActions(GreatswordFolder, "Greatsword", GreatswordSpecifications());
        }

        [MenuItem("Tools/SER494/Create Physical Weapon Actions/Sword and Shield")]
        public static void CreateSwordAndShieldActions()
        {
            CreateActions(
                SwordAndShieldFolder,
                "Sword and Shield",
                SwordAndShieldSpecifications());
        }

        [MenuItem("Tools/SER494/Create Physical Weapon Actions/Glaive and Shield")]
        public static void CreateGlaiveAndShieldActions()
        {
            CreateActions(
                GlaiveAndShieldFolder,
                "Glaive and Shield",
                GlaiveAndShieldSpecifications());
        }

        private static void CreateActions(
            string folder,
            string actionSetName,
            ActionSpec[] specifications)
        {
            EnsureFolderPath(folder);
            var synchronized = new List<ComboActionAsset>(specifications.Length);

            for (int index = 0; index < specifications.Length; index++)
            {
                synchronized.Add(Synchronize(folder, actionSetName, specifications[index]));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = synchronized[0];
            EditorGUIUtility.PingObject(synchronized[0]);
            Debug.Log(
                $"Created or synchronized {synchronized.Count} {actionSetName} Action asset(s) in {folder}.");
        }

        private static ActionSpec[] GreatswordSpecifications()
        {
            const CompatibleWeaponTypes weapon = CompatibleWeaponTypes.Greatsword;
            return new[]
            {
                new ActionSpec(
                    "Greatsword_SweepingSlash_O",
                    "greatsword.opening.sweeping-slash",
                    "Sweeping Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 7,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(
                            offset: 5,
                            multiplier: 0.80f,
                            DamageType.Slashing,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Flinch, 2))
                    }),
                new ActionSpec(
                    "Greatsword_DrawSlash_O",
                    "greatsword.opening.draw-slash",
                    "Draw Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 6,
                    staminaCost: 1,
                    weapon,
                    resolutions: DamageOnly(4, 0.70f, DamageType.Slashing)),
                new ActionSpec(
                    "Greatsword_QuickDrawSlash_O",
                    "greatsword.opening.quick-draw-slash",
                    "Quick Draw Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 6,
                    staminaCost: 1,
                    weapon,
                    resolutions: DamageOnly(3, 0.70f, DamageType.Slashing)),
                new ActionSpec(
                    "Greatsword_LeapingSweep_O",
                    "greatsword.opening.leaping-sweep",
                    "Leaping Sweep [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 8,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(
                            offset: 5,
                            multiplier: 1.10f,
                            DamageType.Slashing,
                            damageTags: DamageResolutionTag.Overcommit,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Flinch, 2))
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "greatsword.leaping-sweep.armor",
                            "Leaping Sweep Armor",
                            startOffset: 4,
                            endOffset: 5,
                            level: 3)
                    }),

                new ActionSpec(
                    "Greatsword_ChargedSlash_M",
                    "greatsword.main.charged-slash",
                    "Charged Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 10,
                    staminaCost: 8,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(
                            offset: 6,
                            multiplier: 1.20f,
                            DamageType.Slashing,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Stagger, 2))
                    },
                    chargeLevels: new[]
                    {
                        new ChargeLevelSpec(additionalTimelineCost: 2, damageMultiplierScale: 4f / 3f),
                        new ChargeLevelSpec(additionalTimelineCost: 4, damageMultiplierScale: 5f / 3f)
                    },
                    disruptionResponses: ChargeCancelThenPause(timelineCost: 10)),
                new ActionSpec(
                    "Greatsword_VerticalSlash_M",
                    "greatsword.main.vertical-slash",
                    "Vertical Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 7,
                    staminaCost: 5,
                    weapon,
                    resolutions: DamageOnly(5, 1.20f, DamageType.Slashing)),
                new ActionSpec(
                    "Greatsword_ClashSlash_M",
                    "greatsword.main.clash-slash",
                    "Clash Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 8,
                    staminaCost: 6,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(
                            offset: 5,
                            multiplier: 1.10f,
                            DamageType.Slashing,
                            damageTags: DamageResolutionTag.Clash,
                            clashDamageReduction: 0.70f,
                            clashSuperArmorLevel: 5)
                    }),
                new ActionSpec(
                    "Greatsword_StrongChargedSlash_M",
                    "greatsword.main.strong-charged-slash",
                    "Strong Charged Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 12,
                    staminaCost: 12,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(
                            offset: 6,
                            multiplier: 1.60f,
                            DamageType.Slashing,
                            damageTags: DamageResolutionTag.Unguardable,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Stagger, 2))
                    },
                    chargeLevels: new[]
                    {
                        new ChargeLevelSpec(additionalTimelineCost: 2, damageMultiplierScale: 1.25f),
                        new ChargeLevelSpec(additionalTimelineCost: 4, damageMultiplierScale: 1.50f)
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "greatsword.strong-charge.armor",
                            "Strong Charge Armor",
                            startOffset: 1,
                            endOffset: 5,
                            level: 3,
                            extendsThroughCharge: true)
                    },
                    disruptionResponses: ChargeCancelThenPause(timelineCost: 12)),

                new ActionSpec(
                    "Greatsword_EmpoweredSheathe_C",
                    "greatsword.closing.empowered-sheathe",
                    "Empowered Sheathe [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 3,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Buff(
                            offset: 3,
                            "greatsword.empowered-sheathe.slashing-damage",
                            "Slashing Damage +20%",
                            duration: 30,
                            modifierDirection: CombatStatusModifierDirection.DamageDealt,
                            modifierScope: DamageModifierScope.Type,
                            modifierType: DamageType.Slashing,
                            modifierAmount: 0.20f,
                            stackingPolicy: CombatStatusStackingPolicy.IndependentInstances)
                    }),
                new ActionSpec(
                    "Greatsword_DoubleSweep_C",
                    "greatsword.closing.double-sweep",
                    "Double Sweep [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 9,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(4, 0.60f, DamageType.Slashing),
                        ResolutionSpec.Damage(
                            6,
                            0.70f,
                            DamageType.Slashing,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Flinch, 2))
                    }),
                new ActionSpec(
                    "Greatsword_SteadfastSlash_C",
                    "greatsword.closing.steadfast-slash",
                    "Steadfast Slash [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 6,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Buff(
                            offset: 1,
                            "greatsword.steadfast-slash.defense",
                            "Damage Taken -20%",
                            duration: 5,
                            modifierDirection: CombatStatusModifierDirection.DamageTaken,
                            modifierScope: DamageModifierScope.All,
                            modifierType: DamageType.Slashing,
                            modifierAmount: -0.20f),
                        ResolutionSpec.Damage(4, 1.00f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "Greatsword_SidestepSheathe_C",
                    "greatsword.closing.sidestep-sheathe",
                    "Sidestep Sheathe [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 4,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Simple(2, ActionResolutionEffect.Evade),
                        ResolutionSpec.Buff(
                            offset: 4,
                            "greatsword.sidestep-sheathe.slashing-damage",
                            "Slashing Damage +10%",
                            duration: 30,
                            modifierDirection: CombatStatusModifierDirection.DamageDealt,
                            modifierScope: DamageModifierScope.Type,
                            modifierType: DamageType.Slashing,
                            modifierAmount: 0.10f)
                    }),

                new ActionSpec(
                    "Greatsword_ShoulderTackle_L",
                    "greatsword.linked.shoulder-tackle",
                    "Shoulder Tackle [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 3,
                    staminaCost: 3,
                    weapon,
                    resolutions: DamageOnly(2, 0.20f, DamageType.Blunt),
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "greatsword.shoulder-tackle.armor",
                            "Shoulder Tackle Armor",
                            startOffset: 1,
                            endOffset: 3,
                            level: 5)
                    }),
                new ActionSpec(
                    "Greatsword_GuardingCharge_L",
                    "greatsword.linked.guarding-charge",
                    "Guarding Charge [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 5,
                    staminaCost: 5,
                    weapon,
                    resolutions: DamageOnly(4, 0.20f, DamageType.Blunt),
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "greatsword.guarding-charge.armor",
                            "Guarding Charge Armor",
                            startOffset: 1,
                            endOffset: 5,
                            level: 5),
                        ActiveWindowSpec.Defense(
                            "greatsword.guarding-charge.defense",
                            "Guarding Charge Defense",
                            startOffset: 1,
                            endOffset: 5,
                            damageReduction: 0.30f)
                    }),
                new ActionSpec(
                    "Greatsword_DraggingCharge_L",
                    "greatsword.linked.dragging-charge",
                    "Dragging Charge [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 7,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(4, 0.20f, DamageType.Blunt),
                        ResolutionSpec.Damage(6, 0.70f, DamageType.Slashing)
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "greatsword.dragging-charge.armor",
                            "Dragging Charge Armor",
                            startOffset: 1,
                            endOffset: 5,
                            level: 5)
                    }),
                new ActionSpec(
                    "Greatsword_VerticalSlash_L",
                    "greatsword.linked.vertical-slash",
                    "Vertical Slash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 5,
                    staminaCost: 5,
                    weapon,
                    resolutions: DamageOnly(3, 0.90f, DamageType.Slashing))
            };
        }

        private static ActionSpec[] SwordAndShieldSpecifications()
        {
            const CompatibleWeaponTypes weapon = CompatibleWeaponTypes.SwordAndShield;
            return new[]
            {
                new ActionSpec(
                    "SwordAndShield_HorizontalSlash_O",
                    "sword-and-shield.opening.horizontal-slash",
                    "Horizontal Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 3,
                    staminaCost: 0,
                    weapon,
                    resolutions: DamageOnly(2, 0.30f, DamageType.Slashing)),
                new ActionSpec(
                    "SwordAndShield_AdvancingSlash_O",
                    "sword-and-shield.opening.advancing-slash",
                    "Advancing Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 6,
                    staminaCost: 2,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Damage(4, 0.40f, DamageType.Blunt)
                    }),
                new ActionSpec(
                    "SwordAndShield_GuardedAdvance_O",
                    "sword-and-shield.opening.guarded-advance",
                    "Guarded Advance [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 7,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(4, 0.40f, DamageType.Piercing),
                        ResolutionSpec.Damage(5, 0.40f, DamageType.Slashing)
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.Defense(
                            "sword-and-shield.guarded-advance.defense",
                            "Guarded Advance Defense",
                            startOffset: 1,
                            endOffset: 7,
                            damageReduction: 0.40f),
                        ActiveWindowSpec.SuperArmor(
                            "sword-and-shield.guarded-advance.armor",
                            "Guarded Advance Armor",
                            startOffset: 1,
                            endOffset: 7,
                            level: 3)
                    }),
                new ActionSpec(
                    "SwordAndShield_RestrainingSlash_O",
                    "sword-and-shield.opening.restraining-slash",
                    "Restraining Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 5,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Simple(4, ActionResolutionEffect.Guard)
                    }),

                new ActionSpec(
                    "SwordAndShield_SwordShieldCombo_M",
                    "sword-and-shield.main.sword-shield-combo",
                    "Sword and Shield Combo [M]",
                    ComboActionCategory.Main,
                    timelineCost: 10,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.10f, DamageType.Slashing),
                        ResolutionSpec.Damage(4, 0.10f, DamageType.Blunt),
                        ResolutionSpec.Damage(5, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Damage(6, 0.20f, DamageType.Blunt),
                        ResolutionSpec.Simple(8, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(9, 0.40f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "SwordAndShield_RisingSlash_M",
                    "sword-and-shield.main.rising-slash",
                    "Rising Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 5,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.60f, DamageType.Slashing),
                        ResolutionSpec.StatusSetup(
                            4,
                            AirborneSetupStatusId,
                            AirborneSetupDisplayName)
                    }),
                new ActionSpec(
                    "SwordAndShield_FourfoldTurningSlash_M",
                    "sword-and-shield.main.fourfold-turning-slash",
                    "Fourfold Turning Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 8,
                    staminaCost: 8,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Simple(3, ActionResolutionEffect.Evade),
                        ResolutionSpec.Damage(4, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Simple(5, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(6, 0.30f, DamageType.Blunt),
                        ResolutionSpec.Damage(7, 0.30f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "SwordAndShield_SwordShieldAssault_M",
                    "sword-and-shield.main.sword-shield-assault",
                    "Sword and Shield Assault [M]",
                    ComboActionCategory.Main,
                    timelineCost: 9,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.10f, DamageType.Piercing),
                        ResolutionSpec.Damage(4, 0.10f, DamageType.Blunt),
                        ResolutionSpec.Damage(5, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Damage(6, 0.20f, DamageType.Blunt),
                        ResolutionSpec.Simple(7, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(8, 0.40f, DamageType.Piercing)
                    }),

                new ActionSpec(
                    "SwordAndShield_RestrainingSlash_C",
                    "sword-and-shield.closing.restraining-slash",
                    "Restraining Slash [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 5,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Simple(4, ActionResolutionEffect.Guard)
                    }),
                new ActionSpec(
                    "SwordAndShield_SpinningUppercut_C",
                    "sword-and-shield.closing.spinning-uppercut",
                    "Spinning Uppercut [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 6,
                    staminaCost: 1,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Simple(1, ActionResolutionEffect.Evade),
                        ResolutionSpec.Damage(4, 0.30f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "SwordAndShield_DoubleTurningSlash_C",
                    "sword-and-shield.closing.double-turning-slash",
                    "Double Turning Slash [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 7,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.30f, DamageType.Slashing),
                        ResolutionSpec.Simple(5, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(6, 0.30f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "SwordAndShield_AerialShieldBash_C",
                    "sword-and-shield.closing.aerial-shield-bash",
                    "Aerial Shield Bash [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 8,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.60f, DamageType.Blunt),
                        ResolutionSpec.Damage(
                            4,
                            0.80f,
                            DamageType.Blunt,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Flinch, 1))
                    },
                    requiredPreviousActionStatusId: AirborneSetupStatusId,
                    requiredPreviousActionStatusDisplayName: AirborneSetupDisplayName),

                new ActionSpec(
                    "SwordAndShield_TurningBackhandSlash_L",
                    "sword-and-shield.linked.turning-backhand-slash",
                    "Turning Backhand Slash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 6,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Simple(2, ActionResolutionEffect.Evade),
                        ResolutionSpec.Damage(4, 0.60f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "SwordAndShield_RisingSlash_L",
                    "sword-and-shield.linked.rising-slash",
                    "Rising Slash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 5,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.50f, DamageType.Slashing),
                        ResolutionSpec.StatusSetup(
                            4,
                            AirborneSetupStatusId,
                            AirborneSetupDisplayName)
                    }),
                new ActionSpec(
                    "SwordAndShield_DescendingShieldBash_L",
                    "sword-and-shield.linked.descending-shield-bash",
                    "Descending Shield Bash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 5,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(
                            2,
                            0.80f,
                            DamageType.Blunt,
                            onHitDisruption: new DisruptionSpec(ActionDisruptionType.Flinch, 2))
                    },
                    requiredPreviousActionStatusId: AirborneSetupStatusId,
                    requiredPreviousActionStatusDisplayName: AirborneSetupDisplayName),
                new ActionSpec(
                    "SwordAndShield_DoubleSlash_L",
                    "sword-and-shield.linked.double-slash",
                    "Double Slash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 3,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(1, 0.30f, DamageType.Slashing),
                        ResolutionSpec.Damage(2, 0.40f, DamageType.Slashing)
                    })
            };
        }

        private static ActionSpec[] GlaiveAndShieldSpecifications()
        {
            const CompatibleWeaponTypes weapon = CompatibleWeaponTypes.GlaiveAndShield;
            return new[]
            {
                new ActionSpec(
                    "GlaiveAndShield_RestrainingSlash_O",
                    "glaive-and-shield.opening.restraining-slash",
                    "Restraining Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 6,
                    staminaCost: 2,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Simple(4, ActionResolutionEffect.Guard)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_LeapingSlash_O",
                    "glaive-and-shield.opening.leaping-slash",
                    "Leaping Slash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 5,
                    staminaCost: 1,
                    weapon,
                    resolutions: DamageOnly(4, 0.50f, DamageType.Slashing),
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "glaive-and-shield.leaping-slash.armor",
                            "Leaping Slash Armor",
                            startOffset: 3,
                            endOffset: 4,
                            level: 3)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_Bash_O",
                    "glaive-and-shield.opening.bash",
                    "Bash [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 5,
                    staminaCost: 0,
                    weapon,
                    resolutions: DamageOnly(3, 0.50f, DamageType.Blunt)),
                new ActionSpec(
                    "GlaiveAndShield_GuardedThrust_O",
                    "glaive-and-shield.opening.guarded-thrust",
                    "Guarded Thrust [O]",
                    ComboActionCategory.Opening,
                    timelineCost: 4,
                    staminaCost: 2,
                    weapon,
                    resolutions: DamageOnly(4, 0.40f, DamageType.Piercing),
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.Defense(
                            "glaive-and-shield.guarded-thrust.defense",
                            "Guarded Thrust Defense",
                            startOffset: 1,
                            endOffset: 4,
                            damageReduction: 0.40f),
                        ActiveWindowSpec.SuperArmor(
                            "glaive-and-shield.guarded-thrust.armor",
                            "Guarded Thrust Armor",
                            startOffset: 1,
                            endOffset: 4,
                            level: 3)
                    }),

                new ActionSpec(
                    "GlaiveAndShield_DiagonalSlash_M",
                    "glaive-and-shield.main.diagonal-slash",
                    "Diagonal Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 5,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.50f, DamageType.Slashing),
                        ResolutionSpec.Damage(4, 0.50f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_FlyingRoundSlash_M",
                    "glaive-and-shield.main.flying-round-slash",
                    "Flying Round Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 8,
                    staminaCost: 7,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(5, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Damage(
                            7,
                            1.10f,
                            DamageType.Slashing,
                            damageTags: DamageResolutionTag.Clash,
                            clashDamageReduction: 0.70f,
                            clashSuperArmorLevel: 5)
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "glaive-and-shield.flying-round-slash-main.armor",
                            "Flying Round Slash Armor",
                            startOffset: 6,
                            endOffset: 6,
                            level: 5)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_GuardedChainSlash_M",
                    "glaive-and-shield.main.guarded-chain-slash",
                    "Guarded Chain Slash [M]",
                    ComboActionCategory.Main,
                    timelineCost: 9,
                    staminaCost: 7,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(3, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Damage(4, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Damage(7, 0.30f, DamageType.Slashing),
                        ResolutionSpec.Damage(8, 0.30f, DamageType.Slashing)
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.Defense(
                            "glaive-and-shield.guarded-chain-slash.defense",
                            "Guarded Chain Slash Defense",
                            startOffset: 1,
                            endOffset: 9,
                            damageReduction: 0.50f)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_RisingSlashChain_M",
                    "glaive-and-shield.main.rising-slash-chain",
                    "Rising Slash Chain [M]",
                    ComboActionCategory.Main,
                    timelineCost: 6,
                    staminaCost: 6,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Simple(3, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(4, 0.40f, DamageType.Slashing)
                    }),

                new ActionSpec(
                    "GlaiveAndShield_FlyingRoundSlash_C",
                    "glaive-and-shield.closing.flying-round-slash",
                    "Flying Round Slash [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 8,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(5, 0.20f, DamageType.Slashing),
                        ResolutionSpec.Damage(7, 1.00f, DamageType.Slashing)
                    },
                    activeWindows: new[]
                    {
                        ActiveWindowSpec.SuperArmor(
                            "glaive-and-shield.flying-round-slash-closing.armor",
                            "Flying Round Slash Armor",
                            startOffset: 7,
                            endOffset: 7,
                            level: 5)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_TurningBackThrust_C",
                    "glaive-and-shield.closing.turning-back-thrust",
                    "Turning Back Thrust [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 6,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Simple(2, ActionResolutionEffect.Evade),
                        ResolutionSpec.Damage(4, 0.70f, DamageType.Piercing)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_EmpoweredDoubleSlash_C",
                    "glaive-and-shield.closing.empowered-double-slash",
                    "Empowered Double Slash [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 6,
                    staminaCost: 7,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Buff(
                            2,
                            "glaive-and-shield.empowered-double-slash.physical-damage",
                            "Empowered Double Slash",
                            duration: 30,
                            modifierDirection: CombatStatusModifierDirection.DamageDealt,
                            modifierScope: DamageModifierScope.Family,
                            modifierType: DamageType.Slashing,
                            modifierAmount: 0.10f),
                        ResolutionSpec.Damage(4, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Damage(5, 0.60f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_RisingSlashChain_C",
                    "glaive-and-shield.closing.rising-slash-chain",
                    "Rising Slash Chain [C]",
                    ComboActionCategory.Closing,
                    timelineCost: 6,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.30f, DamageType.Slashing),
                        ResolutionSpec.Simple(3, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(4, 0.40f, DamageType.Slashing)
                    }),

                new ActionSpec(
                    "GlaiveAndShield_DoubleSlash_L",
                    "glaive-and-shield.linked.double-slash",
                    "Double Slash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 3,
                    staminaCost: 5,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(1, 0.30f, DamageType.Slashing),
                        ResolutionSpec.Damage(2, 0.50f, DamageType.Slashing)
                    }),
                new ActionSpec(
                    "GlaiveAndShield_Bash_L",
                    "glaive-and-shield.linked.bash",
                    "Bash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 5,
                    staminaCost: 0,
                    weapon,
                    resolutions: DamageOnly(3, 0.40f, DamageType.Blunt)),
                new ActionSpec(
                    "GlaiveAndShield_CounterThrust_L",
                    "glaive-and-shield.linked.counter-thrust",
                    "Counter Thrust [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 7,
                    staminaCost: 4,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Simple(2, ActionResolutionEffect.Guard),
                        ResolutionSpec.Damage(6, 0.30f, DamageType.Piercing)
                    },
                    resolutionWindowStart: 2,
                    resolutionWindowEnd: 4),
                new ActionSpec(
                    "GlaiveAndShield_RestrainingSlash_L",
                    "glaive-and-shield.linked.restraining-slash",
                    "Restraining Slash [L]",
                    ComboActionCategory.Linked,
                    timelineCost: 6,
                    staminaCost: 3,
                    weapon,
                    resolutions: new[]
                    {
                        ResolutionSpec.Damage(2, 0.40f, DamageType.Slashing),
                        ResolutionSpec.Simple(4, ActionResolutionEffect.Guard)
                    })
            };
        }

        private static ResolutionSpec[] DamageOnly(
            int offset,
            float multiplier,
            DamageType damageType)
        {
            return new[] { ResolutionSpec.Damage(offset, multiplier, damageType) };
        }

        private static DisruptionResponseSpec[] ChargeCancelThenPause(int timelineCost)
        {
            return new[]
            {
                new DisruptionResponseSpec(
                    startOffset: 1,
                    endOffset: 5,
                    ActionDisruptionResponse.CancelAction),
                new DisruptionResponseSpec(
                    startOffset: 6,
                    endOffset: timelineCost,
                    ActionDisruptionResponse.PauseAndResume)
            };
        }

        private static ComboActionAsset Synchronize(
            string folder,
            string actionSetName,
            ActionSpec specification)
        {
            string assetPath = $"{folder}/{specification.FileName}.asset";
            UnityEngine.Object existingObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (existingObject != null && !(existingObject is ComboActionAsset))
            {
                throw new InvalidOperationException(
                    $"Cannot create Action because {assetPath} contains another asset type.");
            }

            ComboActionAsset asset = existingObject as ComboActionAsset;
            bool isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<ComboActionAsset>();
                asset.name = specification.FileName;
            }
            else
            {
                Undo.RecordObject(asset, $"Synchronize {actionSetName} Action");
            }

            WriteAsset(asset, specification);
            if (!asset.TryCreateDefinition(out _, out string validationError))
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }

                throw new InvalidOperationException(
                    $"{actionSetName} Action '{specification.DisplayName}' is invalid: {validationError}");
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                Undo.RegisterCreatedObjectUndo(asset, $"Create {actionSetName} Action");
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void WriteAsset(ComboActionAsset asset, ActionSpec specification)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("actionId").stringValue = specification.ActionId;
            serialized.FindProperty("displayName").stringValue = specification.DisplayName;
            serialized.FindProperty("category").enumValueIndex = (int)specification.Category;
            serialized.FindProperty("compatibleWeaponTypes").intValue =
                (int)specification.CompatibleWeapons;
            serialized.FindProperty("requiredPreviousActionStatusId").stringValue =
                specification.RequiredPreviousActionStatusId;
            serialized.FindProperty("requiredPreviousActionStatusDisplayName").stringValue =
                specification.RequiredPreviousActionStatusDisplayName;
            serialized.FindProperty("timelineCost").intValue = specification.TimelineCost;
            serialized.FindProperty("resolutionOffset").intValue =
                specification.Resolutions[0].Offset;
            serialized.FindProperty("useResolutionWindow").boolValue =
                specification.ResolutionWindowStart.HasValue;
            serialized.FindProperty("resolutionWindowStart").intValue =
                specification.ResolutionWindowStart ?? specification.Resolutions[0].Offset;
            serialized.FindProperty("resolutionWindowEnd").intValue =
                specification.ResolutionWindowEnd ?? specification.Resolutions[0].Offset;
            serialized.FindProperty("manaCost").intValue = 0;
            serialized.FindProperty("staminaCost").intValue = specification.StaminaCost;
            serialized.FindProperty("cooldown").intValue = 0;

            WriteChargeLevels(serialized.FindProperty("chargeLevels"), specification.ChargeLevels);
            WriteActiveWindows(serialized.FindProperty("activeWindows"), specification.ActiveWindows);
            WriteResolutionEvents(serialized.FindProperty("resolutionEvents"), specification.Resolutions);
            WriteDisruptionResponses(
                serialized.FindProperty("disruptionResponseSegments"),
                specification.DisruptionResponses);

            serialized.FindProperty("useSustainedAttack").boolValue = false;
            serialized.FindProperty("sustainedWindowStartOffset").intValue = 1;
            serialized.FindProperty("sustainedWindowEndOffset").intValue = 1;
            serialized.FindProperty("sustainedFirstHitOffset").intValue = 1;
            serialized.FindProperty("sustainedHitInterval").intValue = 1;
            serialized.FindProperty("sustainedMaximumHitCount").intValue = 1;

            serialized.FindProperty("damageMultiplier").floatValue = 0f;
            serialized.FindProperty("damageType").enumValueIndex = (int)DamageType.Slashing;
            serialized.FindProperty("resolutionEffect").enumValueIndex =
                (int)ActionResolutionEffect.None;
            serialized.FindProperty("damageTags").intValue = (int)DamageResolutionTag.None;
            serialized.FindProperty("clashDamageReduction").floatValue = 0f;
            serialized.FindProperty("clashSuperArmorLevel").intValue = 0;
            serialized.FindProperty("appliesOnHitDisruption").boolValue = false;
            serialized.FindProperty("onHitDisruptionType").enumValueIndex = 0;
            serialized.FindProperty("onHitDisruptionDuration").intValue = 1;
            serialized.FindProperty("appliesOnGuardedDisruption").boolValue = false;
            serialized.FindProperty("onGuardedDisruptionType").enumValueIndex = 0;
            serialized.FindProperty("onGuardedDisruptionDuration").intValue = 1;
            serialized.FindProperty("appliesOnEvadedDisruption").boolValue = false;
            serialized.FindProperty("onEvadedDisruptionType").enumValueIndex = 1;
            serialized.FindProperty("onEvadedDisruptionDuration").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteChargeLevels(
            SerializedProperty property,
            ChargeLevelSpec[] specifications)
        {
            property.arraySize = specifications.Length;
            for (int index = 0; index < specifications.Length; index++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("additionalTimelineCost").intValue =
                    specifications[index].AdditionalTimelineCost;
                element.FindPropertyRelative("damageMultiplierScale").floatValue =
                    specifications[index].DamageMultiplierScale;
            }
        }

        private static void WriteActiveWindows(
            SerializedProperty property,
            ActiveWindowSpec[] specifications)
        {
            property.arraySize = specifications.Length;
            for (int index = 0; index < specifications.Length; index++)
            {
                ActiveWindowSpec specification = specifications[index];
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("windowId").stringValue = specification.WindowId;
                element.FindPropertyRelative("displayName").stringValue = specification.DisplayName;
                element.FindPropertyRelative("startOffset").intValue = specification.StartOffset;
                element.FindPropertyRelative("endOffset").intValue = specification.EndOffset;
                element.FindPropertyRelative("effect").enumValueIndex = (int)specification.Effect;
                element.FindPropertyRelative("damageReduction").floatValue =
                    specification.DamageReduction;
                element.FindPropertyRelative("superArmorLevel").intValue =
                    specification.SuperArmorLevel;
                element.FindPropertyRelative("disruptionPolicy").enumValueIndex =
                    (int)ActiveWindowDisruptionPolicy.PersistThroughDisruption;
                element.FindPropertyRelative("extendsThroughCharge").boolValue =
                    specification.ExtendsThroughCharge;
            }
        }

        private static void WriteResolutionEvents(
            SerializedProperty property,
            ResolutionSpec[] specifications)
        {
            property.arraySize = specifications.Length;
            for (int index = 0; index < specifications.Length; index++)
            {
                ResolutionSpec specification = specifications[index];
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("offset").intValue = specification.Offset;
                element.FindPropertyRelative("effect").enumValueIndex = (int)specification.Effect;
                element.FindPropertyRelative("damageMultiplier").floatValue =
                    specification.DamageMultiplier;
                element.FindPropertyRelative("damageType").enumValueIndex =
                    (int)specification.DamageType;
                element.FindPropertyRelative("damageTags").intValue =
                    (int)specification.DamageTags;
                element.FindPropertyRelative("healingType").enumValueIndex =
                    (int)ActionHealingType.FlatAmount;
                element.FindPropertyRelative("healingAmount").intValue = 0;
                element.FindPropertyRelative("maximumHealthHealingPercentage").floatValue = 0f;
                element.FindPropertyRelative("consumedItemId").stringValue = string.Empty;
                element.FindPropertyRelative("consumedItemDisplayName").stringValue = string.Empty;
                element.FindPropertyRelative("consumedItemAmount").intValue = 0;
                element.FindPropertyRelative("statusId").stringValue = specification.StatusId;
                element.FindPropertyRelative("statusDisplayName").stringValue =
                    specification.StatusDisplayName;
                element.FindPropertyRelative("statusTarget").enumValueIndex =
                    (int)ActionStatusTarget.Self;
                element.FindPropertyRelative("statusDurationType").enumValueIndex =
                    (int)CombatStatusDurationType.TimeSteps;
                element.FindPropertyRelative("statusDuration").intValue = specification.StatusDuration;
                element.FindPropertyRelative("statusModifierDirection").enumValueIndex =
                    (int)specification.ModifierDirection;
                element.FindPropertyRelative("statusModifierScope").enumValueIndex =
                    (int)specification.ModifierScope;
                element.FindPropertyRelative("statusModifierFamily").enumValueIndex =
                    (int)DamageFamily.Physical;
                element.FindPropertyRelative("statusModifierType").enumValueIndex =
                    (int)specification.ModifierType;
                element.FindPropertyRelative("statusModifierAmount").floatValue =
                    specification.ModifierAmount;
                element.FindPropertyRelative("statusStackingPolicy").enumValueIndex =
                    (int)specification.StackingPolicy;
                element.FindPropertyRelative("clashDamageReduction").floatValue =
                    specification.ClashDamageReduction;
                element.FindPropertyRelative("clashSuperArmorLevel").intValue =
                    specification.ClashSuperArmorLevel;
                WriteDisruption(
                    element,
                    "appliesOnHitDisruption",
                    "onHitDisruptionType",
                    "onHitDisruptionDuration",
                    specification.OnHitDisruption);
                WriteDisruption(
                    element,
                    "appliesOnGuardedDisruption",
                    "onGuardedDisruptionType",
                    "onGuardedDisruptionDuration",
                    null);
                WriteDisruption(
                    element,
                    "appliesOnEvadedDisruption",
                    "onEvadedDisruptionType",
                    "onEvadedDisruptionDuration",
                    null);
            }
        }

        private static void WriteDisruption(
            SerializedProperty resolution,
            string enabledProperty,
            string typeProperty,
            string durationProperty,
            DisruptionSpec specification)
        {
            resolution.FindPropertyRelative(enabledProperty).boolValue = specification != null;
            resolution.FindPropertyRelative(typeProperty).enumValueIndex = specification != null
                ? (int)specification.Type - 1
                : 0;
            resolution.FindPropertyRelative(durationProperty).intValue =
                specification?.Duration ?? 1;
        }

        private static void WriteDisruptionResponses(
            SerializedProperty property,
            DisruptionResponseSpec[] specifications)
        {
            property.arraySize = specifications.Length;
            for (int index = 0; index < specifications.Length; index++)
            {
                DisruptionResponseSpec specification = specifications[index];
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("startOffset").intValue = specification.StartOffset;
                element.FindPropertyRelative("endOffset").intValue = specification.EndOffset;
                element.FindPropertyRelative("flinchResponse").enumValueIndex =
                    (int)specification.Response;
                element.FindPropertyRelative("staggerResponse").enumValueIndex =
                    (int)specification.Response;
                element.FindPropertyRelative("knockdownResponse").enumValueIndex =
                    (int)specification.Response;
            }
        }

        private static void EnsureFolderPath(string folderPath)
        {
            string[] components = folderPath.Split('/');
            string current = components[0];
            for (int index = 1; index < components.Length; index++)
            {
                string next = $"{current}/{components[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, components[index]);
                }

                current = next;
            }
        }

        private sealed class ActionSpec
        {
            public ActionSpec(
                string fileName,
                string actionId,
                string displayName,
                ComboActionCategory category,
                int timelineCost,
                int staminaCost,
                CompatibleWeaponTypes compatibleWeapons,
                ResolutionSpec[] resolutions,
                ChargeLevelSpec[] chargeLevels = null,
                ActiveWindowSpec[] activeWindows = null,
                DisruptionResponseSpec[] disruptionResponses = null,
                string requiredPreviousActionStatusId = null,
                string requiredPreviousActionStatusDisplayName = null,
                int? resolutionWindowStart = null,
                int? resolutionWindowEnd = null)
            {
                FileName = fileName;
                ActionId = actionId;
                DisplayName = displayName;
                Category = category;
                TimelineCost = timelineCost;
                StaminaCost = staminaCost;
                CompatibleWeapons = compatibleWeapons;
                Resolutions = resolutions;
                ChargeLevels = chargeLevels ?? Array.Empty<ChargeLevelSpec>();
                ActiveWindows = activeWindows ?? Array.Empty<ActiveWindowSpec>();
                RequiredPreviousActionStatusId = requiredPreviousActionStatusId ?? string.Empty;
                RequiredPreviousActionStatusDisplayName =
                    requiredPreviousActionStatusDisplayName ?? string.Empty;
                ResolutionWindowStart = resolutionWindowStart;
                ResolutionWindowEnd = resolutionWindowEnd;
                DisruptionResponses = disruptionResponses ??
                    new[]
                    {
                        new DisruptionResponseSpec(
                            1,
                            timelineCost,
                            ActionDisruptionResponse.PauseAndResume)
                    };
            }

            public string FileName { get; }
            public string ActionId { get; }
            public string DisplayName { get; }
            public ComboActionCategory Category { get; }
            public int TimelineCost { get; }
            public int StaminaCost { get; }
            public CompatibleWeaponTypes CompatibleWeapons { get; }
            public ResolutionSpec[] Resolutions { get; }
            public ChargeLevelSpec[] ChargeLevels { get; }
            public ActiveWindowSpec[] ActiveWindows { get; }
            public DisruptionResponseSpec[] DisruptionResponses { get; }
            public string RequiredPreviousActionStatusId { get; }
            public string RequiredPreviousActionStatusDisplayName { get; }
            public int? ResolutionWindowStart { get; }
            public int? ResolutionWindowEnd { get; }
        }

        private sealed class ResolutionSpec
        {
            private ResolutionSpec(
                int offset,
                ActionResolutionEffect effect,
                float damageMultiplier,
                DamageType damageType,
                DamageResolutionTag damageTags,
                DisruptionSpec onHitDisruption,
                float clashDamageReduction,
                int clashSuperArmorLevel,
                string statusId,
                string statusDisplayName,
                int statusDuration,
                CombatStatusModifierDirection modifierDirection,
                DamageModifierScope modifierScope,
                DamageType modifierType,
                float modifierAmount,
                CombatStatusStackingPolicy stackingPolicy)
            {
                Offset = offset;
                Effect = effect;
                DamageMultiplier = damageMultiplier;
                DamageType = damageType;
                DamageTags = damageTags;
                OnHitDisruption = onHitDisruption;
                ClashDamageReduction = clashDamageReduction;
                ClashSuperArmorLevel = clashSuperArmorLevel;
                StatusId = statusId ?? string.Empty;
                StatusDisplayName = statusDisplayName ?? string.Empty;
                StatusDuration = statusDuration;
                ModifierDirection = modifierDirection;
                ModifierScope = modifierScope;
                ModifierType = modifierType;
                ModifierAmount = modifierAmount;
                StackingPolicy = stackingPolicy;
            }

            public static ResolutionSpec Damage(
                int offset,
                float multiplier,
                DamageType damageType,
                DamageResolutionTag damageTags = DamageResolutionTag.None,
                DisruptionSpec onHitDisruption = null,
                float clashDamageReduction = 0f,
                int clashSuperArmorLevel = 0)
            {
                return new ResolutionSpec(
                    offset,
                    ActionResolutionEffect.Damage,
                    multiplier,
                    damageType,
                    damageTags,
                    onHitDisruption,
                    clashDamageReduction,
                    clashSuperArmorLevel,
                    null,
                    null,
                    1,
                    CombatStatusModifierDirection.DamageDealt,
                    DamageModifierScope.All,
                    DamageType.Slashing,
                    0f,
                    CombatStatusStackingPolicy.RefreshDuration);
            }

            public static ResolutionSpec Simple(int offset, ActionResolutionEffect effect)
            {
                return new ResolutionSpec(
                    offset,
                    effect,
                    0f,
                    DamageType.Slashing,
                    DamageResolutionTag.None,
                    null,
                    0f,
                    0,
                    null,
                    null,
                    1,
                    CombatStatusModifierDirection.DamageDealt,
                    DamageModifierScope.All,
                    DamageType.Slashing,
                    0f,
                    CombatStatusStackingPolicy.RefreshDuration);
            }

            public static ResolutionSpec Buff(
                int offset,
                string statusId,
                string displayName,
                int duration,
                CombatStatusModifierDirection modifierDirection,
                DamageModifierScope modifierScope,
                DamageType modifierType,
                float modifierAmount,
                CombatStatusStackingPolicy stackingPolicy =
                    CombatStatusStackingPolicy.IndependentInstances)
            {
                return new ResolutionSpec(
                    offset,
                    ActionResolutionEffect.ApplyBuff,
                    0f,
                    DamageType.Slashing,
                    DamageResolutionTag.None,
                    null,
                    0f,
                    0,
                    statusId,
                    displayName,
                    duration,
                    modifierDirection,
                    modifierScope,
                    modifierType,
                    modifierAmount,
                    stackingPolicy);
            }

            public static ResolutionSpec StatusSetup(
                int offset,
                string statusId,
                string displayName)
            {
                return Buff(
                    offset,
                    statusId,
                    displayName,
                    duration: 1,
                    modifierDirection: CombatStatusModifierDirection.DamageDealt,
                    modifierScope: DamageModifierScope.All,
                    modifierType: DamageType.Slashing,
                    modifierAmount: 0f);
            }

            public int Offset { get; }
            public ActionResolutionEffect Effect { get; }
            public float DamageMultiplier { get; }
            public DamageType DamageType { get; }
            public DamageResolutionTag DamageTags { get; }
            public DisruptionSpec OnHitDisruption { get; }
            public float ClashDamageReduction { get; }
            public int ClashSuperArmorLevel { get; }
            public string StatusId { get; }
            public string StatusDisplayName { get; }
            public int StatusDuration { get; }
            public CombatStatusModifierDirection ModifierDirection { get; }
            public DamageModifierScope ModifierScope { get; }
            public DamageType ModifierType { get; }
            public float ModifierAmount { get; }
            public CombatStatusStackingPolicy StackingPolicy { get; }
        }

        private sealed class ChargeLevelSpec
        {
            public ChargeLevelSpec(int additionalTimelineCost, float damageMultiplierScale)
            {
                AdditionalTimelineCost = additionalTimelineCost;
                DamageMultiplierScale = damageMultiplierScale;
            }

            public int AdditionalTimelineCost { get; }
            public float DamageMultiplierScale { get; }
        }

        private sealed class ActiveWindowSpec
        {
            private ActiveWindowSpec(
                string windowId,
                string displayName,
                int startOffset,
                int endOffset,
                ActiveWindowEffect effect,
                float damageReduction,
                int superArmorLevel,
                bool extendsThroughCharge)
            {
                WindowId = windowId;
                DisplayName = displayName;
                StartOffset = startOffset;
                EndOffset = endOffset;
                Effect = effect;
                DamageReduction = damageReduction;
                SuperArmorLevel = superArmorLevel;
                ExtendsThroughCharge = extendsThroughCharge;
            }

            public static ActiveWindowSpec SuperArmor(
                string windowId,
                string displayName,
                int startOffset,
                int endOffset,
                int level,
                bool extendsThroughCharge = false)
            {
                return new ActiveWindowSpec(
                    windowId,
                    displayName,
                    startOffset,
                    endOffset,
                    ActiveWindowEffect.SuperArmor,
                    0f,
                    level,
                    extendsThroughCharge);
            }

            public static ActiveWindowSpec Defense(
                string windowId,
                string displayName,
                int startOffset,
                int endOffset,
                float damageReduction)
            {
                return new ActiveWindowSpec(
                    windowId,
                    displayName,
                    startOffset,
                    endOffset,
                    ActiveWindowEffect.DefensiveState,
                    damageReduction,
                    0,
                    false);
            }

            public string WindowId { get; }
            public string DisplayName { get; }
            public int StartOffset { get; }
            public int EndOffset { get; }
            public ActiveWindowEffect Effect { get; }
            public float DamageReduction { get; }
            public int SuperArmorLevel { get; }
            public bool ExtendsThroughCharge { get; }
        }

        private sealed class DisruptionSpec
        {
            public DisruptionSpec(ActionDisruptionType type, int duration)
            {
                Type = type;
                Duration = duration;
            }

            public ActionDisruptionType Type { get; }
            public int Duration { get; }
        }

        private sealed class DisruptionResponseSpec
        {
            public DisruptionResponseSpec(
                int startOffset,
                int endOffset,
                ActionDisruptionResponse response)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
                Response = response;
            }

            public int StartOffset { get; }
            public int EndOffset { get; }
            public ActionDisruptionResponse Response { get; }
        }
    }
}
