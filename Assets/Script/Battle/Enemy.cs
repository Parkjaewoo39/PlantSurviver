using UnityEngine;

public class Enemy : MonoBehaviour
{
    [HideInInspector] public Battle battle; // 이 적을 생성한 Battle 스크립트 참조

    public Rigidbody2D rb;
    // Battle.plant 방향으로 초기 속도를 가지고 중력을 적용받음
    public float speed = 5f; // 초기 속도
    public float gravity = 9.81f; // plant 방향으로 적용할 중력 가속도

    private Vector2 direction; // 이동 방향

    private void OnEnable()
    {
        // 활성화될 때마다 plant 방향으로 초기 속도 설정
        if (battle != null && battle.plant != null)
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // Unity의 중력 사용 안 함
            direction = (battle.plant.transform.position - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }
    }

    private void FixedUpdate()
    {
        // plant 방향으로 인위적 중력 가속도 적용
        if (rb != null && battle != null && battle.plant != null)
        {
            Vector2 toPlant = (battle.plant.transform.position - transform.position).normalized;
            rb.linearVelocity += toPlant * gravity * Time.fixedDeltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Plant에 충돌 시 적 비활성화
        if (collision.gameObject == battle.plant)
        {
            gameObject.SetActive(false);
        }
        // Sun에 충돌 시 적 비활성화
        if (collision.gameObject == battle.sun)
        {
            gameObject.SetActive(false);
        }
    }
}
