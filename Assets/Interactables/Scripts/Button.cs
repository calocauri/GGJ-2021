using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Interactables
{
    public class Button : MonoBehaviour
    {
        [SerializeField] private UnityEvent onActivate;
        private Coroutine _beginActivation;

        private void Awake()
        {
            onActivate.AddListener(() => Debug.Log("button pressed", this.gameObject));
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            _beginActivation = StartCoroutine(Activate());
        }

        private void OnCollisionExit(Collision other)
        {
            StopCoroutine(_beginActivation);
        }

        private IEnumerator Activate()
        {
            //TODO: Wait for press animation
            yield return null;
            onActivate.Invoke();
        }
    }
}