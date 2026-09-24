using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using EducationalGame.Quiz;

namespace EducationalGame.Editor
{
    /// <summary>
    /// Script de Editor para geração procedural do layout físico escolar 3D completo.
    /// Cria uma escola integrada contendo:
    /// - 1 Hall de Entrada Principal & Recepção na ponta Sul com balcão, vitrines, avisos e vista externa para rua e ônibus escolar.
    /// - 1 Corredor Central amplo de circulação com armários, quadros de avisos, alto-falantes e extintores.
    /// - 4 Salas de Aula temáticas e ricas em cenografia:
    ///     * Sala 1 (Matemática): Quadro auxiliar de fórmulas, réguas, folhas de cálculo e projetor voltado para a lousa.
    ///     * Sala 2 (Português): Estante de biblioteca no fundo com livros de literatura e dicionários.
    ///     * Sala 3 (História): Vitrine de exposição de artefatos históricos e acervo de patrimônio.
    ///     * Sala 4 (Lógica & Tecnologia): Estação auxiliar de programação/maker com PC e periféricos.
    ///   Todas equipadas com 80 notebooks e cadernos perfeitamente assentados sobre as carteiras (sem flutuar nem sobrepor),
    ///   projetores suspensos no teto voltados para o quadro e caixas de som direcionadas para os alunos.
    /// - 1 Grande Biblioteca & Sala de Estudos na ponta Norte com estantes de livros, mesas coletivas, mesas de parede (table3) e balcão encostado à parede.
    /// Utiliza compensação precisa de pivô para todos os módulos de paredes (wall4 1, wall4 e wall001),
    /// garantindo continuidade perfeita sem furos em toda a edificação.
    /// </summary>
    public static class SchoolMapGenerator
    {
        // -------------------------------------------------------------------------
        // Constantes de Grade e Dimensões (Calibradas exatamente com as malhas dos assets)
        // -------------------------------------------------------------------------
        private const float TILE_X = 4.42188f;             // Largura exata de cada módulo de piso (4.42188m)
        private const float TILE_Z = 5.33584f;             // Comprimento exato de cada módulo de piso/parede (5.33584m)
        private const float WALL_HEIGHT = 4.42188f;         // Altura padrão das paredes (4.42188m)

        // Offsets de centro geométrico das malhas (para eliminar furos decorrentes de pivôs descentralizados nos FBXs)
        private static readonly Vector3 PIVOT_WALL_SOLID = new Vector3(0f, 0f, 0.39313f);     // wall4 (1).fbx
        private static readonly Vector3 PIVOT_WALL_DOORWAY = new Vector3(0f, 0f, -0.10978f);  // wall4.fbx
        private static readonly Vector3 PIVOT_WALL_WINDOW = new Vector3(0f, 0f, -0.05176f);   // wall001.fbx

        // Dimensões do Corredor Central: 2 módulos em X (~8.84m) x 6 módulos em Z (~32.02m)
        private const int CORRIDOR_TILES_X = 2;
        private const int CORRIDOR_TILES_Z = 6;

        // Dimensões de Cada Sala de Aula: 3 módulos em X (~13.26m) x 3 módulos em Z (~16.01m)
        private const int ROOM_TILES_X = 3;
        private const int ROOM_TILES_Z = 3;

        // -------------------------------------------------------------------------
        // Caminhos dos Prefabs e Materiais em Assets/school
        // -------------------------------------------------------------------------
        private const string PATH_FLOOR = "Assets/school/Prefabs/road/floor.prefab";
        private const string PATH_WALL_SOLID = "Assets/school/Prefabs/props/wall4 (1).prefab";
        private const string PATH_WALL_DOORWAY = "Assets/school/Prefabs/props/wall4.prefab";
        private const string PATH_WALL_WINDOW = "Assets/school/Prefabs/props/wall001.prefab";
        private const string PATH_DOOR = "Assets/school/Prefabs/props/a door1.prefab";
        private const string PATH_BOARD = "Assets/school/Prefabs/props/board.prefab";
        private const string PATH_NOTICE_BOARD = "Assets/school/Prefabs/props/board2_1.prefab";
        private const string PATH_TEACHER_DESK = "Assets/school/Prefabs/props/table3.prefab";
        private const string PATH_COMPUTER = "Assets/school/Prefabs/props/computer.prefab";
        private const string PATH_KEYBOARD = "Assets/school/Prefabs/props/computer2.prefab";
        private const string PATH_MOUSE = "Assets/school/Prefabs/props/computer3.prefab";
        private const string PATH_TEACHER_CHAIR = "Assets/school/Prefabs/props/chair.prefab";
        private const string PATH_STUDENT_CHAIR = "Assets/school/Prefabs/props/chair1.prefab";
        private const string PATH_LAPTOP = "Assets/school/Prefabs/props/a laptop.prefab";
        private const string PATH_LOCKER = "Assets/school/Prefabs/props/locker_1.prefab";

        // Prefabs para Biblioteca e Recepção
        private const string PATH_BOOKCASE_BOOKS = "Assets/school/Prefabs/props/rack1.prefab";
        private const string PATH_SHOWCASE = "Assets/school/Prefabs/props/showcase.prefab";
        private const string PATH_TABLE_STUDY = "Assets/school/Prefabs/props/table2.prefab";
        private const string PATH_FIRE_EXTINGUISHER = "Assets/school/Prefabs/props/fire.prefab";
        private const string PATH_BUS = "Assets/school/Prefabs/props/bus.prefab";
        private const string PATH_ROAD = "Assets/school/Prefabs/road/doroga.prefab";

        // Prefabs para Cenografia Rica e Detalhamento das Salas de Aula
        private const string PATH_PROJECTOR = "Assets/school/Prefabs/props/projector.prefab";
        private const string PATH_SPEAKER = "Assets/school/Prefabs/props/speaker.prefab";
        private const string PATH_SHEET = "Assets/school/Prefabs/props/sheet.prefab";
        private const string PATH_BOOK = "Assets/school/Prefabs/props/book1.prefab";
        private const string PATH_BOOK_ALT = "Assets/school/Prefabs/props/book2.prefab";

        private const string PATH_MATERIAL_MAIN = "Assets/school/material/Materials/1.mat";
        private const string PATH_MATERIAL_FLOOR = "Assets/school/material/Materials/floor_color.mat";
        private const string PATH_TEXTURE_PALETTE = "Assets/school/material/1.png";

        // -------------------------------------------------------------------------
        // Estrutura de Configuração de Cada Sala de Aula
        // -------------------------------------------------------------------------
        private struct RoomDefinition
        {
            public string Name;
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;
            public bool IsLeftOfCorridor;
            public float DoorZ;          // Posição Z do módulo da porta conectando ao corredor
            public float[] SolidWallZs;  // Posições Z dos módulos de parede sólida no corredor
            public float BoardZ;         // Posição Z da parede frontal com o quadro
            public float BoardRotY;      // Rotação Y do quadro
            public ClassroomSubject Subject; // Disciplina do minigame/questionário interativo
        }

