using UnityEngine;

/// <summary>
/// 축구 플레이어를 WASD 키로 조작하고 Velocity 파라미터를 사용하는 1D 애니메이션 블렌드 트리와 연동하는 컨트롤러
/// W/S: X축 이동, A/D: Z축 이동, 속도에 따라 달리는 모션 전환
/// </summary>
public class SoccerPlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float runThreshold = 0.5f; // 달리기 시작하는 속도 임계값
    
    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string velocityParameter = "Velocity"; // 애니메이터 파라미터 이름
    
    // 애니메이터 파라미터 해시값 (성능 최적화)
    private int velocityHash;
    
    // 입력값 저장
    private float xInput; // W/S 키 입력 (X축 이동)
    private float zInput; // A/D 키 입력 (Z축 이동)
    private Vector3 moveDirection;
    private float currentSpeed;
    
    private void Start()
    {
        // 애니메이터 없으면 자동으로 찾기
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogError("Animator 컴포넌트를 찾을 수 없습니다. SoccerPlayerController에 직접 할당해주세요.");
                }
            }
        }
        
        // 애니메이터 파라미터 해시값 초기화
        velocityHash = Animator.StringToHash(velocityParameter);
    }
    
    private void Update()
    {
        // 키보드 입력 받기 (W/S: X축, A/D: Z축)
        zInput = Input.GetAxis("Horizontal"); // A(-), D(+)
        xInput = -Input.GetAxis("Vertical");   // S(+), W(-)
        
        // 이동 방향 계산
        moveDirection = new Vector3(xInput, 0f, zInput);
        
        // 이동 벡터 정규화 (대각선 이동 속도 보정)
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
        
        // 현재 속도 계산
        currentSpeed = moveDirection.magnitude;
        
        // 애니메이터 파라미터 업데이트
        UpdateAnimator();
        
        // 캐릭터 이동 및 회전
        MoveCharacter();
    }
    
    /// <summary>
    /// 애니메이터 파라미터 업데이트
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            // 1D 블렌드 트리용 Velocity 파라미터 설정
            // 이동 중일 때만 1, 정지 시 0 (Idle과 RunForward 전환)
            float velocityValue = currentSpeed > runThreshold ? 1f : 0f;
            animator.SetFloat(velocityHash, velocityValue);
        }
    }
    
    /// <summary>
    /// 캐릭터 이동 및 회전 처리
    /// </summary>
    private void MoveCharacter()
    {
        // 이동 적용
        if (currentSpeed > 0.1f)
        {
            // 이동 방향으로 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // 이동 방향으로 이동
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
