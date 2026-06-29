using System.Collections.Generic;
using UnityEngine;
using InfiniteRunner.Core;
using InfiniteRunner.Difficulty;


namespace InfiniteRunner.Obstacle
{
    /// <summary>
    /// Tipos de obstáculo disponíveis:
    ///   Ground – cubo preto no chão  (pular ou trocar de faixa).
    ///   Tall   – cubo vermelho suspenso com vão embaixo (rolar ou trocar de faixa).
    ///   Wall   – parede branca alta  (APENAS trocar de faixa).
    /// </summary>
    public enum ObstacleType
    {
        Ground,
        Tall,
        Wall
    }

    /// <summary>
    /// Spawna obstáculos em intervalos regulares em uma das 3 faixas.
    /// A faixa é escolhida aleatoriamente mas nunca se repete duas vezes seguidas.
    /// O tipo de obstáculo também é escolhido aleatoriamente entre Ground e Tall.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Intervalo em segundos entre cada spawn de obstáculo.")]
        [SerializeField] private float spawnInterval = 3.0f;

        [Tooltip("Atraso inicial em segundos antes do primeiro obstáculo aparecer.")]
        [SerializeField] private float initialDelay = 2.0f;

        [Tooltip("Distância Z à frente do jogador onde os obstáculos aparecem.")]
        [SerializeField] private float spawnDistance = 40.0f;

        [Tooltip("Velocidade com que os obstáculos se movem em direção ao jogador (deve coincidir com a velocidade da pista).")]
        [SerializeField] private float obstacleSpeed = 15.0f;

        [Header("Obstacle Appearance")]
        [Tooltip("Tamanho do cubo de obstáculo (para ambos os tipos).")]
        [SerializeField] private Vector3 obstacleSize = new Vector3(1.5f, 1.5f, 1.5f);

        [Header("Tall Obstacle Settings")]
        [Tooltip("Altura do vão livre embaixo do obstáculo suspenso (em metros). " +
                 "O jogador em pé (1.8 m) colidirá; ao rolar (0.9 m) passará por baixo.")]
        [SerializeField] private float tallGapHeight = 1.0f;

        [Tooltip("Probabilidade (0–1) de um obstáculo suspenso ser gerado.")]
        [Range(0f, 1f)]
        [SerializeField] private float tallObstacleChance = 0.35f;

        [Header("Wall Obstacle Settings")]
        [Tooltip("Altura da parede branca (em metros). Deve ser MAIOR que pulo máximo do jogador " +
                 "(jumpHeight 2.5 m + altura do jogador 1.8 m = 4.3 m). Padrão: 5.0 m.")]
        [SerializeField] private float wallHeight = 5.0f;

        [Tooltip("Probabilidade (0–1) de uma parede ser gerada em vez do cubo terrestre.")]
        [Range(0f, 1f)]
        [SerializeField] private float wallObstacleChance = 0.25f;

        [Header("Lane Settings")]
        [Tooltip("Distância lateral entre faixas (deve coincidir com PlayerController.laneDistance).")]
        [SerializeField] private float laneDistance = 3.0f;

        private float spawnTimer;
        private float delayTimer;
        private bool delayElapsed;
        private int lastLane = -1; // -1 = sem faixa anterior

        // Referências dos obstáculos spawnados para limpeza no restart
        private List<GameObject> spawnedObstacles = new List<GameObject>();

        // Materiais criados uma vez em runtime
        private Material groundObstacleMaterial;  // Preto         – obstáculo terrestre
        private Material tallObstacleMaterial;    // Vermelho escuro – obstáculo suspenso
        private Material wallObstacleMaterial;    // Branco          – parede intransponível

