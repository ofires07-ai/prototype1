using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random; // List<>를 사용하기 위해 추가

public class Ability_RandomMiner : CrimerAbility
{
    public override string AbilityName 
    {
        get { return "RandomMining"; } 
    }
    [Header("가중치 랜덤 채굴 설정")]
    [Tooltip("Tier1, Tier2, Tier3, Tier4가 나올 상대적 확률 (Special은 제외)")]
    // 💡 [50, 30, 15, 5]로 설정하면 50%, 30%, 15%, 5% 확률이 됩니다.
    public List<float> resourceWeights = new List<float> { 50f, 30f, 15f, 5f };

    private float totalWeight; // 가중치의 총합 (예: 100f)
    private bool weightsInitialized = false;

    // ApplyAbility: 이 능력이 활성화될 때(유닛 생성 시) 
    // 가중치 총합을 "미리 한 번만" 계산합니다.
    public override void ApplyAbility()
    {
        InitializeWeights();
    }

    private void InitializeWeights()
    {
        if (weightsInitialized) return;

        // ResourceType enum에서 'Special'을 제외한 개수 (Tier1~4 = 4개)
        int resourceTypeCount = System.Enum.GetValues(typeof(ResourceType)).Length - 1;

        if (resourceWeights.Count != resourceTypeCount)
        {
            Debug.LogError( "'resourceWeights' 리스트의 개수(" + resourceWeights.Count + ")가 " +
                           "ResourceType의 개수(" + resourceTypeCount + ")와 일치하지 않습니다!");
            // 비상시, 균등 확률로 대체
            resourceWeights = new List<float>();
            for(int i=0; i<resourceTypeCount; i++) { resourceWeights.Add(1f); }
        }

        totalWeight = 0f;
        foreach (float weight in resourceWeights)
        {
            // 0 미만의 가중치는 허용하지 않음
            if(weight > 0) totalWeight += weight;
        }
        
        Debug.Log("랜덤채굴 활성화! 가중치 총합: " + totalWeight);
        weightsInitialized = true;
    }

    // [핵심] 가중치 랜덤 로직
    public override MiningTickResult ProcessMiningTick(MineableResource node)
    {
        // (혹시 ApplyAbility가 실행 안됐을 경우를 대비한 안전장치)
        if (!weightsInitialized) InitializeWeights();
        
        // 1. 0 ~ 100 (totalWeight) 사이의 랜덤한 float 값을 뽑습니다.
        float roll = Random.Range(0f, totalWeight);
        
        ResourceType selectedType = ResourceType.Tier1; // 기본값(혹시 모를 에러 대비)

        // 2. 가중치를 순회하며 '당첨'을 확인합니다.
        for (int i = 0; i < resourceWeights.Count; i++)
        {
            // 3. 현재 가중치가 0 이하면 건너뜁니다.
            if (resourceWeights[i] <= 0) continue;

            // 4. (예: 72 < 50 ?)
            if (roll < resourceWeights[i])
            {
                // 당첨! (인덱스 'i'가 ResourceType의 0, 1, 2, 3과 일치)
                selectedType = (ResourceType)i;
                break; // 당첨됐으니 루프 중단
            }
            
            // 5. (예: 72 - 50 = 22)
            // 당첨이 아니면, 'roll' 값에서 현재 가중치를 빼고 다음 루프로 넘어갑니다.
            roll -= resourceWeights[i];
        }

        // 6. 최종 결과를 반환합니다.
        return new MiningTickResult
        {
            Type = selectedType,         // 가중치로 뽑힌 '타입'
            Amount = node.amountPerTick  // 양은 '그대로'
        };
    }
}