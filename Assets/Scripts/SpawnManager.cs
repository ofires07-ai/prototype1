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

    // 🔹 웨이브 원본을 건드리지 않기 위한 작업용 카운트
    private Dictionary<string, int> _remainingMonsterCounts = new Dictionary<string, int>(); // 남은 처치 수

    // 🔹 각 EnemySpawn(타입) 별 남은 스폰 수 & 개별 타이머
    private List<int> _remainingToSpawnPerType = new List<int>(); 
    private List<float> _spawnTimersPerType = new List<float>();
    
    [Header("Enemy HP 스케일링")]
    [Tooltip("웨이브가 1 증가할 때마다 추가되는 HP")]
    public int hpIncreasePerWave = 5;

    [Header("Enemy Speed 스케일링")]
    [Tooltip("웨이브 인덱스가 1 증가할 때마다 추가되는 이동 속도 (예: 0.25)")]
    public float moveSpeedIncreasePerWave = 0.25f;

    private int _spawnedCountInCurrentWave = 0;
    private bool _isSpawning = false;

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
        if (_isSpawning && _currentWave != null)
        {
            bool anyLeftToSpawn = false;

            // 🔹 각 EnemySpawn(타입) 별로 "동시에" 스폰 진행
            for (int i = 0; i < _currentWave.enemySpawns.Count; i++)
            {
                if (_remainingToSpawnPerType[i] <= 0)
                    continue;

                anyLeftToSpawn = true;

                _spawnTimersPerType[i] -= Time.deltaTime;
                if (_spawnTimersPerType[i] <= 0f)
                {
                    var cfg = _currentWave.enemySpawns[i];

                    SpawnEnemy(cfg.enemyPrefab, cfg.enemyID, i);

                    _remainingToSpawnPerType[i]--;
                    _spawnedCountInCurrentWave++;

                    // 다음 스폰까지의 간격 재설정
                    _spawnTimersPerType[i] = cfg.spawnInterval;
                }
            }

            // 더 이상 스폰할 몬스터가 없으면 스폰 종료
            if (!anyLeftToSpawn)
            {
                _isSpawning = false;
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
            GameManager.Instance.UpdateWaveStatus("Game Won!");
            OnAllWavesCompleted?.Invoke();
            return;
        }

        _currentWave = waves[waveIndex];
        _currentWaveIndex = waveIndex;

        _spawnedCountInCurrentWave = 0;
        _isSpawning = true;

        // 새 웨이브 시작하니 클리어 알림 플래그 리셋
        _waveClearNotified = false;

        // UI
        GameManager.Instance.UpdateWaveStatus(_currentWave.waveName);
        GameManager.Instance.UpdateMonsterTypesUI(_currentWave.enemySpawns);

        // 🔹 남은 처치 수(= 원본 count)를 enemyID 기준으로 "합산"해서 초기화
        _remainingMonsterCounts.Clear();
        foreach (var s in _currentWave.enemySpawns)
        {
            if (s.count <= 0 || string.IsNullOrEmpty(s.enemyID))
                continue;

            if (!_remainingMonsterCounts.ContainsKey(s.enemyID))
                _remainingMonsterCounts[s.enemyID] = 0;

            _remainingMonsterCounts[s.enemyID] += s.count;
        }

        // 🔹 스폰 잔량 & 타이머도 초기화 (각 타입별로 따로 관리)
        _remainingToSpawnPerType = new List<int>();
        _spawnTimersPerType = new List<float>();

        foreach (var s in _currentWave.enemySpawns)
        {
            _remainingToSpawnPerType.Add(s.count);
            _spawnTimersPerType.Add(s.spawnInterval);  // 0으로 시작하면 웨이브 시작 시 바로 1마리씩 나옴
        }
    }

    //스폰
    private void SpawnEnemy(GameObject enemyPrefab, string enemyID, int enemySpawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn Points not set in Spawn Manager!");
            return;
        }

        int spawnIndex = enemySpawnIndex % spawnPoints.Length;
        Transform point = spawnPoints[spawnIndex];

        GameObject enemyObject = Instantiate(enemyPrefab, point.position, point.rotation);

        // 🔹 웨이브 인덱스 기반 HP/속도 보정값 계산
        int bonusHp = hpIncreasePerWave * _currentWaveIndex;      // HP는 그대로 쓰고 싶으면 유지, 아니면 Inspector에서 0으로
        float speedBonus = moveSpeedIncreasePerWave * _currentWaveIndex;

        // 1) 근접 유닛
        HY_EnemyUnitMovement hyEnemy = enemyObject.GetComponent<HY_EnemyUnitMovement>();
        if (hyEnemy != null)
        {
            hyEnemy.enemyID = enemyID;

            if (bonusHp != 0)
                hyEnemy.ApplyHpBonus(bonusHp);

            if (Mathf.Abs(speedBonus) > 0.0001f)
                hyEnemy.ApplyWaveSpeedBonus(speedBonus);

            return;
        }

        // 2) 원거리 유닛
        HY_Ranged_EnemyUnitMovement rangedEnemy = enemyObject.GetComponent<HY_Ranged_EnemyUnitMovement>();
        if (rangedEnemy != null)
        {
            rangedEnemy.enemyID = enemyID;

            if (bonusHp != 0)
                rangedEnemy.ApplyHpBonus(bonusHp);      // ▶ HP도 같이 쓰고 싶으면, ranged 쪽에도 ApplyHpBonus 만들어주면 됨
                                                        // 안 쓸 거면 이 줄은 빼도 됨

            if (Mathf.Abs(speedBonus) > 0.0001f)
                rangedEnemy.ApplyWaveSpeedBonus(speedBonus);
        }
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
