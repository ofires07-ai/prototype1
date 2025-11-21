using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HY_Scanner))]
public class HY_FogTower : MonoBehaviour
{
    [Header("안개(슬로우) 설정")]
    [SerializeField] private float slowFactor = 0.6f;
    [SerializeField] private GameObject fogVisualPrefab; // 파티클 프리팹

    private HY_Scanner scanner;
    private HashSet<HY_EnemyUnitMovement> previousTargets = new HashSet<HY_EnemyUnitMovement>();
    private GameObject currentFogVisual;
    private ParticleSystem fogParticleSystem; // 파티클 시스템 제어용 변수

    void Start()
    {
        scanner = GetComponent<HY_Scanner>();
        
        // 1. 안개 비주얼 생성
        if (fogVisualPrefab != null)
        {
            currentFogVisual = Instantiate(fogVisualPrefab, transform.position, Quaternion.identity);
            currentFogVisual.transform.SetParent(transform); 
            
            // [수정된 로직] Scale로 크기를 키우는 게 아니라, 파티클의 Shape Radius를 직접 제어합니다.
            // 이렇게 해야 파티클 크기(Size)는 유지되면서 퍼지는 범위(Radius)만 넓어집니다.
            fogParticleSystem = currentFogVisual.GetComponent<ParticleSystem>();
            
            if (fogParticleSystem != null)
            {
                // 파티클 시스템의 Shape 모듈에 접근합니다.
                var shape = fogParticleSystem.shape;
                
                // Shape를 Circle로 강제 설정 (혹시 프리팹에서 안 했을까봐)
                shape.shapeType = ParticleSystemShapeType.Circle;
                
                // 반지름을 스캐너 범위와 일치시킵니다.
                shape.radius = scanner.scanRage;
                
                // 파티클이 원 내부에서 생성되도록 설정 (0이면 테두리, 1이면 내부 전체)
                shape.radiusThickness = 1f;
            }
            else
            {
                // 파티클이 아니라 그냥 스프라이트라면 기존처럼 Scale 조절
                float visualScale = scanner.scanRage * 2.0f; 
                currentFogVisual.transform.localScale = new Vector3(visualScale, visualScale, 1f);
            }
        }
    }

    // ... (Update, OnDisable 등 나머지 코드는 그대로 유지) ...
}