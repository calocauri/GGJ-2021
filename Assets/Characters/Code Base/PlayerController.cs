using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerConnectionState {
	Disconnected,
	ConnectedPlayer1Lead,
	ConnectedPlayer2Lead
}

public enum PlayerId {
	Player1,
	Player2
}

public class PlayerController : MonoBehaviour, PlayerPhysicsDelegate {

	[SerializeField] private float airModifier = 0.5f;
	[SerializeField] private float connectDistance = 2f;
	[SerializeField] private float swapCooldown = 0.5f;

	[Header("Time Slowdown")]
	[SerializeField] private AnimationCurve timeSlowdownInCurve;
	[SerializeField] private float slowdownInDuration;
	[SerializeField] private AnimationCurve timeSlowdownOutCurve;
	[SerializeField] private float slowdownOutDuration;

	[Header("Player 1 Settings")]
	[SerializeField] private float acceleration_1 = 70f;
	[SerializeField] private float topSpeed_1 = 5f;
	[SerializeField] private float jumpMinImpulse = 7f;
	[SerializeField] private float jumpMaxImpulse = 20f;
	[SerializeField] private float jumpMaxCharge = 1f;

	[Header("Player 2 Settings")]
	[SerializeField] private float acceleration_2 = 50f;
	[SerializeField] private float topSpeed_2 = 4f;
	[SerializeField] private float dashMinImpulse = 10f;
	[SerializeField] private float dashMaxImpulse = 30f;
	[SerializeField] private float dashDuration = 2f;
	[SerializeField] private float topSpeedDashing = 7.5f;
	[SerializeField] private float dashMaxCharge = 1f;
	[SerializeField] private float rockImpulse = 30f;

	[Header("Player 1 Refs")]
	[SerializeField] private Rigidbody player1rigidbody;
	[SerializeField] private PlayerPhysics player1physics;
	[SerializeField] private Material player1material;

	[Header("Player 2 Refs")]
	[SerializeField] private Rigidbody player2rigidbody;
	[SerializeField] private PlayerPhysics player2physics;
	[SerializeField] private Material player2material;

	[Header("Physics")]
	[SerializeField] private LayerMask walkableLayer;

	private float distanceToFloor_1 = 0f;
	private float distanceToFloor_2 = 0f;

	private bool isJumping_1 => distanceToFloor_1 > 0.15f;
	private bool isJumping_2 => distanceToFloor_2 > 0.15f;

	private bool isDashing;
	private IEnumerator dashTimer;
	private WaitForSeconds dashWaiter;

	private bool swapOnCooldown;
	private IEnumerator swapTimer;
	private WaitForSeconds swapWaiter;

	private PlayerConnectionState connectionState;
	public PlayerConnectionState ConnectionState => connectionState;

	private List<Connectable> player1connectionTargets = new List<Connectable>();
	private Connectable player1connectionTarget => player1connectionTargets[player1connectionTargets.Count - 1];

	private List<Connectable> player2connectionTargets = new List<Connectable>();
	private Connectable player2connectionTarget => player2connectionTargets[player2connectionTargets.Count - 1];

	private Stack<BodyPartController> player1bodyparts = new Stack<BodyPartController>();
	private Stack<BodyPartController> player2bodyparts = new Stack<BodyPartController>();

	public Animator CharacterAnim1;
	public Animator CharacterAnim2;

	private bool player1anim;
	private bool player2anim;

	private bool isJumpCharging;
	private IEnumerator jumpCharger;
	private float jumpCharge;

	private bool isDashCharging;
	private IEnumerator dashCharger;
	private float dashCharge;

	private bool isRock;
	public bool IsRock => isRock;

	private IEnumerator timeSlowdownAnimation;

	private void Awake() {
		player1physics.@delegate = this;
		player2physics.@delegate = this;
	}

	#region Player Physics Delegate
	void PlayerPhysicsDelegate.OnTriggerEnter(Rigidbody source, Collider other) {
		var body_part = other.GetComponent<BodyPartController>();
		if (body_part) {
			if (source == player1rigidbody) {
				player1connectionTargets.Add(body_part);
			}
			else {
				player2connectionTargets.Add(body_part);
			}
			return;
		}

		var player_physics = other.GetComponent<PlayerPhysics>();
		if (player_physics == player1physics) {
			player2connectionTargets.Add(player_physics);
			return;
		}
		else if (player_physics == player2physics) {
			player1connectionTargets.Add(player_physics);
			return;
		}
	}

