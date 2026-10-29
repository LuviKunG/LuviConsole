using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LuviKunG.Console
{
    public sealed partial class LuviConsole
    {
        private Vector2 swipeStartPosition;
        private bool isTrackingSwipe;

        private void EnableInputActions()
        {
            toggleConsoleAction?.Enable();
            togglePreviewAction?.Enable();
            toggleScreenMessageAction?.Enable();
        }

        private void DisableInputActions()
        {
            toggleConsoleAction?.Disable();
            togglePreviewAction?.Disable();
            toggleScreenMessageAction?.Disable();
            isTrackingSwipe = false;
        }

        private void UpdateInput()
        {
            if (WasPressedThisFrame(toggleConsoleAction))
            {
                IsShowingConsole = !IsShowingConsole;
                if (IsShowingConsole)
                    RequestCommandFieldFocus();
            }
            if (WasPressedThisFrame(togglePreviewAction))
                IsShowingPreview = !IsShowingPreview;
            if (WasPressedThisFrame(toggleScreenMessageAction))
                IsShowingScreenMessage = !IsShowingScreenMessage;
            if (swipeToToggle)
                UpdateSwipeGesture();
        }

        private static bool WasPressedThisFrame(InputAction action) =>
            action != null && action.enabled && action.WasPressedThisFrame();

        // Swipe from the left half of the screen to toggle the console, from the right half to toggle the preview.
        private void UpdateSwipeGesture()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return;
            TouchControl touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                swipeStartPosition = touch.position.ReadValue();
                isTrackingSwipe = true;
                return;
            }
            if (!touch.press.isPressed)
            {
                isTrackingSwipe = false;
                return;
            }
            if (!isTrackingSwipe)
                return;
            Vector2 direction = touch.position.ReadValue() - swipeStartPosition;
            if (direction.magnitude <= Screen.height * swipeRatio || Mathf.Abs(direction.x) < Mathf.Abs(direction.y))
                return;
            float screenDivision = Screen.width / 2.0f;
            if (swipeStartPosition.x < screenDivision)
                IsShowingConsole = direction.x > 0;
            else if (swipeStartPosition.x > screenDivision)
                IsShowingPreview = direction.x < 0;
            isTrackingSwipe = false;
        }
    }
}