        // -------------------------------------------------------------------------
        // Menus do Unity Editor
        // -------------------------------------------------------------------------
        [MenuItem("Tools/Generate School Map")]
        public static void GenerateSchoolMap()
        {
            // 1. Garantir que os materiais do pacote estejam convertidos para URP
            FixSchoolMaterialsInternal(silent: true);

            // 2. Carregar os prefabs necessários
            var prefabs = LoadPrefabs();
            if (prefabs == null)
            {
                EditorUtility.DisplayDialog("Erro", "Não foi possível carregar todos os prefabs de 'Assets/school'. Verifique o Console.", "OK");
                return;
            }

            // 3. Limpar cenário anterior se existir
            ClearExistingEnvironment();

            // 4. Criar nó raiz da hierarquia
            GameObject root = new GameObject("-- ENVIRONMENT --");
            Undo.RegisterCreatedObjectUndo(root, "Generate School Map");

            // 5. Gerar o Corredor Central
            GenerateCorridor(root.transform, prefabs);

            // 6. Definir e Gerar as 4 Salas de Aula Expandidas e Cenografadas
            float corridorHalfX = TILE_X;                       // 4.42188f
            float roomWidth = TILE_X * ROOM_TILES_X;            // 13.26564f
            float roomLength = TILE_Z * ROOM_TILES_Z;           // 16.00752f

            var rooms = new List<RoomDefinition>
            {
                // Sala 1 (Matemática) - Lado Esquerdo, metade Sul
                new RoomDefinition
                {
                    Name = "Sala 1 - Matematica",
                    MinX = -corridorHalfX - roomWidth,
                    MaxX = -corridorHalfX,
                    MinZ = -roomLength,
                    MaxZ = 0f,
                    IsLeftOfCorridor = true,
                    DoorZ = -TILE_Z * 0.5f,
                    SolidWallZs = new float[] { -TILE_Z * 1.5f, -TILE_Z * 2.5f },
                    BoardZ = -roomLength,
                    BoardRotY = 0f,
                    Subject = ClassroomSubject.Matematica
                },
                // Sala 2 (Português) - Lado Direito, metade Sul
                new RoomDefinition
                {
                    Name = "Sala 2 - Portugues",
                    MinX = corridorHalfX,
                    MaxX = corridorHalfX + roomWidth,
                    MinZ = -roomLength,
                    MaxZ = 0f,
                    IsLeftOfCorridor = false,
                    DoorZ = -TILE_Z * 0.5f,
                    SolidWallZs = new float[] { -TILE_Z * 1.5f, -TILE_Z * 2.5f },
                    BoardZ = -roomLength,
                    BoardRotY = 0f,
                    Subject = ClassroomSubject.Portugues
                },
                // Sala 3 (História) - Lado Esquerdo, metade Norte
                new RoomDefinition
                {
                    Name = "Sala 3 - Historia",
                    MinX = -corridorHalfX - roomWidth,
                    MaxX = -corridorHalfX,
                    MinZ = 0f,
                    MaxZ = roomLength,
                    IsLeftOfCorridor = true,
                    DoorZ = TILE_Z * 0.5f,
                    SolidWallZs = new float[] { TILE_Z * 1.5f, TILE_Z * 2.5f },
                    BoardZ = roomLength,
                    BoardRotY = 180f,
                    Subject = ClassroomSubject.Historia
                },
                // Sala 4 (Lógica) - Lado Direito, metade Norte
                new RoomDefinition
                {
                    Name = "Sala 4 - Logica",
                    MinX = corridorHalfX,
                    MaxX = corridorHalfX + roomWidth,
                    MinZ = 0f,
                    MaxZ = roomLength,
                    IsLeftOfCorridor = false,
                    DoorZ = TILE_Z * 0.5f,
                    SolidWallZs = new float[] { TILE_Z * 1.5f, TILE_Z * 2.5f },
                    BoardZ = roomLength,
                    BoardRotY = 180f,
                    Subject = ClassroomSubject.Logica
                }
            };

            foreach (var roomDef in rooms)
            {
                GenerateRoom(root.transform, roomDef, prefabs);
            }

            // 7. Paredes divisórias centrais sólidas entre salas adjacentes (Z = 0)
            GenerateDividingWalls(root.transform, prefabs, corridorHalfX);

            // 8. Gerar o Hall de Entrada Principal & Recepção (Ala Sul)
            GenerateEntranceHall(root.transform, prefabs);

            // 9. Gerar a Grande Biblioteca & Sala de Estudos (Ala Norte)
            GenerateLibrary(root.transform, prefabs);

            // 10. Garantir Colliders e Materiais em toda a hierarquia gerada
            EnsureColliders(root);
            EnsureMaterials(root, prefabs.MainMaterial);

            // 11. Garantir o Gerenciador de UI do Quiz na cena
            EnsureQuizUIManager();

            // 12. Marcar cena como modificada para salvar
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[SchoolMapGenerator] Cenário atualizado com sucesso!</color> Ajustes escolares e plataformas de questionário das 4 salas aplicadas.");
            EditorUtility.DisplayDialog("School Map Generator", "Escola gerada com sucesso!\n\n- 4 Salas de Aula equipadas com plataformas de minigame/quiz (acionamento com [E])\n- Banco de perguntas didáticas para Matemática, Português, História e Lógica\n- Mobiliário das salas e da biblioteca perfeitamente alinhado\n- Hall de Entrada e circulação integrados", "OK");
        }

        [MenuItem("Tools/Clear School Map")]
        public static void ClearSchoolMap()
        {
            if (ClearExistingEnvironment())
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[SchoolMapGenerator] Cenário '-- ENVIRONMENT --' removido com sucesso.");
            }
            else
            {
                Debug.LogWarning("[SchoolMapGenerator] Nenhum objeto '-- ENVIRONMENT --' encontrado na cena ativa.");
            }
        }

        [MenuItem("Tools/Fix School Materials (URP)")]
        public static void FixSchoolMaterialsMenu()
        {
            FixSchoolMaterialsInternal(silent: false);
        }