        private void Start()
        {
            ResetTimers();

            // Material preto para o obstáculo terrestre
            groundObstacleMaterial = new Material(Shader.Find("Standard"));
            groundObstacleMaterial.color = Color.black;
            if (groundObstacleMaterial.HasProperty("_Glossiness")) groundObstacleMaterial.SetFloat("_Glossiness", 0.0f);
            if (groundObstacleMaterial.HasProperty("_Metallic"))   groundObstacleMaterial.SetFloat("_Metallic",   0.0f);

            // Material vermelho-escuro para o obstáculo suspenso (diferenciação visual)
            tallObstacleMaterial = new Material(Shader.Find("Standard"));
            tallObstacleMaterial.color = new Color(0.6f, 0.05f, 0.05f); // Vermelho escuro
            if (tallObstacleMaterial.HasProperty("_Glossiness")) tallObstacleMaterial.SetFloat("_Glossiness", 0.1f);
            if (tallObstacleMaterial.HasProperty("_Metallic"))   tallObstacleMaterial.SetFloat("_Metallic",   0.0f);

            // Material branco para a parede (diferenciação visual)
            wallObstacleMaterial = new Material(Shader.Find("Standard"));
            wallObstacleMaterial.color = Color.white;
            if (wallObstacleMaterial.HasProperty("_Glossiness")) wallObstacleMaterial.SetFloat("_Glossiness", 0.2f);
            if (wallObstacleMaterial.HasProperty("_Metallic"))   wallObstacleMaterial.SetFloat("_Metallic",   0.0f);

            // Inscreve no evento de restart
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted += ClearAllObstacles;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted -= ClearAllObstacles;
            }

