using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpaceShip : MonoBehaviour
{
    [Header("기지 설정")]
    [SerializeField] 
    public int maxHp = 10;
    public int currentHp;

    [Header("상태")]
    public bool isDestroyed = false;
    public CrimerManager crimerManager; 
    [Header("죄수 스폰 설정")]
    [Tooltip("추첨에서 뽑힌 죄수 5명의 프리팹 리스트, init()을 통한 초기화 필수")]
    public List<PickUnit> selectedPrisonerPrefabs; // ⬅️ (추첨 시스템이 이 리스트를 채워야 함)
    [Tooltip("죄수들이 '생성'될 위치 (우주선 문 앞)")]
    private Transform spaceshipSpawnPoint; 
    [Tooltip("죄수들이 '이동할' 최종 도착 지점들 (5개)")]
    public List<Transform> rallyPoints; 
    
    // ✅ [추가] 스폰 완료를 알리는 이벤트 정의
    // 'public static'으로 만들어서 어디서든 쉽게 접근하게 합니다.
    public static event Action OnAllPrisonersSpawned;
    
    void Start()
    {
        currentHp = maxHp;
        Debug.Log($"[Base] 우주선 배치 완료! 체력: {currentHp}");
        GameManager.Instance.UpdateHPUI(currentHp, maxHp);
        GameObject parentObj = GameObject.Find("SpaceShip");
        if (parentObj != null)
        {
            foreach (Transform child in parentObj.transform)
            {
                Debug.Log("rallyPoint 추가!");
                rallyPoints.Add(child);
            }
        }
    }
    
    public void Init()
    {
        if (crimerManager == null)
        {
            crimerManager = GetComponent<CrimerManager>();
        }
        
        selectedPrisonerPrefabs = crimerManager.GetCurrentDraft();
    }
    
    public void SpawnAndMovePrisoners()
    {
        if (selectedPrisonerPrefabs == null || selectedPrisonerPrefabs.Count == 0)
        {
            Debug.LogError("스폰할 죄수가 없습니다!");
            return;
        }

        for (int i = 0; i < selectedPrisonerPrefabs.Count; i++)
        {
            // [핵심 수정]
            // 1. 유닛을 스폰합니다.
            // Instantiate는 'PickUnit' 프리팹을 넣으면,
            // 생성된 오브젝트의 'PickUnit' 컴포넌트를 "바로" 반환해줍니다.
            PickUnit pickUnitScript = Instantiate(
                selectedPrisonerPrefabs[i], // ⬅️ PickUnit 프리팹
                rallyPoints[i].position, 
                rallyPoints[i].rotation
            );
            
            // ✅ [추가] 반복문이 다 끝나면(스폰 완료) 이벤트 발송!
            Debug.Log("[SpaceShip] 모든 죄수 스폰 완료! 이벤트를 보냅니다.");
            OnAllPrisonersSpawned?.Invoke(); // 구독자가 있을 때만 실행
/*  !!!!!!!!!!!!!!!!!!!!나중에 구현할 코드임. 지우기 금지!!!!!!!!!!!!!!!!
            // 2. [수정] GetComponent가 필요 없어졌습니다.
            if (pickUnitScript == null)
            {
                Debug.LogError(selectedPrisonerPrefabs[i].name + " 스폰에 실패했습니다.");
                continue; 
            }

            // 3. 도착 지점 할당
            int spotIndex = i % rallyPoints.Count;
            Vector3 destination = rallyPoints[spotIndex].position;

            // 4. 이동 명령
            pickUnitScript.MoveToPosition(destination);
            */
        } 
    }
    
    /// <summary>
    /// 적(또는 적의 히트박스)이 호출하여 데미지를 입힘
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHp -= damage;
        Debug.Log($"[Base] 우주선 피격! 남은 체력: {currentHp}");
        GameManager.Instance.UpdateHPUI(currentHp, maxHp);
        // (선택) 피격 효과음이나 파티클 재생
        // ...

        if (currentHp <= 0)
        {
            DestroyBase();
        }
    }

    void DestroyBase()
    {
        isDestroyed = true;
        currentHp = 0;
        Debug.Log("[Base] 🚨 우주선 파괴됨! GAME OVER 🚨");
        
        // 여기서 게임 매니저를 불러 게임 오버 처리를 하면 됩니다.
        // 예: GameManager.Instance.GameOver();
        
        // 우주선 터지는 효과 (선택)
        Destroy(gameObject); 
    }
    
    // 외부에서 최대 체력을 증가시키는 메서드
    public void IncreaseMaxHP(int amount)
    {
        maxHp += amount;
        
        // (선택 사항) 최대 체력이 늘어난 만큼 현재 체력도 회복시켜 줄 것인가?
        // 만약 최대 체력만 늘리고 현재 체력은 그대로 두려면 아래 줄 주석 처리
        currentHp += amount; 

        // UI 업데이트 (현재 체력이 maxHp를 넘지 않도록 방어 코드 추가)
        currentHp = Mathf.Min(currentHp, maxHp);
        GameManager.Instance.UpdateHPUI(currentHp, maxHp);

        Debug.Log($"[Base] 최대 체력 {amount} 증가! (Max: {maxHp}, Current: {currentHp})");
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
    
           EnemyMeleeHitbox enemyHitbox = collision.GetComponent<EnemyMeleeHitbox>();
    
        if (enemyHitbox != null)
        {
        // 1. 적군 히트박스(EnemyMeleeHitbox)와 충돌했는지 확인되면
        
        // 2. 데미지를 입고 함수를 종료합니다.
        // (AoE 방지/파괴 로직은 EnemyMeleeHitbox 스크립트에서 처리됩니다.)
        TakeDamage(enemyHitbox.damage);
        return; 
        }
        }
  
}
