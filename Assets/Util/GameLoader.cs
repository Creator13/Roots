using System.Collections;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Roots.Util
{
    public class GameLoader : MonoBehaviour
    {
        [SerializeField] private SceneReference menuScene;
        [SerializeField] private SceneReference gameScene;

        [Space]
        [SerializeField] private float fadeInDuration = 1;
        [SerializeField] private float fadeOutDuration = 1;
        [SerializeField] private float waitTime = 1;

        [Space]
        [SerializeField] private CanvasGroup fadePanel;

        private enum GameState { Uninitialized, Menu, Game }
        private GameState gameState = GameState.Uninitialized;

        private void Awake()
        {
            StartCoroutine(LoadMenuCoroutine());
        }

        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                LoadGame();
            }

            if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.qKey.wasPressedThisFrame)
            {
                Application.Quit();
            }
        }

        private IEnumerator LoadMenuCoroutine()
        {
            SceneManager.LoadScene(menuScene.BuildIndex, LoadSceneMode.Additive);
            // Wait one frame to guarantee menu scene is loaded (won't be allowed to set active scene otherwise).
            yield return null;
            SceneManager.SetActiveScene(menuScene.LoadedScene);
            yield return FadeCoroutine(fadePanel, 0, .4f);
            gameState = GameState.Menu;
        }

        private void LoadGame()
        {
            // Disallow loading the game when we are not in the menu.
            if (gameState != GameState.Menu) return;
            
            if (gameScene.State == SceneReferenceState.Regular)
            {
                // Immediately set the state to game to prevent double loading
                gameState = GameState.Game;
                StartCoroutine(LoadGameSequenceCoroutine());
            }
        }

        private IEnumerator LoadGameSequenceCoroutine()
        {
            // Fade out
            yield return FadeCoroutine(fadePanel, 1, fadeInDuration);

            // Load
            yield return SceneManager.LoadSceneAsync(gameScene.BuildIndex, LoadSceneMode.Additive);

            // After load: set active to new
            SceneManager.SetActiveScene(gameScene.LoadedScene);
            // Unload menu
            SceneManager.UnloadSceneAsync(menuScene.LoadedScene);

            // Wait (keep screen black)
            yield return new WaitForSeconds(waitTime);

            // Fade back in
            yield return FadeCoroutine(fadePanel, 0, fadeOutDuration);
        }

        private static IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}