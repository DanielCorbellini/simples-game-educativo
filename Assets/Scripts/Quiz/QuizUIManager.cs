using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using EducationalGame.Player;

namespace EducationalGame.Quiz
{
    /// <summary>
    /// Gerenciador da Interface do Usuário (UI) dos questionários e desafios educativos.
    /// Apresenta design moderno dark glassmorphism, suporte ao New Input System,
    /// retícula central (Crosshair), mini-HUD permanente de progresso das salas,
    /// Boletim Escolar detalhado com a tecla TAB e celebração comemorativa de Formatura Escolar.
    /// </summary>
    public class QuizUIManager : MonoBehaviour
    {
        public static QuizUIManager Instance { get; private set; }

        [Header("Estado")]
        [SerializeField] private bool isQuizOpen = false;

        private ClassroomSubject _currentSubject;
        private ClassroomInteractionZone _activeZone;
        private List<QuizQuestion> _currentQuestions;
        private int _currentQuestionIndex = 0;
        private int _score = 0;
        private bool _hasAnswered = false;
        private bool _isReportCardOpen = false;
        private bool _hasShownGraduation = false;

        // UI Canvas e Camadas
        private Canvas _canvas;
        private GameObject _crosshairObj;
        private GameObject _backdropScrim;

        // Mini HUD Permanente (Canto Superior Esquerdo)
        private GameObject _progressHudPanel;
        private Text _hudRoomsText;
        private Image[] _hudSubjectDots = new Image[4];

        // Prompt HUD (Parte Inferior)
        private GameObject _promptPanel;
        private Text _promptText;

        // Quiz Card e Header
        private GameObject _quizCard;
        private Text _subjectTitleText;
        private Image _subjectBadgeBg;
        private Outline _subjectBadgeOutline;
        private Text _progressText;
        private RectTransform _progressBarFill;
        private Image _progressBarFillImage;
        private Text _questionBodyText;

        // Alternativas
        private Button[] _optionButtons = new Button[4];
        private Text[] _optionTexts = new Text[4];
        private Image[] _optionBackgrounds = new Image[4];
        private Image[] _optionBadgeBgs = new Image[4];
        private Text[] _optionBadgeTexts = new Text[4];

        // Banner de Feedback Pedagógico
        private GameObject _feedbackPanel;
        private Image _feedbackBg;
        private Image _feedbackAccentBar;
        private Text _feedbackStatusText;
        private Text _feedbackExplanationText;
        private Button _nextButton;
        private Image _nextButtonBg;
        private Text _nextButtonText;

        // Tela de Resultados da Sala
        private GameObject _resultsPanel;
        private Text _resultsStarsText;
        private Text _resultsTitleText;
        private Text _resultsScoreBadgeText;
        private Text _resultsMessageText;
        private Button _resultsRetryButton;
        private Button _resultsCloseButton;

        // Painel de Boletim Escolar (Tecla TAB)
        private GameObject _reportCardPanel;
        private RectTransform _reportOverallBarFill;
        private Text _reportOverallPercentText;
        private Text[] _reportSubjectStatusTexts = new Text[4];
        private Text[] _reportSubjectScoreTexts = new Text[4];
        private Image[] _reportSubjectBadges = new Image[4];

        // Modal Comemorativo de Formatura
        private GameObject _graduationModal;

        private FirstPersonController _playerController;

        // Sprites 9-slice gerados proceduralmente para cantos arredondados suaves
        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;

        public bool IsQuizOpen => isQuizOpen;
        public bool IsReportCardOpen => _isReportCardOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureEventSystem();
            BuildUIHierarchy();
        }

        private void Start()
        {
            HidePrompt();
            if (_backdropScrim != null) _backdropScrim.SetActive(false);
            if (_quizCard != null) _quizCard.SetActive(false);
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            if (_reportCardPanel != null) _reportCardPanel.SetActive(false);
            if (_graduationModal != null) _graduationModal.SetActive(false);
            if (_crosshairObj != null) _crosshairObj.SetActive(true);

            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged += UpdateAllProgressVisuals;
            }

