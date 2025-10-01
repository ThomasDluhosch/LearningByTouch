using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicHands : MonoBehaviour
{
    Rigidbody _rb;

    [Header("Target Tracking")]

    [Tooltip("Wirst from Hand Interaction Visual")]
    [SerializeField] private Transform _targetWrist;
    [Tooltip("Wrist from Righ Hand Physics")]
    [SerializeField] private Transform _physicalWristAnchor;
    [SerializeField] private GameObject physicsCollider;

    [Header("Physics Forces")]
    [SerializeField] private float _moveForce = 10f;
    [SerializeField] private float _rotateForce = 10f;

    private bool _physicsEnabled = false;
    // [SerializeField] private string _ignoreTag = "LocalHand";
    [SerializeField] List<string> _ignoreTags = new() { "LocalHand", "LocalAvatar" };

    private List<Collider> _colliders = new List<Collider>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        SetPhysicsEnabled(false);
    }

    bool IsIgnored(Collider collider) => _ignoreTags.Contains(collider.tag);

    void OnTriggerEnter(Collider other)
    {
        if (IsIgnored(other)) return;

        if (!_colliders.Contains(other)) _colliders.Add(other);
        SetPhysicsEnabled(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (IsIgnored(other)) return;

        _colliders.Remove(other);
        if (_colliders.Count == 0) SetPhysicsEnabled(false);
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        _rb.isKinematic = !enabled;
        _physicsEnabled = enabled;
    }

    private void FixedUpdate()
    {
        physicsCollider.transform.position = _physicalWristAnchor.transform.position;
        physicsCollider.transform.rotation = _physicalWristAnchor.transform.rotation;
        if (_physicsEnabled && _targetWrist != null && _physicalWristAnchor != null)
        {
            Vector3 positionError = _targetWrist.position - _physicalWristAnchor.position;
            _rb.AddForce(positionError * _moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

            Quaternion currentAnchorLocalRotation = _physicalWristAnchor.localRotation;
            Quaternion targetRigidbodyRotation = _targetWrist.rotation * Quaternion.Inverse(currentAnchorLocalRotation);
            Quaternion rotationDifference = targetRigidbodyRotation * Quaternion.Inverse(_rb.rotation);

            rotationDifference.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            if (angleInDegrees > 180f) angleInDegrees -= 360f;
            if (angleInDegrees < -180f) angleInDegrees += 360f;

            if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon)
            {
                Vector3 targetAngularVelocity = rotationAxis.normalized * (angleInDegrees * Mathf.Deg2Rad * _rotateForce);
                _rb.angularVelocity = targetAngularVelocity;
            }
            else
            {
                _rb.angularVelocity = Vector3.Slerp(_rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * _rotateForce);
            }
        }
    }

    private void Update()
    {
        physicsCollider.transform.position = _physicalWristAnchor.transform.position;
        physicsCollider.transform.rotation = _physicalWristAnchor.transform.rotation;
        if (!_physicsEnabled && _targetWrist != null && _physicalWristAnchor != null)
        {
            Quaternion currentAnchorLocalRotation = _physicalWristAnchor.localRotation;
            transform.rotation = _targetWrist.rotation * Quaternion.Inverse(currentAnchorLocalRotation);
            transform.position = _targetWrist.position - (transform.rotation * _physicalWristAnchor.localPosition);
        }
    }
}