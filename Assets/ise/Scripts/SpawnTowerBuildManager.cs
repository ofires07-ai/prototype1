/*
SpawnTowerBuildManager.cs
- 타워 설치 고스트 표시 및 타워 설치 관리
- EnterBuildMode(): 타워 설치 모드 진입
- ExitBuildMode(): 타워 설치 모드 종료
- BuildSpawnTower(Vector3 position): 타워 설치 후 깃발 설치 모드 자동 시작
*/

using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnTowerBuildManager : MonoBehaviour
{
    public static SpawnTowerBuildManager Instance; // 싱글톤

    [Header("프리팹")]
    public GameObject spawnTowerPrefab; // 타워 프리팹

    private GameObject towerGhost; // 마우스를 따라다니는 고스트 타워
    private bool isBuilding = false; // 현재 설치 모드 여부

    private void Awake() => Instance = this; // 싱글톤 초기화

    private void Update()
    {
        if (!isBuilding) return; // 설치 모드가 아니면 종료

        // 마우스 위치 받아오기
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f; // z좌표 0으로 맞춤 (2D)

        // 타워 고스트 위치 업데이트
        if (towerGhost != null)
            towerGhost.transform.position = mouseWorldPos;

        // 마우스 위치 충돌 감지
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);

        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 1f, 1f, 0.5f); // 기본 반투명

            if (hit != null && hit.CompareTag("Buildable")) // 설치 가능 영역 체크
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);
                bool hasTower = false;
                foreach (var h in hits)
                    if (h.gameObject.layer == LayerMask.NameToLayer("Tower"))
                        hasTower = true; // 이미 타워 존재 여부 체크

                sr.color = hasTower ? new Color(1f,1f,1f,0.5f) : new Color(1f,1f,1f,1f); // 설치 가능하면 불투명
            }
        }

        // 좌클릭 설치
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return; // UI 위 클릭 방지

            if (hit != null && hit.CompareTag("Buildable"))
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);
                bool hasTower = false;
                foreach (var h in hits)
                    if (h.gameObject.layer == LayerMask.NameToLayer("Tower"))
                        hasTower = true;

                if (!hasTower)
                    BuildSpawnTower(mouseWorldPos); // 타워 설치
            }
        }

        // 우클릭 설치 취소
        if (Input.GetMouseButtonDown(1))
            ExitBuildMode();
    }

    // 설치 모드 진입
    public void EnterBuildMode()
    {
        if (isBuilding) return; // 이미 설치 모드이면 무시
        isBuilding = true;

        towerGhost = Instantiate(spawnTowerPrefab); // 고스트 생성
        var sr = towerGhost.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f,1f,1f,0.4f); // 초기 고스트 색상
    }

    // 설치 모드 종료
    public void ExitBuildMode()
    {
        isBuilding = false;
        if (towerGhost != null) Destroy(towerGhost); // 고스트 삭제
    }

    // 타워 설치 후 깃발 설치 모드 자동 시작
    private void BuildSpawnTower(Vector3 position)
    {
        position.z = 0f;
        GameObject newTower = Instantiate(spawnTowerPrefab, position, Quaternion.identity); // 실제 타워 설치
        newTower.layer = LayerMask.NameToLayer("Tower"); // 레이어 설정
        Debug.Log($"[SpawnTowerBuildManager] Building tower at {position}");
        ExitBuildMode(); // 설치 모드 종료

        // 깃발 설치 모드 시작
        if (FlagManager.Instance != null)
        {
            FlagManager.Instance.StartFlagPlacement(position);
        }
    }
}
