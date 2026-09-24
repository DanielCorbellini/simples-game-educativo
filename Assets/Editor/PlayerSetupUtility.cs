using UnityEditor;
using UnityEngine;
using EducationalGame.Player;
using System.IO;

namespace EducationalGame.Editor
{
    /// <summary>
    /// Utilitário do Unity Editor para configuração e instanciação automática do Player em Primeira Pessoa.
    /// Acessível via menu superior: Tools > Setup First Person Player.
    /// Configura CharacterController, câmera nos olhos do jogador, FirstPersonController e salva o Prefab.
    /// </summary>
    public static class PlayerSetupUtility
    {
        private const string PREFAB_PATH = "Assets/school/Prefabs/Player.prefab";
        private const string PREFAB_DIR = "Assets/school/Prefabs";

        // Posição padrão no Hall de Entrada / Recepção (olhando em direção ao corredor)
        private static readonly Vector3 DEFAULT_SPAWN_POS = new Vector3(0f, 0.1f, -20.0f);
        private static readonly Quaternion DEFAULT_SPAWN_ROT = Quaternion.Euler(0f, 0f, 0f);

        [MenuItem("Tools/Setup First Person Player")]
        public static void SetupFirstPersonPlayerMenu()
        {
            GameObject player = SetupPlayerInScene(DEFAULT_SPAWN_POS, DEFAULT_SPAWN_ROT);
            if (player != null)
            {
                Selection.activeGameObject = player;
                EditorGUIUtility.PingObject(player);
                EditorUtility.DisplayDialog(
                    "Player Configurado",
                    "O Player em Primeira Pessoa foi configurado e posicionado no Hall de Entrada!\n\n" +
                    "• Controles: WASD (andar), Shift (correr), Espaço (pular), Mouse (olhar).\n" +
                    "• Aperte PLAY no Unity para testar a exploração da escola.",
                    "OK"
                );
            }
        }

        /// <summary>
        /// Instancia ou atualiza o Player na cena ativa.
        /// </summary>
        public static GameObject SetupPlayerInScene(Vector3? spawnPosition = null, Quaternion? spawnRotation = null)
        {
            Vector3 pos = spawnPosition ?? DEFAULT_SPAWN_POS;
            Quaternion rot = spawnRotation ?? DEFAULT_SPAWN_ROT;

            // 1. Verificar se já existe um Player na cena
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
                Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            }
            else
            {
                Undo.RecordObject(playerObj.transform, "Update Player Position");
            }

            playerObj.tag = "Player";
            playerObj.transform.position = pos;
            playerObj.transform.rotation = rot;

            // 2. Configurar CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = Undo.AddComponent<CharacterController>(playerObj);
            }

            cc.height = 1.80f;
            cc.radius = 0.35f; // Diâmetro de 70cm, passa livremente por portas de 1.40m
            cc.center = new Vector3(0f, 0.90f, 0f);
            cc.stepOffset = 0.30f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // 3. Configurar Câmera em Primeira Pessoa
            Camera playerCam = playerObj.GetComponentInChildren<Camera>();
            Transform camTransform;

            if (playerCam == null)
            {
                // Procurar se há uma Main Camera solta na cena
                Camera existingMainCam = Camera.main;
                if (existingMainCam != null && existingMainCam.transform.parent != playerObj.transform)
                {
                    Undo.SetTransformParent(existingMainCam.transform, playerObj.transform, "Parent Main Camera to Player");
                    playerCam = existingMainCam;
                    camTransform = existingMainCam.transform;
                }
                else
                {
                    GameObject camObj = new GameObject("PlayerCamera");
                    Undo.RegisterCreatedObjectUndo(camObj, "Create Player Camera");
                    camObj.transform.SetParent(playerObj.transform);
                    playerCam = Undo.AddComponent<Camera>(camObj);
                    Undo.AddComponent<AudioListener>(camObj);
                    camTransform = camObj.transform;
                }
            }
            else
            {
                camTransform = playerCam.transform;
            }

            playerCam.name = "PlayerCamera";
            playerCam.tag = "MainCamera";
            camTransform.localPosition = new Vector3(0f, 1.65f, 0f); // Altura dos olhos
            camTransform.localRotation = Quaternion.identity;
            playerCam.nearClipPlane = 0.05f; // Evita atravessar paredes próximas
            playerCam.fieldOfView = 65f;

            // 4. Configurar componente FirstPersonController
            FirstPersonController controller = playerObj.GetComponent<FirstPersonController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<FirstPersonController>(playerObj);
            }

            controller.PlayerCamera = playerCam;

            // 5. Salvar/Atualizar Prefab em Assets/school/Prefabs/Player.prefab
            SavePlayerPrefab(playerObj);

            Debug.Log($"[PlayerSetupUtility] Player configurado com sucesso na posição {pos}.");
            return playerObj;
        }

        private static void SavePlayerPrefab(GameObject playerObj)
        {
            if (!Directory.Exists(PREFAB_DIR))
            {
                Directory.CreateDirectory(PREFAB_DIR);
            }

            try
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(playerObj, PREFAB_PATH, InteractionMode.AutomatedAction);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerSetupUtility] Aviso ao salvar Prefab: {ex.Message}");
            }
        }
    }
}
