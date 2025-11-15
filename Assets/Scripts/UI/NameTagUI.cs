using UnityEngine;

public class NameTagUI : MonoBehaviour
{
    // 원래의 크기 저장
    private Vector3 originalScale;
    private Transform parentTransform;

    void Start()
    {
        originalScale = transform.localScale;
        parentTransform = transform.parent;
    }

    void LateUpdate()
    {
        // 부모가 어떻게 뒤집히든, 나는 항상 원래 크기(양수) 방향을 유지한다.
        // 부모의 localScale.x가 음수면, 나의 localScale.x를 음수로 해서 상쇄시킴
        // (음수 * 음수 = 양수)
        
        if (parentTransform.localScale.x < 0)
        {
            // 부모가 뒤집혔으면 나도 로컬에서 뒤집어서 -> 월드에서 정방향으로 만듦
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
        }
        else
        {
            transform.localScale = originalScale;
        }
    }
}
