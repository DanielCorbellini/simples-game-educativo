# Especificação de Design: Game Feel do Jogador e Gamificação Escolar

**Data:** 2026-09-24  
**Status:** Aprovado  
**Escopo:** `FirstPersonController.cs`, `QuizUIManager.cs`, `SchoolProgressManager.cs`, `ClassroomInteractionZone.cs`

---

## 1. Visão Geral
Este documento especifica o aprimoramento de duas frentes essenciais da experiência do jogo educativo em Unity 6:
1. **Game Feel & Câmera do Jogador:** Transformar a movimentação em primeira pessoa de uma transição rígida para uma experiência fluida, imersiva e natural, com balanço de passos (*head bobbing*), aceleração/frenagem suave (*momentum*), FOV dinâmico ao correr e retícula de mira (*crosshair*) minimalista.
2. **Gamificação & Progresso Escolar:** Criar um sistema centralizado de acompanhamento pedagógico com um mini-indicador permanente no canto superior da tela, um painel completo de **Boletim Escolar / Diário de Bordo** acessível via tecla `TAB`, e uma celebração especial de **Formatura Escolar** ao concluir todas as 4 salas de aula.

---

## 2. Componentes e Arquitetura

```
                          ┌──────────────────────────────┐
                          │   ClassroomInteractionZone   │
                          └──────────────┬───────────────┘
                                         │ (Abre Quiz / Finaliza)
                                         ▼
┌──────────────────────────┐    ┌──────────────────────────────────┐
│  FirstPersonController   │◄───┤          QuizUIManager           │
│  - Head Bobbing          │    │  - Mini HUD de Progresso         │
│  - Dynamic FOV           │    │  - Boletim Escolar (Tecla TAB)   │
│  - Inércia / Suavização  │    │  - Modal de Formatura Escolar    │
│  - Retícula Crosshair    │    └────────────────┬─────────────────┘
└──────────────────────────┘                     │
                                                 ▼
                                ┌──────────────────────────────────┐
                                │      SchoolProgressManager       │
                                │  - Registro de 4 Disciplinas     │
                                │  - Pontuação, Estrelas e Status  │
                                └──────────────────────────────────┘
```

---

## 3. Detalhamento Técnico

### A. Game Feel & Câmera (`FirstPersonController.cs`)
1. **Head Bobbing (Passos Suaves):**
   * Altura base dos olhos: `1.65m`.
   * Quando no chão e em movimento (`moveInput.sqrMagnitude > 0.01f`):
     * Frequência: `walkBobFrequency = 10f`, `sprintBobFrequency = 14f`.
     * Amplitude: `walkBobAmount = 0.035m`, `sprintBobAmount = 0.065m`.
     * Efeito senoidal: `yOffset = sin(time * freq) * amount`.
   * Quando parado ou no ar:
     * Retorno suave à altura base através de `Mathf.Lerp` / amortecimento (`returnSpeed = 6f`).
2. **Inércia & Aceleração Suave:**
   * Aceleração: `12f` m/s².
   * Desaceleração / Frenagem: `15f` m/s².
   * Velocidade atual interpola em direção à velocidade alvo (`Vector3.MoveTowards`), eliminando paradas secas artificiais.
3. **FOV Dinâmico ao Correr (Speed Kick):**
   * FOV base: `baseFOV = 65f`.
   * FOV corrida: `sprintFOV = 72f`.
   * Interpolação a cada frame com `Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * 8f)`.
4. **Crosshair Minimalista:**
   * Inserido no Canvas procedural como um ponto sutil central de `4x4px` com cor branca translúcida (`Color(1f, 1f, 1f, 0.75f)`) e contorno suave.

---

### B. Gamificação & Progresso Escolar (`SchoolProgressManager.cs`)
1. **Estrutura de Dados da Disciplina:**
   ```csharp
   [System.Serializable]
   public class SubjectProgressData
   {
       public ClassroomSubject subject;
       public bool isCompleted;
       public int bestScore;
       public int totalQuestions;
       public int stars; // 1 a 3 estrelas
   }
   ```
2. **Métodos de Controle:**
   * `RecordScore(ClassroomSubject subject, int score, int total)`: Atualiza a pontuação máxima, salva as estrelas e marca a sala como concluída se score/total >= 50%.
   * `GetProgress(ClassroomSubject subject)`: Retorna os dados da disciplina.
   * `GetCompletedRoomsCount()`: Quantidade de salas concluídas (0 a 4).
   * `IsGraduated()`: Retorna verdadeiro se todas as 4 salas foram concluídas.

---

### C. Interface do Boletim Escolar (`QuizUIManager.cs`)
1. **Mini-HUD Permanente (Superior Esquerdo):**
   * Pílula compacta translúcida:
     * Ícone de chapéu de formatura / livro: `🎓 Salas: X/4  [TAB]`
     * 4 marcadores redondos em linha com as cores das disciplinas (Azul, Verde, Âmbar, Ciano).
     * Salas pendentes ficam com 30% de opacidade; salas concluídas acendem em 100% de brilho com estrela `★`.
2. **Boletim Escolar Completo (Painel TAB):**
   * Ao pressionar a tecla `TAB`, abre o modal estilizado em *dark glassmorphism*:
     * Cabeçalho: **"Boletim Escolar do Aluno"** + Barra de Conclusão Geral do Ano Letivo.
     * Lista com os 4 cartões das disciplinas:
       * **Matemática** (Sala 1) • Nota / Acertos • Status (`Pendente` ou `Concluído ★★★`).
       * **Língua Portuguesa** (Sala 2) • Nota / Acertos • Status.
       * **História** (Sala 3) • Nota / Acertos • Status.
       * **Lógica & Computação** (Sala 4) • Nota / Acertos • Status.
     * Rodapé: Instrução clara `[TAB] Fechar Boletim`.
3. **Modal de Formatura (Ano Letivo Concluído):**
   * Disparado quando a 4ª sala é concluída.
   * Exibe celebração comemorativa com troféu dourado, mensagem pedagógica de encerramento e certificado de conclusão escolar.

---

## 4. Plano de Validação
* Compilação com Roslyn C# (`csc.dll`) com `Exit code: 0`.
* Teste da movimentação com aceleração e balanço de passos.
* Teste de transição suave do FOV ao segurar Shift.
* Teste de abertura e fechamento do Boletim com `TAB`.
* Teste de persistência e atualização em tempo real ao completar cada questionário.
