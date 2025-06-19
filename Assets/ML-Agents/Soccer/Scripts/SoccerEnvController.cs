using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    /// <summary>
    /// The area bounds.
    /// </summary>

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    [Header("Score Text")]
    public TMPro.TextMeshProUGUI BlueScoreText;
    public TMPro.TextMeshProUGUI PurpleScoreText;
    public int BlueScore { get; private set; }
    public int PurpleScore { get; private set; }

    [Header("Game Over UI")]
    public TMPro.TextMeshProUGUI GameOverText; // 게임 오버 텍스트 UI
    private bool isGameOver = false; // 게임 오버 상태를 추적하는 플래그


    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;

    void Start()
    {
        // Initialize TeamManager
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        // Reset scores
        BlueScore = 0;
        PurpleScore = 0;
        BlueScoreText.text = "BlueScore: " + BlueScore.ToString();
        PurpleScoreText.text = "PurpleScore: " + PurpleScore.ToString();
        GameOverText.gameObject.SetActive(false); // 게임 시작 시 게임 오버 텍스트 비활성화
        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }


    public void ResetBall()
    {
        var randomPosX = Random.Range(-2.5f, 2.5f);
        var randomPosZ = Random.Range(-2.5f, 2.5f);

        ball.transform.position = m_BallStartingPos;// + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

    }

    public void GoalTouched(Team scoredTeam)
    {
        if (isGameOver) return; // 게임 오버 상태면 골 터치 로직을 실행하지 않음
        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_PurpleAgentGroup.AddGroupReward(-1);
            BlueScore++;
            BlueScoreText.text = "BlueScore: " + BlueScore.ToString();
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_BlueAgentGroup.AddGroupReward(-1);
            PurpleScore++;
            PurpleScoreText.text = "PurpleScore: " + PurpleScore.ToString();
        }
        // 점수 확인 및 게임 오버 처리
        if (BlueScore >= 7 || PurpleScore >= 7)
        {
            isGameOver = true;
            GameOverText.gameObject.SetActive(true); // 게임 오버 텍스트 활성화
            Time.timeScale = 0; // 게임 시간 정지
            // 모든 에이전트의 에피소드를 종료하고 더 이상 행동하지 않도록 설정
            m_BlueAgentGroup.EndGroupEpisode();
            m_PurpleAgentGroup.EndGroupEpisode();
            foreach (var item in AgentsList)
            {
                item.Agent.EndEpisode(); // 개별 에이전트 에피소드 종료
                item.Agent.gameObject.SetActive(false); // 에이전트 비활성화 (선택 사항)
            }
            ball.gameObject.SetActive(false); // 공 비활성화 (선택 사항)
            return; // 게임 오버 시 추가 로직 실행 방지
        }

        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();
    }


    public void ResetScene()
    {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos; //+ new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }
}
