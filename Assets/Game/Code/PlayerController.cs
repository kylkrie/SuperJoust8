using System.Collections;
using UnityEngine;
using Rewired;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour {
	public int _playerId = 0; // The Rewired player id of this character

	public Vector2 _moveForce;
	public Vector2 _bounceForce;
	public float _maxSpeedX;
	public BoxCollider2D _playerHitBox;
	public BoxCollider2D _platformHitBox;
	public GameObject _spriteContainer;
	public Rigidbody2D _rb;
	public Animator _anim;
	public AudioController _audioController;
	public SpriteRenderer _rider;

	private float _horizontal;
	private Player _player; // The Rewired Player
	private bool _flap;
	private bool _flapping;
	private Vector2 _prevVelocity;
	private bool _isGrounded;
	private bool _flipped;
	private float _accelX;
	private bool _stopping;
	private bool _isDead;

	[System.NonSerialized] // Don't serialize this so the value is lost on an editor script recompile.
	private bool initialized;

	private void Start() {
		_prevVelocity = Vector2.zero;
		StartCoroutine(SpawnFrames());
	}

	IEnumerator SpawnFrames() {
		_audioController.PlayClip ("Spawn");
		_playerHitBox.enabled = false;
		for (var i = 0; i < 5; i++) {
			yield return new WaitForSeconds(0.3f);
			_spriteContainer.SetActive(false);
			yield return new WaitForSeconds(0.1f);
			_spriteContainer.SetActive(true);
		}
		_playerHitBox.enabled = true;
	}

	public IEnumerator DeathAnimation(){
		_isDead = true;
		_isGrounded = false;
		_rider.enabled = false;
		_playerHitBox.enabled = false;
		_platformHitBox.enabled = false;
		yield return new WaitForSeconds (0.2f);
		_rb.velocity = _maxSpeedX * (_rb.position.x > 0 ? Vector2.right : Vector2.left);
		if ((_rb.velocity.x > 0 && _flipped) || (_rb.velocity.x < 0 && !_flipped)) {
			FlipScaleX();
		}
		for (var i = 0; i < 2; i++) {
			_flap = true;
			_rb.AddForce(Vector2.up * _moveForce.y);
			_audioController.PlayClip("Flap");
			yield return new WaitForSeconds (0.2f);
			_flap = false;
			yield return new WaitForSeconds (0.1f);
			_flap = true;
			_rb.AddForce(Vector2.up * _moveForce.y);
			_audioController.PlayClip("Flap");
			yield return new WaitForSeconds (0.2f);
			_flap = false;
			yield return new WaitForSeconds (0.3f);
		}
		Destroy (gameObject);
	}

	private void Initialize() {
		// Get the Rewired Player object for this player.
		_player = ReInput.players.GetPlayer(_playerId);

		initialized = true;
	}

	private void Update() {
		if (!ReInput.isReady)
			return; // Exit if Rewired isn't ready. This would only happen during a script recompile in the editor.
		if (!initialized) Initialize(); // Reinitialize after a recompile in the editor

		if (_isDead == false) {
			GetInput ();
			ProcessInput();
			DoPhysics ();
		}
		UpdateAnimation();
		_prevVelocity = _rb.velocity;
	}

	void FlipScaleX() {
		var scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
		_flipped = !_flipped;
	}

	void UpdateAnimation() {
		if ((_isGrounded == false && ((_horizontal > 0 && _flipped) || (_horizontal < 0 && !_flipped))) || 
			(_isGrounded && ((_rb.velocity.x > 0 && _flipped) || (_rb.velocity.x < 0 && !_flipped)))) {
			FlipScaleX();
		}
		_anim.SetBool("IsGrounded", _isGrounded);
		_anim.SetFloat("xSpeed", Mathf.Abs(_rb.velocity.x));
		_anim.SetFloat("xAcceleration", _accelX);
		_anim.SetBool("Flap", _flap);
	}

	void DoPhysics() {
		if (_rb.position.x > 13.8f) {
			_rb.position = new Vector2(-13.7f, _rb.position.y);
		}
		if (_rb.position.x < -13.8f) {
			_rb.position = new Vector2(13.7f, _rb.position.y);
		}

		var vel = _rb.velocity;
		vel.x = Mathf.Clamp(vel.x, -_maxSpeedX, _maxSpeedX);

		if (_accelX < 0 && _isGrounded) {
			if (_stopping == false) {
				_audioController.PlayClip("Stop");
			}
			_stopping = true;
			vel.x += vel.x > 0 ? -0.025f : 0.025f;
		}
		else {
			if (_stopping) {
				_audioController.StopClip("Stop");
			}
			_stopping = false;
		}

		if (Mathf.Abs(vel.x) < 0.1f) {
			vel.x = 0f;
		}

		_rb.velocity = vel;
		_accelX = Mathf.Abs(_rb.velocity.x) - Mathf.Abs(_prevVelocity.x);
	}

	private void GetInput() {
		// Get the input from the Rewired Player. All controllers that the Player owns will contribute, so it doesn't matter
		// whether the input is coming from a joystick, the keyboard, mouse, or a custom controller.

		_horizontal = _player.GetAxis("Move"); // get input by name or action id
		_flap = _player.GetButton("Fly");
		if (_flap == false) {
			_flapping = false;
		}
	}

	private void OnCollisionEnter2D(Collision2D collision) {
		if (_isDead) {
			return;
		}
		var other = collision.gameObject.GetComponent<PlayerController>();
		var forceBounce = false;
		if (other != null) {
			if (Mathf.Abs(other._rb.position.y - _rb.position.y) < 0.05f) {
				_audioController.PlayClip("Collide");
				forceBounce = true;
			}
			else if (other._rb.position.y < _rb.position.y) {
				PlayerManager.instance.KillPlayer(_playerId, other);
				_audioController.PlayClip("Kill");
				forceBounce = true;
			}
		}
		var contact = collision.contacts[0];
		if (contact.normal != Vector2.up || forceBounce) {
			var forward = _rb.velocity.normalized;
			if (forward.Equals(Vector2.zero)) {
				var diff = collision.transform.position - transform.position;
				forward = new Vector2(diff.x, diff.y);
			}
			forward -= contact.normal;
			forward.x *= _bounceForce.x;
			forward.y *= _bounceForce.y;
			var dot = Vector3.Dot(contact.normal, (-forward));
			dot *= 2;
			var reflection = contact.normal*dot;
			reflection = reflection + forward;
			_rb.velocity = reflection.normalized*forward.magnitude;
			if(!forceBounce){
				_audioController.PlayClip("Bounce");
			}
		}
		else {
			_anim.SetTrigger(("Land"));
			_isGrounded = true;
		}
	}

	private void OnCollisionExit2D(Collision2D collision) {
		if (collision.gameObject.GetComponent<PlayerController>() == null) {
			if (!_flap) {
				_anim.SetTrigger("Fall");
			}
			_isGrounded = false;
		}
	}

	private void ProcessInput() {
		var yForce = 0f;
		if (_flap && !_flapping) {
			_flapping = true;
			yForce = _moveForce.y;
			_audioController.PlayClip("Flap");
		}	
		_rb.AddForce(new Vector2(_horizontal * _moveForce.x * Mathf.Clamp(Mathf.Abs(_rb.velocity.x * _rb.velocity.x), 1.6f, 6f) * 0.5f, yForce));
	}
}