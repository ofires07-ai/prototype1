# SWD_hyeonjin/Assets/Script/hyeonjin 주요 스크립트 상세 설명

## Bullet.cs

- **역할:**  
  총알 오브젝트의 동작을 담당합니다.  
  Enemy와 충돌 시 Enemy를 파괴하고, firePrefab이 할당되어 있으면 해당 위치에 Fire(불) 오브젝트를 y축 -90도 회전시켜 생성합니다.  
  총알 자신도 충돌 시 파괴됩니다.

- **주요 메서드:**  
  - `OnTriggerEnter2D(Collider2D collision)`  
    - Enemy와 충돌하면:
      - firePrefab이 있으면 Enemy 위치에 Fire 생성
      - Enemy 오브젝트 파괴
      - Bullet(자신) 파괴

---

## Enemy.cs

- **역할:**  
  Enemy(적) 캐릭터의 이동, 애니메이션, 사망 처리를 담당합니다.  
  Target(주로 플레이어)을 추적하며, 죽으면 움직임과 충돌이 비활성화됩니다.

- **주요 변수:**  
  - `Rigidbody2D rigid` : 물리 이동 제어
  - `Animator anim` : 애니메이션 제어
  - `SpriteRenderer spriteRenderer` : 스프라이트 반전
  - `Rigidbody2D Target` : 추적할 대상(플레이어)
  - `bool isLive` : 생존 상태

- **주요 메서드:**  
  - `FixedUpdate()`  
    - Target이 있으면 방향을 계산해 일정 속도로 이동
    - 애니메이터에 속도 전달
    - Target이 왼쪽/오른쪽에 따라 스프라이트 반전
  - `Die()`  
    - isLive를 false로 하여 이동 정지
    - Collider2D 비활성화(충돌 무효화)
    - (선택) 죽음 애니메이션 트리거

---

## Fire.cs

- **역할:**  
  일정 시간 동안 존재하며, 자신의 시각적 크기만큼의 영역에서 Enemy를 주기적으로 감지하여 제거합니다.

- **주요 변수:**  
  - `lifetime` : Fire가 존재하는 시간(초)
  - `checkInterval` : Enemy 감지 주기(초)

- **주요 메서드:**  
  - `Start()`  
    - lifetime 후 Fire 오브젝트 자동 파괴
  - `Update()`  
    - checkInterval마다 KillEnemiesInArea() 호출
  - `KillEnemiesInArea()`  
    - Fire의 SpriteRenderer bounds 크기만큼 OverlapBox로 Enemy를 감지
    - 감지된 Enemy는 Die() 호출 후 파괴

---

## FirePlayer.cs

- **역할:**  
  Enemy가 감지되면 플레이어 앞에 Fire를 생성하고, Enemy가 없으면 계속 이동하는 플레이어 캐릭터입니다.

- **주요 변수:**  
  - `Rigidbody2D rigid` : 플레이어 이동 제어
  - `Animator anim` : 애니메이션 제어
  - `Scanner scanner` : Enemy 감지
  - `firePrefab` : 생성할 Fire 프리팹
  - `fireCooldown` : Fire 생성 쿨타임
  - `lastFireTime` : 마지막 Fire 생성 시각

- **주요 메서드:**  
  - `FixedUpdate()`  
    - Enemy가 감지되면 멈추고 쿨타임마다 Fire 생성
    - Enemy가 없으면 오른쪽으로 이동
    - 애니메이터에 속도값 전달
  - `Fire(Transform target)`  
    - 플레이어 위치에 Fire 프리팹을 생성하고, 움직이지 않게 고정

---

## Player.cs

- **역할:**  
  기본 플레이어 캐릭터의 이동, 애니메이션, 총알 발사(Enemy 감지 시) 기능을 담당합니다.

- **주요 변수:**  
  - `Rigidbody2D rigid` : 이동 제어
  - `Animator anim` : 애니메이션 제어
  - `Scanner scanner` : Enemy 감지
  - `bulletObj` : 발사할 Bullet 프리팹
  - `fireCooldown` : 발사 쿨타임
  - `lastFireTime` : 마지막 발사 시각

- **주요 메서드:**  
  - `FixedUpdate()`  
    - Enemy가 감지되면 멈추고 쿨타임마다 Bullet 발사
    - Enemy가 없으면 오른쪽으로 이동
    - 애니메이터에 속도값 전달
  - `Fire(Transform target)`  
    - target 방향으로 Bullet을 발사

---

## PlayerSpawner.cs

- **역할:**  
  마우스 클릭 위치에 플레이어 프리팹을 생성합니다.

- **주요 변수:**  
  - `playerPrefab1`, `playerPrefab2` : 생성할 플레이어 프리팹

- **주요 메서드:**  
  - `Update()`  
    - 마우스 왼쪽 클릭: playerPrefab1을 클릭 위치에 생성
    - 마우스 오른쪽 클릭: playerPrefab2를 클릭 위치에 생성

---

## Scanner.cs

- **역할:**  
  플레이어/적 주변의 Enemy를 감지하고, 가장 가까운 Enemy를 찾는 기능을 담당합니다.

- **주요 변수:**  
  - `scanRage` : 감지 반경
  - `targetLayer` : 감지할 레이어
  - `targets` : 감지된 Enemy 목록
  - `nearestTarget` : 가장 가까운 Enemy

- **주요 메서드:**  
  - `FixedUpdate()`  
    - CircleCastAll로 주변 Enemy 감지
    - GetNearestTarget()으로 가장 가까운 Enemy 선택
  - `GetNearestTarget()`  
    - 감지된 Enemy 중 가장 가까운 오브젝트 반환
