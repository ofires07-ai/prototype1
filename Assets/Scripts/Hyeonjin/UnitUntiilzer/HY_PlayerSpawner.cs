using UnityEngine;

public class HY_PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab1;
    public GameObject playerPrefab2;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // z축 위치를 0으로 설정
            Instantiate(playerPrefab1, mousePos, Quaternion.identity);
        }
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // z축 위치를 0으로 설정
            Instantiate(playerPrefab2, mousePos, Quaternion.identity);
        }
    }
}