	private void Update() {
		if (player1anim) {
			CharacterAnim1.SetBool("walk", true);
		}
		else {
			CharacterAnim1.SetBool("walk", false);
		}
		if (player1rigidbody.velocity.magnitude < 3.5) {
			player1anim = false;
		}

		if (player2anim) {
			CharacterAnim2.SetBool("walk", true);
		}
		else {
			CharacterAnim2.SetBool("walk", false);
		}
		if (player2rigidbody.velocity.magnitude < 3.5) {
			player2anim = false;
		}

		print($"distance to floor: {distanceToFloor_1}");
	}

	void PlayerPhysicsDelegate.OnTriggerExit(Rigidbody source, Collider other) {
		var body_part = other.GetComponent<BodyPartController>();
		if (body_part) {
			if (source == player1rigidbody) {
				player1connectionTargets.Remove(body_part);
			}
			else {
				player2connectionTargets.Remove(body_part);
			}
			return;
		}

		var player_physics = other.GetComponent<PlayerPhysics>();
		if (player_physics == player1physics) {
			player2connectionTargets.Remove(player_physics);
			return;
		}
		else if (player_physics == player2physics) {
			player1connectionTargets.Remove(player_physics);
			return;
		}
	}
	#endregion

	#region Input System
	public void OnMove_1(InputValue value) {
		if (connectionState == PlayerConnectionState.Disconnected || connectionState == PlayerConnectionState.ConnectedPlayer1Lead) {
			var direction = value.Get<Vector2>();
			Move(direction, acceleration_1, topSpeed_1, player1rigidbody, clamp: true);
			player1anim = true;
		}
	}

	public void OnMove_2(InputValue value) {
		if (connectionState == PlayerConnectionState.Disconnected || connectionState == PlayerConnectionState.ConnectedPlayer2Lead) {
			var direction = value.Get<Vector2>();
			Move(direction, acceleration_2, topSpeed_2, player2rigidbody, clamp: !isDashing);
			player2anim = true;
		}
	}

	public void OnJump(InputValue value) {
		if (connectionState != PlayerConnectionState.ConnectedPlayer2Lead) {
			if (value.isPressed && !isJumping_1) {
				isJumpCharging = true;
				jumpCharger = CJumpCharge();
				StartCoroutine(jumpCharger);
			}
			else {
				isJumpCharging = false;
				if (jumpCharger != null) StopCoroutine(jumpCharger);
				if (!isJumping_1) Jump();
				jumpCharge = 0f;
				player1material.SetFloat($"_Charge", 0f);
			}
		}

		IEnumerator CJumpCharge() {
			while (isJumpCharging) {
				jumpCharge += Time.unscaledDeltaTime;
				jumpCharge = Mathf.Clamp(jumpCharge, 0f, jumpMaxCharge);
				player1material.SetFloat($"_Charge", jumpCharge / jumpMaxCharge);
				yield return null;
			}
			jumpCharger = null;
		}
	}

	public void OnDash(InputValue value) {
		if (!isDashing && connectionState != PlayerConnectionState.ConnectedPlayer1Lead) {
			if (value.isPressed) {
				isDashCharging = true;
				dashCharger = CDashCharge();
				StartCoroutine(dashCharger);

				// if (timeSlowdownAnimation != null) StopCoroutine(timeSlowdownAnimation);
				// timeSlowdownAnimation = CSlowdownTimeIn();
				// StartCoroutine(timeSlowdownAnimation);

				var velocity = player2rigidbody.velocity;
				velocity.y *= 0f;
				player2rigidbody.velocity = velocity;
				player2rigidbody.useGravity = false;
				// player2rigidbody.isKinematic = true;
			}
			else {
				isDashCharging = false;
				player2rigidbody.useGravity = true;

				StopCoroutine(dashCharger);
				isDashing = true;
				Dash();
				dashCharge = 0f;
				player2material.SetFloat($"_Charge", 0f);

				// if (timeSlowdownAnimation != null) StopCoroutine(timeSlowdownAnimation);
				// timeSlowdownAnimation = CSlowdownTimeOut();
				// StartCoroutine(timeSlowdownAnimation);
			}
		}

		IEnumerator CDashCharge() {
			while (isDashCharging) {
				dashCharge += Time.unscaledDeltaTime;
				dashCharge = Mathf.Clamp(dashCharge, 0f, dashMaxCharge);
				player2material.SetFloat($"_Charge", dashCharge / dashMaxCharge);
				yield return null;
			}
			jumpCharger = null;
		}
	}

