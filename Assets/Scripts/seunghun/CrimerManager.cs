using System.Collections.Generic;
using UnityEngine;

public class CrimerManager : MonoBehaviour
{
   public Camera mainCamera;
   public List<PickUnit> allCrimers; // (ScriptableObject 등으로 10명 정보 저장)
   public List<PickUnit> currentDraft; // (현재 뽑힌 5명)
   public int rerollChances = 3;
    
    // [중요] 현재 유저가 선택한 단 하나의 유닛
    private PickUnit selectedUnit;
    // 랜덤 생성기를 멤버 변수로 "한 번"만 생성합니다.
    private System.Random rand = new System.Random();

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        // 1. 좌클릭: 유닛 선택 / 해제
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }

        // 2. 우클릭: "선택된 유닛"에게 명령
        if (Input.GetMouseButtonDown(1))
        {
            HandleCommand();
        }
    }

    // 좌클릭 처리
    private void HandleSelection()
    {
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        // 이전에 선택된 유닛이 있었다면, 일단 "선택 해제"
        if (selectedUnit != null)
        {
            selectedUnit.Deselect();
            selectedUnit = null; // 선택 해제
        }

        // 무언가를 클릭했다면
        if (hit.collider != null)
        {
            // 클릭한 것이 PickUnit인지 확인
            if (hit.collider.TryGetComponent<PickUnit>(out PickUnit unit))
            {
                // 이 유닛을 새로운 "선택된 유닛"으로 지정
                selectedUnit = unit;
                selectedUnit.Select(); // 시각 효과 켜기
            }
        }
    }

    // 우클릭 처리
    private void HandleCommand()
    {
        if (selectedUnit == null) return;

        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            // 1. 클릭한 것이 'SpecialSource'인지 먼저 확인
            if (hit.collider.TryGetComponent<SpecialSource>(out SpecialSource specialSource))
            {
                // 특수 자원을 클릭 -> 특수 자원 채굴 명령  
                selectedUnit.SetTargetSource(specialSource);
            }
            // 2. 클릭한 것이 'Source' (일반 자원)인지 확인
            else if (hit.collider.TryGetComponent<Source>(out Source normalSource))
            {
                // [규칙 1] 일반 자원을 클릭했는데, "안 캔 특수 자원"이 붙어있다면
                if (normalSource.attachedSpecialSource != null && !normalSource.attachedSpecialSource.IsMining())
                {
                    // 일반 자원 대신 "붙어있는 특수 자원"을 채굴하도록 타겟 변경
                    selectedUnit.SetTargetSource(normalSource.attachedSpecialSource);
                }
                else
                {
                    // 특수 자원이 없거나 이미 캐고 있다면 -> 일반 자원 채굴
                    selectedUnit.SetTargetSource(normalSource);
                }
            }
            // 3. 땅을 클릭했다면
            else
            {
                selectedUnit.MoveToPosition(mouseWorldPos);
            }
        }
        else
        {
            selectedUnit.MoveToPosition(mouseWorldPos);
        }
    }
    
    public void SelectRandomCrimers()
    {
        if (rerollChances == 0)
        {
            Debug.Log("reroll 기회를 다 소진하여 현재 reroll을 할 수 없습니다.");
            return;
        }
        // 1. 원본 리스트를 "복사"합니다. (원본이 손상되지 않도록)
        List<PickUnit> availableCrimers = new List<PickUnit>(allCrimers);

        for (int i = 0; i < 5; i++)
        {
            // 2. "현재 남은" 리스트에서 랜덤 인덱스를 뽑습니다.
            // (예: 0 ~ 9, 다음엔 0 ~ 8, ...)
            int index = rand.Next(0, availableCrimers.Count);
            
            // 3. 뽑힌 죄수를 '선택된' 리스트에 추가합니다.
            currentDraft.Add(availableCrimers[index]);
            
            // 4. [핵심] "복사본" 리스트에서 뽑힌 죄수를 "제거"합니다.
            // (다음 추첨에서 또 뽑히지 않도록)
            availableCrimers.RemoveAt(index);
        }

        rerollChances -= 1; // 기회 차감
    }
    
    public List<PickUnit> GetCurrentDraft()
    {
        return currentDraft;
    }
}
