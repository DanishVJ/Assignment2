using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Core.Patterns;

namespace Commodore
{
    /// <summary>
    /// Singleton terminal that handles all Commodore 64 display and input.
    /// Automatically finds and calls the student's CommodoreBehavior script.
    /// </summary>
    public class CommodoreTerminal : Singleton<CommodoreTerminal>
    {
        [Header("References")]
        [SerializeField] private TMP_Text _terminalText;
        [SerializeField] private KeyboardAudioPlayer _audioPlayer;

        [Header("Settings")]
        [SerializeField] private float _cursorBlinkRate = 0.5f;
        [SerializeField] private int _maxLines = 25;
        [SerializeField] private string _prompt = ">";
        [SerializeField] private float _typewriterSpeed = 0.05f;
        [SerializeField] private float _responseDelay = 0.5f;
        [SerializeField] private string _cursorCharacter = "\u2588";

        private TextInputActions _inputActions;
        private CommodoreBehavior _studentBehavior;

        private List<string> _outputLines = new List<string>();
        private string _currentInput = string.Empty;
        private Coroutine _blinkCoroutine;
        private bool _cursorVisible = true;
        private bool _isTyping = false;
        private Coroutine _typewriterCoroutine;

        protected override void Awake()
        {
            base.Awake();

            _inputActions = new TextInputActions();

            // Find the student's behavior script
            _studentBehavior = FindFirstObjectByType<CommodoreBehavior>();

            if (_studentBehavior == null)
            {
                Debug.LogWarning("CommodoreTerminal: No CommodoreBehavior found in scene!");
            }

            if (_terminalText == null)
            {
                Debug.LogError("CommodoreTerminal: TMP_Text reference is not set!");
            }
        }

        private void Start()
        {
            StartCursorBlink();
            UpdateDisplay();
        }

        private void Update()
        {
            // Check for newly pressed keys this frame to play sounds
            var keyboard = Keyboard.current;
            if (_isTyping || keyboard == null || _audioPlayer == null) return;

            if (keyboard.anyKey.wasPressedThisFrame)
            {
                _audioPlayer.PlayKeyPressFromKeyboard(keyboard);
            }
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            _inputActions.Terminal.Enable();

            _inputActions.Terminal.Enter.performed += OnEnter;
            _inputActions.Terminal.Cancel.performed += OnCancel;
            _inputActions.Terminal.Back.performed += OnBack;

            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput += OnKeyboardTextInput;
            }
        }

        private void OnDisable()
        {
            if (_inputActions == null) return;

            _inputActions.Terminal.Enter.performed -= OnEnter;
            _inputActions.Terminal.Cancel.performed -= OnCancel;
            _inputActions.Terminal.Back.performed -= OnBack;

            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= OnKeyboardTextInput;
            }

