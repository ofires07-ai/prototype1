using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Dictionary의 Key를 사용하기 위해 추가

// Wave 클래스: 각 웨이브의 구성 정보를 담습니다.
[System.Serializable]
public class Wave
{
    public string waveName = "Wave 1";
    public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();
    [HideInInspector] public int totalMonsterCount; // 이 웨이브의 총 몬스터 수
}

// EnemySpawn 클래스: 웨이브 내에서 스폰될 특정 몬스터의 타입 및 수량을 정의합니다.
[System.Serializable]
public class EnemySpawn
{
    public string enemyID; // GameManager에서 카운트를 추적하는 고유 ID (예: "Boss", "Normal")
    public GameObject enemyPrefab; // 스폰할 몬스터 프리팹
    public int count; // 스폰할 개수
    public float spawnInterval = 1.0f; // 몬스터가 스폰될 간격
    public Sprite uiIcon; // GameManager의 몬스터 카운트 UI에 표시할 아이콘
}

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Wave 설정")]
    public List<Wave> waves = new List<Wave>();
    public Transform[] spawnPoints; // 몬스터가 스폰될 위치 배열 (Inspector에서 연결)

    // 현재 웨이브 진행 상태 추적
    private Wave _currentWave;
    private int _currentWaveIndex = -1;
    private Dictionary<string, int> _remainingMonsterCounts = new Dictionary<string, int>();

    private float _spawnTimer = 0f;
    private int _spawnedCountInCurrentWave = 0;
    private bool _isSpawning = false;
    private int _currentEnemySpawnIndex = 0; // 현재 스폰 중인 몬스터 타입의 인덱스

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 모든 웨이브의 총 몬스터 수를 미리 계산
            CalculateTotalMonsterCounts();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CalculateTotalMonsterCounts()
    {
        foreach (var wave in waves)
        {
            wave.totalMonsterCount = wave.enemySpawns.Sum(e => e.count);
        }
    }

    void Update()
    {
        if (_isSpawning)
        {
            // 현재 스폰할 몬스터 타입이 남아 있는지 확인
            if (_currentEnemySpawnIndex >= _currentWave.enemySpawns.Count)
            {
                // 현재 웨이브의 모든 타입 스폰이 완료됨
                _isSpawning = false;
                return;
            }

            EnemySpawn currentSpawnConfig = _currentWave.enemySpawns[_currentEnemySpawnIndex];

            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0)
            {
                // 현재 타입의 몬스터를 모두 스폰했는지 확인
                if (currentSpawnConfig.count > 0)
                {
                    // 몬스터 스폰
                    SpawnEnemy(currentSpawnConfig.enemyPrefab, currentSpawnConfig.enemyID);
                    
                    currentSpawnConfig.count--; // 스폰 카운트 감소
                    _spawnedCountInCurrentWave++;
                    
                    // 다음 스폰까지의 대기 시간 설정
                    _spawnTimer = currentSpawnConfig.spawnInterval;
                }
                else
                {
                    // 현재 타입 스폰 완료 -> 다음 타입으로 이동
                    _currentEnemySpawnIndex++;
                    _spawnTimer = 0f; // 즉시 다음 타입 스폰을 시작하도록 타이머 초기화
                }
            }
        }
        
        // 웨이브 종료 조건 검사 (모든 몬스터가 스폰되었고, 남은 몬스터가 0일 때)
        if (!_isSpawning && _spawnedCountInCurrentWave >= _currentWave.totalMonsterCount && _remainingMonsterCounts.Values.All(count => count <= 0))
        {
            // 웨이브 클리어
            GameManager.Instance.OnWaveCleared();
        }
    }

    // --- GameManager가 호출하는 메인 시작 함수 ---
    public void StartWave(int waveIndex)
    {
        if (waveIndex >= waves.Count)
        {
            GameManager.Instance.UpdateWaveStatus("Game Won!");
            return;
        }

        _currentWave = waves[waveIndex];
        _currentWaveIndex = waveIndex;
        _currentEnemySpawnIndex = 0;
        _spawnedCountInCurrentWave = 0;
        _isSpawning = true;
        _spawnTimer = 0f; // 즉시 스폰 시작

        // 1. UI 상태 업데이트
        GameManager.Instance.UpdateWaveStatus(_currentWave.waveName);
        GameManager.Instance.UpdateMonsterTypesUI(_currentWave.enemySpawns);

        // 2. 남은 몬스터 수 초기화 (딕셔너리 생성)
        _remainingMonsterCounts.Clear();
        foreach (var spawn in _currentWave.enemySpawns)
        {
            _remainingMonsterCounts.Add(spawn.enemyID, spawn.count);
        }
    }


    // --- 몬스터 스폰 로직 ---
    private void SpawnEnemy(GameObject enemyPrefab, string enemyID)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn Points not set in Spawn Manager!");
            return;
        }

        // 1. 랜덤 스폰 지점 선택
        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // 2. 몬스터 생성
        GameObject enemyObject = Instantiate(enemyPrefab, randomPoint.position, randomPoint.rotation);

        // 3. 몬스터 스크립트에 타입 ID 할당 (가장 중요한 부분: Enemy_Y로 타입 변경!)
        Enemy_Y enemyScript = enemyObject.GetComponent<Enemy_Y>();
        if (enemyScript != null)
        {
            enemyScript.enemyID = enemyID;
        }
    }

    // --- 몬스터 사망 시 호출 (Enemy_Y 스크립트에서 호출됨) ---
    public void OnMonsterDied(string enemyID)
    {
        if (_remainingMonsterCounts.ContainsKey(enemyID))
        {
            _remainingMonsterCounts[enemyID]--;

            // 1. GameManager UI 업데이트
            GameManager.Instance.UpdateSingleMonsterCount(enemyID, _remainingMonsterCounts[enemyID]);
            
            // 2. 웨이브 종료 검사는 Update()에서 처리
        }
    }
}