            if (groundObstacleMaterial != null) Destroy(groundObstacleMaterial);
            if (tallObstacleMaterial   != null) Destroy(tallObstacleMaterial);
            if (wallObstacleMaterial   != null) Destroy(wallObstacleMaterial);
        }

        private void Update()
        {
            // Só spawna enquanto o jogo está rodando
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Running)
                return;

            // Aguarda o atraso inicial antes de começar a spawnar
            if (!delayElapsed)
            {
                delayTimer -= Time.deltaTime;
                if (delayTimer <= 0f)
                {
                    delayElapsed = true;
                    
                    float interval = spawnInterval;
                    if (DifficultyManager.Instance != null && DifficultyManager.Instance.CurrentSpeed > 0f)
                    {
                        interval = DifficultyManager.Instance.CurrentSpawnDistance / DifficultyManager.Instance.CurrentSpeed;
                    }
                    spawnTimer = interval;
                }
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnObstacle();
                
                float interval = spawnInterval;
                if (DifficultyManager.Instance != null && DifficultyManager.Instance.CurrentSpeed > 0f)
                {
                    interval = DifficultyManager.Instance.CurrentSpawnDistance / DifficultyManager.Instance.CurrentSpeed;
                }
                spawnTimer = interval;
            }
        }

        /// <summary>
        /// Escolhe aleatoriamente o tipo e a faixa do obstáculo e o cria.
        /// Probabilidades:
        ///   Wall  = wallObstacleChance
        ///   Tall  = tallObstacleChance  (do restante)
        ///   Ground = o resto
        /// </summary>
        private void SpawnObstacle()
        {
            float roll = Random.value;

            ObstacleType type;
            if (roll < wallObstacleChance)
                type = ObstacleType.Wall;
            else if (roll < wallObstacleChance + tallObstacleChance)
                type = ObstacleType.Tall;
            else
                type = ObstacleType.Ground;

            int lane = PickRandomLane();
            float xPos = (lane - 1) * laneDistance; // faixa 0=esq, 1=centro, 2=dir

            switch (type)
            {
                case ObstacleType.Ground: SpawnGroundObstacle(xPos); break;
                case ObstacleType.Tall:   SpawnTallObstacle(xPos);   break;
                case ObstacleType.Wall:   SpawnWallObstacle(xPos);   break;
            }

            CleanNullReferences();
        }

        /// <summary>
        /// Cria um cubo preto no chão (obstáculo terrestre clássico).
        /// O jogador precisa pular ou trocar de faixa para desviar.
        /// </summary>
        private void SpawnGroundObstacle(float xPos)
        {
            // Y = metade da altura do cubo (assenta no chão)
            float yPos = obstacleSize.y / 2f;
            Vector3 spawnPos = new Vector3(xPos, yPos, spawnDistance);

            GameObject obstacle = CreateObstacleCube(
                "Obstacle_Ground",
                spawnPos,
                obstacleSize,
                groundObstacleMaterial
            );

            spawnedObstacles.Add(obstacle);
        }

        /// <summary>
        /// Cria um cubo vermelho suspenso com vão livre embaixo.
        /// O vão (tallGapHeight) é MENOR que a altura do jogador em pé (1.8 m)
        /// mas MAIOR que a altura do jogador ao rolar (0.9 m), forçando o uso
        /// de rolamento ou troca de faixa para passar.
        /// </summary>
        private void SpawnTallObstacle(float xPos)
        {
            // Y = vão + metade da altura do cubo → a face inferior fica exatamente em tallGapHeight
            float yPos = tallGapHeight + obstacleSize.y / 2f;
            Vector3 spawnPos = new Vector3(xPos, yPos, spawnDistance);

            GameObject obstacle = CreateObstacleCube(
                "Obstacle_Tall",
                spawnPos,
                obstacleSize,
                tallObstacleMaterial
            );

            spawnedObstacles.Add(obstacle);
        }

        /// <summary>
        /// Cria uma parede branca alta que bloqueia tanto o pulo quanto o rolamento.
        /// A única forma de desviar é trocar de faixa.
        ///
        /// Cálculo de altura mínima para bloquear o pulo:
        ///   jumpHeight (2.5 m) + altura do jogador (1.8 m) = 4.3 m → usamos 5.0 m.
        /// Rolamento também é bloqueado pois a parede começa no chão (sem vão).
        /// </summary>
        private void SpawnWallObstacle(float xPos)
        {
            // Tamanho: mesma largura/profundidade do cubo padrão, altura = wallHeight
            Vector3 wallSize = new Vector3(obstacleSize.x, wallHeight, obstacleSize.z);

            // Y = metade da altura → assenta perfeitamente no chão sem nenhum vão
            float yPos = wallHeight / 2f;
            Vector3 spawnPos = new Vector3(xPos, yPos, spawnDistance);

            GameObject obstacle = CreateObstacleCube(
                "Obstacle_Wall",
                spawnPos,
                wallSize,
                wallObstacleMaterial
            );

            spawnedObstacles.Add(obstacle);
        }

        /// <summary>
        /// Método auxiliar que cria o cubo primitivo, aplica material,
        /// configura o trigger e adiciona o ObstacleController.
        /// </summary>
        private GameObject CreateObstacleCube(string objName, Vector3 position, Vector3 size, Material mat)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = objName;
            obstacle.tag  = "Obstacle";
            obstacle.transform.position   = position;
            obstacle.transform.localScale = size;

            // Aplica o material
            Renderer rend = obstacle.GetComponent<Renderer>();
            if (rend != null && mat != null)
            {
                rend.material = mat;
            }

            // Trigger – não empurra o jogador fisicamente
            BoxCollider col = obstacle.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Script de movimento e detecção de colisão
            ObstacleController controller = obstacle.AddComponent<ObstacleController>();
            float speed = obstacleSpeed;
            if (DifficultyManager.Instance != null)
            {
                speed = DifficultyManager.Instance.CurrentSpeed;
            }
            controller.moveSpeed = speed;

            return obstacle;
        }

        /// <summary>
        /// Escolhe aleatoriamente uma faixa (0, 1 ou 2) diferente da última,
        /// garantindo que o jogador sempre tenha chance de desviar.
        /// </summary>
        private int PickRandomLane()
        {
            int lane;
            if (lastLane < 0)
            {
                lane = Random.Range(0, 3);
            }
            else
            {
                lane = Random.Range(0, 2);
                if (lane >= lastLane) lane++;
            }

            lastLane = lane;
            return lane;
        }

        /// <summary>
        /// Destrói todos os obstáculos ativos e reseta os timers.
        /// Chamado quando o jogo é reiniciado.
        /// </summary>
        public void ClearAllObstacles()
        {
            foreach (GameObject obs in spawnedObstacles)
            {
                if (obs != null) Destroy(obs);
            }
            spawnedObstacles.Clear();
            lastLane = -1;
            ResetTimers();
        }

        /// <summary>
        /// Reseta os timers de atraso inicial e de spawn.
        /// </summary>
        private void ResetTimers()
        {
            delayTimer    = initialDelay;
            delayElapsed  = false;
            spawnTimer    = spawnInterval;
        }

        /// <summary>
        /// Remove referências nulas da lista (obstáculos já destruídos).
        /// </summary>
        private void CleanNullReferences()
        {
            spawnedObstacles.RemoveAll(o => o == null);
        }
    }
}
