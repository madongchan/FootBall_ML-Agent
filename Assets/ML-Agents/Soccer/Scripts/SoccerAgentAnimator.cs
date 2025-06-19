using UnityEngine;

/// <summary>
/// ML-Agents 기반 축구 AI 에이전트의 애니메이션을 속도에 따라 제어하는 컨트롤러
/// Rigidbody의 속도를 측정하여 Velocity 파라미터를 통해 애니메이션 블렌드 트리와 연동
/// </summary>
public class SoccerAgentAnimator : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string velocityParameter = "Velocity"; // 애니메이터 파라미터 이름
    [SerializeField] private float runThreshold = 0.5f; // 달리기 시작하는 속도 임계값
    [SerializeField] private float maxSpeed = 5f; // 최대 속도 (1.0 애니메이션 파라미터에 매핑)
    
    [Header("디버깅")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 애니메이터 파라미터 해시값 (성능 최적화)
    private int velocityHash;
    
    // 에이전트 컴포넌트
    private Rigidbody agentRigidbody;
    private AgentSoccer agentSoccer;
    
    // 현재 속도 값
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
                    Debug.LogError("Animator 컴포넌트를 찾을 수 없습니다. SoccerAgentAnimator에 직접 할당해주세요.");
                }
            }
        }
        
        // Rigidbody 찾기
        agentSoccer = GetComponent<AgentSoccer>();
        if (agentSoccer != null)
        {
            agentRigidbody = agentSoccer.agentRb;
        }
        else
        {
            agentRigidbody = GetComponent<Rigidbody>();
            if (agentRigidbody == null)
            {
                Debug.LogError("Rigidbody 컴포넌트를 찾을 수 없습니다. SoccerAgentAnimator에 직접 할당해주세요.");
            }
        }
        
        // 애니메이터 파라미터 해시값 초기화
        velocityHash = Animator.StringToHash(velocityParameter);
    }
    
    private void Update()
    {
        if (agentRigidbody != null && animator != null)
        {
            // 수평 속도 계산 (XZ 평면)
            Vector3 horizontalVelocity = new Vector3(agentRigidbody.linearVelocity.x, 0f, agentRigidbody.linearVelocity.z);
            currentSpeed = horizontalVelocity.magnitude;
            
            // 애니메이터 파라미터 업데이트
            UpdateAnimator();
            
            // 디버그 정보 표시
            if (showDebugInfo)
            {
                //Debug.Log($"Agent Speed: {currentSpeed}, Animation Value: {(currentSpeed > runThreshold ? 1f : 0f)}");
            }
        }
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
            
            // 부드러운 전환을 위한 보간 (옵션)
            float currentValue = animator.GetFloat(velocityHash);
            float smoothValue = Mathf.Lerp(currentValue, velocityValue, Time.deltaTime * 10f);
            
            animator.SetFloat(velocityHash, smoothValue);
        }
    }
}