        // -------------------------------------------------------------------------
        // Conversão e Correção Automática de Materiais para URP
        // -------------------------------------------------------------------------
        private static void FixSchoolMaterialsInternal(bool silent)
        {
            Shader targetShader = Shader.Find("Universal Render Pipeline/Lit");
            if (targetShader == null)
            {
                targetShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            if (targetShader == null)
            {
                targetShader = Shader.Find("Standard");
            }

            if (targetShader == null)
            {
                Debug.LogWarning("[SchoolMapGenerator] Nenhum shader URP/Standard compatível encontrado.");
                return;
            }

            Texture2D paletteTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PATH_TEXTURE_PALETTE);

            // 1. Atualizar 1.mat
            Material mainMat = AssetDatabase.LoadAssetAtPath<Material>(PATH_MATERIAL_MAIN);
            if (mainMat != null)
            {
                mainMat.shader = targetShader;
                if (paletteTex != null)
                {
                    mainMat.SetTexture("_BaseMap", paletteTex);
                    mainMat.SetTexture("_MainTex", paletteTex);
                }
                mainMat.SetColor("_BaseColor", Color.white);
                mainMat.SetColor("_Color", Color.white);
                EditorUtility.SetDirty(mainMat);
            }

            // 2. Atualizar floor_color.mat
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(PATH_MATERIAL_FLOOR);
            if (floorMat != null)
            {
                floorMat.shader = targetShader;
                Color floorColor = new Color(0.41f, 0.41f, 0.41f, 1f);
                floorMat.SetColor("_BaseColor", floorColor);
                floorMat.SetColor("_Color", floorColor);
                EditorUtility.SetDirty(floorMat);
            }

            AssetDatabase.SaveAssets();

            // 3. Se o cenário já estiver na cena, atualizar renderers com slots nulos/rosa
            GameObject env = GameObject.Find("-- ENVIRONMENT --");
            if (env != null && mainMat != null)
            {
                EnsureMaterials(env, mainMat);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (!silent)
            {
                Debug.Log("[SchoolMapGenerator] Materiais do pacote escolar corrigidos com sucesso para URP.");
                EditorUtility.DisplayDialog("Materiais URP", "Materiais da escola atualizados com sucesso para o shader Universal Render Pipeline (URP)!", "OK");
            }
        }

        // -------------------------------------------------------------------------
        // Carregamento Seguro de Assets
        // -------------------------------------------------------------------------
        private class PrefabBundle
        {
            public GameObject Floor;
            public GameObject WallSolid;
            public GameObject WallDoorway;
            public GameObject WallWindow;
            public GameObject Door;
            public GameObject Board;
            public GameObject NoticeBoard;
            public GameObject TeacherDesk;
            public GameObject Computer;
            public GameObject Keyboard;
            public GameObject Mouse;
            public GameObject TeacherChair;
            public GameObject StudentChair;
            public GameObject Laptop;
            public GameObject Locker;
            public GameObject BookcaseWithBooks;
            public GameObject Showcase;
            public GameObject TableStudy;
            public GameObject FireExtinguisher;
            public GameObject Bus;
            public GameObject Road;
            public GameObject Projector;
            public GameObject Speaker;
            public GameObject SheetPaper;
            public GameObject Book;
            public GameObject BookAlt;
            public Material MainMaterial;
        }

        private static PrefabBundle LoadPrefabs()
        {
            var bundle = new PrefabBundle
            {
                Floor = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_FLOOR),
                WallSolid = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_WALL_SOLID),
                WallDoorway = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_WALL_DOORWAY),
                WallWindow = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_WALL_WINDOW),
                Door = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_DOOR),
                Board = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BOARD),
                NoticeBoard = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_NOTICE_BOARD),
                TeacherDesk = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_TEACHER_DESK),
                Computer = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_COMPUTER),
                Keyboard = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_KEYBOARD),
                Mouse = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_MOUSE),
                TeacherChair = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_TEACHER_CHAIR),
                StudentChair = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_STUDENT_CHAIR),
                Laptop = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_LAPTOP),
                Locker = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_LOCKER),
                BookcaseWithBooks = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BOOKCASE_BOOKS),
                Showcase = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SHOWCASE),
                TableStudy = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_TABLE_STUDY),
                FireExtinguisher = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_FIRE_EXTINGUISHER),
                Bus = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BUS),
                Road = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_ROAD),
                Projector = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_PROJECTOR),
                Speaker = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SPEAKER),
                SheetPaper = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SHEET),
                Book = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BOOK),
                BookAlt = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BOOK_ALT),
                MainMaterial = AssetDatabase.LoadAssetAtPath<Material>(PATH_MATERIAL_MAIN)
            };

            bool missing = false;
            if (bundle.Floor == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_FLOOR}"); missing = true; }
            if (bundle.WallSolid == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_WALL_SOLID}"); missing = true; }
            if (bundle.WallDoorway == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_WALL_DOORWAY}"); missing = true; }
            if (bundle.WallWindow == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_WALL_WINDOW}"); missing = true; }
            if (bundle.Door == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_DOOR}"); missing = true; }
            if (bundle.Board == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_BOARD}"); missing = true; }
            if (bundle.NoticeBoard == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_NOTICE_BOARD}"); missing = true; }
            if (bundle.TeacherDesk == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_TEACHER_DESK}"); missing = true; }
            if (bundle.Computer == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_COMPUTER}"); missing = true; }
            if (bundle.Keyboard == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_KEYBOARD}"); missing = true; }
            if (bundle.TeacherChair == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_TEACHER_CHAIR}"); missing = true; }
            if (bundle.StudentChair == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_STUDENT_CHAIR}"); missing = true; }
            if (bundle.Laptop == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_LAPTOP}"); missing = true; }
            if (bundle.Locker == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_LOCKER}"); missing = true; }
            if (bundle.BookcaseWithBooks == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_BOOKCASE_BOOKS}"); missing = true; }
            if (bundle.Showcase == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_SHOWCASE}"); missing = true; }
            if (bundle.TableStudy == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_TABLE_STUDY}"); missing = true; }
            if (bundle.FireExtinguisher == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_FIRE_EXTINGUISHER}"); missing = true; }
            if (bundle.Bus == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_BUS}"); missing = true; }
            if (bundle.Road == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_ROAD}"); missing = true; }
            if (bundle.Projector == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_PROJECTOR}"); missing = true; }
            if (bundle.Speaker == null) { Debug.LogError($"[SchoolMapGenerator] Prefab não encontrado: {PATH_SPEAKER}"); missing = true; }

            return missing ? null : bundle;
        }

        private static bool ClearExistingEnvironment()
        {
            GameObject existing = GameObject.Find("-- ENVIRONMENT --");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                return true;
            }
            return false;
        }

        // -------------------------------------------------------------------------
        // Geração do Corredor Central (Com Avisos, Armários, Caixas de Som e Extintores)
        // -------------------------------------------------------------------------
        private static void GenerateCorridor(Transform root, PrefabBundle prefabs)
        {
            GameObject corridorNode = CreateChild(root, "Corredor");
            Transform floorsGroup = CreateChild(corridorNode.transform, "Floors").transform;
            Transform ceilingsGroup = CreateChild(corridorNode.transform, "Ceilings").transform;
            Transform propsGroup = CreateChild(corridorNode.transform, "Corridor_Props").transform;

            // O corredor central possui 2 módulos de largura em X e 6 módulos de comprimento em Z
            float[] xOffsets = { -0.5f * TILE_X, 0.5f * TILE_X };
            float[] zOffsets = { -2.5f * TILE_Z, -1.5f * TILE_Z, -0.5f * TILE_Z, 0.5f * TILE_Z, 1.5f * TILE_Z, 2.5f * TILE_Z };

            foreach (float x in xOffsets)
            {
                foreach (float z in zOffsets)
                {
                    // Piso do corredor
                    Spawn(prefabs.Floor, new Vector3(x, 0f, z), Quaternion.identity, floorsGroup);

                    // Teto do corredor (floor invertido a Y = WALL_HEIGHT)
                    Spawn(prefabs.Floor, new Vector3(x, WALL_HEIGHT, z), Quaternion.Euler(180f, 0f, 0f), ceilingsGroup);
                }
            }

            // Quadros de Avisos no Corredor Central (Paredes em X = -TILE_X e X = +TILE_X)
            float wallLeftX = -TILE_X + 0.08f;
            float wallRightX = TILE_X - 0.08f;

            // Quadro de avisos na parede esquerda (voltado para o corredor, +X)
            GameObject notice1 = Spawn(prefabs.NoticeBoard, new Vector3(wallLeftX, 1.6f, -0.2f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            notice1.name = "NoticeBoard_Corridor_Left";

            // Quadro de avisos na parede direita (voltado para o corredor, -X)
            GameObject notice2 = Spawn(prefabs.NoticeBoard, new Vector3(wallRightX, 1.6f, 0.2f), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            notice2.name = "NoticeBoard_Corridor_Right";

            // Armários escolares ao longo das seções sólidas do corredor
            float lockerLeftX = -TILE_X + 0.35f;
            float lockerRightX = TILE_X - 0.35f;
            float[] lockerZPoints = { -14.5f, -13.8f, -13.1f, -12.4f, -9.5f, -8.8f, -8.1f, -7.4f, 7.4f, 8.1f, 8.8f, 9.5f, 12.4f, 13.1f, 13.8f, 14.5f };

            foreach (float lz in lockerZPoints)
            {
                Spawn(prefabs.Locker, new Vector3(lockerLeftX, 0f, lz), Quaternion.Euler(0f, 90f, 0f), propsGroup);
                Spawn(prefabs.Locker, new Vector3(lockerRightX, 0f, lz), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            }

            // Extintores de Incêndio no Corredor Central
            Spawn(prefabs.FireExtinguisher, new Vector3(wallLeftX, 1.1f, -6.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            Spawn(prefabs.FireExtinguisher, new Vector3(wallRightX, 1.1f, 6.5f), Quaternion.Euler(0f, -90f, 0f), propsGroup);

            // Caixas de Som / Interfone no Corredor (no alto das paredes a Y = 3.6m voltadas para o corredor)
            if (prefabs.Speaker != null)
            {
                Spawn(prefabs.Speaker, new Vector3(wallLeftX + 0.15f, 3.6f, -4.5f), Quaternion.Euler(15f, 90f, 0f), propsGroup);
                Spawn(prefabs.Speaker, new Vector3(wallRightX - 0.15f, 3.6f, 4.5f), Quaternion.Euler(15f, -90f, 0f), propsGroup);
            }
        }

        // -------------------------------------------------------------------------
        // Geração de Cada Sala de Aula Expandida (3x3 módulos)
        // -------------------------------------------------------------------------
        private static void GenerateRoom(Transform root, RoomDefinition def, PrefabBundle prefabs)
        {
            GameObject roomNode = CreateChild(root, def.Name);
            Transform floorsGroup = CreateChild(roomNode.transform, "Floors").transform;
            Transform ceilingsGroup = CreateChild(roomNode.transform, "Ceilings").transform;
            Transform wallsGroup = CreateChild(roomNode.transform, "Walls").transform;
            Transform doorsGroup = CreateChild(roomNode.transform, "Doors").transform;
            Transform interactiveGroup = CreateChild(roomNode.transform, "Interactive_Board").transform;
            Transform furnitureGroup = CreateChild(roomNode.transform, "Furniture").transform;

            // 1. Pisos e Tetos (Grade 3x3 = 9 blocos)
            float[] xCols = {
                def.MinX + 0.5f * TILE_X,
                def.MinX + 1.5f * TILE_X,
                def.MinX + 2.5f * TILE_X
            };

            float[] zRows = {
                def.MinZ + 0.5f * TILE_Z,
                def.MinZ + 1.5f * TILE_Z,
                def.MinZ + 2.5f * TILE_Z
            };

            foreach (float x in xCols)
            {
                foreach (float z in zRows)
                {
                    // Piso
                    Spawn(prefabs.Floor, new Vector3(x, 0f, z), Quaternion.identity, floorsGroup);

                    // Teto (floor invertido a Y = WALL_HEIGHT)
                    Spawn(prefabs.Floor, new Vector3(x, WALL_HEIGHT, z), Quaternion.Euler(180f, 0f, 0f), ceilingsGroup);
                }
            }

            // 2. Paredes Exteriores da Fachada do Prédio (com Janelas)
            float outerX = def.IsLeftOfCorridor ? def.MinX : def.MaxX;
            float outerRotY = def.IsLeftOfCorridor ? 180f : 0f;
            Quaternion outerRot = Quaternion.Euler(0f, outerRotY, 0f);

            foreach (float z in zRows)
            {
                SpawnWall(prefabs.WallWindow, new Vector3(outerX, 0f, z), outerRot, PIVOT_WALL_WINDOW, wallsGroup);
            }

            // 3. Paredes Frontal e Traseira da Sala (Paredes 100% SÓLIDAS sem janelas)
            Vector3 wallScaleX = new Vector3(1f, 1f, TILE_X / TILE_Z);
            Quaternion wallRotX = Quaternion.Euler(0f, 90f, 0f);

            // Parede Sul da sala (sólida)
            foreach (float x in xCols)
            {
                SpawnWall(prefabs.WallSolid, new Vector3(x, 0f, def.MinZ), wallRotX, PIVOT_WALL_SOLID, wallsGroup, wallScaleX);
            }

            // Parede Norte da sala (sólida)
            foreach (float x in xCols)
            {
                SpawnWall(prefabs.WallSolid, new Vector3(x, 0f, def.MaxZ), wallRotX, PIVOT_WALL_SOLID, wallsGroup, wallScaleX);
            }

            // 4. Parede Conectando ao Corredor (Paredes 100% contínuas sem furos)
            float corridorWallX = def.IsLeftOfCorridor ? def.MaxX : def.MinX;
            float corridorWallRotY = def.IsLeftOfCorridor ? 180f : 0f;
            Quaternion corridorWallRot = Quaternion.Euler(0f, corridorWallRotY, 0f);

            foreach (float solidZ in def.SolidWallZs)
            {
                SpawnWall(prefabs.WallSolid, new Vector3(corridorWallX, 0f, solidZ), corridorWallRot, PIVOT_WALL_SOLID, wallsGroup);
            }

            // Módulo de Porta no Corredor (wall4 com abertura precisa e porta a door1 integrada)
            SpawnDoorway(doorsGroup, wallsGroup, prefabs, corridorWallX, def.DoorZ, corridorWallRot);

            // 5. Quadro Interativo Centralizado Verticalmente e Alargado na Parede Frontal
            float roomCenterX = (def.MinX + def.MaxX) * 0.5f;
            float boardOffsetZ = def.BoardRotY == 0f ? 0.15f : -0.15f;
            Vector3 boardPos = new Vector3(roomCenterX, 0.95f, def.BoardZ + boardOffsetZ);
            Vector3 boardScale = new Vector3(1.55f, 1.05f, 1.0f);
            GameObject boardObj = Spawn(prefabs.Board, boardPos, Quaternion.Euler(0f, def.BoardRotY, 0f), interactiveGroup, boardScale);
            boardObj.name = "Interactive_Board";

            // 6. Mobília, Cenografia Rica e Personalização Temática da Sala
            SpawnClassroomFurniture(furnitureGroup, prefabs, def);
        }

        // -------------------------------------------------------------------------
        // Geração do Hall de Entrada Principal & Recepção (Ala Sul)
        // -------------------------------------------------------------------------
        private static void GenerateEntranceHall(Transform root, PrefabBundle prefabs)
        {
            GameObject hallNode = CreateChild(root, "Hall_Entrada_Recepcao");
            Transform floorsGroup = CreateChild(hallNode.transform, "Floors").transform;
            Transform ceilingsGroup = CreateChild(hallNode.transform, "Ceilings").transform;
            Transform wallsGroup = CreateChild(hallNode.transform, "Walls").transform;
            Transform doorsGroup = CreateChild(hallNode.transform, "Doors").transform;
            Transform propsGroup = CreateChild(hallNode.transform, "Reception_Props").transform;
            Transform exteriorGroup = CreateChild(hallNode.transform, "Exterior_Area").transform;

            // O Hall de Entrada mede 4 módulos em X (17.69m) x 2 módulos em Z (10.67m)
            // Z de -3 * TILE_Z (-16.01m) até -5 * TILE_Z (-26.68m)
            float[] xCols = { -1.5f * TILE_X, -0.5f * TILE_X, 0.5f * TILE_X, 1.5f * TILE_X };
            float[] zRows = { -3.5f * TILE_Z, -4.5f * TILE_Z };

            // 1. Pisos e Tetos do Hall
            foreach (float x in xCols)
            {
                foreach (float z in zRows)
                {
                    Spawn(prefabs.Floor, new Vector3(x, 0f, z), Quaternion.identity, floorsGroup);
                    Spawn(prefabs.Floor, new Vector3(x, WALL_HEIGHT, z), Quaternion.Euler(180f, 0f, 0f), ceilingsGroup);
                }
            }

            // 2. Paredes Laterais do Hall (Oeste em X = -2 * TILE_X; Leste em X = +2 * TILE_X)
            float westX = -2f * TILE_X;
            float eastX = 2f * TILE_X;

            foreach (float z in zRows)
            {
                // Parede Oeste sólida (onde ficam vitrines e armários)
                SpawnWall(prefabs.WallSolid, new Vector3(westX, 0f, z), Quaternion.Euler(0f, 180f, 0f), PIVOT_WALL_SOLID, wallsGroup);

                // Parede Leste com amplas janelas para entrada de luz natural
                SpawnWall(prefabs.WallWindow, new Vector3(eastX, 0f, z), Quaternion.identity, PIVOT_WALL_WINDOW, wallsGroup);
            }

            // 3. Fachada Sul (Entrada Principal do Prédio Escolar em Z = -5 * TILE_Z)
            float southFacadeZ = -5f * TILE_Z;
            Vector3 wallScaleX = new Vector3(1f, 1f, TILE_X / TILE_Z);
            Quaternion facadeRot = Quaternion.Euler(0f, 90f, 0f);

            // Coluna 0 (Oeste): Janela ampla
            SpawnWall(prefabs.WallWindow, new Vector3(xCols[0], 0f, southFacadeZ), facadeRot, PIVOT_WALL_WINDOW, wallsGroup, wallScaleX);

            // Coluna 1: Porta Dupla Principal 1 (Entrada da Escola)
            SpawnDoorway(doorsGroup, wallsGroup, prefabs, xCols[1], southFacadeZ, facadeRot, wallScaleX);

            // Coluna 2: Porta Dupla Principal 2 (Entrada da Escola)
            SpawnDoorway(doorsGroup, wallsGroup, prefabs, xCols[2], southFacadeZ, facadeRot, wallScaleX);

            // Coluna 3 (Leste): Janela ampla
            SpawnWall(prefabs.WallWindow, new Vector3(xCols[3], 0f, southFacadeZ), facadeRot, PIVOT_WALL_WINDOW, wallsGroup, wallScaleX);

            // 4. Mobília e Cenografia da Recepção
            float deskX = -3.8f;
            float deskZ = -20.5f;
            GameObject receptionDesk = Spawn(prefabs.TeacherDesk, new Vector3(deskX, 0f, deskZ), Quaternion.Euler(0f, 0f, 0f), propsGroup);
            receptionDesk.name = "Reception_Desk";

            // Computador e acessórios da secretária/recepcionista
            Spawn(prefabs.Computer, new Vector3(deskX, 1.15f, deskZ + 0.15f), Quaternion.Euler(0f, 180f, 0f), propsGroup);
            Spawn(prefabs.Keyboard, new Vector3(deskX, 1.15f, deskZ - 0.15f), Quaternion.Euler(0f, 180f, 0f), propsGroup);
            if (prefabs.Mouse != null)
                Spawn(prefabs.Mouse, new Vector3(deskX + 0.38f, 1.15f, deskZ - 0.15f), Quaternion.Euler(0f, 180f, 0f), propsGroup);

            // Cadeira da recepcionista atrás da mesa
            Spawn(prefabs.TeacherChair, new Vector3(deskX, 0f, deskZ - 0.75f), Quaternion.identity, propsGroup);

            // Vitrines de Troféus e Conquistas Escolares na parede Oeste
            float westWallInnerX = westX + 0.85f;
            GameObject sc1 = Spawn(prefabs.Showcase, new Vector3(westWallInnerX, 0f, -19.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            sc1.name = "Showcase_Trophies_1";
            GameObject sc2 = Spawn(prefabs.Showcase, new Vector3(westWallInnerX, 0f, -23.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            sc2.name = "Showcase_Trophies_2";

            // Quadro de Avisos e Boas-Vindas da Escola
            GameObject welcomeBoard = Spawn(prefabs.NoticeBoard, new Vector3(westX + 0.08f, 1.6f, -21.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            welcomeBoard.name = "NoticeBoard_Welcome";

            // Extintor de Emergência próximo à entrada
            Spawn(prefabs.FireExtinguisher, new Vector3(westX + 0.08f, 1.1f, -25.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);

            // Armários escolares no hall de entrada
            float lockerX = eastX - 0.35f;
            float[] hallLockersZ = { -19.0f, -19.7f, -20.4f, -22.5f, -23.2f, -23.9f };
            foreach (float lz in hallLockersZ)
            {
                Spawn(prefabs.Locker, new Vector3(lockerX, 0f, lz), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            }

            // 5. Área Externa Frontal: Rua Asfaltada e Ônibus Escolar
            float streetZ = -31.35f;
            float[] roadXs = { -7f, 0f, 7f };
            foreach (float rx in roadXs)
            {
                Spawn(prefabs.Road, new Vector3(rx, 0f, streetZ), Quaternion.Euler(0f, 0f, 0f), exteriorGroup);
            }

            // Ônibus Escolar Amarelo estacionado na frente da escola (visível pelas janelas e portas do hall)
            GameObject busObj = Spawn(prefabs.Bus, new Vector3(2.0f, 0f, streetZ), Quaternion.Euler(0f, 90f, 0f), exteriorGroup);
            busObj.name = "School_Bus_Exterior";
        }

        // -------------------------------------------------------------------------
        // Geração da Grande Biblioteca & Sala de Estudos (Ala Norte)
        // -------------------------------------------------------------------------
        private static void GenerateLibrary(Transform root, PrefabBundle prefabs)
        {
            GameObject libNode = CreateChild(root, "Grande_Biblioteca");
            Transform floorsGroup = CreateChild(libNode.transform, "Floors").transform;
            Transform ceilingsGroup = CreateChild(libNode.transform, "Ceilings").transform;
            Transform wallsGroup = CreateChild(libNode.transform, "Walls").transform;
            Transform propsGroup = CreateChild(libNode.transform, "Library_Props").transform;

            // A Biblioteca mede 4 módulos em X (17.69m) x 2 módulos em Z (10.67m)
            // Z de +3 * TILE_Z (+16.01m) até +5 * TILE_Z (+26.68m)
            float[] xCols = { -1.5f * TILE_X, -0.5f * TILE_X, 0.5f * TILE_X, 1.5f * TILE_X };
            float[] zRows = { 3.5f * TILE_Z, 4.5f * TILE_Z };

            // 1. Pisos e Tetos da Biblioteca
            foreach (float x in xCols)
            {
                foreach (float z in zRows)
                {
                    Spawn(prefabs.Floor, new Vector3(x, 0f, z), Quaternion.identity, floorsGroup);
                    Spawn(prefabs.Floor, new Vector3(x, WALL_HEIGHT, z), Quaternion.Euler(180f, 0f, 0f), ceilingsGroup);
                }
            }

            // 2. Paredes Laterais da Biblioteca (Oeste em X = -2 * TILE_X; Leste em X = +2 * TILE_X)
            float westX = -2f * TILE_X;
            float eastX = 2f * TILE_X;

            foreach (float z in zRows)
            {
                // Parede Oeste sólida (onde ficam as estantes de acervo)
                SpawnWall(prefabs.WallSolid, new Vector3(westX, 0f, z), Quaternion.Euler(0f, 180f, 0f), PIVOT_WALL_SOLID, wallsGroup);

                // Parede Leste sólida (onde ficam as bancadas de estudo e pesquisa)
                SpawnWall(prefabs.WallSolid, new Vector3(eastX, 0f, z), Quaternion.identity, PIVOT_WALL_SOLID, wallsGroup);
            }

            // 3. Fachada Norte (Paredes de Vidro com Iluminação Natural em Z = +5 * TILE_Z)
            float northFacadeZ = 5f * TILE_Z;
            Vector3 wallScaleX = new Vector3(1f, 1f, TILE_X / TILE_Z);
            Quaternion facadeRot = Quaternion.Euler(0f, 90f, 0f);

            foreach (float x in xCols)
            {
                SpawnWall(prefabs.WallWindow, new Vector3(x, 0f, northFacadeZ), facadeRot, PIVOT_WALL_WINDOW, wallsGroup, wallScaleX);
            }

            // 4. Mobília e Cenografia da Biblioteca
            // Mesa de Atendimento da Bibliotecária: encostada na parede Leste com o mesmo sentido e alinhamento dos outros computadores
            float pcDeskX = eastX - 0.85f; // ~7.99m (encostada na parede Leste)
            float libDeskX = pcDeskX;
            float libDeskZ = 17.2f;
            GameObject libDesk = Spawn(prefabs.TeacherDesk, new Vector3(libDeskX, 0f, libDeskZ), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            libDesk.name = "Librarian_Desk";

            // Computador, teclado e cadeira da bibliotecária virados para o interior, no mesmo sentido dos terminais de pesquisa
            Spawn(prefabs.Computer, new Vector3(libDeskX, 1.15f, libDeskZ), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            Spawn(prefabs.Keyboard, new Vector3(libDeskX - 0.25f, 1.15f, libDeskZ), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            Spawn(prefabs.TeacherChair, new Vector3(libDeskX - 0.75f, 0f, libDeskZ), Quaternion.Euler(0f, 90f, 0f), propsGroup);

            // Corredores de Estantes Recheadas de Livros (rack1) - estante vazia removida conforme solicitado
            // Estantes da parede Oeste
            Spawn(prefabs.BookcaseWithBooks, new Vector3(westX + 0.85f, 0f, 19.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            Spawn(prefabs.BookcaseWithBooks, new Vector3(westX + 0.85f, 0f, 23.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);

            // Corredor Central de Leitura A (estantes duplas paralelas)
            Spawn(prefabs.BookcaseWithBooks, new Vector3(-4.2f, 0f, 20.0f), Quaternion.identity, propsGroup);
            Spawn(prefabs.BookcaseWithBooks, new Vector3(-4.2f, 0f, 23.8f), Quaternion.identity, propsGroup);

            // Corredor Central de Leitura B
            Spawn(prefabs.BookcaseWithBooks, new Vector3(-1.2f, 0f, 20.0f), Quaternion.identity, propsGroup);
            Spawn(prefabs.BookcaseWithBooks, new Vector3(-1.2f, 0f, 23.8f), Quaternion.identity, propsGroup);

            // Mesa Coletiva Ampla de Estudos em Grupo (table2 com 4 bancos integrados)
            GameObject studyTable = Spawn(prefabs.TableStudy, new Vector3(4.5f, 0f, 23.2f), Quaternion.identity, propsGroup);
            studyTable.name = "Group_Study_Table";

            // Terminais de Consulta Rápida ao Acervo na parede Leste: UTILIZANDO O MODELO table3 (mesmas do professor)
            // (Utiliza pcDeskX já configurado junto à parede Leste)

            // Terminal 1 (table3 com PC completo)
            GameObject pc1 = Spawn(prefabs.TeacherDesk, new Vector3(pcDeskX, 0f, 20.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            pc1.name = "Search_Terminal_1";
            Spawn(prefabs.Computer, new Vector3(pcDeskX, 1.15f, 20.5f), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            Spawn(prefabs.Keyboard, new Vector3(pcDeskX - 0.25f, 1.15f, 20.5f), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            Spawn(prefabs.TeacherChair, new Vector3(pcDeskX - 0.75f, 0f, 20.5f), Quaternion.Euler(0f, 90f, 0f), propsGroup);

            // Terminal 2 (table3 com PC completo)
            GameObject pc2 = Spawn(prefabs.TeacherDesk, new Vector3(pcDeskX, 0f, 24.0f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
            pc2.name = "Search_Terminal_2";
            Spawn(prefabs.Computer, new Vector3(pcDeskX, 1.15f, 24.0f), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            Spawn(prefabs.Keyboard, new Vector3(pcDeskX - 0.25f, 1.15f, 24.0f), Quaternion.Euler(0f, -90f, 0f), propsGroup);
            Spawn(prefabs.TeacherChair, new Vector3(pcDeskX - 0.75f, 0f, 24.0f), Quaternion.Euler(0f, 90f, 0f), propsGroup);

            // Vitrine de Obras Raras e Manuscritos Históricos
            GameObject showcaseRare = Spawn(prefabs.Showcase, new Vector3(-1.2f, 0f, 17.2f), Quaternion.Euler(0f, 0f, 0f), propsGroup);
            showcaseRare.name = "Showcase_Rare_Manuscripts";

            // Extintor de Incêndio da Biblioteca
            Spawn(prefabs.FireExtinguisher, new Vector3(westX + 0.08f, 1.1f, 17.2f), Quaternion.Euler(0f, 90f, 0f), propsGroup);
        }

        // -------------------------------------------------------------------------
        // Construção do Módulo da Porta com Parede wall4 e Batente a door1
        // -------------------------------------------------------------------------
        private static void SpawnDoorway(Transform doorsGroup, Transform wallsGroup, PrefabBundle prefabs, float wallX, float doorZ, Quaternion wallRot, Vector3? scale = null)
        {
            Vector3 targetCenter = new Vector3(wallX, 0f, doorZ);

            // 1. Parede com abertura integrada de fábrica (wall4)
            GameObject wallObj = SpawnWall(prefabs.WallDoorway, targetCenter, wallRot, PIVOT_WALL_DOORWAY, wallsGroup, scale);
            wallObj.name = "School_Wall_Doorway";

            // 2. Batente e folhas de porta (a door1) integrados perfeitamente ao vão
            Quaternion doorRot = wallRot * Quaternion.Euler(0f, -90f, 0f);

            Vector3 doorScale = Vector3.one;
            Vector3 baseOffset = new Vector3(-0.044f, 0f, 0.110f);

            if (scale.HasValue)
            {
                // Como a porta está rotacionada em -90° relativo à parede, seu eixo X local alinha-se com o eixo Z local da parede
                doorScale = new Vector3(scale.Value.z, scale.Value.y, scale.Value.x);
                baseOffset = Vector3.Scale(baseOffset, scale.Value);
            }

            Vector3 doorPos = targetCenter + wallRot * baseOffset;
            GameObject doorInstance = Spawn(prefabs.Door, doorPos, doorRot, doorsGroup, doorScale);
            doorInstance.name = "School_Door";

            // 3. Configurar folhas da porta como triggers para passagem suave em primeira pessoa
            ConfigureDoorPassage(doorInstance);
        }

        private static void ConfigureDoorPassage(GameObject doorInstance)
        {
            Transform leaf1 = doorInstance.transform.Find("Cube_10");
            Transform leaf2 = doorInstance.transform.Find("Cube_14");

            if (leaf1 != null)
            {
                Collider c = leaf1.GetComponent<Collider>();
                if (c != null) c.isTrigger = true;
                leaf1.localRotation = Quaternion.Euler(0f, 75f, 0f);
            }

            if (leaf2 != null)
            {
                Collider c = leaf2.GetComponent<Collider>();
                if (c != null) c.isTrigger = true;
                leaf2.localRotation = Quaternion.Euler(0f, -75f, 0f);
            }
        }

        // -------------------------------------------------------------------------
        // Paredes Divisórias Centrais 100% Sólidas entre Salas (Z = 0)
        // -------------------------------------------------------------------------
        private static void GenerateDividingWalls(Transform root, PrefabBundle prefabs, float corridorHalfX)
        {
            Transform rootTransform = root;
            Vector3 wallScaleX = new Vector3(1f, 1f, TILE_X / TILE_Z);
            Quaternion wallRotX = Quaternion.Euler(0f, 90f, 0f);

            // Divisória entre Sala 1 e Sala 3 (X de -17.68m a -4.42m = 3 módulos sólidos)
            float[] leftDivCols = {
                -corridorHalfX - 2.5f * TILE_X,
                -corridorHalfX - 1.5f * TILE_X,
                -corridorHalfX - 0.5f * TILE_X
            };
            foreach (float x in leftDivCols)
            {
                SpawnWall(prefabs.WallSolid, new Vector3(x, 0f, 0f), wallRotX, PIVOT_WALL_SOLID, rootTransform, wallScaleX);
            }

            // Divisória entre Sala 2 e Sala 4 (X de +4.42m a +17.68m = 3 módulos sólidos)
            float[] rightDivCols = {
                corridorHalfX + 0.5f * TILE_X,
                corridorHalfX + 1.5f * TILE_X,
                corridorHalfX + 2.5f * TILE_X
            };
            foreach (float x in rightDivCols)
            {
                SpawnWall(prefabs.WallSolid, new Vector3(x, 0f, 0f), wallRotX, PIVOT_WALL_SOLID, rootTransform, wallScaleX);
            }
        }

        // -------------------------------------------------------------------------
        // Mobília da Sala: Mesa do Professor, Carteiras, Projetor, Caixas de Som e Cenografia Temática
        // -------------------------------------------------------------------------
        private static void SpawnClassroomFurniture(Transform furnitureGroup, PrefabBundle prefabs, RoomDefinition def)
        {
            float roomCenterX = (def.MinX + def.MaxX) * 0.5f;
            float roomCenterZ = (def.MinZ + def.MaxZ) * 0.5f;

            // =========================================================================
            // 1. Mesa do Professor (table3) com PC, Teclado, Mouse e Material de Apoio
            // =========================================================================
            float teacherSideX = def.IsLeftOfCorridor ? (roomCenterX - 3.4f) : (roomCenterX + 3.4f);
            float teacherZ = def.BoardRotY == 0f ? (def.BoardZ + 2.6f) : (def.BoardZ - 2.6f);

            float teacherFacingRotY = def.BoardRotY == 0f ? 0f : 180f;

            GameObject desk = Spawn(prefabs.TeacherDesk, new Vector3(teacherSideX, 0f, teacherZ), Quaternion.Euler(0f, teacherFacingRotY, 0f), furnitureGroup);
            desk.name = "Teacher_Desk_Table3";

            float tableTopY = 1.15f;
            float monitorRotY = teacherFacingRotY + 180f;
            float monitorOffsetZ = def.BoardRotY == 0f ? 0.15f : -0.15f;
            Vector3 monitorPos = new Vector3(teacherSideX, tableTopY, teacherZ + monitorOffsetZ);
            GameObject monitor = Spawn(prefabs.Computer, monitorPos, Quaternion.Euler(0f, monitorRotY, 0f), furnitureGroup);
            monitor.name = "Teacher_PC_Monitor";

            float keyboardOffsetZ = def.BoardRotY == 0f ? -0.15f : 0.15f;
            Vector3 keyboardPos = new Vector3(teacherSideX, tableTopY, teacherZ + keyboardOffsetZ);
            GameObject keyboard = Spawn(prefabs.Keyboard, keyboardPos, Quaternion.Euler(0f, monitorRotY, 0f), furnitureGroup);
            keyboard.name = "Teacher_PC_Keyboard";

            if (prefabs.Mouse != null)
            {
                float mouseOffsetX = def.BoardRotY == 0f ? 0.38f : -0.38f;
                Vector3 mousePos = new Vector3(teacherSideX + mouseOffsetX, tableTopY, teacherZ + keyboardOffsetZ);
                GameObject mouse = Spawn(prefabs.Mouse, mousePos, Quaternion.Euler(0f, monitorRotY, 0f), furnitureGroup);
                mouse.name = "Teacher_PC_Mouse";
            }

            // Cadeira do Professor (chair) atrás da mesa
            float chairOffsetZ = def.BoardRotY == 0f ? -0.75f : 0.75f;
            Vector3 teacherChairPos = new Vector3(teacherSideX, 0f, teacherZ + chairOffsetZ);
            GameObject tChair = Spawn(prefabs.TeacherChair, teacherChairPos, Quaternion.Euler(0f, teacherFacingRotY, 0f), furnitureGroup);
            tChair.name = "Teacher_Chair";

            // Folhas de prova / chamada do professor sobre a mesa
            if (prefabs.SheetPaper != null)
            {
                float sheetOffsetX = def.BoardRotY == 0f ? -0.32f : 0.32f;
                Vector3 teacherSheetPos = new Vector3(teacherSideX + sheetOffsetX, tableTopY + 0.005f, teacherZ);
                Spawn(prefabs.SheetPaper, teacherSheetPos, Quaternion.Euler(90f, 0f, teacherFacingRotY), furnitureGroup, new Vector3(0.60f, 0.60f, 0.60f));
            }

            // Livro do professor sobre a mesa (perfeitamente encaixado e assentado sobre table3)
            if (prefabs.Book != null)
            {
                float bookOffsetX = def.BoardRotY == 0f ? -0.52f : 0.52f;
                float bookOffsetZ = def.BoardRotY == 0f ? 0.05f : -0.05f;
                Vector3 teacherBookPos = new Vector3(teacherSideX + bookOffsetX, tableTopY + 0.026f, teacherZ + bookOffsetZ);
                Quaternion teacherBookRot = Quaternion.Euler(0f, teacherFacingRotY + 90f, 90f);
                Spawn(prefabs.Book, teacherBookPos, teacherBookRot, furnitureGroup, new Vector3(0.48f, 0.48f, 0.48f));
            }

            // =========================================================================
            // 2. Projetor Multimídia Suspenso no Teto Voltado para a Lousa
            // =========================================================================
            if (prefabs.Projector != null)
            {
                // Posicionado a ~4.8m da lousa, suspenso a partir do teto (Y = 3.09m)
                // Rotação calibrada para que o canhão de projeção aponte de frente para o quadro
                float projectorZ = def.BoardRotY == 0f ? (def.BoardZ + 4.8f) : (def.BoardZ - 4.8f);
                float projectorRotY = def.BoardRotY == 0f ? 0f : 180f;
                Vector3 projectorPos = new Vector3(roomCenterX, WALL_HEIGHT - 1.33f, projectorZ);
                GameObject projObj = Spawn(prefabs.Projector, projectorPos, Quaternion.Euler(0f, projectorRotY, 0f), furnitureGroup);
                projObj.name = "Ceiling_Projector";
            }

            // =========================================================================
            // 3. Caixa de Som para Avisos Escolares Direcionada para os Alunos
            // =========================================================================
            if (prefabs.Speaker != null)
            {
                // Fixada na parede frontal ao lado da lousa (Y = 3.6m), apontada diretamente para a turma
                float speakerOffsetZ = def.BoardRotY == 0f ? 0.20f : -0.20f;
                float speakerRotY = def.BoardRotY == 0f ? 0f : 180f;
                Vector3 speakerPos = new Vector3(roomCenterX - 3.2f, 3.6f, def.BoardZ + speakerOffsetZ);
                GameObject spkObj = Spawn(prefabs.Speaker, speakerPos, Quaternion.Euler(15f, speakerRotY, 0f), furnitureGroup);
                spkObj.name = "Wall_Speaker";
            }

            // =========================================================================
            // 4. 5 Fileiras de 4 Cadeiras dos Alunos (chair1) com Notebooks e Cadernos Assentados
            // =========================================================================
            float studentFacingRotY = def.BoardRotY == 0f ? 180f : 0f;

            float[] colXs = {
                roomCenterX - 3.8f,
                roomCenterX - 2.0f,
                roomCenterX + 2.0f,
                roomCenterX + 3.8f
            };

            float[] rowZs;
            if (def.BoardRotY == 0f)
            {
                rowZs = new float[] {
                    def.BoardZ + 5.2f,  // Fileira 1
                    def.BoardZ + 7.4f,  // Fileira 2
                    def.BoardZ + 9.6f,  // Fileira 3
                    def.BoardZ + 11.8f, // Fileira 4
                    def.BoardZ + 14.0f  // Fileira 5
                };
            }
            else
            {
                rowZs = new float[] {
                    def.BoardZ - 5.2f,  // Fileira 1
                    def.BoardZ - 7.4f,  // Fileira 2
                    def.BoardZ - 9.6f,  // Fileira 3
                    def.BoardZ - 11.8f, // Fileira 4
                    def.BoardZ - 14.0f  // Fileira 5
                };
            }

            for (int r = 0; r < rowZs.Length; r++)
            {
                for (int c = 0; c < colXs.Length; c++)
                {
                    Vector3 chairPos = new Vector3(colXs[c], 0f, rowZs[r]);
                    Quaternion chairRot = Quaternion.Euler(0f, studentFacingRotY, 0f);
                    GameObject studentDeskObj = Spawn(prefabs.StudentChair, chairPos, chairRot, furnitureGroup);
                    studentDeskObj.name = $"StudentDesk_Row{r + 1}_Col{c + 1}";

                    // 1. Notebook posicionado no tampo da prancheta, deslocado à esquerda para centralizar com o aluno, base perfeitamente assentada (Y = 0.995m)
                    if (prefabs.Laptop != null)
                    {
                        Vector3 laptopLocalOffset = new Vector3(-0.33f, 0.995f, 0.44f);
                        Vector3 laptopPos = chairPos + chairRot * laptopLocalOffset;
                        Quaternion laptopRot = chairRot * Quaternion.Euler(0f, 180f, 0f);
                        Vector3 laptopScale = new Vector3(0.60f, 0.60f, 0.60f);
                        GameObject laptopObj = Spawn(prefabs.Laptop, laptopPos, laptopRot, furnitureGroup, laptopScale);
                        laptopObj.name = $"Laptop_Row{r + 1}_Col{c + 1}";
                    }

                    // 2. Caderno / Livro didático posicionado ao lado esquerdo da prancheta, assentado sem sobreposição nem flutuação
                    int deskIndex = r * colXs.Length + c;
                    if (deskIndex % 3 == 0 && prefabs.SheetPaper != null)
                    {
                        // Folha de prova/exercício deitada plana sobre o lado esquerdo da carteira
                        Vector3 sheetOffset = new Vector3(0.03f, 0.993f, 0.40f);
                        Vector3 sheetPos = chairPos + chairRot * sheetOffset;
                        Quaternion sheetRot = chairRot * Quaternion.Euler(90f, 0f, 0f);
                        Vector3 sheetScale = new Vector3(0.50f, 0.50f, 0.50f);
                        Spawn(prefabs.SheetPaper, sheetPos, sheetRot, furnitureGroup, sheetScale);
                    }
                    else if (deskIndex % 3 == 1 && prefabs.Book != null)
                    {
                        // Caderno/Livro didático deitado plano sobre o lado esquerdo da carteira
                        GameObject chosenBook = (deskIndex % 2 == 0 && prefabs.BookAlt != null) ? prefabs.BookAlt : prefabs.Book;
                        Vector3 bookOffset = new Vector3(0.03f, 1.015f, 0.46f);
                        Vector3 bookPos = chairPos + chairRot * bookOffset;
                        Quaternion bookRot = chairRot * Quaternion.Euler(0f, 90f, 90f);
                        Vector3 bookScale = new Vector3(0.45f, 0.45f, 0.45f);
                        Spawn(chosenBook, bookPos, bookRot, furnitureGroup, bookScale);
                    }
                }
            }

            // =========================================================================
            // 5. Personalização Temática Única por Disciplina
            // =========================================================================
            float dir = def.BoardRotY == 0f ? 1f : -1f;
            float backWallZ = def.BoardRotY == 0f ? def.MaxZ : def.MinZ;

            if (def.Name.Contains("Matematica"))
            {
                // Sala 1 (Matemática): Quadro auxiliar de fórmulas na parede oposta às janelas (parede divisória sólida com o corredor)
                float oppositeWallX = def.MaxX - 0.13f; // ou -0.13
                GameObject mathBoard = Spawn(prefabs.NoticeBoard, new Vector3(oppositeWallX, 1.6f, roomCenterZ), Quaternion.Euler(0f, -90f, 0f), furnitureGroup);
                mathBoard.name = "Math_Formulas_Board";
            }
            else if (def.Name.Contains("Portugues"))
            {
                // Sala 2 (Português): Estante de livros de literatura e gramática no fundo da sala
                Vector3 bookcasePos = new Vector3(roomCenterX, 0f, backWallZ - 0.85f * dir);
                Quaternion bookcaseRot = def.BoardRotY == 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                GameObject literatureShelf = Spawn(prefabs.BookcaseWithBooks, bookcasePos, bookcaseRot, furnitureGroup);
                literatureShelf.name = "Portuguese_Literature_Shelf";
            }
            else if (def.Name.Contains("Historia"))
            {
                // Sala 3 (História): Vitrine de artefatos históricos e acervo cultural
                float historyWallX = def.MinX + 0.85f;
                GameObject historyVitrine = Spawn(prefabs.Showcase, new Vector3(historyWallX, 0f, roomCenterZ), Quaternion.Euler(0f, 90f, 0f), furnitureGroup);
                historyVitrine.name = "History_Artifacts_Showcase";
            }
            else if (def.Name.Contains("Logica"))
            {
                // Sala 4 (Lógica): Estação maker de programação auxiliar
                float techDeskX = def.MaxX - 1.15f;
                float techDeskZ = def.BoardZ - 2.8f * dir;
                GameObject logicDesk = Spawn(prefabs.TeacherDesk, new Vector3(techDeskX, 0f, techDeskZ), Quaternion.Euler(0f, -90f, 0f), furnitureGroup);
                logicDesk.name = "Logic_Maker_Workstation";

                Spawn(prefabs.Computer, new Vector3(techDeskX, 1.15f, techDeskZ), Quaternion.Euler(0f, -90f, 0f), furnitureGroup);
                Spawn(prefabs.TeacherChair, new Vector3(techDeskX - 0.7f, 0f, techDeskZ), Quaternion.Euler(0f, 90f, 0f), furnitureGroup);
            }

            // =========================================================================
            // 6. Plataforma Circular Iluminada e Zona de Interação do Minigame da Sala
            // =========================================================================
            SpawnClassroomQuizZone(furnitureGroup, def, roomCenterX);
        }

        // -------------------------------------------------------------------------
        // Plataforma Circular Iluminada e Zona de Interação do Minigame da Sala
        // -------------------------------------------------------------------------
        private static void SpawnClassroomQuizZone(Transform parent, RoomDefinition def, float roomCenterX)
        {
            float dir = def.BoardRotY == 0f ? 1f : -1f;
            float zoneZ = def.BoardZ + 2.2f * dir;
            Vector3 zonePos = new Vector3(roomCenterX, 0.02f, zoneZ);

            GameObject zoneObj = new GameObject($"Quiz_InteractionZone_{def.Subject}");
            zoneObj.transform.SetParent(parent, false);
            zoneObj.transform.position = zonePos;

            // Trigger de interação por proximidade (raio de 1.8 metros)
            SphereCollider sc = zoneObj.AddComponent<SphereCollider>();
            sc.radius = 1.8f;
            sc.isTrigger = true;
            sc.center = new Vector3(0f, 0.5f, 0f);

            ClassroomInteractionZone zoneComponent = zoneObj.AddComponent<ClassroomInteractionZone>();
            zoneComponent.Subject = def.Subject;

            Color themeColor = QuizDatabase.GetSubjectThemeColor(def.Subject);

            // 1. Disco visual no chão (plataforma circular suave)
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Floor_Glow_Marker";
            marker.transform.SetParent(zoneObj.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(2.6f, 0.01f, 2.6f);

            // Remove o collider do cilindro visual para não interferir na física do jogador
            Collider markerCol = marker.GetComponent<Collider>();
            if (markerCol != null) Object.DestroyImmediate(markerCol);

            // Material com cor temática da disciplina
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material markerMat = new Material(urpLit);
            markerMat.color = themeColor;
            if (markerMat.HasProperty("_EmissionColor"))
            {
                markerMat.SetColor("_EmissionColor", themeColor * 0.8f);
                markerMat.EnableKeyword("_EMISSION");
            }
            Renderer markerRend = marker.GetComponent<Renderer>();
            markerRend.sharedMaterial = markerMat;
            zoneComponent.MarkerRenderer = markerRend;

            // 2. Luz de destaque suave (Point Light)
            GameObject lightObj = new GameObject("Zone_PointLight");
            lightObj.transform.SetParent(zoneObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = themeColor;
            light.intensity = 2.0f;
            light.range = 5.0f;
            zoneComponent.ZoneLight = light;
        }

        private static void EnsureQuizUIManager()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            }

            if (Object.FindAnyObjectByType<SchoolProgressManager>() == null)
            {
                GameObject progressObj = new GameObject("School_Progress_Manager");
                progressObj.AddComponent<SchoolProgressManager>();
                Undo.RegisterCreatedObjectUndo(progressObj, "Create School Progress Manager");
            }

            if (Object.FindAnyObjectByType<QuizUIManager>() == null)
            {
                GameObject quizManagerObj = new GameObject("Quiz_Manager");
                quizManagerObj.AddComponent<QuizUIManager>();
                Undo.RegisterCreatedObjectUndo(quizManagerObj, "Create Quiz Manager");
            }
        }

        // -------------------------------------------------------------------------
        // Garantia de Colliders Físicos sem Bloqueio de Portas
        // -------------------------------------------------------------------------
        private static void EnsureColliders(GameObject root)
        {
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                if (mf.gameObject.name == "Floor_Glow_Marker") continue;

                Collider col = mf.GetComponent<Collider>();
                if (col == null)
                {
                    MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
                else
                {
                    col.enabled = true;
                    if (col is MeshCollider mc)
                    {
                        if (mc.sharedMesh == null)
                        {
                            mc.sharedMesh = mf.sharedMesh;
                        }
                        mc.convex = mc.isTrigger;
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // Garantia de Materiais Válidos (Evita slots nulos ou rosa)
        // -------------------------------------------------------------------------
        private static void EnsureMaterials(GameObject root, Material fallbackMaterial)
        {
            if (fallbackMaterial == null) return;

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                if (mr.gameObject.name == "Floor_Glow_Marker") continue;

                Material[] mats = mr.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == null || mats[i].shader.name.Contains("Error") || mats[i].shader.name == "Standard")
                    {
                        mats[i] = fallbackMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    mr.sharedMaterials = mats;
                    EditorUtility.SetDirty(mr);
                }
            }
        }

        // -------------------------------------------------------------------------
        // Utilitários de Instanciação e Posicionamento com Compensação de Pivô
        // -------------------------------------------------------------------------
        private static GameObject SpawnWall(GameObject prefab, Vector3 targetCenter, Quaternion rotation, Vector3 pivotOffset, Transform parent, Vector3? scale = null)
        {
            Vector3 scaledOffset = pivotOffset;
            if (scale.HasValue)
            {
                scaledOffset = Vector3.Scale(pivotOffset, scale.Value);
            }
            Vector3 spawnPos = targetCenter - (rotation * scaledOffset);
            return Spawn(prefab, spawnPos, rotation, parent, scale);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child, "Generate School Map");
            return child;
        }

        private static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Vector3? scale = null)
        {
            GameObject instance;
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                instance = Object.Instantiate(prefab, parent);
            }

            instance.transform.position = position;
            instance.transform.rotation = rotation;

            if (scale.HasValue)
            {
                instance.transform.localScale = scale.Value;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Generate School Map");
            return instance;
        }
    }
}
