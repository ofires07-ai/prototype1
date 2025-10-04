using UnityEngine;
using System.Collections.Generic;

// 이 속성이 있어야 인스펙터에서 이 데이터 파일을 생성할 수 있습니다.
[CreateAssetMenu(fileName = "NewWaveData", menuName = "TowerDefense/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave 정보")]
    public int waveNumber = 1;

    [Header("적 스폰 정보")]
    // 이 배열에 웨이브 동안 스폰할 적 유닛의 종류와 수량, 간격 정보를 정의합니다.
    public List<EnemySpawnInfo> spawnSequence = new List<EnemySpawnInfo>();
}

[System.Serializable]
public class EnemySpawnInfo
{
    // TODO: 팀원들이 만든 'Enemy' 프리팹을 여기에 연결합니다.
    public GameObject enemyPrefab; 
    
    [Range(1, 50)]
    public int count;
    
    [Range(0.1f, 5f)]
    public float timeBetweenSpawns;
}