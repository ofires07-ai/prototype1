using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    // 애니메이션이 끝난 후 추가로 대기할 시간 (0이면 바로 파괴)
    public float delay = 0f; 

    // 이 스크립트가 붙은 오브젝트가 생성(Awake)될 때 호출됨
    void Awake()
    {
        // 1. 내 Animator 컴포넌트를 찾습니다.
        Animator anim = GetComponent<Animator>();

        // 2. Animator에서 현재 재생 중인 애니메이션 클립의 정보를 가져옵니다.
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // 3. 애니메이션의 총 길이 (초) + 추가 대기 시간(delay) 만큼의 시간이 지난 뒤에,
        //    "DestroyObject" 함수를 실행하도록 예약합니다.
        Destroy(gameObject, stateInfo.length + delay);
    }
}