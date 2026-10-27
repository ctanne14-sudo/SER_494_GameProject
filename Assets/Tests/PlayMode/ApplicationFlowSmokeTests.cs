using System.Collections;
using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.ExplorationMenu.Unity;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SER494.Gameplay.PlayModeTests
{
    [Category("PlayModeSmoke")]
    public sealed class ApplicationFlowSmokeTests
    {
        private const string ExplorationSceneName = "Floor1_Cell_A";
        private const string CombatSceneName = "CombatSandbox";

        private SoloComboLoadoutSession _runtimeLoadoutSession;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (BattleSessionService.TryGetActive(out BattleSessionService service)
                && service.HasActiveSession)
            {
                service.ClearCurrentSession();
            }

            if (_runtimeLoadoutSession != null)
            {
                Object.Destroy(_runtimeLoadoutSession);
                _runtimeLoadoutSession = null;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TitleMenu_LoadsRequiredPresentationAndServices()
        {
            yield return LoadScene(TitleMenuBootstrap.TitleSceneName);

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(TitleMenuBootstrap.TitleSceneName));
            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True,
                "Title Menu requires an active PlayerStateService.");
            Assert.That(stateService.State, Is.Not.Null);
            Assert.That(PlayerSaveService.TryGetActive(out PlayerSaveService saveService),
                Is.True,
                "Title Menu requires an active PlayerSaveService.");
            Assert.That(saveService.IsAuthority, Is.True);

            TitleMenuController controller =
                Object.FindAnyObjectByType<TitleMenuController>();
            Assert.That(controller, Is.Not.Null,
                "Title Menu bootstrap did not create its controller.");
            Assert.That(controller.isActiveAndEnabled, Is.True);

            UIDocument document = controller.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("newGameButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("loadGameButton"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("quitGameButton"), Is.Not.Null);

            GameObject titleCamera = GameObject.Find("Title Camera");
            Assert.That(titleCamera, Is.Not.Null,
                "Title Menu bootstrap did not create the title camera.");
            Assert.That(titleCamera.GetComponent<Camera>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ExplorationScene_LoadsRequiredPlayerAndIntegrationObjects()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(SceneManager.GetActiveScene().name,
                Is.EqualTo(ExplorationSceneName));
            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(stateService.State, Is.Not.Null);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            Assert.That(battleService.IsAuthority, Is.True);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null,
                "Exploration scene must contain an active Player object.");
            Assert.That(Camera.main, Is.Not.Null,
                "Exploration scene must contain an enabled Main Camera.");
            Assert.That(Object.FindAnyObjectByType<ExplorationMenuController>(), Is.Not.Null,
                "Exploration Menu bootstrap did not create its controller.");
            Assert.That(Object.FindAnyObjectByType<ExplorationBattleTrigger>(), Is.Not.Null,
                "Exploration scene must contain a configured battle trigger.");
            Assert.That(GameObject.Find("Save Point"), Is.Not.Null,
                "Exploration scene must contain its interactive Save Point.");
        }

        [UnityTest]
        public IEnumerator CombatScene_ConsumesPreparedBattleSession()
        {
            yield return LoadScene(ExplorationSceneName);

            Assert.That(PlayerStateService.TryGetActive(out PlayerStateService stateService),
                Is.True);
            Assert.That(BattleSessionService.TryGetActive(out BattleSessionService battleService),
                Is.True);
            if (battleService.HasActiveSession)
            {
                battleService.ClearCurrentSession();
            }

            stateService.StartNewGame();
            _runtimeLoadoutSession =
                ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            Assert.That(
                PlayerStateLoadoutSessionBuilder.TryPrepare(
                    stateService.State,
                    _runtimeLoadoutSession,
                    SoloComboTestScenario.GreatswordTrial,
                    out string loadoutError),
                Is.True,
                loadoutError);
            Assert.That(
                SoloComboEncounterLibrary.TryLoad(
                    SoloComboTestScenario.GreatswordTrial,
                    out SoloComboEncounterDefinitionAsset encounter,
                    out string encounterError),
                Is.True,
                encounterError);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null);
            Assert.That(
                battleService.TryBeginBattle(
                    encounter,
                    _runtimeLoadoutSession,
                    "play-mode-smoke.enemy",
                    ExplorationSceneName,
                    CombatSceneName,
                    player.transform.position,
                    player.transform.rotation,
                    out string beginError),
                Is.True,
                beginError);
            BattleSession expectedSession = battleService.CurrentSession;

            yield return LoadScene(CombatSceneName);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(CombatSceneName));
            Assert.That(battleService.HasActiveSession, Is.True);
            Assert.That(battleService.CurrentSession, Is.SameAs(expectedSession));
            Assert.That(battleService.CurrentSession.LoadoutSession,
                Is.SameAs(_runtimeLoadoutSession));
            Assert.That(battleService.CurrentSession.Encounter, Is.SameAs(encounter));

            GameObject sandbox = GameObject.Find("CombatSandbox");
            Assert.That(sandbox, Is.Not.Null,
                "Combat scene must contain the CombatSandbox root object.");
            Component controller = sandbox.GetComponent("CombatSandboxController");
            Assert.That(controller, Is.Not.Null,
                "CombatSandbox root must contain its runtime controller.");
            Assert.That(((Behaviour)controller).isActiveAndEnabled, Is.True);
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            Assert.That(Application.CanStreamedLevelBeLoaded(sceneName), Is.True,
                $"Scene '{sceneName}' must be enabled in Build Settings.");
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            // Allow scene-loaded bootstraps and component Start methods to run.
            yield return null;
        }
    }
}
