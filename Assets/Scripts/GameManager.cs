using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// EnemySpawn 클래스는 SpawnManager.cs 파일에 정의되어 있으므로, 
// 이 파일에서는 SpawnManager를 통해 간접적으로 사용합니다.

public class GameManager : MonoBehaviour
{
    // 1. 싱글톤 패턴
    public static GameManager Instance;

    // 2. 게임 스탯 및 자원 (기존 필드 유지)
    public int maxBaseHealth = 10;
    private int _currentBaseHealth;
    
    [Header("다중 자원 관리")]
    public int resourceTier1 = 50; 
    public int resourceTier2 = 50;
    public int resourceTier3 = 50;
    public int resourceTier4 = 50;

    // 3. UI 연결 (기존 필드 유지)
    [Header("Unity UI 연결")]
    public TextMeshProUGUI hpText; 
    public Slider baseHPSlider;
    
    public TextMeshProUGUI resource1Text; 
    public TextMeshProUGUI resource2Text; 
    public TextMeshProUGUI resource3Text; 
    public TextMeshProUGUI resource4Text; 
    
    // 4. 게임 상태 정의
    public enum GameState
    {
        Preparing, // 웨이브 준비 중 (타이머)
        WaveInProgress // 웨이브 진행 중 (몬스터 카운트)
    }
    private GameState _currentState;

    // 5. 웨이브 및 타이머 관리
    [Header("Wave & Timer")]
    // 첫 웨이브에만 적용될 초기 대기 시간 (2분 = 120초)
    public float initialTimeBetweenWaves = 120f; 
    
    private float _waveCountdown;
    private int _currentWaveIndex = 0;
    private bool _isFirstWave = true; // 첫 웨이브인지 추적하는 변수

    // 6. Wave UI 참조 (Inspector에서 연결 필요)
    [Header("Wave UI References")]
    public GameObject waveInfoPanel; // WaveStatusText/TimerText를 담는 부모 패널 (배경)
    public TextMeshProUGUI timerText; // 타이머 텍스트
    public TextMeshProUGUI waveStatusText; // "Wave 1" / "BossWave" 텍스트
    
    // **새 필드** 몬스터 카운트 UI 동적 생성을 위한 부모 오브젝트 및 프리팹
    public GameObject monsterCountPanel; // 몬스터 카운트 UI의 부모 (Horizontal Layout Group 필요)
    public GameObject monsterCountItemPrefab; // 몬스터 아이콘 + 텍스트 프리팹 (Inspector에서 연결)

    // 몬스터 ID별로 동적으로 생성된 UI 아이템을 추적
    private Dictionary<string, TextMeshProUGUI> _monsterCountUIs = new Dictionary<string, TextMeshProUGUI>();


    // 7. 시스템 참조
    public SpawnManager spawnManager; // Inspector에서 연결

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _currentBaseHealth = maxBaseHealth;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 초기 UI 업데이트
        UpdateResourceUI();
        UpdateHPUI();

