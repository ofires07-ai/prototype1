# Tower Defense Game - Prisoner Management System

## 🎮 게임 개요

이 프로젝트는 Unity 2022.3.x 버전을 사용하여 개발된 타워 디펜스 게임입니다. 전통적인 타워 디펜스 장르에 독창적인 죄수 관리 시스템을 결합하여, 플레이어가 죄수들을 직접 조작하여 자원을 채굴하고 방어 시설을 구축하는 새로운 게임 경험을 제공합니다.

## 🎯 핵심 게임플레이

### 죄수 관리 시스템
- **좌클릭**: 죄수 선택/해제
- **우클릭**: 선택된 죄수에게 명령 (이동, 자원 채굴)
- **자원 채굴**: 일반 자원(T1~T4)과 특수 자원(T5) 채굴
- **스마트 타겟팅**: 일반 자원에 특수 자원이 부착되어 있으면 자동으로 특수 자원을 우선 채굴

### 타워 디펜스 메커니즘
- **웨이브 기반 적 스폰**: 각 웨이브마다 다양한 적 유형과 수량
- **타워 건설**: 마우스 클릭을 통한 직관적인 타워 배치
- **자원 소모**: 5가지 티어의 자원을 소모하여 유닛/타워 건설
- **업그레이드 시스템**: 타워 및 유닛 강화 기능

## 📁 프로젝트 구조

```
Assets/
├── Scenes/                     # 게임 씬 파일들
│   ├── Title.unity            # 메인 메뉴
│   ├── Story.unity            # 스토리 소개
│   ├── Stage1.unity           # 1스테이지
│   ├── Stage2.unity           # 2스테이지
│   ├── Stage3.unity           # 3스테이지
│   ├── Stage4.unity           # 4스테이지
│   ├── Result.unity           # 결과 화면
│   ├── GameOver.unity         # 게임오버 화면
│   └── EndingScene.unity      # 엔딩 씬
├── Scripts/                   # C# 스크립트
│   ├── UI/                    # UI 관련 스크립트
│   │   ├── Tutorial/          # 튜토리얼 시스템
│   │   │   ├── TutorialManager.cs
│   │   │   ├── TutorialStep.cs
│   │   │   └── Stage1TutorialController.cs
│   │   ├── TitleUIController.cs
│   │   ├── ResultUIController.cs
│   │   ├── GameOverUIController.cs
│   │   ├── StoryUIController.cs
│   │   └── EndingUIController.cs
│   ├── seunghun/              # 승훈 담당 시스템
│   │   ├── Map/               # 맵 관련
│   │   ├── Abilities/         # 능력 시스템
│   │   ├── CrimerManager.cs   # 죄수 관리 시스템
│   │   ├── InventoryManager.cs # 인벤토리 관리
│   │   └── SourceManager.cs   # 자원 관리
│   ├── Hyeonjin/              # 현진 담당 시스템
│   │   ├── Enemy/             # 적 관련
│   │   ├── Tower/             # 타워 관련
│   │   │   ├── Magic/         # 마법 타워
│   │   │   └── slow/          # 슬로우 타워
│   │   └── Unit/              # 유닛 관련
│   ├── ise/                   # 이세 담당 시스템
│   │   ├── FlagManager.cs     # 플래그 관리
│   │   └── TowerBuildManager.cs # 타워 건설 관리
│   ├── GameManager.cs         # 핵심 게임 매니저
│   ├── GameFlowManager.cs     # 게임 흐름 관리
│   ├── SpawnManager.cs        # 적 스폰 관리
│   ├── BuildSystem.cs         # 건설 시스템
│   └── PathManager.cs         # 경로 관리
├── Prefabs/                   # 프리팹 파일들
├── Sprites/                   # 이미지 리소스
├── Audio/                     # 오디오 리소스
└── Craftfix/                  # 외부 아트 에셋
```

## 🔧 핵심 시스템 아키텍처

### 1. 게임 관리 시스템

#### GameManager (GameManager.cs)
게임의 핵심 상태를 관리하는 싱글톤 매니저
- **웨이브 관리**: 웨이브 준비 상태와 진행 상태 전환
- **자원 관리**: 5가지 티어의 자원 추가/소모 처리
- **UI 업데이트**: HP, 자원, 웨이브 정보 실시간 갱신
- **몬스터 카운트**: 웨이브별 적 타입 및 수량 동적 표시
- **할인 시스템**: 유닛/타워 비용 할인율 적용