	public void OnRock(InputValue value) {
		if (value.isPressed) {
			isRock = !isRock;

			if (isRock) {
				if (isJumping_2) {
					RockDown();
				}
			}

			// Set rock graphic here!!!
		}
	}

	public void OnDPad(InputValue value) {
		var dpad_vector = value.Get<Vector2>();

		if (dpad_vector.y > 0f) { // UP
			if (player1bodyparts.Count != 0 && connectionState != PlayerConnectionState.ConnectedPlayer2Lead) {
				DisconnectBodyPart(PlayerId.Player1);
			}
			else {
				Disconnect();
			}
		}
		else if (dpad_vector.y < 0f) { // DOWN
			if (player1connectionTargets.Count > 0) {
				if (player1connectionTarget.connectableType == ConnectableType.BodyPart) {
					ConnectBodyPart(PlayerId.Player1, player1connectionTarget.rigidbody.GetComponent<BodyPartController>());
				}
				else if (player1connectionTarget.connectableType == ConnectableType.Head) {
					Connect(PlayerId.Player1);
				}
			}
		}
	}

	public void OnSwap() {
		if (connectionState != PlayerConnectionState.Disconnected && !swapOnCooldown) {
			swapOnCooldown = true;
			Swap();
		}
	}

	public void OnConnect() {
		if (player2connectionTargets.Count > 0) {
			if (player2connectionTarget.connectableType == ConnectableType.BodyPart) {
				ConnectBodyPart(PlayerId.Player2, player2connectionTarget.rigidbody.GetComponent<BodyPartController>());
			}
			else if (player2connectionTarget.connectableType == ConnectableType.Head) {
				Connect(PlayerId.Player2);
			}
		}
	}

