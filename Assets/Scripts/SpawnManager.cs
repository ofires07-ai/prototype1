using UnityEngine;
using System.Collections;
using TMPro;

public class SpawnManager : MonoBehaviour
{
    // --- 웨이브 상태를 정의하는 Enum ---
    public enum WaveState
    {
        BUILD_PHASE,    // 건설 단계 (2분 카운트다운)
        WARNING_PHASE,  // 경고 단계 (짧은 카운트다운)
        FIGHT_PHASE     // 전투 단계 (적 스폰 및 웨이브 진행)
    }

    // 웨이브 데이터를 Scriptable Object로 만들어 여기에 연결합니다.
    [Header("웨이브 설정")]
    public WaveData currentWaveData; // Scriptable Object로 정의될 데이터
    public Transform spawnPoint; // 적이 생성될 시작 지점
    
    // TODO: 인스펙터에서 UI Text 컴포넌트를 여기에 연결하세요.
    public TextMeshProUGUI timerText; // 타이머 UI 연결

    // 상태 관리 변수
    private WaveState _currentState = WaveState.BUILD_PHASE;
    private float _waveTimer = 120f; // 초기 건설 시간 2분 (120초)
    private readonly float _warningTime = 10f; // Warning 단계 시간 (10초)
    private bool _isGameRunning = true; // 게임 실행 상태 관리

    // 스폰 및 전투 관련 변수
    private int _enemiesRemainingToSpawn;
    private float _timeBetweenSpawns;
    private Coroutine _spawnCoroutine;

    void Start()
    {
        // 첫 시작을 건설 단계로 설정
        _currentState = WaveState.BUILD_PHASE;
        _waveTimer = 120f; 
        
        // GameManager에서 이 스크립트의 참조를 설정해야 함 (이미 하셨을 것으로 가정)
    }

    void Update()
    {
        if (!_isGameRunning) return; 

        switch (_currentState)
        {
            case WaveState.BUILD_PHASE:
                // 1. 타이머 감소
                _waveTimer -= Time.deltaTime;
                
                // 2. UI 업데이트
                int minutes = Mathf.FloorToInt(_waveTimer / 60F);
                int seconds = Mathf.FloorToInt(_waveTimer % 60F);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

                // 3. 시간 종료 시 상태 전환
                if (_waveTimer <= 0)
                {
                    _currentState = WaveState.WARNING_PHASE;
                    _waveTimer = _warningTime; // 경고 타이머 시간 설정
                    timerText.text = "WARNING"; // UI 텍스트 전환
                }
                break;

            case WaveState.WARNING_PHASE:
                _waveTimer -= Time.deltaTime;
                
                // 텍스트는 "WARNING"으로 유지

                if (_waveTimer <= 0)
                {
                    // 전투 단계로 전환 및 웨이브 시작
                    _currentState = WaveState.FIGHT_PHASE;
                    timerText.text = "전투 시작"; 
                    
                    // 실제 웨이브 시작 함수 호출
                    StartWave(); 
                }
                break;

            case WaveState.FIGHT_PHASE:
                // 몬스터가 모두 죽을 때까지 "전투 시작" 상태를 유지하거나,
                // 필요하다면 "전투 중" 텍스트로 전환할 수 있습니다.
                // timerText.text = "전투 중";
                break;
        }
    }

    public void StartWave()
    {
        // TODO: 실제로는 Scriptable Object에서 로드
        _enemiesRemainingToSpawn = 10; 
        _timeBetweenSpawns = 1.0f;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        _spawnCoroutine = StartCoroutine(SpawnEnemiesRoutine());
    }

    IEnumerator SpawnEnemiesRoutine()
    {
        // TODO: GameManager 상태를 'Playing'으로 변경하는 로직 추가

        while (_enemiesRemainingToSpawn > 0)
        {
            SpawnEnemy();
            _enemiesRemainingToSpawn--;
            yield return new WaitForSeconds(_timeBetweenSpawns);
        }
        // 이 시점에서 스폰은 끝났지만, 모든 적이 죽을 때까지 기다려야 합니다.
    }

    void SpawnEnemy()
    {
        // **팀원이 구현할 Enemy 프리팹을 인스턴스화하는 부분입니다.**
        // GameObject newEnemy = Instantiate(currentWaveData.enemyPrefab, spawnPoint.position, Quaternion.identity);
        // newEnemy.GetComponent<Enemy>().Initialize(/* ...필요한 데이터... */); 
        Debug.Log("적 스폰 요청됨. (프리팹 연결 필요)");
    }

    public void EnemyDied(Enemy enemy)
    {
        // 적이 죽었을 때 GameManager의 activeEnemies 목록에서 제거하고 웨이브 종료 체크
        GameManager.Instance.activeEnemies.Remove(enemy);
        
        // 스폰이 완료되었고, 맵에 남은 적이 0마리일 때 웨이브 종료
        if (_enemiesRemainingToSpawn <= 0 && GameManager.Instance.activeEnemies.Count == 0)
        {
            // TODO: 웨이브 종료 보상 지급 및 다음 건설 단계(BUILD_PHASE)로 전환하는 로직 호출
            Debug.Log("웨이브 완료!");
            
            // 다음 건설 단계로 전환 예시:
            // _currentState = WaveState.BUILD_PHASE;
            // _waveTimer = 120f;
        }
    }
}