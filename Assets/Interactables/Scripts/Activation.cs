using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interactables
{
    public class Activation : MonoBehaviour
    {
        private enum Type
        {
            Slide,
            Rotate,
            Break
        }

        [FormerlySerializedAs("_type")] [SerializeField] private Type type;
        [Header("Slide")]
        [SerializeField][ContextMenuItem("Set to Current", "SetPosA")] private Vector3 positionA;
        [SerializeField][ContextMenuItem("Set to Current", "SetPosB")] private Vector3 positionB;
        [SerializeField] private float slideDuration;
        [Header("Rotate")]
        [SerializeField][ContextMenuItem("Set to Current", "SetRotA")] private Vector3 eulerRotationA;
        [SerializeField][ContextMenuItem("Set to Current", "SetRotB")] private Vector3 eulerRotationB;
        [SerializeField] private float rotateDuration;

        private void SetPosA() => positionA = transform.position;
        private void SetPosB() => positionB = transform.position;
        private void SetRotA() => eulerRotationA = transform.rotation.eulerAngles;
        private void SetRotB() => eulerRotationB = transform.rotation.eulerAngles;

        public Coroutine Run()
        {
            switch (type)
            {
                case Type.Slide:
                    return StartCoroutine(BeginSlide());
                case Type.Rotate:
                    return StartCoroutine(BeginRotate());
                case Type.Break:
                    break;
            }
 
            return null;
        }

        IEnumerator BeginSlide()
        {
            var time = 0f;
            yield return new WaitUntil(SlideDone);
            
            bool SlideDone()
            {
                transform.position = Vector3.Lerp(positionA, positionB, Mathf.Min(1f, (time += Time.deltaTime) / slideDuration));
                return time >= slideDuration;
            }
        }
        IEnumerator BeginRotate()
        {
            var time = 0f;
            yield return new WaitUntil(RotationDone);

            bool RotationDone()
            {
                transform.rotation = Quaternion.Lerp(Quaternion.Euler(eulerRotationA), Quaternion.Euler(eulerRotationB), Mathf.Min(1f, (time += Time.deltaTime) / rotateDuration));
                return time >= rotateDuration;
            }
        }
    }
}