        // 게임 시작 설정: 첫 웨이브는 120초부터 시작
        _waveCountdown = initialTimeBetweenWaves;
        _currentState = GameState.Preparing;
        ShowPreparationUI();
    }

    void Update()
    {
        if (_currentState == GameState.Preparing)
        {
            _waveCountdown -= Time.deltaTime;
            
            // ✅ 수정: 타이머가 0 이하인지 가장 먼저 체크하고, 0으로 고정 후 웨이브 시작
            if (_waveCountdown <= 0f)
            {
                _waveCountdown = 0f; // 0으로 고정하여 텍스트가 마이너스 표시되는 것을 방지

                // 타이머 종료 -> 웨이브 시작 로직 실행
                _currentState = GameState.WaveInProgress;
                ShowWaveUI();
                
                if (_currentWaveIndex < spawnManager.waves.Count) 
                {
                    spawnManager.StartWave(_currentWaveIndex);
                }
                else
                {
                    Debug.Log("모든 웨이브 완료!");
                }
            }

            // 텍스트 포맷팅은 웨이브 시작 로직 이후에 위치
            int minutes = Mathf.FloorToInt(_waveCountdown / 60f);
            int seconds = Mathf.FloorToInt(_waveCountdown % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}"; 
        }
    }

    // --- UI 전환 로직 ---
    private void ShowPreparationUI()
    {
        // 타이머 UI 활성화
        waveInfoPanel.SetActive(true);
        timerText.gameObject.SetActive(true);
        waveStatusText.gameObject.SetActive(false);
        
        // 몬스터 카운트 UI 비활성화/정리
        monsterCountPanel.SetActive(false);
        ClearMonsterCountUIs();
    }

    private void ShowWaveUI()
    {
        // 웨이브 상태 UI 활성화
        waveInfoPanel.SetActive(true);
        timerText.gameObject.SetActive(false);
        waveStatusText.gameObject.SetActive(true);
        
        // 몬스터 카운트 패널 활성화
        monsterCountPanel.SetActive(true); 
    }
    
    // 이전에 생성된 모든 몬스터 카운트 UI 요소를 삭제
    private void ClearMonsterCountUIs()
    {
        foreach (var uiText in _monsterCountUIs.Values)
        {
            if (uiText != null && uiText.transform.parent != null)
            {
                Destroy(uiText.transform.parent.gameObject);
            }
        }
        _monsterCountUIs.Clear();
    }

    // --- SpawnManager가 호출할 함수들 ---
    
    public void UpdateWaveStatus(string status)
    {
        waveStatusText.text = status;
    }

    // **신규**: 웨이브 시작 시 모든 몬스터 타입의 UI를 동적으로 생성
    // (EnemySpawn은 SpawnManager.cs 파일에 정의된 Wave 클래스 내부 구조입니다.)
    public void UpdateMonsterTypesUI(List<EnemySpawn> enemySpawns)
    {
        ClearMonsterCountUIs();
        
        foreach (var spawn in enemySpawns)
        {
            if (monsterCountItemPrefab == null)
            {
                Debug.LogError("MonsterCountItemPrefab is not assigned in GameManager Inspector!");
                continue;
            }

            // 1. 프리팹 생성 (monsterCountPanel의 자식으로)
            GameObject item = Instantiate(monsterCountItemPrefab, monsterCountPanel.transform);
            
            // 2. 이미지(아이콘) 설정
            Image icon = item.GetComponentInChildren<Image>();
            if (icon != null)
            {
                icon.sprite = spawn.uiIcon; 
            }
            
            // 3. 텍스트 설정
            TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                // 초기 수량 표시: (X [Count])
                text.text = $"X {spawn.count}";
                _monsterCountUIs.Add(spawn.enemyID, text); // 딕셔너리에 저장
            }
        }
    }

    // 3. **신규**: 특정 몬스터 타입의 남은 개수만 업데이트 (몬스터 사망 시 호출)
    public void UpdateSingleMonsterCount(string enemyID, int remaining)
    {
        if (_monsterCountUIs.ContainsKey(enemyID))
        {
            _monsterCountUIs[enemyID].text = $"X {remaining}";
        }
    }

    // 웨이브 클리어 시 호출
    public void OnWaveCleared()
    {
        Debug.Log("Wave " + _currentWaveIndex + " Cleared!");
        _currentWaveIndex++; 
        
        // --- 수정된 로직: 첫 웨이브 이후에는 무조건 0초 대기 ---
        _waveCountdown = 0f; 
        _isFirstWave = false; // 첫 웨이브가 끝났음을 표시 (이후 로직에 영향 없음)
        // --- 끝 ---

        _currentState = GameState.Preparing; 
        ShowPreparationUI();
    }
    
    // --- 자원 관리 ---
    // (기존 자원 관리 로직 유지)
    public void AddResource(int tier, int amount)
    {
        switch (tier)
        {
            case 1: resourceTier1 += amount; break;
            case 2: resourceTier2 += amount; break;
            case 3: resourceTier3 += amount; break;
            case 4: resourceTier4 += amount; break;
            default: Debug.LogWarning("잘못된 자원 등급(" + tier + ")입니다."); break;
        }
        UpdateResourceUI();
    }

    public bool TrySpendMultipleResources(int[] costs)
    {
        // 1. 자원 부족 검사
        if (resourceTier1 < costs[0] || resourceTier2 < costs[1] || 
            resourceTier3 < costs[2] || resourceTier4 < costs[3])
        {
            Debug.Log("자원이 부족하여 유닛을 생성할 수 없습니다.");
            return false;
        }

        // 2. 자원 소모
        resourceTier1 -= costs[0];
        resourceTier2 -= costs[1];
        resourceTier3 -= costs[2];
        resourceTier4 -= costs[3];
        
        UpdateResourceUI();
        return true;
    }

    // --- 기지 HP 관리 ---
    public void TakeDamage(int damage)
    {
        _currentBaseHealth -= damage;
        UpdateHPUI();

        if (_currentBaseHealth <= 0)
        {
            _currentBaseHealth = 0;
            Time.timeScale = 0; // 게임 오버
            Debug.Log("게임 오버!");
        }
    }

    // --- UI 업데이트 ---
    private void UpdateResourceUI()
    {
        if (resource1Text != null) resource1Text.text = "X " + resourceTier1;
        if (resource2Text != null) resource2Text.text = "X " + resourceTier2;
        if (resource3Text != null) resource3Text.text = "X " + resourceTier3;
        if (resource4Text != null) resource4Text.text = "X " + resourceTier4;
    }

    private void UpdateHPUI()
    {
        // 1. 텍스트 업데이트
        if (hpText != null)
        {
            hpText.text = "HP : " + _currentBaseHealth + " / " + maxBaseHealth;
        }

        // 2. 슬라이더 업데이트
        if (baseHPSlider != null)
        {
            baseHPSlider.maxValue = maxBaseHealth;
            baseHPSlider.value = _currentBaseHealth; 
        }
    }
}
