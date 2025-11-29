using System.Collections.Generic;
using UnityEngine;

public class Conveyer : MonoBehaviour
{
    [Header("기본 설정")]
    public float speed = 2.0f;       
    public float pieceLength = 1.0f; 

    [Header("자동 경계 설정 (왼쪽 덮개를 넣으세요)")]
    public Transform endPointObject; // ⭐ 여기에 '왼쪽 기계장치(Cover_Left)'를 연결!
    public float offset = 0.0f;      // (선택) 기계장치 중심보다 조금 더 안쪽으로 넣고 싶을 때 미세조정

    private List<Transform> beltPieces = new List<Transform>();

    void Start()
    {
        // 자식들 등록 및 정렬
        foreach (Transform child in transform)
        {
            beltPieces.Add(child);
        }
        beltPieces.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
    }

    void Update()
    {
        // 1. 이동
        foreach (Transform piece in beltPieces)
        {
            piece.localPosition += Vector3.left * speed * Time.deltaTime;
        }

        // 2. 리셋 체크
        CheckAndRecycle();
    }

    void CheckAndRecycle()
    {
        if (beltPieces.Count == 0 || endPointObject == null) return;

        // ⭐ 핵심 마법: 기계장치(World좌표)가 내 기준(Local)으로 x값이 몇인지 자동 변환!
        // 부모가 회전해있든, 기계장치가 어디 있든 상관없이 정확한 로컬 x값을 찾아냅니다.
        Vector3 limitLocalPos = transform.InverseTransformPoint(endPointObject.position);
        
        // 최종 한계선 = 기계장치 위치 + 미세조정값
        float finalLimitX = limitLocalPos.x + offset;

        Transform leader = beltPieces[0];

        // 자동 계산된 위치보다 더 왼쪽으로 갔니?
        if (leader.localPosition.x <= finalLimitX)
        {
            Transform tail = beltPieces[beltPieces.Count - 1];

            // 꼬리 뒤로 이동
            Vector3 newPos = tail.localPosition;
            newPos.x += pieceLength;
            leader.localPosition = newPos;

            // 리스트 갱신
            beltPieces.RemoveAt(0);
            beltPieces.Add(leader);
        }
    }
}