```csharp
// 주요 기능
public void AddResource(int tier, int amount)           // 자원 추가
public bool TrySpendMultipleResources(int[] costs)     // 자원 소모 시도
public void OnWaveCleared()                             // 웨이브 클리어 처리
public void RegisterUnitCostModifier(float modifier)   // 유닛 할인율 등록
```

#### GameFlowManager (GameFlowManager.cs)
게임 전체 흐름과 씬 전환을 담당하는 매니저
- **씬 전환**: 타이틀 → 스토리 → 스테이지 → 결과/엔딩
- **점수 관리**: 스테이지별 점수 및 총점 계산
- **페이드 효과**: 씬 전환 시 자연스러운 페이드 인/아웃
- **로딩 팁**: 스테이지 간 로딩 화면에서 게임 팁 표시

### 2. 죄수 관리 시스템

#### CrimerManager (CrimerManager.cs)
죄수들의 선택과 명령을 관리하는 핵심 시스템
- **단일 선택**: 한 번에 하나의 죄수만 선택 가능
- **명령 체계**: 이동, 자원 채굴 명령 전달
- **스마트 타겟팅**: 특수 자원 우선 채굴 로직
- **랜덤 드래프트**: 죄수 재선택 시스템 (리롤 기회 제한)

```csharp
// 핵심 메서드
private void HandleSelection()      // 좌클릭 선택 처리
private void HandleCommand()        // 우클릭 명령 처리
public void SelectRandomCrimers()   // 죄수 재선택
```

### 3. 자원 관리 시스템

#### InventoryManager (InventoryManager.cs)
게임 내 모든 자원을 체계적으로 관리
- **5티어 자원**: T1~T4(일반), T5(특수) 자원 분류
- **실시간 UI**: 자원 변화 시 즉시 UI 업데이트
- **자원 검증**: 충분한 자원 보유 여부 확인

### 4. 적 스폰 시스템

#### SpawnManager (SpawnManager.cs)
웨이브 기반 적 생성과 관리를 담당
- **웨이브 설정**: 적 타입, 수량, 스폰 간격 등 세부 설정
- **스케일링**: 웨이브 진행에 따른 적 HP/속도 증가
- **스폰 포인트**: 다중 스폰 포인트를 통한 다양한 경로
- **순차 스폰**: 같은 스폰 포인트에서 타입별 순차 등장

```csharp
// 주요 구조
[System.Serializable]
public class Wave {
    public string waveName;
    public List<EnemySpawn> enemySpawns;
    public int totalMonsterCount;
}

[System.Serializable]
public class EnemySpawn {
    public string enemyID;
    public GameObject enemyPrefab;
    public int count;
    public float spawnInterval;
    public Sprite uiIcon;
}
```

### 5. 건설 시스템

#### BuildSystem (BuildSystem.cs)
유닛과 타워 건설을 통합 관리
- **통합 UI**: 10개 버튼으로 모든 건설 가능 객체 관리
- **타입별 처리**: SOLDIER, TOWER, UNIT 타입별 다른 건설 로직
- **할인 적용**: 게임 상황에 따른 동적 비용 할인
- **콜백 시스템**: 건설 완료 후 추가 로직 실행

#### TowerBuildManager (TowerBuildManager.cs)
타워 배치의 세부적인 처리
- **배치 모드**: 마우스 클릭을 통한 직관적 배치
- **위치 검증**: 겹침 방지 및 적절한 배치 위치 확인
- **시각적 피드백**: 배치 가능/불가능 상태 표시

### 6. 튜토리얼 시스템

#### TutorialManager (TutorialManager.cs)
신규 플레이어를 위한 가이드 시스템
- **단계별 진행**: 순차적인 튜토리얼 스텝
- **UI 하이라이트**: 특정 UI 요소 강조 표시
- **월드 좌표 지원**: 게임 월드 내 오브젝트 가이드
- **일시정지**: 튜토리얼 중 게임 시간 정지

```csharp
// 튜토리얼 단계 정의
[System.Serializable]
public class TutorialStep {
    public string instructionText;      // 설명 텍스트
    public RectTransform targetUI;      // UI 타겟
    public Transform targetWorldObject; // 월드 오브젝트 타겟
    public InstructionPos boxPosition;  // 설명창 위치
    public float boxWidth, boxHeight;   // 설명창 크기
}
```

## 🎮 게임 흐름

