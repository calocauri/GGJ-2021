using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Nest : MonoBehaviour
{
    private PlayerController _player;
    [SerializeField][ContextMenuItem("Create Slots", "CreateSlots")] private int requiredParts;
    private Vector3[] _slotPositions;

    private void Awake()
    {
        CreateSlots();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.transform.gameObject.CompareTag("Player")) return;
        if ( _player = other.transform.gameObject.GetComponent<PlayerController>() )
        {
            //TODO: Get children and put inside nest, then activate if children == requiredParts
        }
        else
        {
            Debug.LogWarning($"Could not find component \"PlayerController\"", other.transform.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _player = null;
    }
    
    //TODO: subscribe to children pick-up action
    public void OnChildrenPickup(PlayerController player)
    {
        
    }

    private void CreateSlots()
    {
        var increment = 0.5f;
        _slotPositions = new Vector3[requiredParts];
        for (int i = requiredParts - 1; i >= 0; i--)
        {
            _slotPositions[i] = transform.TransformPoint(new Vector3(0f, increment * i, 0f));
            
        }
    }
    
    private void OnDrawGizmos()
    {
        if (_slotPositions != null) foreach (var slot in _slotPositions.ToList()) Gizmos.DrawSphere(slot, 0.1f);
    }
}
