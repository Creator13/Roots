using System;
using System.Collections;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Roots
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
            yield return null;
            SceneManager.SetActiveScene(menuScene.LoadedScene);
            fadePanel.alpha = 0;
        }

        private void LoadGame()
        {
            if (gameScene.State == SceneReferenceState.Regular)
            {
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
