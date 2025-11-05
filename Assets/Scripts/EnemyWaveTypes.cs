using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawn
{
    [Header("타입/프리팹/스폰 간격/수량")]
    public string enemyID;              // UI/카운트에 사용할 ID
    public GameObject enemyPrefab;      // 스폰할 프리팹
    public float spawnInterval = 1f;    // 이 타입의 스폰 간격
    public int count = 5;               // 이 타입의 총 스폰 수

    [Header("UI 표시용 아이콘(둘 중 하나를 GameManager가 씀)")]
    public Sprite uiIcon;
}

[System.Serializable]
public class Wave
{
    [Header("웨이브 이름과 구성")]
    public string waveName = "Wave 1";
    public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();

    [HideInInspector] public int totalMonsterCount; // 계산용(Inspector 표시 안 함)
}
