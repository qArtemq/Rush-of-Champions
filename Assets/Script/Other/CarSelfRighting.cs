using System;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public class CarSelfRighting : MonoBehaviour
    {
        [SerializeField] private float m_WaitTime = 3f;
        [SerializeField] private float m_VelocityThreshold = 1f;

        private float m_LastOkTime;
        private Rigidbody m_Rigidbody;


        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }


        private void Update()
        {
            if (transform.up.y > 0f || m_Rigidbody.linearVelocity.magnitude > m_VelocityThreshold)
            {
                m_LastOkTime = Time.time;
            }

            if (Time.time > m_LastOkTime + m_WaitTime)
            {
                RightCar();
            }
        }


        private void RightCar()
        {
            transform.position += Vector3.up;
            transform.rotation = Quaternion.LookRotation(transform.forward);
        }
    }
}
