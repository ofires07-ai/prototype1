using UnityEngine;

public class CrimerManager : MonoBehaviour
{
   public Camera mainCamera;
    
    // [중요] 현재 유저가 선택한 단 하나의 유닛
    private PickUnit selectedUnit;

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
        // [핵심] "선택된 유닛"이 있을 때만 명령을 처리
        if (selectedUnit == null)
        {
            return; // 선택된 유닛이 없으면 아무것도 안 함
        }

        // --- (이 로직은 PickUnit의 HandleRightClick과 동일합니다) ---

        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            // 1. 광물을 우클릭했다면?
            if (hit.collider.TryGetComponent<Source>(out Source source))
            {
                // "선택된 유닛"에게 광물 채굴 명령
                selectedUnit.SetTargetSource(source);
            }
            // 2. 땅을 우클릭했다면?
            else
            {
                // "선택된 유닛"에게 위치 이동 명령
                selectedUnit.MoveToPosition(mouseWorldPos);
            }
        }
        // 3. 허공을 우클릭했다면?
        else
        {
            selectedUnit.MoveToPosition(mouseWorldPos);
        }
    }
}
