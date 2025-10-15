using System;
using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets.Utility
{
    public class WaypointProgressTracker : MonoBehaviour
    {
        [SerializeField] private WaypointCircuit circuit;

        [SerializeField] private float lookAheadForTargetOffset = 5;

        [SerializeField] private float lookAheadForTargetFactor = .1f;

        [SerializeField] private float lookAheadForSpeedOffset = 10;

        [SerializeField] private float lookAheadForSpeedFactor = .2f;

        [SerializeField] private ProgressStyle progressStyle = ProgressStyle.SmoothAlongRoute;

        [SerializeField] private float pointToPointThreshold = 4;

        public enum ProgressStyle
        {
            SmoothAlongRoute,
            PointToPoint,
        }

        public WaypointCircuit.RoutePoint targetPoint { get; private set; }
        public WaypointCircuit.RoutePoint speedPoint { get; private set; }
        public WaypointCircuit.RoutePoint progressPoint { get; private set; }

        public Transform target;

        private float progressDistance;
        private int progressNum;
        private Vector3 lastPosition;
        private float speed;

        private void Start()
        {
            if (target == null)
            {
                target = new GameObject(name + " Waypoint Target").transform;
            }

            GameObject circuitObject = GameObject.Find("Ai_Road");

            circuit = circuitObject.GetComponent<WaypointCircuit>();

            Reset();
        }


        public void Reset()
        {
            progressDistance = 0;
            progressNum = 0;
            if (progressStyle == ProgressStyle.PointToPoint)
            {
                target.position = circuit.Waypoints[progressNum].position;
                target.rotation = circuit.Waypoints[progressNum].rotation;
            }
        }


        private void Update()
        {
            if (progressStyle == ProgressStyle.SmoothAlongRoute)
            {
                if (Time.deltaTime > 0)
                {
                    speed = Mathf.Lerp(speed, (lastPosition - transform.position).magnitude / Time.deltaTime,
                                       Time.deltaTime);
                }
                target.position =
                    circuit.GetRoutePoint(progressDistance + lookAheadForTargetOffset + lookAheadForTargetFactor * speed)
                           .position;
                target.rotation =
                    Quaternion.LookRotation(
                        circuit.GetRoutePoint(progressDistance + lookAheadForSpeedOffset + lookAheadForSpeedFactor * speed)
                               .direction);


                progressPoint = circuit.GetRoutePoint(progressDistance);
                Vector3 progressDelta = progressPoint.position - transform.position;
                if (Vector3.Dot(progressDelta, progressPoint.direction) < 0)
                {
                    progressDistance += progressDelta.magnitude * 0.5f;
                }

                lastPosition = transform.position;
            }
            else
            {

                Vector3 targetDelta = target.position - transform.position;
                if (targetDelta.magnitude < pointToPointThreshold)
                {
                    progressNum = (progressNum + 1) % circuit.Waypoints.Length;
                }


                target.position = circuit.Waypoints[progressNum].position;
                target.rotation = circuit.Waypoints[progressNum].rotation;

                progressPoint = circuit.GetRoutePoint(progressDistance);
                Vector3 progressDelta = progressPoint.position - transform.position;
                if (Vector3.Dot(progressDelta, progressPoint.direction) < 0)
                {
                    progressDistance += progressDelta.magnitude;
                }
                lastPosition = transform.position;
            }
        }


        private void OnDrawGizmos()
        {
            if (Application.isPlaying && circuit != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireSphere(circuit.GetRoutePosition(progressDistance), 1);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(target.position, target.position + target.forward);
            }
        }
    }
}
