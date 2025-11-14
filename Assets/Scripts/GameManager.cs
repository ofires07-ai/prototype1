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

    [Header("시스템 연동")]
    public InventoryManager inventoryManager;
    public BuildSystem buildSystem;

    // 2. 게임 스탯 및 자원 (기존 필드 유지)
    public int maxBaseHealth = 10;
    public int _currentBaseHealth;
    // 현재 적용 중인 유닛 비용 할인율
    public float unitCostModifier = 1.0f;
    // 현재 적용 중인 타워 비용 할인율 (1.0 = 100% = 할인 없음)
    public float towerCostModifier = 1.0f;

    [Header("다중 자원 관리")]
    //public int resourceTier1 = 50; 
    //public int resourceTier2 = 50;
    //public int resourceTier3 = 50;
    //public int resourceTier4 = 50;
    //public int resourceTier5 = 10; // T5 (특수 자원) 추가

    // 3. UI 연결 (기존 필드 유지)
    [Header("Unity UI 연결")]
    public TextMeshProUGUI hpText;
    public Slider baseHPSlider;

    public TextMeshProUGUI resource1Text;
    public TextMeshProUGUI resource2Text;
    public TextMeshProUGUI resource3Text;
    public TextMeshProUGUI resource4Text;
    public TextMeshProUGUI specialText;

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
    // 1. GameManager UI 필드 초기화 (UpdateHPUI는 여기서 호출되어야 함)
    UpdateResourceUI(); // (새로 추가됨)
    UpdateHPUI();

    // 2. 게임 시작 설정: 첫 웨이브는 120초부터 시작
    _waveCountdown = initialTimeBetweenWaves;
    _currentState = GameState.Preparing;
    ShowPreparationUI();
    
    // ✅ 안전 장치: InventoryManager가 초기화되었는지 확인 후 UI 재갱신
    if (inventoryManager != null)
    {
        // InventoryManager의 Start() 함수가 실행될 때까지 기다릴 필요 없이,
        // GameManager가 모든 UI 연결을 마친 후 InventoryManager의 데이터로 갱신을 요청합니다.
        UpdateResourceUI(); 
    }
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
                text.text = $"{spawn.count}";
                _monsterCountUIs.Add(spawn.enemyID, text); // 딕셔너리에 저장
            }
        }
    }

    // 3. **신규**: 특정 몬스터 타입의 남은 개수만 업데이트 (몬스터 사망 시 호출)
    public void UpdateSingleMonsterCount(string enemyID, int remaining)
    {
        if (_monsterCountUIs.ContainsKey(enemyID))
        {
            _monsterCountUIs[enemyID].text = $" {remaining}";
        }
    }

    // 웨이브 클리어 시 호출
    public void OnWaveCleared()
    {
        Debug.Log("Wave " + _currentWaveIndex + " Cleared!");

        // 🔸 마지막 웨이브인지 확인
        if (spawnManager != null && _currentWaveIndex >= spawnManager.waves.Count - 1)
        {
            Debug.Log("모든 웨이브 완료! (마지막 웨이브)");
            
            // StageSceneController가 구독하고 있는 이벤트 호출
            if (spawnManager.OnAllWavesCompleted != null)
            {
                spawnManager.OnAllWavesCompleted.Invoke();
            }

            // 여기서는 더 이상 다음 웨이브를 준비하지 않습니다.
            return;
        }

        // 🔹 마지막 웨이브가 아니면, 기존처럼 다음 웨이브 준비
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
        // 1. 인덱스를 사용하여 ResourceType으로 변환 (Tier 1 = 인덱스 0)
        if (tier >= 1 && tier <= 5)
        {
            ResourceType type = (ResourceType)(tier - 1);
            
            if (inventoryManager != null)
            {
                // ✅ 핵심: InventoryManager에게 자원 추가를 위임합니다.
                inventoryManager.AddResource(type, amount);
            }
        }
        else
        {
            Debug.LogWarning($"Invalid resource tier ({tier}).");
        }

        // 주의: UpdateResourceUI()는 InventoryManager의 AddResource 내부에서 호출됩니다.
    }

    public bool TrySpendMultipleResources(int[] costs)
    {
        // 1. 자원 부족 검사 및 소모
        for (int i = 0; i < costs.Length; i++)
        {
            if (costs[i] > 0)
            {
                ResourceType type = (ResourceType)i; // 인덱스를 ResourceType으로 변환

                // InventoryManager에게 자원량을 확인하고 소모를 시도합니다.
                if (inventoryManager == null)
                {
                    Debug.LogError("InventoryManager가 연결되지 않았습니다!");
                    return false;
                }

                if (inventoryManager.GetTotalAmount(type) < costs[i])
                {
                    Debug.Log(type + " 자원이 부족하여 유닛을 생성할 수 없습니다. (필요: " + costs[i] + ")");
                    return false;
                }
            }
        }

        // 2. 검사가 모두 통과하면 실제 자원 소모 실행
        for (int i = 0; i < costs.Length; i++)
        {
            if (costs[i] > 0)
            {
                ResourceType type = (ResourceType)i;
                inventoryManager.RemoveResource(type, costs[i]); // InventoryManager에 소모 요청
            }
        }

        // UI 업데이트는 InventoryManager의 RemoveResource 내부에서 이루어집니다.
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
    public void UpdateResourceUI()
{
    if (inventoryManager == null) return;

    if (resource1Text != null) 
        resource1Text.text = "" + inventoryManager.GetTotalAmount(ResourceType.Tier1);
    if (resource2Text != null) 
        resource2Text.text = "" + inventoryManager.GetTotalAmount(ResourceType.Tier2);
    if (resource3Text != null) 
        resource3Text.text = "" + inventoryManager.GetTotalAmount(ResourceType.Tier3);
    if (resource4Text != null) 
        resource4Text.text = "" + inventoryManager.GetTotalAmount(ResourceType.Tier4);
    // 새 이름을 쓰고 싶다면:
    if (specialText != null)
        specialText.text = "" + inventoryManager.GetTotalAmount(ResourceType.Special);
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
    
    /// 유닛 할인 어빌리티가 이 함수를 호출하여 할인율을 등록합니다.
    public void RegisterUnitCostModifier(float modifier)
    {
        unitCostModifier = modifier;
        buildSystem.RefreshAllButtonUI();
    }
    
    /// 타워 할인 어빌리티가 이 함수를 호출하여 할인율을 등록합니다.
    public void RegisterTowerCostModifier(float modifier)
    {
        towerCostModifier = modifier;
        buildSystem.RefreshAllButtonUI();
    }
}