            _inputActions.Terminal.Disable();
        }

        private void OnKeyboardTextInput(char inputChar)
        {
            // Don't accept input while typing response
            if (_isTyping) return;

            // Filter out control characters
            if (char.IsControl(inputChar)) return;

            // Convert to uppercase (C64 style)
            char upperChar = char.ToUpper(inputChar);
            _currentInput += upperChar;
            UpdateDisplay();

            // Sound is played in Update() to avoid playing on key repeat
        }

        private void OnEnter(InputAction.CallbackContext context)
        {
            // Don't accept input while typing response
            if (_isTyping) return;

            // Play enter key sound
            if (_audioPlayer != null && Keyboard.current != null)
            {
                _audioPlayer.PlayKeyPressFromKeyboard(Keyboard.current);
            }

            string command = _currentInput.Trim();

            // Add the command line to output
            AddOutputLine(_prompt + command);

            // Clear current input immediately
            _currentInput = string.Empty;
            UpdateDisplay();

            // Process the command if not empty
            if (!string.IsNullOrWhiteSpace(command) && _studentBehavior != null)
            {
                string response = _studentBehavior.HandleCommand(command);
                if (!string.IsNullOrEmpty(response))
                {
                    // Type out the response with typewriter effect
                    _isTyping = true;
                    UpdateDisplay();
                    if (_typewriterCoroutine != null)
                    {
                        StopCoroutine(_typewriterCoroutine);
                    }
                    _typewriterCoroutine = StartCoroutine(TypewriterEffect(response));
                }
            }
            else if (!string.IsNullOrWhiteSpace(command))
            {
                // Type out default error message
                _isTyping = true;
                UpdateDisplay();
                if (_typewriterCoroutine != null)
                {
                    StopCoroutine(_typewriterCoroutine);
                }
                const string defaultErrorMessage = "I DO NOT UNDERSTAND";
                _typewriterCoroutine = StartCoroutine(TypewriterEffect(defaultErrorMessage));
            }
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            _currentInput = string.Empty;
            UpdateDisplay();
        }

        private void OnBack(InputAction.CallbackContext context)
        {
            // Don't accept input while typing response
            if (_isTyping) return;

            if (_currentInput.Length > 0)
            {
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                UpdateDisplay();

                // Sound is played in Update() to avoid playing on key repeat
            }
        }

        private IEnumerator TypewriterEffect(string text)
        {
            // Wait before starting response
            yield return new WaitForSeconds(_responseDelay);

            // Add an empty line that we'll build up character by character
            _outputLines.Add("");

            foreach (char c in text)
            {
                // Always use the last line (in case trimming removed earlier lines)
                int lineIndex = _outputLines.Count - 1;
                _outputLines[lineIndex] += c;
                UpdateDisplay();
                TrimToMaxVisualLines();

                // Play typewriter sound (you can use same audio player or a different one)
                _audioPlayer?.PlayTypewriterSound();

                // Wait before next character
                yield return new WaitForSeconds(_typewriterSpeed);
            }

            _isTyping = false;
            _typewriterCoroutine = null;
        }

        private void AddOutputLine(string line)
        {
            _outputLines.Add(line);

            // Trim output based on actual visual line count
            TrimToMaxVisualLines();
        }

        private void TrimToMaxVisualLines()
        {
            if (_terminalText == null) return;

            // Force TMP to update so we can read line count
            _terminalText.ForceMeshUpdate();

            // Get actual line count from TextMeshPro
            int visualLineCount = _terminalText.textInfo.lineCount;

            // Remove oldest output lines until we're under max
            while (visualLineCount > _maxLines && _outputLines.Count > 0)
            {
                _outputLines.RemoveAt(0);

                // Rebuild text and check again
                UpdateDisplay();
                _terminalText.ForceMeshUpdate();
                visualLineCount = _terminalText.textInfo.lineCount;
            }
        }

        private void UpdateDisplay()
        {
            if (_terminalText == null) return;

            StringBuilder sb = new StringBuilder();

            // Add all output lines
            foreach (var line in _outputLines)
            {
                sb.AppendLine(line);
            }

            // Only show prompt and input if not typing response
            if (!_isTyping)
            {
                // Add current input line with prompt
                sb.Append(_prompt);
                sb.Append(_currentInput);

                string baseText = sb.ToString();

                // Update with cursor
                if (_cursorVisible)
                {
                    _terminalText.text = baseText + _cursorCharacter;
                }
                else
                {
                    _terminalText.text = baseText;
                }

                // Trim if needed (including the current input line in the count)
                TrimToMaxVisualLines();
            }
            else
            {
                // No cursor while typing response
                _terminalText.text = sb.ToString();
            }
        }

        private void StartCursorBlink()
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
            }

            _blinkCoroutine = StartCoroutine(BlinkCursor());
        }

        private IEnumerator BlinkCursor()
        {
            while (true)
            {
                _cursorVisible = !_cursorVisible;
                UpdateDisplay();
                yield return new WaitForSeconds(_cursorBlinkRate);
            }
        }

        private void OnDestroy()
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
            }

            _inputActions?.Dispose();
        }
    }
}
