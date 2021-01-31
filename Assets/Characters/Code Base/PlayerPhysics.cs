using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPhysics : MonoBehaviour, Connectable {

	private new Rigidbody rigidbody;

	public PlayerPhysicsDelegate @delegate { private get; set; }

	ConnectableType Connectable.connectableType => ConnectableType.Head;
	Rigidbody Connectable.rigidbody => rigidbody;

	private void Awake() {
		rigidbody = GetComponent<Rigidbody>();
	}

	#region Physics	
	private void OnTriggerEnter(Collider other) {
		@delegate?.OnTriggerEnter(rigidbody, other);
	}

	private void OnTriggerExit(Collider other) {
		@delegate?.OnTriggerExit(rigidbody, other);
	}
	#endregion
}

public interface PlayerPhysicsDelegate {
	void OnTriggerEnter(Rigidbody rigidbody, Collider other);
	void OnTriggerExit(Rigidbody rigidbody, Collider other);
}