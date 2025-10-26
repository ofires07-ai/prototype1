using UnityEngine;
using TMPro; // UI 연결을 위해 필요
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // 1. 싱글톤 패턴
    public static GameManager Instance;

    // 2. 게임 스탯 및 자원
    public int currentGold = 500;
    public int maxBaseHealth = 10;
    private int _currentBaseHealth;
    
    // 3. UI 연결
    [Header("Unity UI 연결")]
    public TextMeshProUGUI hpText; 
    public TextMeshProUGUI resourceText;
    public Slider baseHPSlider;
    
    
    // 4. 시스템 참조 (다른 매니저들과의 연결)
    [HideInInspector] public List<Enemy> activeEnemies = new List<Enemy>(); 
    public SpawnManager spawnManager; 

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
        UpdateResourceUI();
        UpdateHPUI();
        // TODO: 게임 시작 시 웨이브를 자동으로 시작할지, 버튼 클릭으로 시작할지 결정하여 주석을 풀어주세요.
        // spawnManager.StartWave(); 
    }

    // --- 자원 관리 --- (완료)
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateResourceUI();
    }

    public bool TrySpendGold(int cost)
    {
        if (currentGold >= cost)
        {
            currentGold -= cost;
            UpdateResourceUI();
            return true;
        }
        Debug.Log("골드가 부족합니다! (필요: " + cost + ", 현재: " + currentGold + ")");
        return false;
    }

    // --- 기지 HP 관리 ---
    public void TakeDamage(int damage)
    {
        _currentBaseHealth -= damage;
        UpdateHPUI();

        if (_currentBaseHealth <= 0)
        {
            _currentBaseHealth = 0;
            
            Time.timeScale = 0; 
            
            Debug.Log("게임 오버!");
        }
    }

    // --- UI 업데이트 --- (완료)
    private void UpdateResourceUI()
    {
        if (resourceText != null)
        {
            resourceText.text = "resource : " + currentGold;
        }
    }

    private void UpdateHPUI()
    {
        if (hpText != null)
        {
            hpText.text = "HP : " + _currentBaseHealth + " / " + maxBaseHealth;
        }
        if (baseHPSlider != null)
        {
            // 슬라이더의 최대값을 기지 최대 HP로 설정
            baseHPSlider.maxValue = maxBaseHealth;
            // 슬라이더의 현재 값을 현재 HP로 설정 (그래픽이 채워지는 부분)
            baseHPSlider.value = _currentBaseHealth; 
        }
    }
}