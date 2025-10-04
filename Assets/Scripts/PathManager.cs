using UnityEngine;

public class PathManager : MonoBehaviour
{
    // 맵에 배치된 Waypoint 오브젝트들을 순서대로 연결합니다.
    public Transform[] waypoints;

    void Awake()
    {
        if (waypoints.Length == 0)
        {
            Debug.LogError("PathManager에 Waypoint가 설정되지 않았습니다!");
        }
    }

    // 적이 PathManager를 참조하여 경로를 따라 이동합니다.
    public Vector3 GetWaypointPosition(int index)
    {
        if (index >= 0 && index < waypoints.Length)
        {
            return waypoints[index].position;
        }
        // 경로가 끝났음을 알리기 위해 기본 위치 반환 또는 예외 처리
        return Vector3.zero; 
    }

    public int GetPathLength()
    {
        return waypoints.Length;
    }
}