using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Tests
{
    public sealed class PlayerSaveServiceTests
    {
        private static readonly MethodInfo PlayerStateAwake =
            typeof(PlayerStateService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo PlayerSaveAwake =
            typeof(PlayerSaveService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo StorageField =
            typeof(PlayerSaveService).GetField(
                "_storage",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private string _saveDirectory;

        [SetUp]
        public void SetUp()
        {
            _saveDirectory = Path.Combine(
                Path.GetTempPath(),
                "SER494-PlayerSaveServiceTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
            if (Directory.Exists(_saveDirectory))
            {
                Directory.Delete(_saveDirectory, true);
            }
        }

        [Test]
        public void SaveThenLoad_ReplacesAuthoritativePlayerState()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            stateService.State.SetCurrentHealth(64);

            saveService.SaveSlot(1);
            stateService.State.SetCurrentHealth(12);
            PlayerState beforeLoad = stateService.State;

            LoadedPlayerSave loaded = saveService.LoadSlot(1);

            Assert.That(stateService.State, Is.SameAs(loaded.State));
            Assert.That(stateService.State, Is.Not.SameAs(beforeLoad));
            Assert.That(stateService.State.CurrentHealth, Is.EqualTo(64));
        }

        [Test]
        public void FailedLoad_DoesNotReplaceAuthoritativePlayerState()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            PlayerState stateBeforeLoad = stateService.State;

            Directory.CreateDirectory(_saveDirectory);
            var storage = (PlayerSaveStorage)StorageField.GetValue(saveService);
            File.WriteAllText(storage.GetSavePath(1), "broken save");

            Assert.Throws<InvalidDataException>(() => saveService.LoadSlot(1));
            Assert.That(stateService.State, Is.SameAs(stateBeforeLoad));
        }

        private (PlayerStateService, PlayerSaveService) CreateServices()
        {
            var gameObject = new GameObject("Player State and Save Services");
            _createdObjects.Add(gameObject);

            Assert.That(PlayerStateAwake, Is.Not.Null);
            Assert.That(PlayerSaveAwake, Is.Not.Null);
            Assert.That(StorageField, Is.Not.Null);

            PlayerStateService stateService = gameObject.AddComponent<PlayerStateService>();
            PlayerStateAwake.Invoke(stateService, null);

            PlayerSaveService saveService = gameObject.AddComponent<PlayerSaveService>();
            PlayerSaveAwake.Invoke(saveService, null);

            StorageField.SetValue(saveService, new PlayerSaveStorage(_saveDirectory));
            return (stateService, saveService);
        }
    }
}
