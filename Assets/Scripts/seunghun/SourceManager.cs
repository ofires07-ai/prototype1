using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Tier1, // 인덱스 0
    Tier2, // 인덱스 1
    Tier3, // 인덱스 2
    Tier4, // 인덱스 3
    Special
}

public class SourceManager : MonoBehaviour
{
    private struct MiningJob
    {
        public PickUnit Miner;         // "누가" (일꾼)
        public MineableResource Resource;  // "무엇을" (자원)
        
        // [추가] 15.5f 중 15만 정산되고 남은 0.5f (float)를 저장합니다.
        public float miningRemainder;
    }
    // [중요] 현재 활발하게 채굴 중인 자원들만 담는 리스트
    private List<MiningJob> activeJobs = new List<MiningJob>();
    
    // 자원 총량을 관리할 인벤토리 (인스펙터에서 연결)
    public InventoryManager inventoryManager;

    [Header("채굴 설정")]
    public float collectionInterval = 1.0f; // 1초마다 수집
    private float tickTimer = 0f;

    void Update()
    {
        // 1. 타이머
        tickTimer += Time.deltaTime;

        // 2. 수집 주기 도달
        if (tickTimer >= collectionInterval)
        {
            tickTimer -= collectionInterval;
            CollectResources();
        }
    }

    private void CollectResources()
    {   // (역순 for 루프)
        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            MiningJob job = activeJobs[i];
            
            if (job.Miner == null || job.Resource == null)
            {
                activeJobs.RemoveAt(i);
                continue;
            }

            // 1. [핵심] 유닛에게 "이번 틱 '작업 원장' 줘!"라고 한 번만 묻습니다.
            MiningTickResult result = job.Miner.GetMiningTickResult();

            // 2. 캘 양을 '원장'에서 가져옵니다. (예: 15.5f)
            float amountThisTick = result.Amount;
            // 캘 타입도 '원장'에서 가져옵니다. (예: ResourceType.Special)
            ResourceType typeToMine = result.Type;
            
            // 3. 특수 자원 '노드'의 용량 체크 (이 로직은 그대로 유지)
            bool depletedThisTick = false;
            if (job.Resource is SpecialSource specialSource)
            {
                // [수정] 'amountThisTick' (15.5f)과 'remaining' 중 더 작은 값을 선택
                float finalAmountToMine = Mathf.Min(amountThisTick, specialSource.remaining);
                specialSource.remaining -= finalAmountToMine;
                
                if (specialSource.remaining <= 0) depletedThisTick = true;
                
                amountThisTick = finalAmountToMine; // 캘 양을 (줄어들었다면) 갱신
            }

            // 4. [잔액 합산 로직] (소수점 저축)
            float totalToProcess = amountThisTick + job.miningRemainder;
            int amountToAdd = Mathf.FloorToInt(totalToProcess);
            job.miningRemainder = totalToProcess - amountToAdd;
            activeJobs[i] = job; // 잔액 덮어쓰기

            // 5. 인벤토리에 '정산액'과 '정산 타입' 추가
            if (amountToAdd > 0)
            {
                Debug.Log(typeToMine + ": " + amountToAdd);
                inventoryManager.AddResource(typeToMine, amountToAdd);
            }

            // 6. 자원 노드 고갈 시 작업 리스트에서 제거
            if (depletedThisTick)
            {
                job.Resource.StopMining();
                activeJobs.RemoveAt(i);
                Destroy(job.Resource.gameObject);
            }
        }
    }
    
    // [수정] PickUnit unit 인자를 받습니다.
    public void TryActivateSource(MineableResource targetSource, PickUnit unit)
    {
        // 1. 유닛이 이미 다른 작업을 하고 있는지 확인 (중복 작업 방지)
        if (activeJobs.Any(job => job.Miner == unit))
        {
            Debug.Log(unit.name + "은(는) 이미 다른 작업을 하고 있습니다.");
            return;
        }

        // 2. 규칙 검사 (부모 무시 능력 또는 채굴 가능)
        bool canIgnoreRules = unit.CanIgnoreParentRule();
        if (canIgnoreRules || targetSource.CanStartMining())
        {
            if (canIgnoreRules && !targetSource.CanStartMining())
            {
                Debug.Log(unit.name + "이(가) 규칙을 무시하고 " + targetSource.name + " 채굴을 시작합니다!");
            }
            
            // 3. 자원 상태 변경 (isMining = true)
            targetSource.StartMining();
            
            // 4. [핵심] "작업 일지(MiningJob)"를 생성하여 리스트에 추가
            activeJobs.Add(new MiningJob { Miner = unit, Resource = targetSource, miningRemainder = 0f });
            Debug.Log(unit.name + "이(가) " + targetSource.name + " 작업을 시작합니다!");
        }
        else
        {
            // 유닛이 규칙 무시 능력도 없고, 자원도 잠겨있음
            Debug.LogWarning(targetSource.name + " 자원은 현재 채굴 불가능합니다 (부모 미활성).");
        }
    }
    
    // [수정] MineableResource 대신 PickUnit unit을 받습니다.
    public void DeactivateSource(PickUnit unit)
    {
        if (unit == null) return;
        
        // 1. "이 유닛(unit)이 Miner로 등록된 '작업(Job)'을 찾습니다."
        MiningJob jobToStop = activeJobs.FirstOrDefault(job => job.Miner == unit);

        // 2. (jobToStop.Miner != null)는 'jobToStop'을 찾았다는 의미입니다.
        if (jobToStop.Miner != null) 
        {
            // 3. 작업이 참조하던 자원의 상태를 '채굴 중 아님'으로 변경
            if (jobToStop.Resource != null)
            {
                jobToStop.Resource.StopMining();
            }

            // 4. 활성 작업 리스트에서 이 "작업"을 제거
            activeJobs.Remove(jobToStop);
            Debug.Log(unit.name + "의 " + jobToStop.Resource.name + " 작업을 중지합니다.");
        }
    }
}
