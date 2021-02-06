using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPartController : MonoBehaviour, Connectable {
	private new Rigidbody rigidbody;
	private SpringJoint joint;

	ConnectableType Connectable.connectableType => ConnectableType.BodyPart;
	Rigidbody Connectable.rigidbody => rigidbody;

	private void Awake() {
		rigidbody = GetComponent<Rigidbody>();
		joint = GetComponent<SpringJoint>();
		joint.connectedAnchor = transform.position;
	}

	private void FixedUpdate()
	{
		rigidbody.AddForce(-Physics.gravity);
	}

	private void Update() {
		if(!joint.connectedBody) joint.connectedAnchor = transform.position;
	}

	public void AttachTo(Rigidbody rigidbody)
	{
		if (rigidbody)
		{
			joint.connectedBody = rigidbody;
			joint.connectedAnchor = Vector3.zero;
		}
		else
		{
			joint.connectedBody = null;
			joint.connectedAnchor = transform.position;
		}
	}

	public void AttachTo(Vector3 worldAnchor)
	{
		joint.connectedBody = null;
		joint.connectedAnchor = worldAnchor;
	}
}