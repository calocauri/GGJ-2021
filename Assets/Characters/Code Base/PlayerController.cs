using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {

	[SerializeField] private float airModifier = 0.5f;

	[Header("Player 1 Settings")]
	[SerializeField] private float acceleration_1 = 70f;
	[SerializeField] private float topSpeed_1 = 5f;
	[SerializeField] private float jumpImpulse = 100f;

	[Header("Player 2 Settings")]
	[SerializeField] private float acceleration_2 = 50f;
	[SerializeField] private float topSpeed_2 = 4f;
	[SerializeField] private float dashImpulse = 100f;
	[SerializeField] private float dashDuration = 2f;
	[SerializeField] private float topSpeedDashing = 7.5f;

	[Header("Player 1 Refs")]
	[SerializeField] private Rigidbody player1rigidbody;

	[Header("Player 2 Refs")]
	[SerializeField] private Rigidbody player2rigidbody;

	[Header("Physics")]
	[SerializeField] private LayerMask walkableLayer;

	private float distanceToFloor_1 = 0f;
	private float distanceToFloor_2 = 0f;

	private bool isJumping_1 => distanceToFloor_1 > 0.01f;
	private bool isJumping_2 => distanceToFloor_2 > 0.01f;

	private bool isDashing;
	private IEnumerator dashTimer;
	private WaitForSeconds dashWaiter;

	#region Input System
	public void OnMove_1(InputValue value) {
		var direction = value.Get<Vector2>();
		Move(direction, acceleration_1, topSpeed_1, player1rigidbody, clamp: true);
	}

	public void OnMove_2(InputValue value) {
		var direction = value.Get<Vector2>();
		Move(direction, acceleration_2, topSpeed_2, player2rigidbody, clamp: !isDashing);
	}

	public void OnJump() {
		if (!isJumping_1) {
			Jump();
		}
	}

	public void OnDash() {
		if (!isDashing) {
			isDashing = true;
			Dash();
		}
	}
	#endregion

	private void FixedUpdate() {
		RaycastHit hit;
		bool did_hit_1 = Physics.Raycast(player1rigidbody.transform.position, Vector3.down, out hit, 100f, walkableLayer);
		if (did_hit_1) {
			distanceToFloor_1 = (player1rigidbody.transform.position - hit.point).y;
		}

		bool did_hit_2 = Physics.Raycast(player2rigidbody.transform.position, Vector3.down, out hit, 100f, walkableLayer);
		if (did_hit_2) {
			distanceToFloor_2 = (player2rigidbody.transform.position - hit.point).y;
		}

		player1rigidbody.rotation = player1rigidbody.velocity.x > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
		player2rigidbody.rotation = player2rigidbody.velocity.x > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
	}

	private void Move(Vector2 direction, float acceleration, float topSpeed, Rigidbody rigidbody, bool clamp) {
		var horizontal = Vector3.right * direction.x * acceleration * Time.fixedDeltaTime;
		if (isJumping_1) horizontal *= airModifier;
		rigidbody.AddForce(horizontal, ForceMode.Impulse);

		var velocity = rigidbody.velocity;
		if (clamp) {
			velocity.x = Mathf.Clamp(velocity.x, -topSpeed, topSpeed);
		}
		else {
			velocity.x = Mathf.Clamp(velocity.x, -topSpeedDashing, topSpeedDashing);
		}
		rigidbody.velocity = velocity;
	}

	private void Jump() {
		var vertical = transform.up * jumpImpulse;
		player1rigidbody.AddForce(vertical, ForceMode.Impulse);
	}

	private void Dash() {
		var horizontal = player2rigidbody.transform.right * dashImpulse;
		player2rigidbody.AddForce(horizontal, ForceMode.Impulse);

		dashTimer = CDashTimer();
		StartCoroutine(dashTimer);

		IEnumerator CDashTimer() {

			dashWaiter = new WaitForSeconds(dashDuration);
			yield return dashWaiter;
			isDashing = false;
			dashTimer = null;
		}
	}
}