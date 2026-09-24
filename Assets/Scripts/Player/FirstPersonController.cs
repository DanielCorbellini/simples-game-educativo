using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using EducationalGame.Quiz;

namespace EducationalGame.Player
{
    /// <summary>
    /// Controlador em primeira pessoa suave e responsivo para o jogo educativo escolar.
    /// Utiliza o New Input System (UnityEngine.InputSystem) nativamente para total compatibilidade com Unity 6.
    /// Gerencia movimentação WASD, corrida com Shift, mouse look vertical/horizontal com trava de cursor,
    /// e física de colisão com portas, corredores e paredes via CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Velocidade normal de caminhada pela escola (m/s).")]
        [SerializeField] private float walkSpeed = 4.0f;

        [Tooltip("Velocidade de corrida ao segurar Shift (m/s).")]
        [SerializeField] private float sprintSpeed = 6.5f;

        [Tooltip("Altura máxima do pulo (metros).")]
        [SerializeField] private float jumpHeight = 1.1f;

        [Tooltip("Aceleração da gravidade (m/s²).")]
        [SerializeField] private float gravity = -15.0f;

        [Header("Game Feel & Dynamics")]
        [Tooltip("Aceleração do movimento ao iniciar a caminhada (m/s²).")]
        [SerializeField] private float acceleration = 14.0f;

        [Tooltip("Desaceleração do movimento ao soltar as teclas (m/s²).")]
        [SerializeField] private float deceleration = 18.0f;

        [Tooltip("Ativar balanço suave de passos da câmera (Head Bobbing).")]
        [SerializeField] private bool enableHeadBobbing = true;

        [Tooltip("Frequência do balanço ao caminhar.")]
        [SerializeField] private float walkBobFrequency = 10.0f;

        [Tooltip("Amplitude do balanço ao caminhar.")]
        [SerializeField] private float walkBobAmount = 0.035f;

        [Tooltip("Frequência do balanço ao correr.")]
        [SerializeField] private float sprintBobFrequency = 14.0f;

        [Tooltip("Amplitude do balanço ao correr.")]
        [SerializeField] private float sprintBobAmount = 0.065f;

        [Tooltip("Ativar FOV dinâmico ao correr (Speed Kick).")]
        [SerializeField] private bool enableDynamicFOV = true;

        [Tooltip("FOV normal da câmera ao caminhar.")]
        [SerializeField] private float baseFOV = 65.0f;

        [Tooltip("FOV ampliado da câmera ao correr.")]
        [SerializeField] private float sprintFOV = 72.0f;

        [Tooltip("Velocidade de transição suave do FOV.")]
        [SerializeField] private float fovTransitionSpeed = 8.0f;

        [Header("Look Settings")]
        [Tooltip("Câmera dos olhos do jogador.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Sensibilidade do movimento do mouse.")]
        [SerializeField] private float mouseSensitivity = 1.5f;

        [Tooltip("Limite inferior para olhar para baixo (graus).")]
        [SerializeField] private float minPitch = -85.0f;

        [Tooltip("Limite superior para olhar para cima (graus).")]
        [SerializeField] private float maxPitch = 85.0f;

        [Header("Cursor Settings")]
        [Tooltip("Travar e ocultar o cursor automaticamente ao iniciar o jogo.")]
        [SerializeField] private bool autoLockCursor = true;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _cameraPitch;
        private Vector3 _currentHorizontalVelocity;
        private float _defaultCameraPosY = 1.65f;
        private float _bobTimer = 0f;