	public void OnDisconnect() {
		if (player2bodyparts.Count != 0 && connectionState != PlayerConnectionState.ConnectedPlayer1Lead) {
			DisconnectBodyPart(PlayerId.Player2);
		}
		else {
			Disconnect();
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

		if (rigidbody == player2rigidbody && isDashCharging && isJumping_2) {
			horizontal *= 0.01f;
		}

		if (rigidbody == player2rigidbody && isDashing) {
			return;
		}

		if (rigidbody == player1rigidbody && isJumping_1) horizontal *= airModifier;
		if (rigidbody == player2rigidbody && isJumping_2) horizontal *= airModifier;

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
		var impulse = Mathf.Lerp(jumpMinImpulse, jumpMaxImpulse, (jumpCharge / jumpMaxCharge));
		var vertical = transform.up * impulse;
		player1rigidbody.AddForce(vertical, ForceMode.Impulse);
	}

	private void Dash() {
		var impulse = Mathf.Lerp(dashMinImpulse, dashMaxImpulse, (dashCharge / dashMaxCharge));
		var horizontal = player2rigidbody.transform.right * impulse;
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

	private void RockDown() {
		var impulse = rockImpulse;
		var vertical = Vector3.down * impulse;
		player2rigidbody.AddForce(vertical, ForceMode.Impulse);
	}

	private void Swap() {
		if (connectionState == PlayerConnectionState.ConnectedPlayer1Lead) {
			player2rigidbody.transform.position = player1rigidbody.transform.position;
			player2rigidbody.velocity = player1rigidbody.velocity;
			player2rigidbody.gameObject.SetActive(true);
			player1rigidbody.gameObject.SetActive(false);
			connectionState = PlayerConnectionState.ConnectedPlayer2Lead;
		}
		else if (connectionState == PlayerConnectionState.ConnectedPlayer2Lead) {
			player1rigidbody.transform.position = player2rigidbody.transform.position;
			player1rigidbody.velocity = player2rigidbody.velocity;
			player1rigidbody.gameObject.SetActive(true);
			player2rigidbody.gameObject.SetActive(false);
			connectionState = PlayerConnectionState.ConnectedPlayer1Lead;
		}

		swapTimer = CSwapTimer();
		StartCoroutine(swapTimer);

		IEnumerator CSwapTimer() {
			swapWaiter = new WaitForSeconds(swapCooldown);
			yield return swapWaiter;
			swapOnCooldown = false;
			swapTimer = null;
		}
	}

	private void Connect(PlayerId caller) {
		if (connectionState == PlayerConnectionState.Disconnected) {
			var distance = Vector3.Distance(player1rigidbody.transform.position, player2rigidbody.transform.position);
			if (distance <= connectDistance) {
				if (caller == PlayerId.Player1) {
					player2rigidbody.gameObject.SetActive(false);
					connectionState = PlayerConnectionState.ConnectedPlayer1Lead;
				}
				else {
					player1rigidbody.gameObject.SetActive(false);
					connectionState = PlayerConnectionState.ConnectedPlayer2Lead;
				}
			}
		}
	}

	public void RecoverBodyparts(Rigidbody rigidbody, ref List<BodyPartController> bodyParts)
	{
		PlayerId caller = rigidbody.Equals(player1rigidbody) ? PlayerId.Player1 : PlayerId.Player2;
		Debug.Log(bodyParts.Count);
		foreach (var part in bodyParts)
		{
			ConnectBodyPart(caller, part);
			Debug.Log("connected body part");
		}
		bodyParts.Clear();
	}
	private void ConnectBodyPart(PlayerId caller, BodyPartController bodyPart) {
		if (caller == PlayerId.Player1) {
			player1bodyparts.Push(bodyPart);
			// bodyPart.gameObject.SetActive(false);
			bodyPart.AttachTo(player1rigidbody);
		}
		else
		{
			player2bodyparts.Push(bodyPart);
			// bodyPart.gameObject.SetActive(false);
			bodyPart.AttachTo(player1rigidbody);
		}
	}

	private void Disconnect() {
		if (connectionState != PlayerConnectionState.Disconnected) {
			if (connectionState == PlayerConnectionState.ConnectedPlayer1Lead) {
				SpawnCloseTo(player2rigidbody.transform, player1rigidbody.transform);
			}
			else if (connectionState == PlayerConnectionState.ConnectedPlayer2Lead) {
				SpawnCloseTo(player1rigidbody.transform, player2rigidbody.transform);
			}
			connectionState = PlayerConnectionState.Disconnected;
		}

		void SpawnCloseTo(Transform spawned, Transform source) {
			var angle = Random.Range(-1f, 1f);
			Vector3 position = (new Vector3(Mathf.Sin(angle), Mathf.Cos(angle)) * 1.1f) + source.position;
			spawned.position = position;
			spawned.gameObject.SetActive(true);
		}
	}

	public void ReleaseBodyParts(Rigidbody rigidbody, Nest nest)
	{
		PlayerId caller = rigidbody.Equals(player1rigidbody) ? PlayerId.Player1 : PlayerId.Player2;
		while ( (caller == PlayerId.Player1 ? player1bodyparts : player2bodyparts).Count > 0) DisconnectBodyPart(caller, nest);
	}
	private void DisconnectBodyPart(PlayerId caller, Nest nest = null)
	{
		var body_part = caller == PlayerId.Player1 ? player1bodyparts.Pop() : player2bodyparts.Pop();
		if (nest) nest.Attach(body_part);
		else body_part.AttachTo(null);
	}

	private IEnumerator CSlowdownTimeIn() {
		float elapsed = 0f;
		float duration = slowdownInDuration;

		float initial_time_scale = Time.timeScale;
		float final_time_scale = 0f;

		while (elapsed < duration) {
			float t = timeSlowdownInCurve.Evaluate(elapsed / duration);
			SetValues(t);
			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}
		SetValues(1f);

		void SetValues(float t) {
			Time.timeScale = Mathf.Lerp(initial_time_scale, final_time_scale, t);
		}
	}

	private IEnumerator CSlowdownTimeOut() {
		float elapsed = 0f;
		float duration = slowdownOutDuration;

		float initial_time_scale = Time.timeScale;
		float final_time_scale = 1f;

		while (elapsed < duration) {
			float t = timeSlowdownOutCurve.Evaluate(elapsed / duration);
			SetValues(t);
			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}
		SetValues(1f);

		void SetValues(float t) {
			Time.timeScale = Mathf.Lerp(initial_time_scale, final_time_scale, t);
		}
	}
}

public interface Connectable {
	ConnectableType connectableType { get; }
	Rigidbody rigidbody { get; }
}

public enum ConnectableType {
	Head,
	BodyPart
}