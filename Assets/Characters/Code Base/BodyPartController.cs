using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPartController : MonoBehaviour, Connectable {
	private new Rigidbody rigidbody;

	ConnectableType Connectable.connectableType => ConnectableType.BodyPart;
	Rigidbody Connectable.rigidbody => rigidbody;

	private void Awake() {
		rigidbody = GetComponent<Rigidbody>();
	}
}