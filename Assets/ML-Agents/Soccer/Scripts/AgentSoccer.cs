using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Defender,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;


    [HideInInspector]
    public Rigidbody agentRb;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    //EnvironmentParameters m_ResetParams;

    public string BehaviorName
    {
        get { return m_BehaviorParameters.BehaviorName; }
        set { m_BehaviorParameters.BehaviorName = value; }
    }

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x, 0.5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x, 0.5f, transform.position.z);
            rotSign = -1f;
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
            BehaviorName = "GoalieAI";
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
            BehaviorName = "StrikerAI";
        }
        else if (position == Position.Defender)
        {
            m_LateralSpeed = 0.5f;
            m_ForwardSpeed = 1.0f;
            BehaviorName = "DefenderAI";
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
            BehaviorName = "GenericAI";
        }
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        //m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * 4f, ForceMode.VelocityChange);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent's position and velocity (모든 에이전트 공통)
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(agentRb.linearVelocity);

        // Ball's position and velocity (모든 에이전트 공통)
        GameObject ball = GameObject.FindGameObjectWithTag("ball");
        if (ball != null)
        {
            sensor.AddObservation(ball.transform.localPosition);
            sensor.AddObservation(ball.GetComponent<Rigidbody>().linearVelocity);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
        }

        // Own goal and opposing goal positions (모든 에이전트 공통)
        GameObject ownGoal = GameObject.FindGameObjectWithTag(team == Team.Blue ? "blueGoal" : "purpleGoal");
        GameObject opposingGoal = GameObject.FindGameObjectWithTag(team == Team.Blue ? "purpleGoal" : "blueGoal");

        if (ownGoal != null) sensor.AddObservation(ownGoal.transform.localPosition);
        else sensor.AddObservation(Vector3.zero);

        if (opposingGoal != null) sensor.AddObservation(opposingGoal.transform.localPosition);
        else sensor.AddObservation(Vector3.zero);

        // Distance to ball (모든 에이전트 공통)
        if (ball != null)
        {
            sensor.AddObservation(Vector3.Distance(transform.localPosition, ball.transform.localPosition));
        }
        else
        {
            sensor.AddObservation(0f);
        }

        // Position-specific observations (포지션별 특화 관측)
        if (position == Position.Striker)
        {
            // Striker specific observations
            // Distance to opposing goal
            if (opposingGoal != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.localPosition, opposingGoal.transform.localPosition));
            }
            else
            {
                sensor.AddObservation(0f);
            }
            // Angle to opposing goal (simplified: forward vector dot product with direction to goal)
            if (ball != null && opposingGoal != null)
            {
                Vector3 directionToGoal = (opposingGoal.transform.localPosition - ball.transform.localPosition).normalized;
                sensor.AddObservation(Vector3.Dot(transform.forward, directionToGoal));
            }
            else
            {
                sensor.AddObservation(0f);
            }
        }
        else if (position == Position.Defender)
        {
            // Defender specific observations
            // Distance to own goal
            if (ownGoal != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.localPosition, ownGoal.transform.localPosition));
            }
            else
            {
                sensor.AddObservation(0f);
            }
            // Distance to nearest opponent with ball (if any)
            GameObject nearestOpponentWithBall = null;
            float minDistance = float.MaxValue;
            // SoccerEnvController의 AgentsList를 통해 모든 에이전트 정보에 접근
            foreach (var playerInfo in GetComponentInParent<SoccerEnvController>().AgentsList)
            {
                // 상대 팀의 공격수이면서 공을 소유한 것으로 판단되는 에이전트 찾기
                if (playerInfo.Agent.team != team && playerInfo.Agent.position == Position.Striker && ball != null && Vector3.Distance(playerInfo.Agent.transform.localPosition, ball.transform.localPosition) < 1.5f)
                {
                    float dist = Vector3.Distance(transform.localPosition, playerInfo.Agent.transform.localPosition);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestOpponentWithBall = playerInfo.Agent.gameObject;
                    }
                }
            }
            if (nearestOpponentWithBall != null)
            {
                sensor.AddObservation(minDistance);
                sensor.AddObservation((nearestOpponentWithBall.transform.localPosition - transform.localPosition).normalized); // 상대 공격수 방향
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(Vector3.zero);
            }
        }
        else if (position == Position.Goalie)
        {
            // Goalie specific observations
            // Distance to own goal
            if (ownGoal != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.localPosition, ownGoal.transform.localPosition));
            }
            else
            {
                sensor.AddObservation(0f);
            }
            // Ball's position relative to own goal
            if (ball != null && ownGoal != null)
            {
                sensor.AddObservation(ball.transform.localPosition - ownGoal.transform.localPosition);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
            }
        }
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (position == Position.Goalie)
        {
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            AddReward(-m_Existential);
        }
        else if (position == Position.Defender)
        {
            AddReward(m_Existential * 0.5f); // 수비수는 작은 생존 보너스
        }

        MoveAgent(actionBuffers.DiscreteActions);

        // Position-specific rewards (포지션별 특화 보상)
        if (position == Position.Striker)
        {
            // Striker specific rewards
            // Reward for moving towards the opposing goal when having the ball
            GameObject ball = GameObject.FindGameObjectWithTag("ball");
            GameObject opposingGoal = GameObject.FindGameObjectWithTag(team == Team.Blue ? "purpleGoal" : "blueGoal");
            if (ball != null && opposingGoal != null && Vector3.Distance(transform.localPosition, ball.transform.localPosition) < 1.5f) // 공을 소유한 것으로 간주
            {
                float distToGoal = Vector3.Distance(transform.localPosition, opposingGoal.transform.localPosition);
                AddReward(1f / (distToGoal + 0.1f)); // 골대에 가까워질수록 보상 (0으로 나누는 것 방지)
            }
        }
        else if (position == Position.Defender)
        {
            // Defender specific rewards
            // Reward for being between the ball and own goal
            GameObject ball = GameObject.FindGameObjectWithTag("ball");
            GameObject ownGoal = GameObject.FindGameObjectWithTag(team == Team.Blue ? "blueGoal" : "purpleGoal");
            if (ball != null && ownGoal != null)
            {
                Vector3 ballToGoal = ownGoal.transform.localPosition - ball.transform.localPosition;
                Vector3 agentToGoal = ownGoal.transform.localPosition - transform.localPosition;
                float dotProduct = Vector3.Dot(ballToGoal.normalized, agentToGoal.normalized);
                if (dotProduct > 0.8f) // 에이전트가 공과 골대 사이에 위치할 때
                {
                    AddReward(0.001f); // 좋은 수비 위치에 대한 작은 지속 보상
                }
            }
        }
        else if (position == Position.Goalie)
        {
            // Goalie specific rewards
            // Reward for being close to own goal
            GameObject ownGoal = GameObject.FindGameObjectWithTag(team == Team.Blue ? "blueGoal" : "purpleGoal");
            if (ownGoal != null)
            {
                float distToOwnGoal = Vector3.Distance(transform.localPosition, ownGoal.transform.localPosition);
                AddReward(1f / (distToOwnGoal + 1f)); // 골대에 가까이 머무는 것에 대한 보상
            }
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
        MoveAgent(actionsOut.DiscreteActions);
    }

    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power; // 골키퍼는 항상 최대 힘으로 공을 찰 수 있도록
        }
        // if (c.gameObject.CompareTag("ball"))
        // {
        //     AddReward(.2f * m_BallTouch); // 공 터치 보상
        //     var dir = c.contacts[0].point - transform.position;
        //     dir = dir.normalized;
        //     c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);

        //     // Striker specific reward for kicking towards goal
        //     if (position == Position.Striker)
        //     {
        //         GameObject opposingGoal = GameObject.FindGameObjectWithTag(team == Team.Blue ? "purpleGoal" : "blueGoal");
        //         if (opposingGoal != null)
        //         {
        //             Vector3 kickDirection = (c.gameObject.transform.position - transform.position).normalized; // 공이 튕겨나가는 방향
        //             Vector3 directionToGoal = (opposingGoal.transform.localPosition - c.gameObject.transform.localPosition).normalized; // 공에서 골대 방향
        //             float dot = Vector3.Dot(kickDirection, directionToGoal);
        //             AddReward(dot * 0.1f); // 공을 상대 골대 방향으로 찰수록 보상
        //         }
        //     }
        // }
    }

    public override void OnEpisodeBegin()
    {
        //m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

}


