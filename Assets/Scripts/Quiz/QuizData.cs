using System.Collections.Generic;
using UnityEngine;

namespace EducationalGame.Quiz
{
    public enum ClassroomSubject
    {
        Matematica,
        Portugues,
        Historia,
        Logica
    }

    [System.Serializable]
    public class QuizQuestion
    {
        public string questionText;
        public string[] options;
        public int correctOptionIndex;
        public string explanation;

        public QuizQuestion(string question, string[] opts, int correctIndex, string expl)
        {
            questionText = question;
            options = opts;
            correctOptionIndex = correctIndex;
            explanation = expl;
        }
    }

    /// <summary>
    /// Banco de dados didático contendo questões curriculares para as 4 salas temáticas.
    /// </summary>
    public static class QuizDatabase
    {
        public static string GetSubjectDisplayName(ClassroomSubject subject)
        {
            switch (subject)
            {
                case ClassroomSubject.Matematica: return "Matemática";
                case ClassroomSubject.Portugues: return "Língua Portuguesa";
                case ClassroomSubject.Historia: return "História Geral e do Brasil";
                case ClassroomSubject.Logica: return "Lógica e Computação";
                default: return "Desafio Escolar";
            }
        }

        public static Color GetSubjectThemeColor(ClassroomSubject subject)
        {
            switch (subject)
            {
                case ClassroomSubject.Matematica: return new Color(0.20f, 0.50f, 1.00f); // Azul vibrante
                case ClassroomSubject.Portugues: return new Color(0.18f, 0.75f, 0.28f);  // Verde esmeralda
                case ClassroomSubject.Historia: return new Color(1.00f, 0.65f, 0.05f);   // Âmbar / Dourado
                case ClassroomSubject.Logica: return new Color(0.00f, 0.78f, 0.88f);     // Ciano cibernético
                default: return Color.white;
            }
        }

