using System;
using UnityEngine;
using Random = UnityEngine.Random;

#pragma warning disable 649
namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarAIControl : MonoBehaviour
    {
        public enum BrakeCondition
        {
            NeverBrake,
            TargetDirectionDifference, 
            TargetDistance,
        }

        [SerializeField] [Range(0, 1)] private float m_CautiousSpeedFactor = 0.05f;  
        [SerializeField] [Range(0, 180)] private float m_CautiousMaxAngle = 50f;           
        [SerializeField] private float m_CautiousMaxDistance = 100f;                     
        [SerializeField] private float m_CautiousAngularVelocityFactor = 30f;                 
        [SerializeField] private float m_SteerSensitivity = 0.05f;                                
        [SerializeField] private float m_AccelSensitivity = 0.04f;                           
        [SerializeField] private float m_BrakeSensitivity = 1f;                                 
        [SerializeField] private float m_LateralWanderDistance = 3f;                            
        [SerializeField] private float m_LateralWanderSpeed = 0.1f;                             
        [SerializeField] [Range(0, 1)] private float m_AccelWanderAmount = 0.1f;                 
        [SerializeField] private float m_AccelWanderSpeed = 0.1f;                               
        [SerializeField] private BrakeCondition m_BrakeCondition = BrakeCondition.TargetDistance;
        [SerializeField] private bool m_Driving;  
        [SerializeField] private Transform m_Target;
        [SerializeField] private bool m_StopWhenTargetReached;
        [SerializeField] private float m_ReachTargetThreshold = 2;

        private float m_RandomPerlin;
        private CarController m_CarController;
        private float m_AvoidOtherCarTime;
        private float m_AvoidOtherCarSlowdown;
        private float m_AvoidPathOffset;
        private Rigidbody m_Rigidbody;


        private float stuckTimer = 0f;
        private float stuckThreshold = 3f;
        private bool isReversing = false;
        private float reverseDuration = 2f;
        private float reverseTimer = 0f;

        RaceManager m_RaceManager;


        private void Awake()
        {
            
            m_CarController = GetComponent<CarController>();

            m_RandomPerlin = Random.value*100;

            m_Rigidbody = GetComponent<Rigidbody>();

            m_RaceManager = FindObjectOfType<RaceManager>();
        }


        private void FixedUpdate()
        {
            if (m_Target == null)
            {
                m_Target = GameObject.FindGameObjectWithTag("Player").transform;
            }

            if (m_Target == null || !m_Driving)
            {
                m_CarController.Move(0, 0, -1f, 1f);
            }
            else
            {
                if (m_RaceManager.raceStarted && !isReversing && m_Rigidbody.linearVelocity.magnitude < 0.5f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= stuckThreshold)
                    {
                        isReversing = true;
                        reverseTimer = 0f;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }

                if (isReversing)
                {
                    reverseTimer += Time.deltaTime;
                    m_CarController.Move(0, 0, -1f, 0f);

                    if (reverseTimer >= reverseDuration)
                    {
                        isReversing = false;
                    }
                    return;
                }

                Vector3 fwd = transform.forward;
                if (m_Rigidbody.linearVelocity.magnitude > m_CarController.MaxSpeed*0.1f)
                {
                    fwd = m_Rigidbody.linearVelocity;
                }

                float desiredSpeed = m_CarController.MaxSpeed;

                switch (m_BrakeCondition)
                {
                    case BrakeCondition.TargetDirectionDifference:
                        {
                            float approachingCornerAngle = Vector3.Angle(m_Target.forward, fwd);

                            float spinningAngle = m_Rigidbody.angularVelocity.magnitude*m_CautiousAngularVelocityFactor;

                            float cautiousnessRequired = Mathf.InverseLerp(0, m_CautiousMaxAngle,
                                                                           Mathf.Max(spinningAngle,
                                                                                     approachingCornerAngle));
                            desiredSpeed = Mathf.Lerp(m_CarController.MaxSpeed, m_CarController.MaxSpeed*m_CautiousSpeedFactor,
                                                      cautiousnessRequired);
                            break;
                        }

                    case BrakeCondition.TargetDistance:
                        {
                            Vector3 delta = m_Target.position - transform.position;
                            float distanceCautiousFactor = Mathf.InverseLerp(m_CautiousMaxDistance, 0, delta.magnitude);

                            float spinningAngle = m_Rigidbody.angularVelocity.magnitude*m_CautiousAngularVelocityFactor;

                            float cautiousnessRequired = Mathf.Max(
                                Mathf.InverseLerp(0, m_CautiousMaxAngle, spinningAngle), distanceCautiousFactor);
                            desiredSpeed = Mathf.Lerp(m_CarController.MaxSpeed, m_CarController.MaxSpeed*m_CautiousSpeedFactor,
                                                      cautiousnessRequired);
                            break;
                        }

                    case BrakeCondition.NeverBrake:
                        break;
                }

                Vector3 offsetTargetPos = m_Target.position;

                if (Time.time < m_AvoidOtherCarTime)
                {
                    desiredSpeed *= m_AvoidOtherCarSlowdown;

                    offsetTargetPos += m_Target.right*m_AvoidPathOffset;
                }
                else
                {
                    offsetTargetPos += m_Target.right*
                                       (Mathf.PerlinNoise(Time.time*m_LateralWanderSpeed, m_RandomPerlin)*2 - 1)*
                                       m_LateralWanderDistance;
                }

                float accelBrakeSensitivity = (desiredSpeed < m_CarController.CurrentSpeed)
                                                  ? m_BrakeSensitivity
                                                  : m_AccelSensitivity;

                float accel = Mathf.Clamp((desiredSpeed - m_CarController.CurrentSpeed)*accelBrakeSensitivity, -1, 1);

                accel *= (1 - m_AccelWanderAmount) +
                         (Mathf.PerlinNoise(Time.time*m_AccelWanderSpeed, m_RandomPerlin)*m_AccelWanderAmount);

                Vector3 localTarget = transform.InverseTransformPoint(offsetTargetPos);

                float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z)*Mathf.Rad2Deg;

                float steer = Mathf.Clamp(targetAngle*m_SteerSensitivity, -1, 1)*Mathf.Sign(m_CarController.CurrentSpeed);

                m_CarController.Move(steer, accel, accel, 0f);

                if (m_StopWhenTargetReached && localTarget.magnitude < m_ReachTargetThreshold)
                {
                    m_Driving = false;
                }
            }
        }


        private void OnCollisionStay(Collision col)
        {
            if (col.rigidbody != null)
            {
                var otherAI = col.rigidbody.GetComponent<CarAIControl>();
                if (otherAI != null)
                {
                    m_AvoidOtherCarTime = Time.time + 1;

                    if (Vector3.Angle(transform.forward, otherAI.transform.position - transform.position) < 90)
                    {
                        m_AvoidOtherCarSlowdown = 0.5f;
                    }
                    else
                    {
                        m_AvoidOtherCarSlowdown = 1;
                    }

                    var otherCarLocalDelta = transform.InverseTransformPoint(otherAI.transform.position);
                    float otherCarAngle = Mathf.Atan2(otherCarLocalDelta.x, otherCarLocalDelta.z);
                    m_AvoidPathOffset = m_LateralWanderDistance*-Mathf.Sign(otherCarAngle);
                }
            }
        }


        public void SetTarget(Transform target)
        {
            m_Target = target;
            m_Driving = true;
        }
    }
}