            UpdateAllProgressVisuals();
        }

        private void OnDestroy()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged -= UpdateAllProgressVisuals;
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                // Se o quiz não estiver ativo e o modal de formatura não estiver na tela:
                if (!isQuizOpen && (_graduationModal == null || !_graduationModal.activeSelf))
                {
                    bool tabHeld = kb.tabKey.isPressed;
                    if (tabHeld != _isReportCardOpen)
                    {
                        SetReportCardVisible(tabHeld);
                    }
                }
            }
        }

        /// <summary>
        /// Garante que exista um EventSystem com InputSystemUIInputModule ativo e configurado com ações padrão na cena.
        /// </summary>
        private void EnsureEventSystem()
        {
            var eventSys = EventSystem.current;
            if (eventSys == null)
            {
                eventSys = FindAnyObjectByType<EventSystem>();
            }

            InputSystemUIInputModule modern = null;

            if (eventSys == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                eventSys = esObj.AddComponent<EventSystem>();
                modern = esObj.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                var legacy = eventSys.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Destroy(legacy);
                }

                modern = eventSys.GetComponent<InputSystemUIInputModule>();
                if (modern == null)
                {
                    modern = eventSys.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            if (modern != null)
            {
                modern.AssignDefaultActions();
                modern.enabled = true;
            }

            if (eventSys != null)
            {
                eventSys.enabled = true;
            }
        }

        // =========================================================================
        // 1. Controle do Prompt HUD ("[E] Iniciar Desafio...")
        // =========================================================================

        public void ShowPrompt(string text)
        {
            if (isQuizOpen || _isReportCardOpen) return;

            if (_promptPanel != null && _promptText != null)
            {
                _promptText.text = text;
                _promptPanel.SetActive(true);
            }
        }

        public void HidePrompt()
        {
            if (_promptPanel != null)
            {
                _promptPanel.SetActive(false);
            }
        }

        // =========================================================================
        // 2. Fluxo do Questionário
        // =========================================================================

        public void StartQuiz(ClassroomSubject subject, ClassroomInteractionZone zone)
        {
            EnsureEventSystem();

            if (_reportCardPanel != null) _reportCardPanel.SetActive(false);
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            if (_graduationModal != null) _graduationModal.SetActive(false);
            _isReportCardOpen = false;

            _currentSubject = subject;
            _activeZone = zone;
            _currentQuestions = QuizDatabase.GetQuestions(subject);
            _currentQuestionIndex = 0;
            _score = 0;
            isQuizOpen = true;

            HidePrompt();
            if (_crosshairObj != null) _crosshairObj.SetActive(false);
            SetPlayerControlsLocked(true);

            if (_backdropScrim != null) _backdropScrim.SetActive(true);
            if (_quizCard != null) _quizCard.SetActive(true);

            DisplayCurrentQuestion();
        }

        private void DisplayCurrentQuestion()
        {
            if (_currentQuestions == null || _currentQuestionIndex >= _currentQuestions.Count)
            {
                ShowResults();
                return;
            }

            _hasAnswered = false;
            QuizQuestion q = _currentQuestions[_currentQuestionIndex];
            Color themeColor = QuizDatabase.GetSubjectThemeColor(_currentSubject);

            // 1. Atualizar Título e Badges no Cabeçalho
            string subjectName = QuizDatabase.GetSubjectDisplayName(_currentSubject).ToUpper();
            _subjectTitleText.text = subjectName;
            _subjectTitleText.color = themeColor;
            _subjectBadgeBg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.15f);
            if (_subjectBadgeOutline != null)
            {
                _subjectBadgeOutline.effectColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.6f);
            }

            // 2. Contador de Progresso ("Questão 01 de 04")
            _progressText.text = $"Questão {(_currentQuestionIndex + 1):D2} de {_currentQuestions.Count:D2}";

            // 3. Barra de Progresso visual
            float progressRatio = (float)(_currentQuestionIndex + 1) / _currentQuestions.Count;
            if (_progressBarFill != null)
            {
                _progressBarFill.anchorMax = new Vector2(progressRatio, 1f);
            }
            if (_progressBarFillImage != null)
            {
                _progressBarFillImage.color = themeColor;
            }

            // 4. Enunciado da Pergunta
            _questionBodyText.text = q.questionText;

            // 5. Botões de Alternativas
            for (int i = 0; i < 4; i++)
            {
                if (i < q.options.Length)
                {
                    _optionButtons[i].gameObject.SetActive(true);
                    _optionButtons[i].interactable = true;
                    _optionTexts[i].text = q.options[i];
                    _optionBackgrounds[i].color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

                    // Badge com letra (A, B, C, D)
                    _optionBadgeTexts[i].text = ((char)('A' + i)).ToString();
                    _optionBadgeTexts[i].color = Color.white;
                    _optionBadgeBgs[i].color = new Color(0.20f, 0.26f, 0.38f, 1.0f);
                }
                else
                {
                    _optionButtons[i].gameObject.SetActive(false);
                }
            }

            // 6. Ocultar feedback e botão próximo até resposta ser dada
            _feedbackPanel.SetActive(false);
            _nextButton.gameObject.SetActive(false);
        }

        public void SelectOption(int optionIndex)
        {
            if (_hasAnswered) return;
            _hasAnswered = true;

            QuizQuestion q = _currentQuestions[_currentQuestionIndex];
            bool isCorrect = (optionIndex == q.correctOptionIndex);

            // Cores semânticas modernas
            Color correctColor = new Color(0.06f, 0.65f, 0.38f, 1.0f); // Verde Esmeralda vibrante
            Color wrongColor = new Color(0.85f, 0.20f, 0.25f, 1.0f);   // Vermelho Carmim

            if (isCorrect)
            {
                _score++;
                _optionBackgrounds[optionIndex].color = correctColor;
                _optionBadgeBgs[optionIndex].color = new Color(0.04f, 0.45f, 0.26f, 1.0f);
                _optionBadgeTexts[optionIndex].text = "✓";
            }
            else
            {
                _optionBackgrounds[optionIndex].color = wrongColor;
                _optionBadgeBgs[optionIndex].color = new Color(0.55f, 0.12f, 0.16f, 1.0f);
                _optionBadgeTexts[optionIndex].text = "✕";

                // Destaca a alternativa correta para fixação didática
                _optionBackgrounds[q.correctOptionIndex].color = correctColor;
                _optionBadgeBgs[q.correctOptionIndex].color = new Color(0.04f, 0.45f, 0.26f, 1.0f);
                _optionBadgeTexts[q.correctOptionIndex].text = "✓";
            }

            // Trava cliques em outras opções
            for (int i = 0; i < 4; i++)
            {
                _optionButtons[i].interactable = false;
            }

            // Banner explicativo com visual pedagógico moderno
            _feedbackPanel.SetActive(true);
            _feedbackBg.color = isCorrect
                ? new Color(0.06f, 0.28f, 0.18f, 0.94f)
                : new Color(0.32f, 0.10f, 0.14f, 0.94f);

            _feedbackAccentBar.color = isCorrect ? correctColor : wrongColor;
            _feedbackStatusText.text = isCorrect ? "✓ RESPOSTA CORRETA!" : "✕ RESPOSTA INCORRETA";
            _feedbackStatusText.color = isCorrect ? new Color(0.40f, 1.0f, 0.65f) : new Color(1.0f, 0.55f, 0.55f);
            _feedbackExplanationText.text = q.explanation;

            // Configurar botão de avançar com a cor do tema
            _nextButton.gameObject.SetActive(true);
            bool isLast = (_currentQuestionIndex + 1 >= _currentQuestions.Count);
            _nextButtonText.text = isLast ? "Ver Resultados 🏆" : "Próxima Pergunta →";
            Color themeColor = QuizDatabase.GetSubjectThemeColor(_currentSubject);
            _nextButtonBg.color = themeColor;
        }

        public void OnNextButtonClicked()
        {
            _currentQuestionIndex++;
            if (_currentQuestionIndex < _currentQuestions.Count)
            {
                DisplayCurrentQuestion();
            }
            else
            {
                ShowResults();
            }
        }

        // =========================================================================
        // 3. Tela de Resultados e Conclusão
        // =========================================================================

        private void ShowResults()
        {
            if (_quizCard != null) _quizCard.SetActive(false);
            if (_resultsPanel != null) _resultsPanel.SetActive(true);

            int total = _currentQuestions.Count;
            float ratio = (float)_score / total;

            // Salva o progresso no SchoolProgressManager
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.RecordCompletion(_currentSubject, _score, total);
            }

            _resultsTitleText.text = $"Desafio de {QuizDatabase.GetSubjectDisplayName(_currentSubject)}";
            _resultsScoreBadgeText.text = $"Você acertou {_score} de {total} questões  •  {Mathf.RoundToInt(ratio * 100)}% de aproveitamento";

            if (ratio >= 0.75f)
            {
                _resultsStarsText.text = "★ ★ ★";
                _resultsStarsText.color = new Color(1.0f, 0.85f, 0.20f);
                _resultsMessageText.text = "Desempenho Excepcional!\nVocê demonstrou domínio completo dos conteúdos curriculares trabalhados nesta sala!";
            }
            else if (ratio >= 0.50f)
            {
                _resultsStarsText.text = "★ ★ ☆";
                _resultsStarsText.color = new Color(0.95f, 0.80f, 0.30f);
                _resultsMessageText.text = "Bom trabalho!\nVocê compreendeu os conceitos fundamentais. Vale a pena revisar para fixar o restante!";
            }
            else
            {
                _resultsStarsText.text = "★ ☆ ☆";
                _resultsStarsText.color = new Color(0.80f, 0.75f, 0.65f);
                _resultsMessageText.text = "Vale a pena revisar!\nExplore os livros nas carteiras e a biblioteca da escola, depois tente novamente para melhorar sua nota!";
            }

            // Se obteve 50% ou mais de acerto, marca a sala como concluída no ambiente 3D
            if (ratio >= 0.50f && _activeZone != null)
            {
                _activeZone.SetCompleted(true);
            }
        }

        public void RetryCurrentQuiz()
        {
            StartQuiz(_currentSubject, _activeZone);
        }

        public void CloseQuiz()
        {
            if (_backdropScrim != null) _backdropScrim.SetActive(false);
            if (_quizCard != null) _quizCard.SetActive(false);
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            isQuizOpen = false;

            SetPlayerControlsLocked(false);

            if (_activeZone != null)
            {
                _activeZone.UpdatePromptMessage();
            }

            if (_crosshairObj != null)
            {
                _crosshairObj.SetActive(true);
            }

            // Verifica se concluiu todas as 4 salas da escola para celebrar a Formatura
            if (SchoolProgressManager.Instance != null && SchoolProgressManager.Instance.IsAllCompleted() && !_hasShownGraduation)
            {
                _hasShownGraduation = true;
                ShowGraduationModal();
            }
        }

        private void SetPlayerControlsLocked(bool locked)
        {
            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<FirstPersonController>();
            }

            if (_playerController != null)
            {
                _playerController.enabled = !locked;
                _playerController.SetCursorLock(!locked);
            }

            // Assegura estado direto no cursor
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // =========================================================================
        // 4. Boletim Escolar (TAB) e Atualização de Progresso
        // =========================================================================

        public void SetReportCardVisible(bool visible)
        {
            if (isQuizOpen) return;

            _isReportCardOpen = visible;

            if (_reportCardPanel != null)
            {
                _reportCardPanel.SetActive(visible);
            }

            // Não ativa o fundo cinza escuro (o mundo 3D permanece 100% nítido)
            if (_backdropScrim != null && !isQuizOpen)
            {
                _backdropScrim.SetActive(false);
            }

            // Não pausa nem congela o jogador: o jogador continua se movendo com WASD e olhando livremente!
            // Não chama SetPlayerControlsLocked

            if (visible)
            {
                UpdateAllProgressVisuals();
            }
            else
            {
                if (_activeZone != null)
                {
                    _activeZone.UpdatePromptMessage();
                }
            }
        }

        public void ToggleReportCard(bool open)
        {
            SetReportCardVisible(open);
        }

        public void UpdateAllProgressVisuals()
        {
            if (SchoolProgressManager.Instance == null) return;

            int completed = SchoolProgressManager.Instance.GetCompletedCount();
            int total = SchoolProgressManager.Instance.GetTotalRoomsCount();
            float globalPct = SchoolProgressManager.Instance.GetGlobalCompletionPercentage();

            // 1. Atualizar Mini HUD Permanente no Canto Superior Esquerdo
            if (_hudRoomsText != null)
            {
                _hudRoomsText.text = $"🎓 Salas: {completed}/{total}  [TAB]";
            }

            for (int i = 0; i < 4; i++)
            {
                ClassroomSubject subj = (ClassroomSubject)i;
                var p = SchoolProgressManager.Instance.GetProgress(subj);
                Color theme = QuizDatabase.GetSubjectThemeColor(subj);

                if (_hudSubjectDots != null && i < _hudSubjectDots.Length && _hudSubjectDots[i] != null)
                {
                    if (p != null && p.isCompleted)
                    {
                        _hudSubjectDots[i].color = theme;
                    }
                    else
                    {
                        _hudSubjectDots[i].color = new Color(theme.r, theme.g, theme.b, 0.25f);
                    }
                }

                // 2. Atualizar Linhas do Boletim Escolar
                if (_reportSubjectStatusTexts != null && i < _reportSubjectStatusTexts.Length && _reportSubjectStatusTexts[i] != null)
                {
                    if (p != null && p.isCompleted)
                    {
                        string starsStr = new string('★', Mathf.Max(1, p.stars));
                        _reportSubjectStatusTexts[i].text = $"Concluído {starsStr}";
                        _reportSubjectStatusTexts[i].color = new Color(0.20f, 0.85f, 0.40f);
                    }
                    else
                    {
                        _reportSubjectStatusTexts[i].text = "Pendente";
                        _reportSubjectStatusTexts[i].color = new Color(0.6f, 0.65f, 0.75f);
                    }
                }

                if (_reportSubjectScoreTexts != null && i < _reportSubjectScoreTexts.Length && _reportSubjectScoreTexts[i] != null)
                {
                    if (p != null && p.bestScore > 0)
                    {
                        int pct = Mathf.RoundToInt(p.ScorePercentage * 100f);
                        _reportSubjectScoreTexts[i].text = $"Melhor Nota: {p.bestScore}/{p.totalQuestions} ({pct}%)";
                    }
                    else
                    {
                        _reportSubjectScoreTexts[i].text = "Não Realizado (--/--)";
                    }
                }
            }

            // 3. Atualizar Barra de Progresso Geral do Boletim
            if (_reportOverallBarFill != null)
            {
                _reportOverallBarFill.anchorMax = new Vector2(globalPct, 1f);
            }
            if (_reportOverallPercentText != null)
            {
                _reportOverallPercentText.text = $"Conclusão Geral do Ano Letivo: {Mathf.RoundToInt(globalPct * 100f)}%";
            }
        }

        private void ShowGraduationModal()
        {
            if (_graduationModal != null)
            {
                _graduationModal.SetActive(true);
                if (_backdropScrim != null) _backdropScrim.SetActive(true);
                if (_crosshairObj != null) _crosshairObj.SetActive(false);
                SetPlayerControlsLocked(true);
            }
        }

        // =========================================================================
        // 5. Construção Procedural do Canvas e Layout Moderno uGUI
        // =========================================================================

        private void BuildUIHierarchy()
        {
            // 1. Criar Canvas Overlay
            GameObject canvasObj = new GameObject("Quiz_Canvas");
            canvasObj.transform.SetParent(transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 2. Retícula Central (Crosshair)
            CreateCrosshairHUD(canvasObj.transform);

            // 3. Mini-HUD Permanente no Canto Superior Esquerdo
            CreateSchoolProgressHUD(canvasObj.transform, font);

            // 4. HUD de Prompt na parte inferior
            CreatePromptHUD(canvasObj.transform, font);

            // 5. Fundo de desfoque/escurecimento da cena 3D (Backdrop Scrim)
            CreateBackdropScrim(canvasObj.transform);

            // 6. Card Central do Questionário
            CreateQuizCard(canvasObj.transform, font);

            // 7. Painel de Resultados do Desafio
            CreateResultsPanel(canvasObj.transform, font);

            // 8. Painel Completo de Boletim Escolar (TAB)
            CreateReportCardPanel(canvasObj.transform, font);

            // 9. Modal Comemorativo de Formatura Escolar
            CreateGraduationModal(canvasObj.transform, font);
        }

        private void CreateCrosshairHUD(Transform parent)
        {
            _crosshairObj = new GameObject("Crosshair");
            _crosshairObj.transform.SetParent(parent, false);

            RectTransform rt = _crosshairObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(6f, 6f);

            Image img = _crosshairObj.AddComponent<Image>();
            img.sprite = GetCircleSprite();
            img.color = new Color(1f, 1f, 1f, 0.75f);
            img.raycastTarget = false;

            Outline outline = _crosshairObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void CreateSchoolProgressHUD(Transform parent, Font font)
        {
            _progressHudPanel = new GameObject("SchoolProgressHUD");
            _progressHudPanel.transform.SetParent(parent, false);

            RectTransform rt = _progressHudPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(28f, -24f);
            rt.sizeDelta = new Vector2(370f, 48f);

            Image bg = _progressHudPanel.AddComponent<Image>();
            bg.sprite = GetRoundedRectSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.88f);

            Button btn = _progressHudPanel.AddComponent<Button>();
            btn.onClick.AddListener(() => ToggleReportCard(!_isReportCardOpen));

            Outline outline = _progressHudPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Texto: "🎓 Salas: 0/4  [Segure TAB]"
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(_progressHudPanel.transform, false);
            RectTransform trt = txtObj.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(14f, 0f);
            trt.offsetMax = new Vector2(-120f, 0f);

            _hudRoomsText = txtObj.AddComponent<Text>();
            _hudRoomsText.font = font;
            _hudRoomsText.fontSize = 17;
            _hudRoomsText.fontStyle = FontStyle.Bold;
            _hudRoomsText.alignment = TextAnchor.MiddleLeft;
            _hudRoomsText.color = new Color(0.95f, 0.95f, 1f);
            _hudRoomsText.raycastTarget = false;
            _hudRoomsText.text = "🎓 Salas: 0/4  [Segure TAB]";

            // 4 Dots coloridos à direita
            float dotStartX = -95f;
            float dotSpacing = 22f;
            for (int i = 0; i < 4; i++)
            {
                GameObject dotObj = new GameObject($"Dot_{i}");
                dotObj.transform.SetParent(_progressHudPanel.transform, false);
                RectTransform drt = dotObj.AddComponent<RectTransform>();
                drt.anchorMin = new Vector2(1f, 0.5f);
                drt.anchorMax = new Vector2(1f, 0.5f);
                drt.pivot = new Vector2(0.5f, 0.5f);
                drt.anchoredPosition = new Vector2(dotStartX + (i * dotSpacing), 0f);
                drt.sizeDelta = new Vector2(14f, 14f);

                _hudSubjectDots[i] = dotObj.AddComponent<Image>();
                _hudSubjectDots[i].sprite = GetCircleSprite();
                _hudSubjectDots[i].raycastTarget = false;

                ClassroomSubject subj = (ClassroomSubject)i;
                Color c = QuizDatabase.GetSubjectThemeColor(subj);
                _hudSubjectDots[i].color = new Color(c.r, c.g, c.b, 0.25f);
            }
        }

        private void CreatePromptHUD(Transform parent, Font font)
        {
            _promptPanel = new GameObject("PromptHUD");
            _promptPanel.transform.SetParent(parent, false);

            RectTransform rt = _promptPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.10f);
            rt.anchorMax = new Vector2(0.5f, 0.10f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 60f);

            Image bg = _promptPanel.AddComponent<Image>();
            bg.sprite = GetRoundedRectSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.90f);

            Outline outline = _promptPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.40f, 0.60f, 0.40f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Badge de tecla [ E ]
            GameObject keyBadgeObj = new GameObject("KeyBadge");
            keyBadgeObj.transform.SetParent(_promptPanel.transform, false);
            RectTransform kRt = keyBadgeObj.AddComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0f, 0.5f);
            kRt.anchorMax = new Vector2(0f, 0.5f);
            kRt.pivot = new Vector2(0f, 0.5f);
            kRt.anchoredPosition = new Vector2(16f, 0f);
            kRt.sizeDelta = new Vector2(38f, 38f);

            Image keyBg = keyBadgeObj.AddComponent<Image>();
            keyBg.sprite = GetRoundedRectSprite();
            keyBg.type = Image.Type.Sliced;
            keyBg.color = new Color(0.95f, 0.75f, 0.20f, 0.95f);

            GameObject keyTxtObj = new GameObject("KeyText");
            keyTxtObj.transform.SetParent(keyBadgeObj.transform, false);
            RectTransform ktRt = keyTxtObj.AddComponent<RectTransform>();
            ktRt.anchorMin = Vector2.zero;
            ktRt.anchorMax = Vector2.one;
            ktRt.sizeDelta = Vector2.zero;
            Text keyTxt = keyTxtObj.AddComponent<Text>();
            keyTxt.font = font;
            keyTxt.fontSize = 20;
            keyTxt.fontStyle = FontStyle.Bold;
            keyTxt.alignment = TextAnchor.MiddleCenter;
            keyTxt.color = new Color(0.1f, 0.1f, 0.1f);
            keyTxt.text = "E";

            // Texto descritivo
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(_promptPanel.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(66f, 0f);
            trt.offsetMax = new Vector2(-20f, 0f);

            _promptText = textObj.AddComponent<Text>();
            _promptText.font = font;
            _promptText.fontSize = 20;
            _promptText.fontStyle = FontStyle.Bold;
            _promptText.alignment = TextAnchor.MiddleLeft;
            _promptText.color = Color.white;
            _promptText.text = "Iniciar Desafio Escolar";
        }

        private void CreateBackdropScrim(Transform parent)
        {
            _backdropScrim = new GameObject("BackdropScrim");
            _backdropScrim.transform.SetParent(parent, false);

            RectTransform rt = _backdropScrim.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image bg = _backdropScrim.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.08f, 0.82f);
            bg.raycastTarget = true;
        }

        private void CreateQuizCard(Transform parent, Font font)
        {
            _quizCard = new GameObject("QuizCard");
            _quizCard.transform.SetParent(parent, false);

            RectTransform cardRt = _quizCard.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(980f, 690f);

            Image cardBg = _quizCard.AddComponent<Image>();
            cardBg.sprite = GetRoundedRectSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

            Outline cardOutline = _quizCard.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.20f, 0.28f, 0.44f, 0.50f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // ==================== CABEÇALHO DO CARD ====================
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(_quizCard.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0f, 74f);

            // 1. Badge da Disciplina (Lado Esquerdo)
            GameObject badgeObj = new GameObject("SubjectBadge");
            badgeObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRt = badgeObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(24f, 0f);
            bRt.sizeDelta = new Vector2(280f, 42f);

            _subjectBadgeBg = badgeObj.AddComponent<Image>();
            _subjectBadgeBg.sprite = GetRoundedRectSprite();
            _subjectBadgeBg.type = Image.Type.Sliced;
            _subjectBadgeBg.color = new Color(0.2f, 0.5f, 1f, 0.15f);

            _subjectBadgeOutline = badgeObj.AddComponent<Outline>();
            _subjectBadgeOutline.effectColor = new Color(0.2f, 0.5f, 1f, 0.6f);
            _subjectBadgeOutline.effectDistance = new Vector2(1f, -1f);

            GameObject titleTxtObj = new GameObject("TitleText");
            titleTxtObj.transform.SetParent(badgeObj.transform, false);
            RectTransform tRt = titleTxtObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            _subjectTitleText = titleTxtObj.AddComponent<Text>();
            _subjectTitleText.font = font;
            _subjectTitleText.fontSize = 18;
            _subjectTitleText.fontStyle = FontStyle.Bold;
            _subjectTitleText.alignment = TextAnchor.MiddleCenter;
            _subjectTitleText.color = Color.white;
            _subjectTitleText.raycastTarget = false;

            // 2. Contador de Progresso (Centro)
            GameObject progObj = new GameObject("ProgressBadge");
            progObj.transform.SetParent(headerObj.transform, false);
            RectTransform pRt = progObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.anchoredPosition = new Vector2(0f, 0f);
            pRt.sizeDelta = new Vector2(200f, 38f);

            Image progBg = progObj.AddComponent<Image>();
            progBg.sprite = GetRoundedRectSprite();
            progBg.type = Image.Type.Sliced;
            progBg.color = new Color(0.13f, 0.17f, 0.25f, 0.90f);

            GameObject progTxtObj = new GameObject("Text");
            progTxtObj.transform.SetParent(progObj.transform, false);
            RectTransform ptRt = progTxtObj.AddComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.sizeDelta = Vector2.zero;

            _progressText = progTxtObj.AddComponent<Text>();
            _progressText.font = font;
            _progressText.fontSize = 17;
            _progressText.fontStyle = FontStyle.Normal;
            _progressText.alignment = TextAnchor.MiddleCenter;
            _progressText.color = new Color(0.85f, 0.90f, 0.98f);
            _progressText.raycastTarget = false;

            // 3. Botão Fechar [X] (Canto Direito)
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(headerObj.transform, false);
            RectTransform cRt = closeObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 0.5f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.sizeDelta = new Vector2(40f, 40f);
            cRt.anchoredPosition = new Vector2(-24f, 0f);

            Button closeBtn = closeObj.AddComponent<Button>();
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.sprite = GetCircleSprite();
            closeBg.color = new Color(0.16f, 0.20f, 0.30f, 0.90f);

            ColorBlock closeCb = closeBtn.colors;
            closeCb.highlightedColor = new Color(0.85f, 0.22f, 0.25f, 0.95f);
            closeCb.pressedColor = new Color(0.65f, 0.15f, 0.18f, 1.0f);
            closeBtn.colors = closeCb;
            closeBtn.onClick.AddListener(CloseQuiz);

            GameObject closeTxtObj = new GameObject("X");
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            RectTransform cxRt = closeTxtObj.AddComponent<RectTransform>();
            cxRt.anchorMin = Vector2.zero;
            cxRt.anchorMax = Vector2.one;
            cxRt.sizeDelta = Vector2.zero;

            Text closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.font = font;
            closeTxt.fontSize = 20;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
            closeTxt.text = "✕";
            closeTxt.raycastTarget = false;

            // Barra Fina de Progresso Logo Abaixo do Cabeçalho
            GameObject barTrackObj = new GameObject("ProgressBarTrack");
            barTrackObj.transform.SetParent(_quizCard.transform, false);
            RectTransform btRt = barTrackObj.AddComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0f, 1f);
            btRt.anchorMax = new Vector2(1f, 1f);
            btRt.pivot = new Vector2(0.5f, 1f);
            btRt.anchoredPosition = new Vector2(0f, -74f);
            btRt.sizeDelta = new Vector2(0f, 4f);

            Image btBg = barTrackObj.AddComponent<Image>();
            btBg.color = new Color(0.12f, 0.15f, 0.22f, 1f);

            GameObject barFillObj = new GameObject("Fill");
            barFillObj.transform.SetParent(barTrackObj.transform, false);
            _progressBarFill = barFillObj.AddComponent<RectTransform>();
            _progressBarFill.anchorMin = new Vector2(0f, 0f);
            _progressBarFill.anchorMax = new Vector2(0.25f, 1f);
            _progressBarFill.pivot = new Vector2(0f, 0.5f);
            _progressBarFill.sizeDelta = Vector2.zero;

            _progressBarFillImage = barFillObj.AddComponent<Image>();
            _progressBarFillImage.color = new Color(0.20f, 0.50f, 1.00f);

            // ==================== ENUNCIADO DA QUESTÃO ====================
            GameObject questionBoxObj = new GameObject("QuestionBox");
            questionBoxObj.transform.SetParent(_quizCard.transform, false);
            RectTransform qbRt = questionBoxObj.AddComponent<RectTransform>();
            qbRt.anchorMin = new Vector2(0f, 1f);
            qbRt.anchorMax = new Vector2(1f, 1f);
            qbRt.pivot = new Vector2(0.5f, 1f);
            qbRt.anchoredPosition = new Vector2(0f, -92f);
            qbRt.sizeDelta = new Vector2(-48f, 112f);

            Image qbBg = questionBoxObj.AddComponent<Image>();
            qbBg.sprite = GetRoundedRectSprite();
            qbBg.type = Image.Type.Sliced;
            qbBg.color = new Color(0.11f, 0.14f, 0.21f, 0.85f);

            GameObject bodyObj = new GameObject("QuestionBody");
            bodyObj.transform.SetParent(questionBoxObj.transform, false);
            RectTransform bBodyRt = bodyObj.AddComponent<RectTransform>();
            bBodyRt.anchorMin = Vector2.zero;
            bBodyRt.anchorMax = Vector2.one;
            bBodyRt.offsetMin = new Vector2(24f, 12f);
            bBodyRt.offsetMax = new Vector2(-24f, -12f);

            _questionBodyText = bodyObj.AddComponent<Text>();
            _questionBodyText.font = font;
            _questionBodyText.fontSize = 22;
            _questionBodyText.fontStyle = FontStyle.Normal;
            _questionBodyText.alignment = TextAnchor.MiddleLeft;
            _questionBodyText.lineSpacing = 1.15f;
            _questionBodyText.color = new Color(0.96f, 0.97f, 1.0f);
            _questionBodyText.raycastTarget = false;

            // ==================== 4 BOTÕES DE ALTERNATIVAS ====================
            float startY = -220f;
            float spacingY = 66f;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                GameObject optObj = new GameObject($"OptionButton_{i}");
                optObj.transform.SetParent(_quizCard.transform, false);
                RectTransform oRt = optObj.AddComponent<RectTransform>();
                oRt.anchorMin = new Vector2(0f, 1f);
                oRt.anchorMax = new Vector2(1f, 1f);
                oRt.pivot = new Vector2(0.5f, 1f);
                oRt.anchoredPosition = new Vector2(0f, startY - (i * spacingY));
                oRt.sizeDelta = new Vector2(-48f, 56f);

                _optionBackgrounds[i] = optObj.AddComponent<Image>();
                _optionBackgrounds[i].sprite = GetRoundedRectSprite();
                _optionBackgrounds[i].type = Image.Type.Sliced;
                _optionBackgrounds[i].color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
                _optionBackgrounds[i].raycastTarget = true;

                _optionButtons[i] = optObj.AddComponent<Button>();
                _optionButtons[i].targetGraphic = _optionBackgrounds[i];
                _optionButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };

                ColorBlock cb = _optionButtons[i].colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(0.20f, 0.26f, 0.38f, 1.0f);
                cb.pressedColor = new Color(0.26f, 0.34f, 0.50f, 1.0f);
                cb.selectedColor = Color.white;
                cb.disabledColor = Color.white;
                _optionButtons[i].colors = cb;
                _optionButtons[i].onClick.AddListener(() => SelectOption(index));

                // Badge de Letra Circular à esquerda (A, B, C, D)
                GameObject badgeLetObj = new GameObject("LetterBadge");
                badgeLetObj.transform.SetParent(optObj.transform, false);
                RectTransform blRt = badgeLetObj.AddComponent<RectTransform>();
                blRt.anchorMin = new Vector2(0f, 0.5f);
                blRt.anchorMax = new Vector2(0f, 0.5f);
                blRt.pivot = new Vector2(0f, 0.5f);
                blRt.anchoredPosition = new Vector2(12f, 0f);
                blRt.sizeDelta = new Vector2(36f, 36f);

                _optionBadgeBgs[i] = badgeLetObj.AddComponent<Image>();
                _optionBadgeBgs[i].sprite = GetCircleSprite();
                _optionBadgeBgs[i].color = new Color(0.20f, 0.26f, 0.38f, 1.0f);
                _optionBadgeBgs[i].raycastTarget = false;

                GameObject badgeTxtObj = new GameObject("Text");
                badgeTxtObj.transform.SetParent(badgeLetObj.transform, false);
                RectTransform btxRt = badgeTxtObj.AddComponent<RectTransform>();
                btxRt.anchorMin = Vector2.zero;
                btxRt.anchorMax = Vector2.one;
                btxRt.sizeDelta = Vector2.zero;

                _optionBadgeTexts[i] = badgeTxtObj.AddComponent<Text>();
                _optionBadgeTexts[i].font = font;
                _optionBadgeTexts[i].fontSize = 18;
                _optionBadgeTexts[i].fontStyle = FontStyle.Bold;
                _optionBadgeTexts[i].alignment = TextAnchor.MiddleCenter;
                _optionBadgeTexts[i].color = Color.white;
                _optionBadgeTexts[i].raycastTarget = false;

                // Texto da Alternativa
                GameObject optTxtObj = new GameObject("Text");
                optTxtObj.transform.SetParent(optObj.transform, false);
                RectTransform otRt = optTxtObj.AddComponent<RectTransform>();
                otRt.anchorMin = Vector2.zero;
                otRt.anchorMax = Vector2.one;
                otRt.offsetMin = new Vector2(60f, 0f);
                otRt.offsetMax = new Vector2(-20f, 0f);

                _optionTexts[i] = optTxtObj.AddComponent<Text>();
                _optionTexts[i].font = font;
                _optionTexts[i].fontSize = 19;
                _optionTexts[i].alignment = TextAnchor.MiddleLeft;
                _optionTexts[i].color = Color.white;
                _optionTexts[i].raycastTarget = false;
            }

            // ==================== BANNER DE FEEDBACK EXPLICATIVO ====================
            _feedbackPanel = new GameObject("FeedbackPanel");
            _feedbackPanel.transform.SetParent(_quizCard.transform, false);
            RectTransform fbRt = _feedbackPanel.AddComponent<RectTransform>();
            fbRt.anchorMin = new Vector2(0f, 0f);
            fbRt.anchorMax = new Vector2(1f, 0f);
            fbRt.pivot = new Vector2(0.5f, 0f);
            fbRt.anchoredPosition = new Vector2(0f, 88f);
            fbRt.sizeDelta = new Vector2(-48f, 84f);

            _feedbackBg = _feedbackPanel.AddComponent<Image>();
            _feedbackBg.sprite = GetRoundedRectSprite();
            _feedbackBg.type = Image.Type.Sliced;
            _feedbackBg.color = new Color(0.06f, 0.28f, 0.18f, 0.94f);

            // Barra lateral decorativa colorida
            GameObject accentObj = new GameObject("AccentBar");
            accentObj.transform.SetParent(_feedbackPanel.transform, false);
            RectTransform acRt = accentObj.AddComponent<RectTransform>();
            acRt.anchorMin = new Vector2(0f, 0f);
            acRt.anchorMax = new Vector2(0f, 1f);
            acRt.pivot = new Vector2(0f, 0.5f);
            acRt.sizeDelta = new Vector2(6f, 0f);

            _feedbackAccentBar = accentObj.AddComponent<Image>();
            _feedbackAccentBar.color = new Color(0.06f, 0.65f, 0.38f, 1f);

            // Título de Status (✓ CORRETO! ou ✕ INCORRETO)
            GameObject fbStatusObj = new GameObject("StatusText");
            fbStatusObj.transform.SetParent(_feedbackPanel.transform, false);
            RectTransform fsRt = fbStatusObj.AddComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0f, 1f);
            fsRt.anchorMax = new Vector2(1f, 1f);
            fsRt.pivot = new Vector2(0f, 1f);
            fsRt.anchoredPosition = new Vector2(22f, -10f);
            fsRt.sizeDelta = new Vector2(-34f, 24f);

            _feedbackStatusText = fbStatusObj.AddComponent<Text>();
            _feedbackStatusText.font = font;
            _feedbackStatusText.fontSize = 17;
            _feedbackStatusText.fontStyle = FontStyle.Bold;
            _feedbackStatusText.alignment = TextAnchor.MiddleLeft;
            _feedbackStatusText.color = new Color(0.40f, 1.0f, 0.65f);
            _feedbackStatusText.raycastTarget = false;

            // Corpo da Explicação Didática
            GameObject fbTxtObj = new GameObject("ExplanationText");
            fbTxtObj.transform.SetParent(_feedbackPanel.transform, false);
            RectTransform fbtRt = fbTxtObj.AddComponent<RectTransform>();
            fbtRt.anchorMin = new Vector2(0f, 0f);
            fbtRt.anchorMax = new Vector2(1f, 1f);
            fbtRt.offsetMin = new Vector2(22f, 8f);
            fbtRt.offsetMax = new Vector2(-16f, -34f);

            _feedbackExplanationText = fbTxtObj.AddComponent<Text>();
            _feedbackExplanationText.font = font;
            _feedbackExplanationText.fontSize = 16;
            _feedbackExplanationText.fontStyle = FontStyle.Normal;
            _feedbackExplanationText.alignment = TextAnchor.MiddleLeft;
            _feedbackExplanationText.lineSpacing = 1.15f;
            _feedbackExplanationText.color = new Color(0.95f, 0.95f, 0.98f);
            _feedbackExplanationText.raycastTarget = false;

            // ==================== BOTÃO "PRÓXIMA PERGUNTA" ====================
            GameObject nextObj = new GameObject("NextButton");
            nextObj.transform.SetParent(_quizCard.transform, false);
            RectTransform nRt = nextObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(1f, 0f);
            nRt.anchorMax = new Vector2(1f, 0f);
            nRt.pivot = new Vector2(1f, 0f);
            nRt.anchoredPosition = new Vector2(-24f, 20f);
            nRt.sizeDelta = new Vector2(270f, 52f);

            _nextButtonBg = nextObj.AddComponent<Image>();
            _nextButtonBg.sprite = GetRoundedRectSprite();
            _nextButtonBg.type = Image.Type.Sliced;
            _nextButtonBg.color = new Color(0.20f, 0.50f, 1.00f);

            _nextButton = nextObj.AddComponent<Button>();
            ColorBlock nCb = _nextButton.colors;
            nCb.highlightedColor = new Color(0.35f, 0.65f, 1.00f, 1f);
            nCb.pressedColor = new Color(0.15f, 0.40f, 0.85f, 1f);
            _nextButton.colors = nCb;
            _nextButton.onClick.AddListener(OnNextButtonClicked);

            GameObject nextTxtObj = new GameObject("Text");
            nextTxtObj.transform.SetParent(nextObj.transform, false);
            RectTransform ntxRt = nextTxtObj.AddComponent<RectTransform>();
            ntxRt.anchorMin = Vector2.zero;
            ntxRt.anchorMax = Vector2.one;
            ntxRt.sizeDelta = Vector2.zero;

            _nextButtonText = nextTxtObj.AddComponent<Text>();
            _nextButtonText.font = font;
            _nextButtonText.fontSize = 20;
            _nextButtonText.fontStyle = FontStyle.Bold;
            _nextButtonText.alignment = TextAnchor.MiddleCenter;
            _nextButtonText.color = Color.white;
            _nextButtonText.text = "Próxima Pergunta →";
            _nextButtonText.raycastTarget = false;
        }

        private void CreateResultsPanel(Transform parent, Font font)
        {
            _resultsPanel = new GameObject("ResultsPanel");
            _resultsPanel.transform.SetParent(parent, false);

            RectTransform rRt = _resultsPanel.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 0.5f);
            rRt.anchorMax = new Vector2(0.5f, 0.5f);
            rRt.pivot = new Vector2(0.5f, 0.5f);
            rRt.sizeDelta = new Vector2(820f, 520f);

            Image rBg = _resultsPanel.AddComponent<Image>();
            rBg.sprite = GetRoundedRectSprite();
            rBg.type = Image.Type.Sliced;
            rBg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);

            Outline rOutline = _resultsPanel.AddComponent<Outline>();
            rOutline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.45f);
            rOutline.effectDistance = new Vector2(2f, -2f);

            // Estrelas / Troféu Decorativo
            GameObject starsObj = new GameObject("Stars");
            starsObj.transform.SetParent(_resultsPanel.transform, false);
            RectTransform stRt = starsObj.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 1f);
            stRt.anchorMax = new Vector2(0.5f, 1f);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.anchoredPosition = new Vector2(0f, -32f);
            stRt.sizeDelta = new Vector2(300f, 44f);

            _resultsStarsText = starsObj.AddComponent<Text>();
            _resultsStarsText.font = font;
            _resultsStarsText.fontSize = 36;
            _resultsStarsText.fontStyle = FontStyle.Bold;
            _resultsStarsText.alignment = TextAnchor.MiddleCenter;
            _resultsStarsText.color = new Color(1.0f, 0.85f, 0.20f);

            // Título do Desafio Concluído
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_resultsPanel.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -80f);
            tRt.sizeDelta = new Vector2(0f, 50f);

            _resultsTitleText = titleObj.AddComponent<Text>();
            _resultsTitleText.font = font;
            _resultsTitleText.fontSize = 28;
            _resultsTitleText.fontStyle = FontStyle.Bold;
            _resultsTitleText.alignment = TextAnchor.MiddleCenter;
            _resultsTitleText.color = Color.white;

            // Badge de Pontuação
            GameObject scoreBadgeObj = new GameObject("ScoreBadge");
            scoreBadgeObj.transform.SetParent(_resultsPanel.transform, false);
            RectTransform sbRt = scoreBadgeObj.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 1f);
            sbRt.anchorMax = new Vector2(0.5f, 1f);
            sbRt.pivot = new Vector2(0.5f, 1f);
            sbRt.anchoredPosition = new Vector2(0f, -145f);
            sbRt.sizeDelta = new Vector2(620f, 54f);

            Image sbBg = scoreBadgeObj.AddComponent<Image>();
            sbBg.sprite = GetRoundedRectSprite();
            sbBg.type = Image.Type.Sliced;
            sbBg.color = new Color(0.13f, 0.17f, 0.26f, 0.95f);

            GameObject scoreTxtObj = new GameObject("Text");
            scoreTxtObj.transform.SetParent(scoreBadgeObj.transform, false);
            RectTransform scRt = scoreTxtObj.AddComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero;
            scRt.anchorMax = Vector2.one;
            scRt.sizeDelta = Vector2.zero;

            _resultsScoreBadgeText = scoreTxtObj.AddComponent<Text>();
            _resultsScoreBadgeText.font = font;
            _resultsScoreBadgeText.fontSize = 21;
            _resultsScoreBadgeText.fontStyle = FontStyle.Bold;
            _resultsScoreBadgeText.alignment = TextAnchor.MiddleCenter;
            _resultsScoreBadgeText.color = new Color(0.40f, 0.85f, 1.0f);

            // Mensagem Pedagógica
            GameObject msgCardObj = new GameObject("MessageCard");
            msgCardObj.transform.SetParent(_resultsPanel.transform, false);
            RectTransform mcRt = msgCardObj.AddComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0.5f, 0.5f);
            mcRt.anchorMax = new Vector2(0.5f, 0.5f);
            mcRt.pivot = new Vector2(0.5f, 0.5f);
            mcRt.anchoredPosition = new Vector2(0f, -30f);
            mcRt.sizeDelta = new Vector2(720f, 110f);

            Image mcBg = msgCardObj.AddComponent<Image>();
            mcBg.sprite = GetRoundedRectSprite();
            mcBg.type = Image.Type.Sliced;
            mcBg.color = new Color(0.11f, 0.14f, 0.20f, 0.80f);

            GameObject msgTxtObj = new GameObject("Text");
            msgTxtObj.transform.SetParent(msgCardObj.transform, false);
            RectTransform mtRt = msgTxtObj.AddComponent<RectTransform>();
            mtRt.anchorMin = Vector2.zero;
            mtRt.anchorMax = Vector2.one;
            mtRt.offsetMin = new Vector2(24f, 12f);
            mtRt.offsetMax = new Vector2(-24f, -12f);

            _resultsMessageText = msgTxtObj.AddComponent<Text>();
            _resultsMessageText.font = font;
            _resultsMessageText.fontSize = 19;
            _resultsMessageText.lineSpacing = 1.2f;
            _resultsMessageText.alignment = TextAnchor.MiddleCenter;
            _resultsMessageText.color = new Color(0.90f, 0.92f, 0.97f);

            // Linha de Ações: "Refazer Desafio" e "Continuar Explorando"
            // 1. Refazer Desafio
            GameObject retryObj = new GameObject("RetryBtn");
            retryObj.transform.SetParent(_resultsPanel.transform, false);
            RectTransform retryRt = retryObj.AddComponent<RectTransform>();
            retryRt.anchorMin = new Vector2(0.5f, 0f);
            retryRt.anchorMax = new Vector2(0.5f, 0f);
            retryRt.pivot = new Vector2(1f, 0f);
            retryRt.anchoredPosition = new Vector2(-12f, 32f);
            retryRt.sizeDelta = new Vector2(240f, 54f);

            Image retryBg = retryObj.AddComponent<Image>();
            retryBg.sprite = GetRoundedRectSprite();
            retryBg.type = Image.Type.Sliced;
            retryBg.color = new Color(0.18f, 0.22f, 0.32f, 0.95f);

            _resultsRetryButton = retryObj.AddComponent<Button>();
            ColorBlock retCb = _resultsRetryButton.colors;
            retCb.highlightedColor = new Color(0.25f, 0.32f, 0.46f, 1f);
            retCb.pressedColor = new Color(0.15f, 0.20f, 0.30f, 1f);
            _resultsRetryButton.colors = retCb;
            _resultsRetryButton.onClick.AddListener(RetryCurrentQuiz);

            GameObject retryTxtObj = new GameObject("Text");
            retryTxtObj.transform.SetParent(retryObj.transform, false);
            RectTransform rtxRt = retryTxtObj.AddComponent<RectTransform>();
            rtxRt.anchorMin = Vector2.zero;
            rtxRt.anchorMax = Vector2.one;
            rtxRt.sizeDelta = Vector2.zero;
            Text retryTxt = retryTxtObj.AddComponent<Text>();
            retryTxt.font = font;
            retryTxt.fontSize = 19;
            retryTxt.fontStyle = FontStyle.Bold;
            retryTxt.alignment = TextAnchor.MiddleCenter;
            retryTxt.color = Color.white;
            retryTxt.text = "Refazer Desafio ↺";
            retryTxt.raycastTarget = false;

            // 2. Continuar Explorando
            GameObject btnObj = new GameObject("ContinueBtn");
            btnObj.transform.SetParent(_resultsPanel.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0f, 0f);
            bRt.anchoredPosition = new Vector2(12f, 32f);
            bRt.sizeDelta = new Vector2(280f, 54f);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.sprite = GetRoundedRectSprite();
            btnBg.type = Image.Type.Sliced;
            btnBg.color = new Color(0.06f, 0.65f, 0.38f, 1.0f);

            _resultsCloseButton = btnObj.AddComponent<Button>();
            ColorBlock clCb = _resultsCloseButton.colors;
            clCb.highlightedColor = new Color(0.10f, 0.78f, 0.46f, 1f);
            clCb.pressedColor = new Color(0.04f, 0.50f, 0.28f, 1f);
            _resultsCloseButton.colors = clCb;
            _resultsCloseButton.onClick.AddListener(CloseQuiz);

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btxRt = btnTxtObj.AddComponent<RectTransform>();
            btxRt.anchorMin = Vector2.zero;
            btxRt.anchorMax = Vector2.one;
            btxRt.sizeDelta = Vector2.zero;

            Text btnTxt = btnTxtObj.AddComponent<Text>();
            btnTxt.font = font;
            btnTxt.fontSize = 20;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
            btnTxt.text = "Continuar Explorando ✓";
            btnTxt.raycastTarget = false;
        }

        private void CreateReportCardPanel(Transform parent, Font font)
        {
            _reportCardPanel = new GameObject("ReportCardPanel");
            _reportCardPanel.transform.SetParent(parent, false);

            RectTransform cardRt = _reportCardPanel.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(860f, 520f);

            Image cardBg = _reportCardPanel.AddComponent<Image>();
            cardBg.sprite = GetRoundedRectSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.06f, 0.08f, 0.14f, 0.94f); // Translúcido sobre o mundo 3D sem escurecer

            Outline cardOutline = _reportCardPanel.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.55f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // 1. Cabeçalho do Boletim (Y: -22f, Height: 36f)
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(_reportCardPanel.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -22f);
            hRt.sizeDelta = new Vector2(-48f, 36f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = new Vector2(-120f, 0f);

            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = font;
            tTxt.fontSize = 20;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleLeft;
            tTxt.color = Color.white;
            tTxt.text = "🎓 BOLETIM ESCOLAR DO ALUNO";

            // Indicador de Segure TAB à direita no topo perfeitamente alinhado com o título
            GameObject tabHintObj = new GameObject("TabHint");
            tabHintObj.transform.SetParent(headerObj.transform, false);
            RectTransform tabRt = tabHintObj.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(1f, 0f);
            tabRt.anchorMax = new Vector2(1f, 1f);
            tabRt.pivot = new Vector2(1f, 0.5f);
            tabRt.offsetMin = new Vector2(-110f, 0f);
            tabRt.offsetMax = Vector2.zero;

            Text tabHintTxt = tabHintObj.AddComponent<Text>();
            tabHintTxt.font = font;
            tabHintTxt.fontSize = 15;
            tabHintTxt.fontStyle = FontStyle.Bold;
            tabHintTxt.alignment = TextAnchor.MiddleRight;
            tabHintTxt.color = new Color(1.0f, 0.85f, 0.25f);
            tabHintTxt.text = "[ TAB ]";

            // 2. Seção de Progresso Geral
            // Label de Porcentagem (Y: -68f, Height: 20f)
            GameObject percentTxtObj = new GameObject("PercentText");
            percentTxtObj.transform.SetParent(_reportCardPanel.transform, false);
            RectTransform prt = percentTxtObj.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -68f);
            prt.sizeDelta = new Vector2(-48f, 20f);

            _reportOverallPercentText = percentTxtObj.AddComponent<Text>();
            _reportOverallPercentText.font = font;
            _reportOverallPercentText.fontSize = 15;
            _reportOverallPercentText.fontStyle = FontStyle.Bold;
            _reportOverallPercentText.alignment = TextAnchor.MiddleLeft;
            _reportOverallPercentText.color = new Color(0.85f, 0.90f, 0.98f);
            _reportOverallPercentText.text = "Conclusão Geral do Ano Letivo: 0%";

            // Barra fina de progresso geral (Y: -94f, Height: 8f)
            GameObject progTrackObj = new GameObject("ProgressTrack");
            progTrackObj.transform.SetParent(_reportCardPanel.transform, false);
            RectTransform ptRt = progTrackObj.AddComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0f, 1f);
            ptRt.anchorMax = new Vector2(1f, 1f);
            ptRt.pivot = new Vector2(0.5f, 1f);
            ptRt.anchoredPosition = new Vector2(0f, -94f);
            ptRt.sizeDelta = new Vector2(-48f, 8f);

            Image ptBg = progTrackObj.AddComponent<Image>();
            ptBg.sprite = GetRoundedRectSprite();
            ptBg.type = Image.Type.Sliced;
            ptBg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

            GameObject progFillObj = new GameObject("Fill");
            progFillObj.transform.SetParent(progTrackObj.transform, false);
            _reportOverallBarFill = progFillObj.AddComponent<RectTransform>();
            _reportOverallBarFill.anchorMin = new Vector2(0f, 0f);
            _reportOverallBarFill.anchorMax = new Vector2(0f, 1f);
            _reportOverallBarFill.pivot = new Vector2(0f, 0.5f);
            _reportOverallBarFill.sizeDelta = Vector2.zero;

            Image pfImg = progFillObj.AddComponent<Image>();
            pfImg.sprite = GetRoundedRectSprite();
            pfImg.type = Image.Type.Sliced;
            pfImg.color = new Color(0.18f, 0.75f, 0.35f, 1f);

            // 3. 4 Linhas de Disciplinas (Start Y: -118f, Row Height: 68f, Spacing: 76f)
            float startY = -118f;
            float rowHeight = 68f;
            float rowSpacing = 76f;

            for (int i = 0; i < 4; i++)
            {
                ClassroomSubject subj = (ClassroomSubject)i;
                Color theme = QuizDatabase.GetSubjectThemeColor(subj);

                GameObject rowObj = new GameObject($"SubjectRow_{i}");
                rowObj.transform.SetParent(_reportCardPanel.transform, false);
                RectTransform rRt = rowObj.AddComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0f, 1f);
                rRt.anchorMax = new Vector2(1f, 1f);
                rRt.pivot = new Vector2(0.5f, 1f);
                rRt.anchoredPosition = new Vector2(0f, startY - (i * rowSpacing));
                rRt.sizeDelta = new Vector2(-48f, rowHeight);

                Image rBg = rowObj.AddComponent<Image>();
                rBg.sprite = GetRoundedRectSprite();
                rBg.type = Image.Type.Sliced;
                rBg.color = new Color(0.10f, 0.13f, 0.19f, 0.88f);

                // Faixa lateral com a cor da matéria
                GameObject stripeObj = new GameObject("Stripe");
                stripeObj.transform.SetParent(rowObj.transform, false);
                RectTransform sRt = stripeObj.AddComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0f, 0f);
                sRt.anchorMax = new Vector2(0f, 1f);
                sRt.pivot = new Vector2(0f, 0.5f);
                sRt.sizeDelta = new Vector2(6f, 0f);
                _reportSubjectBadges[i] = stripeObj.AddComponent<Image>();
                _reportSubjectBadges[i].color = theme;

                // Coluna 1: Nome da Disciplina (Centralizado dentro do componente)
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(rowObj.transform, false);
                RectTransform nRt = nameObj.AddComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0f, 0f);
                nRt.anchorMax = new Vector2(0.38f, 1f);
                nRt.offsetMin = new Vector2(16f, 0f);
                nRt.offsetMax = new Vector2(-8f, 0f);

                Text nTxt = nameObj.AddComponent<Text>();
                nTxt.font = font;
                nTxt.fontSize = 18;
                nTxt.fontStyle = FontStyle.Bold;
                nTxt.alignment = TextAnchor.MiddleCenter;
                nTxt.color = Color.white;
                nTxt.text = $"Sala {i + 1} — {QuizDatabase.GetSubjectDisplayName(subj)}";

                // Coluna 2: Pontuação (Centralizado dentro do componente)
                GameObject scoreObj = new GameObject("Score");
                scoreObj.transform.SetParent(rowObj.transform, false);
                RectTransform scRt = scoreObj.AddComponent<RectTransform>();
                scRt.anchorMin = new Vector2(0.38f, 0f);
                scRt.anchorMax = new Vector2(0.70f, 1f);
                scRt.offsetMin = new Vector2(8f, 0f);
                scRt.offsetMax = new Vector2(-8f, 0f);

                _reportSubjectScoreTexts[i] = scoreObj.AddComponent<Text>();
                _reportSubjectScoreTexts[i].font = font;
                _reportSubjectScoreTexts[i].fontSize = 17;
                _reportSubjectScoreTexts[i].alignment = TextAnchor.MiddleCenter;
                _reportSubjectScoreTexts[i].color = new Color(0.85f, 0.90f, 0.98f);
                _reportSubjectScoreTexts[i].text = "Não Realizado (--/--)";

                // Coluna 3: Status (Pendente / Concluído ★★★) (Centralizado dentro do componente)
                GameObject statusObj = new GameObject("Status");
                statusObj.transform.SetParent(rowObj.transform, false);
                RectTransform stRt = statusObj.AddComponent<RectTransform>();
                stRt.anchorMin = new Vector2(0.70f, 0f);
                stRt.anchorMax = new Vector2(1f, 1f);
                stRt.offsetMin = new Vector2(8f, 0f);
                stRt.offsetMax = new Vector2(-16f, 0f);

                _reportSubjectStatusTexts[i] = statusObj.AddComponent<Text>();
                _reportSubjectStatusTexts[i].font = font;
                _reportSubjectStatusTexts[i].fontSize = 17;
                _reportSubjectStatusTexts[i].fontStyle = FontStyle.Bold;
                _reportSubjectStatusTexts[i].alignment = TextAnchor.MiddleCenter;
                _reportSubjectStatusTexts[i].color = new Color(0.6f, 0.65f, 0.75f);
                _reportSubjectStatusTexts[i].text = "Pendente";
            }

            // 4. Rodapé Informativo (Y: 16f da base)
            GameObject bottomHintObj = new GameObject("BottomHint");
            bottomHintObj.transform.SetParent(_reportCardPanel.transform, false);
            RectTransform bbrt = bottomHintObj.AddComponent<RectTransform>();
            bbrt.anchorMin = new Vector2(0f, 0f);
            bbrt.anchorMax = new Vector2(1f, 0f);
            bbrt.pivot = new Vector2(0.5f, 0f);
            bbrt.anchoredPosition = new Vector2(0f, 16f);
            bbrt.sizeDelta = new Vector2(-48f, 26f);

            Text bbTxt = bottomHintObj.AddComponent<Text>();
            bbTxt.font = font;
            bbTxt.fontSize = 15;
            bbTxt.fontStyle = FontStyle.Normal;
            bbTxt.alignment = TextAnchor.MiddleCenter;
            bbTxt.color = new Color(0.75f, 0.80f, 0.90f, 0.80f);
            bbTxt.text = "Solte a tecla [TAB] para retornar à visão normal";
        }

        private void CreateGraduationModal(Transform parent, Font font)
        {
            _graduationModal = new GameObject("GraduationModal");
            _graduationModal.transform.SetParent(parent, false);

            RectTransform rt = _graduationModal.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(860f, 540f);

            Image bg = _graduationModal.AddComponent<Image>();
            bg.sprite = GetRoundedRectSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.10f, 0.17f, 0.98f);

            Outline outline = _graduationModal.AddComponent<Outline>();
            outline.effectColor = new Color(1.0f, 0.85f, 0.20f, 0.70f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            // Estrelas Douradas
            GameObject starObj = new GameObject("Stars");
            starObj.transform.SetParent(_graduationModal.transform, false);
            RectTransform sRt = starObj.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 1f);
            sRt.anchorMax = new Vector2(0.5f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.anchoredPosition = new Vector2(0f, -32f);
            sRt.sizeDelta = new Vector2(400f, 48f);
            Text sTxt = starObj.AddComponent<Text>();
            sTxt.font = font;
            sTxt.fontSize = 40;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = new Color(1.0f, 0.85f, 0.20f);
            sTxt.text = "★ ★ ★ FORMATURA ESCOLAR ★ ★ ★";

            // Título de Parabéns
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_graduationModal.transform, false);
            RectTransform tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -90f);
            tRt.sizeDelta = new Vector2(0f, 50f);
            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = font;
            tTxt.fontSize = 30;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = Color.white;
            tTxt.text = "Ano Letivo Concluído com Sucesso!";

            // Certificado / Caixa de Honra
            GameObject cardObj = new GameObject("HonorCard");
            cardObj.transform.SetParent(_graduationModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, -20f);
            cRt.sizeDelta = new Vector2(740f, 170f);

            Image cBg = cardObj.AddComponent<Image>();
            cBg.sprite = GetRoundedRectSprite();
            cBg.type = Image.Type.Sliced;
            cBg.color = new Color(0.12f, 0.16f, 0.24f, 0.85f);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(cardObj.transform, false);
            RectTransform ctRt = cTxtObj.AddComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = new Vector2(28f, 16f);
            ctRt.offsetMax = new Vector2(-28f, -16f);
            Text cTxt = cTxtObj.AddComponent<Text>();
            cTxt.font = font;
            cTxt.fontSize = 20;
            cTxt.lineSpacing = 1.3f;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = new Color(0.92f, 0.95f, 1f);
            cTxt.text = "🎓 CERTIFICADO DE EXCELÊNCIA PEDAGÓGICA\n\n" +
                        "Parabéns pelo seu empenho e dedicação aos estudos!\n" +
                        "Você explorou as 4 salas temáticas e dominou todos os desafios de " +
                        "Matemática, Língua Portuguesa, História e Lógica!";

            // Botão de Continuar
            GameObject btnObj = new GameObject("CloseBtn");
            btnObj.transform.SetParent(_graduationModal.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 32f);
            bRt.sizeDelta = new Vector2(340f, 54f);

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = GetRoundedRectSprite();
            bBg.type = Image.Type.Sliced;
            bBg.color = new Color(1.0f, 0.75f, 0.15f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                _graduationModal.SetActive(false);
                if (_backdropScrim != null) _backdropScrim.SetActive(false);
                if (_crosshairObj != null) _crosshairObj.SetActive(true);
                SetPlayerControlsLocked(false);
            });

            GameObject bTxtObj = new GameObject("Text");
            bTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = bTxtObj.AddComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            Text bTxt = bTxtObj.AddComponent<Text>();
            bTxt.font = font;
            bTxt.fontSize = 20;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = new Color(0.1f, 0.1f, 0.1f);
            bTxt.text = "Continuar Explorando a Escola ✓";
        }

        // =========================================================================
        // 6. Utilitários Gráficos Procedurais (9-Slice Rounded Rects & Circles)
        // =========================================================================

        private static Sprite GetRoundedRectSprite(int size = 64, int radius = 16)
        {
            if (_roundedSprite != null) return _roundedSprite;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color transparent = new Color(1f, 1f, 1f, 0f);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    float dy = 0f;

                    if (x < radius) dx = radius - x;
                    else if (x >= size - radius) dx = x - (size - radius - 1);

                    if (y < radius) dy = radius - y;
                    else if (y >= size - radius) dy = y - (size - radius - 1);

                    if (dx > 0 && dy > 0)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist > radius)
                        {
                            tex.SetPixel(x, y, transparent);
                        }
                        else
                        {
                            float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            _roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return _roundedSprite;
        }

        private static Sprite GetCircleSprite(int size = 64)
        {
            if (_circleSprite != null) return _circleSprite;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = (size - 1) * 0.5f;
            float radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circleSprite;
        }
    }
}
