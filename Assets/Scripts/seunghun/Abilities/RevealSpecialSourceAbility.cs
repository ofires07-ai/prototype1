using System.Collections;
using UnityEngine;

public class RevealSpecialSourceAbility : CrimerAbility
{
    private bool hasActivated = false; // 이 어빌리티는 씬에서 '딱 한 번'만 실행되어야 함

    // PickUnit의 Start()에서 호출되는 ApplyAbility()를 사용합니다.
    public override void ApplyAbility()
    {
        // 이미 한 번 활성화되었다면 중복 실행 방지
        if (hasActivated) return;

        hasActivated = true;
        Debug.Log("특수자원 발견 어빌리티 활성화 시도...");

        // FogOfWar의 Start()가 끝날 때까지 기다려야 하므로 코루틴을 사용합니다.
        StartCoroutine(RevealAfterFogInit());
    }

    private IEnumerator RevealAfterFogInit()
    {
        // HY_FogOfWar.Instance가 설정되고 (Awake)
        // InitializeFog()가 끝날 때까지 (Start) 기다립니다.
        // 가장 안전한 방법은 첫 프레임의 끝까지 기다리는 것입니다.
        yield return new WaitForEndOfFrame();

        if (HY_FogOfWar.Instance != null)
        {
            // [핵심] Fog 매니저에게 명령을 내립니다!
            HY_FogOfWar.Instance.RevealAllSpecialSources();
        }
        else
        {
            Debug.LogError("HY_FogOfWar 인스턴스를 찾을 수 없어 특수 자원을 밝히지 못했습니다!");
        }
    }
}