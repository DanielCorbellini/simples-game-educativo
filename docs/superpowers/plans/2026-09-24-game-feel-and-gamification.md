# Game Feel e Gamificação Escolar: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar melhorias de Game Feel em primeira pessoa (head bobbing, inércia de velocidade, FOV dinâmico e crosshair) e um sistema completo de gamificação escolar (rastreamento de progresso nas 4 matérias, mini-HUD permanente, Boletim Escolar via tecla TAB e celebração de formatura).

**Architecture:** 
- `SchoolProgressManager.cs` gerencia o estado e pontuação máxima das 4 salas temáticas (Matemática, Português, História, Lógica) e emite eventos de progresso.
- `FirstPersonController.cs` ganha balanço senoidal de câmera ao andar/correr, interpolação de aceleração com inércia física e ampliação suave de campo de visão (FOV kick) no sprint.
- `QuizUIManager.cs` incorpora a retícula central de mira (*crosshair*), o mini-HUD discreto no canto superior esquerdo, o modal de Boletim Escolar com a tecla `TAB` e o anúncio de Formatura Escolar ao concluir as 4 salas.

**Tech Stack:** Unity 6 (6000.3.10f1), Universal Render Pipeline (URP), New Input System (`UnityEngine.InputSystem`), C# 9.0 (.NET Standard 2.1).

## Global Constraints
- Sem comandos ou commits de git (restrição explícita do ambiente).
- Utilizar exclusivamente o New Input System (`UnityEngine.InputSystem`).
- Manter compatibilidade com sprites procedurais 9-slice para cantos arredondados suaves.
- Não utilizar bibliotecas externas inexistentes no projeto.

---

### Task 1: Gerenciador de Progresso Escolar (`SchoolProgressManager.cs`)

**Files:**
- Create: `Assets/Scripts/Quiz/SchoolProgressManager.cs`
- Modify: `Assets/Scripts/Quiz/ClassroomInteractionZone.cs`

**Interfaces:**
- Produces: 
  - `public static SchoolProgressManager Instance { get; }`
  - `public void RecordCompletion(ClassroomSubject subject, int score, int total)`
  - `public SubjectProgressData GetProgress(ClassroomSubject subject)`
  - `public int GetCompletedCount()`
  - `public bool IsAllCompleted()`
  - `public event System.Action OnProgressChanged`

- [ ] **Step 1: Criar o script `Assets/Scripts/Quiz/SchoolProgressManager.cs`**
Implementar o Singleton com dicionário/lista serializada para as 4 matérias, cálculo de estrelas (1 a 3) e disparador de eventos `OnProgressChanged`.

- [ ] **Step 2: Conectar o `ClassroomInteractionZone.cs` ao `SchoolProgressManager`**
Garantir que a zona de interação consulte o progresso persistido para exibir a iluminação temática dourada e o prompt `(Concluído ★)` se a sala já tiver sido finalizada.

- [ ] **Step 3: Testar compilação**
Executar `python scratch/test_compile.py` e verificar `Exit code: 0`.

---

### Task 2: Game Feel & Movimentação Fluida no `FirstPersonController.cs`

**Files:**
- Modify: `Assets/Scripts/Player/FirstPersonController.cs`

**Interfaces:**
- Consumes: `Camera playerCamera`, `CharacterController _characterController`
- Produces:
  - `HandleHeadBobbing(Vector2 moveInput, bool isGrounded, bool isSprinting)`
  - `HandleDynamicFOV(bool isSprinting, bool isMoving)`
  - Movimentação horizontal com inércia (`Vector3.MoveTowards`)

- [ ] **Step 1: Adicionar variáveis de configuração de Game Feel no topo de `FirstPersonController.cs`**
  - `walkBobFrequency = 10f`, `walkBobAmount = 0.035f`
  - `sprintBobFrequency = 14f`, `sprintBobAmount = 0.065f`
  - `acceleration = 14f`, `deceleration = 18f`
  - `baseFOV = 65f`, `sprintFOV = 72f`, `fovSpeed = 8f`
  - `_defaultCameraPosY = 1.65f`, `_bobTimer = 0f`

- [ ] **Step 2: Implementar interpolação de aceleração na movimentação horizontal**
Substituir a atribuição direta de velocidade por `_currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, rate * Time.deltaTime)`.

- [ ] **Step 3: Implementar o método `HandleHeadBobbing`**
Calcular o deslocamento senoidal vertical `Mathf.Sin(_bobTimer) * bobAmount` e aplicar suavemente à posição local Y da câmera, amortecendo de volta a `1.65m` quando parado.

- [ ] **Step 4: Implementar o método `HandleDynamicFOV`**
Interpolar `playerCamera.fieldOfView` entre `baseFOV` e `sprintFOV` quando `isSprinting && moveInput.sqrMagnitude > 0.01f`.

- [ ] **Step 5: Testar compilação**
Executar `python scratch/test_compile.py` e verificar `Exit code: 0`.

---

### Task 3: Retícula Crosshair, Mini-HUD e Boletim Escolar (TAB) no `QuizUIManager.cs`

**Files:**
- Modify: `Assets/Scripts/Quiz/QuizUIManager.cs`

**Interfaces:**
- Consumes: `SchoolProgressManager.Instance`, `Keyboard.current.tabKey`
- Produces:
  - `CreateCrosshairHUD(Transform parent)`
  - `CreateSchoolProgressHUD(Transform parent, Font font)`
  - `CreateReportCardPanel(Transform parent, Font font)`
  - `CreateGraduationModal(Transform parent, Font font)`
  - `ToggleReportCard(bool open)`
  - `UpdateProgressHUDVisuals()`

- [ ] **Step 1: Criar a Retícula Central Minimalista (Crosshair)**
Adicionar um ponto central sutil de 4x4 pixels translúcido no Canvas overlay com contorno suave, visível durante a exploração e ocultado automaticamente durante o quiz ou boletim.

- [ ] **Step 2: Criar o Mini-HUD Permanente no Canto Superior Esquerdo**
Adicionar uma pílula translúcida `🎓 Salas: X/4  [TAB]` com 4 indicadores circulares coloridos (Azul, Verde, Âmbar, Ciano), que acendem com estrela dourada quando cada matéria for concluída.

- [ ] **Step 3: Criar o Painel Completo de Boletim Escolar (Tecla TAB)**
Construir o modal com o resumo das 4 salas, notas/acertos obtidos, estrelas conquistadas e barra de progresso geral do ano letivo.

- [ ] **Step 4: Capturar a tecla `TAB` no `Update()`**
Abrir/fechar o Boletim Escolar ao pressionar `TAB` enquanto o questionário principal não estiver ativo, liberando o cursor temporariamente se o boletim estiver visível.

- [ ] **Step 5: Registrar acertos no `SchoolProgressManager` ao fechar o questionário**
Em `ShowResults()`, chamar `SchoolProgressManager.Instance.RecordCompletion(_currentSubject, _score, total)`. Se todas as 4 salas forem concluídas, disparar a celebração de Formatura Escolar.

- [ ] **Step 6: Testar compilação**
Executar `python scratch/test_compile.py` e verificar `Exit code: 0`.

---

### Task 4: Validação Integrada e Documentação

**Files:**
- Modify: `walkthrough.md`

- [ ] **Step 1: Compilar todos os scripts do projeto**
Executar `python scratch/test_compile.py` para garantir 0 erros e 0 warnings.

- [ ] **Step 2: Atualizar o Walkthrough com instruções de teste**
Documentar como testar o head bobbing, o FOV dinâmico ao correr, o crosshair, a tecla TAB para abrir o Boletim e o fluxo de formatura.
