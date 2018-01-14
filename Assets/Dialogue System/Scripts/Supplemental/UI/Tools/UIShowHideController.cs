#if !(UNITY_4_3 || UNITY_4_5)
using UnityEngine;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    public class UIShowHideController
    {

        public enum TransitionMode { State, Trigger }

        /// <summary>
        /// Maximum number of seconds to wait for show/hide animation.
        /// </summary>
        public static float maxWaitDuration = 5;

        public Component panel;

        private Animator animator = null;

        private bool lookedForAnimator = false;

        private TransitionMode transitionMode;

        private Coroutine animCoroutine;

        public UIShowHideController(GameObject go, Component panel, TransitionMode animationMode = TransitionMode.Trigger)
        {
            this.panel = panel;
            this.animator = (go != null) ? go.GetComponent<Animator>() : null;
            if (animator == null && panel != null) animator = panel.GetComponent<Animator>();
            this.animCoroutine = null;
            this.transitionMode = animationMode;
        }

        public void Show(string showState, bool pauseAfterAnimation, System.Action callback, bool wait = true)
        {
            CancelCurrentAnim();
            switch (transitionMode)
            {
                case TransitionMode.State:
                    animCoroutine = DialogueManager.Instance.StartCoroutine(WaitForAnimationState(showState, pauseAfterAnimation, true, wait, callback));
                    break;
                case TransitionMode.Trigger:
                    animCoroutine = DialogueManager.Instance.StartCoroutine(WaitForAnimationTrigger(showState, pauseAfterAnimation, true, wait, callback));
                    break;
            }
        }

        public void Hide(string hideState, System.Action callback)
        {
            CancelCurrentAnim();
            switch (transitionMode)
            {
                case TransitionMode.State:
                    animCoroutine = DialogueManager.Instance.StartCoroutine(WaitForAnimationState(hideState, false, false, true, callback));
                    break;
                case TransitionMode.Trigger:
                    animCoroutine = DialogueManager.Instance.StartCoroutine(WaitForAnimationTrigger(hideState, false, false, true, callback));
                    break;
            }
        }

        private IEnumerator WaitForAnimationState(string stateName, bool pauseAfterAnimation, bool panelActive, bool wait, System.Action callback)
        {
            // Activate panel if necessary:
            if (panel != null && !panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
                yield return null;
            }

            // Play animation:
            if (CanTriggerAnimation(stateName))
            {
                CheckAnimatorModeAndTimescale(stateName);
                animator.Play(stateName);
                if (wait)
                {
                    yield return null; // Wait to enter state.
                    var clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
                    if (Mathf.Approximately(0, Time.timeScale))
                    {
                        var timeout = Time.realtimeSinceStartup + clipLength;
                        while (Time.realtimeSinceStartup < timeout)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(clipLength);
                    }
                }
            }

            // Finish:
            if (!panelActive) Tools.SetGameObjectActive(panel, false);
            if (pauseAfterAnimation) Time.timeScale = 0;
            animCoroutine = null;
            if (callback != null) callback.Invoke();
        }

        private IEnumerator WaitForAnimationTrigger(string triggerName, bool pauseAfterAnimation, bool panelActive, bool wait, System.Action callback)
        {
            if (panelActive)
            {
                if (panel != null && !panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(true);
                    yield return null;
                }
            }
            if (CanTriggerAnimation(triggerName) && animator.gameObject.activeSelf)
            {
                CheckAnimatorModeAndTimescale(triggerName);
                float timeout = Time.realtimeSinceStartup + maxWaitDuration;
                var goalHashID = Animator.StringToHash(triggerName);
                var oldHashId = UITools.GetAnimatorNameHash(animator.GetCurrentAnimatorStateInfo(0));
                var currentHashID = oldHashId;
                animator.SetTrigger(triggerName);
                if (wait)
                {
                    // Wait while we're not at the goal state and we're in the original state and we haven't timed out:
                    while ((currentHashID != goalHashID) && (currentHashID == oldHashId) && (Time.realtimeSinceStartup < timeout))
                    {
                        yield return null;
                        var isAnimatorValid = animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.layerCount > 0;
                        currentHashID = isAnimatorValid ? UITools.GetAnimatorNameHash(animator.GetCurrentAnimatorStateInfo(0)) : currentHashID;
                    }
                    // If we're in the goal state and we haven't timed out, wait for the duration of the goal state:
                    if (currentHashID == goalHashID && Time.realtimeSinceStartup < timeout)
                    {
                        var clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
                        if (Mathf.Approximately(0, Time.timeScale))
                        {
                            timeout = Time.realtimeSinceStartup + clipLength;
                            while (Time.realtimeSinceStartup < timeout)
                            {
                                yield return null;
                            }
                        }
                        else
                        {
                            yield return new WaitForSeconds(clipLength);
                        }
                    }
                }
            }
            if (!panelActive) Tools.SetGameObjectActive(panel, false);
            if (pauseAfterAnimation) Time.timeScale = 0;
            animCoroutine = null;
            if (callback != null) callback.Invoke();
        }

        private void CheckAnimatorModeAndTimescale(string triggerName)
        {
            if (Mathf.Approximately(0, Time.timeScale) && (animator.updateMode != AnimatorUpdateMode.UnscaledTime) && DialogueDebug.LogWarnings)
            {
                Debug.LogWarning("Dialogue System: Time is paused but animator mode isn't set to Unscaled Time; the animation triggered by " + triggerName + " won't play.", animator);
            }
        }

        private void CancelCurrentAnim()
        {
            if (animCoroutine != null)
            {
                DialogueManager.Instance.StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
        }

        public void ClearTrigger(string triggerName)
        {
            if (HasAnimator() && !string.IsNullOrEmpty(triggerName) && animator.isActiveAndEnabled)
            {
                animator.ResetTrigger(triggerName);
            }
        }

        private bool CanTriggerAnimation(string stateName)
        {
            return HasAnimator() && !string.IsNullOrEmpty(stateName);
        }

        private bool HasAnimator()
        {
            if ((animator == null) && !lookedForAnimator)
            {
                lookedForAnimator = true;
                if (panel != null)
                {
                    animator = panel.GetComponent<Animator>();
                    if (animator == null) animator = panel.GetComponentInChildren<Animator>();
                }
            }
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
            return (animator != null) && animator.isInitialized && animator.gameObject.activeSelf;
#else
            return (animator != null) && animator.gameObject.activeSelf;
#endif
        }

    }

}
#endif