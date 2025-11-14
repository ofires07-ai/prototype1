using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[DefaultExecutionOrder(-100)]  // ProductionTower.Start()보다 먼저 실행되도록
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Wave 설정")]
    public List<Wave> waves = new List<Wave>();

    [Header("스폰 포인트")]
    public Transform[] spawnPoints; 
    [Tooltip("씬의 ProductionTower들을 적 스폰 포인트로 자동 사용")]
    public bool useProductionTowersAsSpawnPoints = true;
    [Tooltip("적 스폰을 SpawnManager가 전담하도록, ProductionTower를 자동 비활성화")]
    public bool takeoverProductionTowers = true;

    // 🔹 스테이지 클리어(모든 웨이브 완료) 이벤트
    public Action OnAllWavesCompleted;

    // 진행 상태
    private Wave _currentWave;
    private int _currentWaveIndex = -1;

    private int _spawnPointCursor = 0;

    // 🔹 웨이브 원본을 건드리지 않기 위한 작업용 카운트
    private Dictionary<string, int> _remainingMonsterCounts = new Dictionary<string, int>(); // 남은 처치 수
    private List<int> _remainingToSpawnPerType = new List<int>(); // 각 타입별 남은 스폰 수

    private float _spawnTimer = 0f;
    private int _spawnedCountInCurrentWave = 0;
    private bool _isSpawning = false;
    private int _currentEnemySpawnIndex = 0;

    // ✅ 이 웨이브가 끝났다는 사실을 GameManager에 이미 알렸는지 여부
    private bool _waveClearNotified = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // 1) 필요 시 ProductionTower를 스폰 포인트로 수집
            if (useProductionTowersAsSpawnPoints)
            {
                var towers = FindObjectsOfType<ProductionTower>();
                var points = new List<Transform>();
                foreach (var t in towers)
                {
                    var p = (t.spawnPoint != null) ? t.spawnPoint : t.transform;
                    points.Add(p);
                }
                if (points.Count > 0)
                    spawnPoints = points.ToArray();
            }

            // 2) 필요 시 ProductionTower 생산 비활성화 (코루틴 시작 전에 막음)
            if (takeoverProductionTowers)
            {
                foreach (var t in FindObjectsOfType<ProductionTower>())
                {
                    t.enabled = false; // Start()의 생산 코루틴 시작 차단
                }
            }

            // 총 몬스터 수 미리 계산(표시용)
            CalculateTotalMonsterCounts();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CalculateTotalMonsterCounts()
    {
        foreach (var w in waves)
            w.totalMonsterCount = w.enemySpawns.Sum(e => e.count);
    }

    void Update()
    {
        if (_isSpawning)
        {
            if (_currentEnemySpawnIndex >= _currentWave.enemySpawns.Count)
            {
                _isSpawning = false;
            }
            else
            {
                var cfg = _currentWave.enemySpawns[_currentEnemySpawnIndex];

                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    if (_remainingToSpawnPerType[_currentEnemySpawnIndex] > 0)
                    {
                        SpawnEnemy(cfg.enemyPrefab, cfg.enemyID);
                        _remainingToSpawnPerType[_currentEnemySpawnIndex]--;
                        _spawnedCountInCurrentWave++;
                        _spawnTimer = cfg.spawnInterval;
                    }
                    else
                    {
                        _currentEnemySpawnIndex++;
                        _spawnTimer = 0f;
                    }
                }
            }
        }

        // 웨이브 종료: 모두 스폰되었고, 남은 처치 수가 0
        if (_currentWave != null 
            && !_isSpawning
            && _spawnedCountInCurrentWave >= _currentWave.totalMonsterCount
            && _remainingMonsterCounts.Values.All(v => v <= 0)
            && !_waveClearNotified)   // ✅ 한 번만
        {
            _waveClearNotified = true;
            GameManager.Instance.OnWaveCleared();
        }
    }

    // --- GameManager가 호출 ---
    public void StartWave(int waveIndex)
    {
        // 🔹 모든 웨이브를 다 돌았으면 스테이지 클리어
        if (waveIndex >= waves.Count)
        {
            // 여기로 오는 로직은 이제 거의 없지만, 안전망으로 남겨둠
            GameManager.Instance.UpdateWaveStatus("Game Won!");
            OnAllWavesCompleted?.Invoke();
            return;
        }

        _currentWave = waves[waveIndex];
        _currentWaveIndex = waveIndex;

        _currentEnemySpawnIndex = 0;
        _spawnedCountInCurrentWave = 0;
        _isSpawning = true;
        _spawnTimer = 0f;

        // 새 웨이브 시작하니 클리어 알림 플래그 리셋
        _waveClearNotified = false;

        // UI
        GameManager.Instance.UpdateWaveStatus(_currentWave.waveName);
        GameManager.Instance.UpdateMonsterTypesUI(_currentWave.enemySpawns);

        // 🔹 남은 처치 수(= 원본 count)를 복사해서 초기화
        _remainingMonsterCounts.Clear();
        foreach (var s in _currentWave.enemySpawns)
            _remainingMonsterCounts[s.enemyID] = s.count;

        // 🔹 스폰 잔량도 별도 복사(원본 불변)
        _remainingToSpawnPerType = _currentWave.enemySpawns.Select(s => s.count).ToList();
    }

    // --- 스폰 ---
    private void SpawnEnemy(GameObject enemyPrefab, string enemyID)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn Points not set in Spawn Manager!");
            return;
        }

        Transform point = spawnPoints[_spawnPointCursor % spawnPoints.Length];
        _spawnPointCursor++;
        GameObject enemyObject = Instantiate(enemyPrefab, point.position, point.rotation);

        // 타입 ID 전달
        Enemy_Y enemyScript = enemyObject.GetComponent<Enemy_Y>();
        if (enemyScript != null)
            enemyScript.enemyID = enemyID;

        HY_EnemyUnitMovement hyEnemy = enemyObject.GetComponent<HY_EnemyUnitMovement>();
        if (hyEnemy != null)
            hyEnemy.enemyID = enemyID;
    }

    // --- 몬스터 사망 콜백 ---
    public void OnMonsterDied(string enemyID)
    {
        if (_remainingMonsterCounts.ContainsKey(enemyID))
        {
            _remainingMonsterCounts[enemyID]--;
            GameManager.Instance.UpdateSingleMonsterCount(enemyID, _remainingMonsterCounts[enemyID]);
        }
    }
}