        public Camera PlayerCamera
        {
            get => playerCamera;
            set => playerCamera = value;
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera != null)
            {
                _cameraPitch = playerCamera.transform.localEulerAngles.x;
                if (_cameraPitch > 180f) _cameraPitch -= 360f;
                _defaultCameraPosY = playerCamera.transform.localPosition.y;
                if (baseFOV <= 0f) baseFOV = playerCamera.fieldOfView;
            }
        }

        private void Start()
        {
            if (autoLockCursor)
            {
                SetCursorLock(true);
            }
        }

        private void Update()
        {
            // Se o questionário ou interface estiver aberta, suspende controles de primeira pessoa
            if (QuizUIManager.Instance != null && QuizUIManager.Instance.IsQuizOpen)
            {
                return;
            }

            HandleCursorToggle();
            HandleMouseLook();
            HandleMovement();
        }

        /// <summary>
        /// Alterna a trava do cursor: ESC libera para usar a interface ou sair, clique do mouse trava de volta.
        /// </summary>
        private void HandleCursorToggle()
        {
            if (QuizUIManager.Instance != null && QuizUIManager.Instance.IsQuizOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLock(false);
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                // Se o ponteiro estiver sobre algum botão ou elemento de UI, não trava o cursor
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                SetCursorLock(true);
            }
        }

        /// <summary>
        /// Rotação da visão em primeira pessoa via mouse.
        /// O movimento horizontal (X) gira o corpo do jogador.
        /// O movimento vertical (Y) inclina a câmera (pitch) com clamp para não inverter a cabeça.
        /// </summary>
        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseDelta = mouse.delta.ReadValue() * (mouseSensitivity * 0.1f);

            // 1. Rotação horizontal do corpo no eixo Y
            transform.Rotate(Vector3.up * mouseDelta.x);

            // 2. Inclinação vertical da câmera no eixo X com clamp
            _cameraPitch -= mouseDelta.y;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            }
        }

        /// <summary>
        /// Movimentação do jogador com WASD, corrida com Shift e pulo com Espaço.
        /// </summary>
        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;

            // 1. Coletar entrada de movimento
            Vector2 moveInput = Vector2.zero;
            bool isSprinting = false;
            bool jumpPressed = false;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;

                isSprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            }

            // Normalizar entrada diagonal para evitar velocidade excessiva
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            // 2. Calcular velocidade e direção relativa ao jogador com inércia suave
            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * targetSpeed;

            float accelRate = (moveInput.sqrMagnitude > 0.01f) ? acceleration : deceleration;
            _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, accelRate * Time.deltaTime);

            // 3. Gravidade e Pulo
            if (_characterController.isGrounded)
            {
                // Leve força descendente para manter contato firme com o chão
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2.0f;
                }

                if (jumpPressed)
                {
                    // v = sqrt(2 * g * h)
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                }
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            // 4. Mover o CharacterController
            Vector3 finalVelocity = _currentHorizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(finalVelocity * Time.deltaTime);

            // 5. Aplicar Head Bobbing e FOV Dinâmico
            HandleHeadBobbing(moveInput, _characterController.isGrounded, isSprinting);
            HandleDynamicFOV(isSprinting, moveInput.sqrMagnitude > 0.01f);
        }

        /// <summary>
        /// Aplica balanço suave senoidal na altura da câmera ao caminhar/correr.
        /// </summary>
        private void HandleHeadBobbing(Vector2 moveInput, bool isGrounded, bool isSprinting)
        {
            if (!enableHeadBobbing || playerCamera == null) return;

            bool isMoving = isGrounded && moveInput.sqrMagnitude > 0.01f;
            Vector3 camPos = playerCamera.transform.localPosition;

            if (isMoving)
            {
                float freq = isSprinting ? sprintBobFrequency : walkBobFrequency;
                float amount = isSprinting ? sprintBobAmount : walkBobAmount;

                _bobTimer += Time.deltaTime * freq;
                float newY = _defaultCameraPosY + Mathf.Sin(_bobTimer) * amount;
                camPos.y = Mathf.Lerp(camPos.y, newY, Time.deltaTime * 15f);
            }
            else
            {
                _bobTimer = 0f;
                camPos.y = Mathf.Lerp(camPos.y, _defaultCameraPosY, Time.deltaTime * 6f);
            }

            playerCamera.transform.localPosition = camPos;
        }

        /// <summary>
        /// Aplica transição suave no campo de visão (FOV) ao correr para efeito de velocidade.
        /// </summary>
        private void HandleDynamicFOV(bool isSprinting, bool isMoving)
        {
            if (!enableDynamicFOV || playerCamera == null) return;

            float targetFOV = (isSprinting && isMoving) ? sprintFOV : baseFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
        }

        /// <summary>
        /// Configura a trava e visibilidade do cursor do mouse.
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
