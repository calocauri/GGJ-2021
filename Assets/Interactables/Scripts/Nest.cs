using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Interactables;
using UnityEngine;
using UnityEngine.Serialization;

public class Nest : MonoBehaviour
{
    
    [FormerlySerializedAs("_player")] [SerializeField] private PlayerController player;
    [SerializeField] private int requiredParts;
    private Vector3[] _slotPositions;
    [SerializeField] float increment = 0.5f;
    [SerializeField] private List<BodyPartController> bodyParts = new List<BodyPartController>();
    [SerializeField] private Activation activation;

    private void Awake()
    {
        CreateSlots();
    }
    private void OnValidate()
    {
        CreateSlots();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.gameObject.CompareTag("Player")) return;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.transform.gameObject.CompareTag("Player")) return;
        Debug.Log(bodyParts.Count);
        player.RecoverBodyparts(other.transform.gameObject.GetComponent<Rigidbody>(), ref bodyParts);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.transform.gameObject.CompareTag("Player")) return;
        var body = other.transform.GetComponent<Rigidbody>();
        player.ReleaseBodyParts(body, this);
    }

    public void Attach(BodyPartController bodyPart)
    {
        bodyPart.AttachTo(_slotPositions[bodyParts.Count]);
        bodyParts.Add(bodyPart);
        Debug.Log(bodyParts.Count);
        if (bodyParts.Count == requiredParts) activation.Run();
    }

    private void CreateSlots()
    {
        _slotPositions = new Vector3[requiredParts];
        for (int i = requiredParts - 1; i >= 0; i--)
        {
            _slotPositions[i] = transform.TransformPoint(new Vector3(0f, increment * i, 0f));
        }
    }
    
    private void OnDrawGizmos()
    {
        if (_slotPositions != null) foreach (var slot in _slotPositions.ToList()) Gizmos.DrawSphere(slot, 0.2f);
    }
}