### 1. 게임 시작
1. **타이틀 화면**: 게임 시작, 설정, 종료 옵션
2. **스토리 씬**: 게임 배경 스토리 소개
3. **죄수 선택**: 5명의 죄수 중 선택 (리롤 기회 제한)

### 2. 스테이지 진행
1. **준비 시간**: 첫 웨이브 전 120초 준비 시간
2. **자원 채굴**: 죄수들을 조작하여 자원 수집
3. **방어 준비**: 타워 건설 및 배치
4. **웨이브 전투**: 적 웨이브와의 전투
5. **다음 스테이지**: 성공 시 다음 스테이지로 진행

### 3. 게임 종료
1. **승리**: 모든 스테이지 클리어 → 엔딩 씬 → 결과 화면
2. **패배**: 기지 HP 0 → 게임오버 화면

## 🛠️ 기술적 특징

### Unity 패키지 관리
- **Unity 2022.3.x LTS** 기반
- **Universal Render Pipeline (URP)** 사용
- **TextMeshPro** UI 텍스트 처리
- **Unity Input System** 모던 입력 처리

### 디자인 패턴 활용
- **싱글톤 패턴**: 핵심 매니저들의 전역 접근성 보장
- **옵저버 패턴**: 이벤트 기반 시스템 간 통신
- **전략 패턴**: 다양한 타워/유닛 타입별 행동 정의
- **콜백 패턴**: 비동기 작업 완료 후 처리

### 성능 최적화
- **오브젝트 풀링**: 자주 생성/삭제되는 오브젝트 재사용
- **레이어 마스크**: 물리 연산 최적화
- **코루틴**: 프레임 분산 처리로 성능 향상

## 🎨 아트 및 UI

### 비주얼 스타일
- **2D 픽셀 아트**: 레트로 게임 감성의 픽셀 아트 스타일
- **컨베이어 벨트**: 산업적 테마의 자원 운송 시스템
- **우주선**: SF 요소가 가미된 기지 디자인

### UI/UX 설계
- **직관적 조작**: 마우스 클릭만으로 모든 조작 가능
- **실시간 피드백**: 자원, HP 등 게임 상태 실시간 표시
- **시각적 가이드**: 튜토리얼 하이라이트 및 명확한 UI 표시

## 🔮 향후 개발 계획

### 추가 기능
1. **더 많은 스테이지**: 다양한 환경과 도전 과제
2. **새로운 타워 타입**: 특수 능력을 가진 방어 시설
3. **죄수 특성 시스템**: 개별 죄수의 고유한 능력
4. **멀티플레이어**: 협동 또는 대전 모드

### 기술적 개선
1. **모바일 포팅**: 터치 입력 지원
2. **세이브/로드**: 게임 진행 상황 저장
3. **설정 시스템**: 사운드, 그래픽 옵션
4. **지역화**: 다국어 지원

## 👥 개발팀
- **최승훈**: 죄수 선택 및 이동, 죄수 애니메이션 및 이름표 UI, 죄수 능력 개발, 자원 채굴 관리, 자원 인벤토리 관리, 우주선 및 다리 구현, 튜토리얼 제작
- **서현진**: 적 유닛 이동, 타워 전투 메커니즘 , 아군 유닛 전투 메커니즘, 아군 유닛 움직임 로직, 유닛 타워 생산 로직, 적군 전투 메커니즘, 수비 타워 애니메이션 제작 
- **양이세**: 플래그 시스템, 타워 배치, 아군 유닛 전투 메커니즘 리팩토링, 유닛, 적군 및 유닛 타워 애니메이션 제작, 맵 디자인
- **임영훈**: UI/UX, 게임 시스템 설계, 밸런싱 및 병합, 컷씬 제작, 스토리 제작, 안개 구현, 타워 및 업그레이드 정산 시스템 구현

## 🚀 실행 방법

### 개발 환경 설정
1. **Unity Hub** 설치
2. **Unity 2022.3.x LTS** 버전 설치
3. 프로젝트 폴더를 Unity Hub에서 열기
4. 필요한 패키지 자동 설치 대기

### 게임 실행
1. `Assets/Scenes/Title.unity` 씬 로드
2. Unity Editor에서 Play 버튼 클릭
3. 또는 Build Settings에서 플랫폼별 빌드

### 조작 방법
- **좌클릭**: 죄수 선택, UI 조작
- **우클릭**: 선택된 죄수에게 명령
- **마우스 휠**: 카메라 줌 인/아웃

---

*이 프로젝트는 세종대학교 SW설계기초 과목의 일환으로 개발되었습니다.*
