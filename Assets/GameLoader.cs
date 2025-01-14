using System.Collections;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Roots
{
    public class GameLoader : MonoBehaviour
    {
        [SerializeField] private SceneReference gameScene;

        [Space]
        [SerializeField] private float fadeInDuration = 1;
        [SerializeField] private float fadeOutDuration = 1;
        [SerializeField] private float waitTime = 1;

        [Space]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private GameObject[] visualElements;

        private bool isLoadingGame;
        
        private void Update()
        {
            if (!isLoadingGame && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LoadGame();
            }
        }

        private void LoadGame()
        {
            Assert.IsTrue(gameScene.State == SceneReferenceState.Regular);

            isLoadingGame = true;
            StartCoroutine(LoadGameSequenceCoroutine());
        }

        private IEnumerator LoadGameSequenceCoroutine()
        {
            // Fade out
            yield return FadeCoroutine(fadePanel, 1, fadeInDuration);

            // Load
            yield return SceneManager.LoadSceneAsync(gameScene.BuildIndex, LoadSceneMode.Additive);

            // After load: set active to new
            SceneManager.SetActiveScene(gameScene.LoadedScene);
            // Hide all visuals in the menu scene
            foreach (var obj in visualElements)
            {
                obj.SetActive(false);
            }

            // Wait (keep screen black)
            yield return new WaitForSeconds(waitTime);

            // Fade back in
            yield return FadeCoroutine(fadePanel, 0, fadeOutDuration);

            // Unload menu
            SceneManager.UnloadSceneAsync(gameObject.scene);
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