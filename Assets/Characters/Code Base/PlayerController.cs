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
	[SerializeField] private PlayerPhysics player1physics;

	[Header("Player 2 Refs")]
	[SerializeField] private Rigidbody player2rigidbody;
	[SerializeField] private PlayerPhysics player2physics;

	[Header("Physics")]
	[SerializeField] private LayerMask walkableLayer;

	private float distanceToFloor_1 = 0f;
	private float distanceToFloor_2 = 0f;

	private bool isJumping_1 => distanceToFloor_1 > 0.01f;
	private bool isJumping_2 => distanceToFloor_2 > 0.01f;

	private bool isDashing;
	private IEnumerator dashTimer;
	private WaitForSeconds dashWaiter;

	private bool swapOnCooldown;
	private IEnumerator swapTimer;
	private WaitForSeconds swapWaiter;

	private PlayerConnectionState connectionState;
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


		 if(player1anim){
			 CharacterAnim1.SetBool("walk", true);
		 } else{
			 CharacterAnim1.SetBool("walk", false);
		 }
		if (player1rigidbody.velocity.magnitude < 3.5 ){
			player1anim = false;
		}
		 if(player2anim){
			 CharacterAnim2.SetBool("walk", true);
		 } else{
			 CharacterAnim2.SetBool("walk", false);
		 }
		if (player2rigidbody.velocity.magnitude < 3.5 ){
			player2anim = false;
		}
		

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

	private void ConnectBodyPart(PlayerId caller, BodyPartController bodyPart) {
		if (caller == PlayerId.Player1) {
			player1bodyparts.Push(bodyPart);
			bodyPart.gameObject.SetActive(false);
		}
		else {
			player2bodyparts.Push(bodyPart);
			bodyPart.gameObject.SetActive(false);
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

	private void DisconnectBodyPart(PlayerId caller) {
		if (caller == PlayerId.Player1) {
			var body_part = player1bodyparts.Pop();
			body_part.gameObject.SetActive(true);
			SpawnCloseTo(body_part.transform, player1rigidbody.transform);
		}
		else if (caller == PlayerId.Player2) {
			var body_part = player2bodyparts.Pop();
			body_part.gameObject.SetActive(true);
			SpawnCloseTo(body_part.transform, player2rigidbody.transform);
		}

		void SpawnCloseTo(Transform spawned, Transform source) {
			var angle = Random.Range(-1f, 1f);
			Vector3 position = (new Vector3(Mathf.Sin(angle), Mathf.Cos(angle)) * 1.1f) + source.position;
			spawned.position = position;
			spawned.gameObject.SetActive(true);
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