        public static List<QuizQuestion> GetQuestions(ClassroomSubject subject)
        {
            switch (subject)
            {
                case ClassroomSubject.Matematica:
                    return new List<QuizQuestion>
                    {
                        new QuizQuestion(
                            "Uma pizza foi dividida em 8 fatias iguais. Se você comeu 3 fatias, qual fração representa o que sobrou?",
                            new string[] { "3/8", "5/8", "1/2", "8/5" },
                            1,
                            "8/8 (total) menos 3/8 (consumido) resulta em 5/8 restantes da pizza."
                        ),
                        new QuizQuestion(
                            "Qual é a área de uma sala de aula retangular que mede 6 metros de largura por 8 metros de comprimento?",
                            new string[] { "28 m²", "48 m²", "14 m²", "54 m²" },
                            1,
                            "A área do retângulo é calculada multiplicando a largura pelo comprimento: 6 × 8 = 48 m²."
                        ),
                        new QuizQuestion(
                            "Um livro didático custava R$ 100,00 e recebeu um desconto de 25%. Qual é o novo preço?",
                            new string[] { "R$ 85,00", "R$ 70,00", "R$ 75,00", "R$ 80,00" },
                            2,
                            "25% de 100 é 25. Subtraindo o desconto: 100 - 25 = R$ 75,00."
                        ),
                        new QuizQuestion(
                            "Qual é o valor da raiz quadrada exata de 144?",
                            new string[] { "11", "12", "14", "16" },
                            1,
                            "12 multiplicado por 12 é exatamente igual a 144."
                        )
                    };

                case ClassroomSubject.Portugues:
                    return new List<QuizQuestion>
                    {
                        new QuizQuestion(
                            "Em qual das alternativas a concordância verbal está empregada corretamente?",
                            new string[] {
                                "Fazem muitos anos que não venho à escola.",
                                "Havia muitos alunos estudando na biblioteca.",
                                "Houveram muitas dúvidas na aula de hoje.",
                                "Devem haver outras soluções para o problema."
                            },
                            1,
                            "O verbo 'haver' no sentido de existir ou tempo transcorrido é impessoal e não flexiona no plural."
                        ),
                        new QuizQuestion(
                            "Na frase 'O tempo voou durante as férias', que figura de linguagem foi utilizada?",
                            new string[] { "Metáfora", "Pleonasmo", "Ironia", "Eufemismo" },
                            0,
                            "A metáfora atribui características de voo ao tempo por associação expressiva e poética."
                        ),
                        new QuizQuestion(
                            "Qual das palavras abaixo é classificada como PROPAROXÍTONA?",
                            new string[] { "Café", "Lâmpada", "Papel", "Janela" },
                            1,
                            "A palavra 'Lâm-pa-da' tem a antepenúltima sílaba tônica, e na língua portuguesa toda proparoxítona é acentuada."
                        ),
                        new QuizQuestion(
                            "Qual é o antônimo exato da palavra 'EFÊMERO'?",
                            new string[] { "Passageiro", "Curto", "Duradouro", "Frágil" },
                            2,
                            "Efêmero significa algo breve e passageiro; seu oposto exato é duradouro ou permanente."
                        )
                    };

                case ClassroomSubject.Historia:
                    return new List<QuizQuestion>
                    {
                        new QuizQuestion(
                            "Em qual data e ano foi proclamada a República no Brasil pelo Marechal Deodoro da Fonseca?",
                            new string[] { "7 de Setembro de 1822", "15 de Novembro de 1889", "13 de Maio de 1888", "22 de Abril de 1500" },
                            1,
                            "A Proclamação da República ocorreu no dia 15 de novembro de 1889 na Praça da Aclamação (atual Praça da República no Rio de Janeiro)."
                        ),
                        new QuizQuestion(
                            "Qual grande civilização da antiguidade construiu o complexo das Pirâmides de Gizé e a Esfinge?",
                            new string[] { "Civilização Romana", "Civilização Grega", "Civilização Egípcia", "Mesopotâmia" },
                            2,
                            "As grandes pirâmides foram erguidas no Antigo Egito pelos faraós Quéops, Quéfren e Miquerinos."
                        ),
                        new QuizQuestion(
                            "Qual documento assinado em 13 de Maio de 1888 aboliu formalmente a escravidão no território brasileiro?",
                            new string[] { "Lei Áurea", "Lei dos Sexagenários", "Lei do Ventre Livre", "Tratado de Tordesilhas" },
                            0,
                            "A Lei Áurea (Lei Imperial n.º 3.353) foi assinada pela Princesa Isabel, extinguindo a escravidão no Brasil."
                        ),
                        new QuizQuestion(
                            "Em que ano terminou a Segunda Guerra Mundial e foi fundada a Organização das Nações Unidas (ONU)?",
                            new string[] { "1918", "1939", "1945", "1960" },
                            2,
                            "A Segunda Guerra Mundial encerrou-se em 1945, mesmo ano da assinatura da Carta das Nações Unidas em São Francisco."
                        )
                    };

                case ClassroomSubject.Logica:
                    return new List<QuizQuestion>
                    {
                        new QuizQuestion(
                            "Em uma estrutura condicional 'SE (nota >= 7) ENTÃO Aprovado SENÃO Reprovado', o que acontece se nota for 5?",
                            new string[] { "Aprovado", "Reprovado", "Erro de sintaxe", "O programa para" },
                            1,
                            "Como a condição (5 >= 7) é FALSA, o fluxo do programa desvia automaticamente para o bloco alternativo (SENÃO)."
                        ),
                        new QuizQuestion(
                            "Descubra o padrão da sequência lógica: 2, 4, 8, 16, ___. Qual é o próximo termo?",
                            new string[] { "24", "32", "20", "64" },
                            1,
                            "Cada termo da progressão geométrica é o dobro do número anterior: 16 × 2 = 32."
                        ),
                        new QuizQuestion(
                            "Qual operador lógico resulta em VERDADEIRO somente quando AMBAS as condições forem simultaneamente verdadeiras?",
                            new string[] { "OU (OR)", "E (AND)", "NÃO (NOT)", "XOR" },
                            1,
                            "Na tabela-verdade do operador 'E' (AND), o resultado só é verdadeiro se todas as entradas forem verdadeiras."
                        ),
                        new QuizQuestion(
                            "Qual estrutura de repetição é mais recomendada quando já sabemos previamente quantas vezes o laço deve ser executado?",
                            new string[] { "Laço PARA (for)", "Laço ENQUANTO (while)", "Condicional SE (if)", "Comutador ESCOLHA (switch)" },
                            0,
                            "O laço 'for' foi concebido especificamente para contagem delimitada com início, limite e passo de incremento definidos."
                        )
                    };

                default:
                    return new List<QuizQuestion>();
            }
        }
    }
}
