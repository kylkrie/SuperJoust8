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
	public GameObject _spriteContainer;
	public Rigidbody2D _rb;
	public Animator _anim;
	public AudioSource _audioSource;
	public AudioSource _killSource;
	public AudioClip _jumpClip;
	public AudioClip _killClip;
	public AudioClip _stopClip;

	private float _horizontal;
	private Player _player; // The Rewired Player
	private bool _flap;
	private bool _flapping;
	private Vector2 _prevVelocity;
	private bool _isGrounded;
	private bool _flipped;
	private float _accelX;
	private bool _stopping;

	[System.NonSerialized] // Don't serialize this so the value is lost on an editor script recompile.
	private bool initialized;

	private void Awake() {
		_prevVelocity = Vector2.zero;
		StartCoroutine(SpawnFrames());
	}

	IEnumerator SpawnFrames() {
		_playerHitBox.enabled = false;
		for (var i = 0; i < 5; i++) {
			yield return new WaitForSeconds(0.3f);
			_spriteContainer.SetActive(false);
			yield return new WaitForSeconds(0.1f);
			_spriteContainer.SetActive(true);
		}
		_playerHitBox.enabled = true;
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

		GetInput();
		ProcessInput();
		DoPhysics();
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
				PlayClip(_stopClip);
			}
			_stopping = true;
			vel.x += vel.x > 0 ? -0.03f : 0.03f;
		}
		else {
			if (_stopping) {
				StopStoppingClip();
			}
			_stopping = false;
		}

		if (Mathf.Abs(vel.x) < 0.1f) {
			vel.x = 0f;
		}
		_rb.velocity = vel;
		_accelX = Mathf.Abs(_rb.velocity.x) - Mathf.Abs(_prevVelocity.x);
	}

	void StopStoppingClip() {
		if (_audioSource.isPlaying && _audioSource.clip == _stopClip) {
			_audioSource.Stop();
		}
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
		var other = collision.gameObject.GetComponent<PlayerController>();
		var forceBounce = false;
		if (other != null) {
			if (Mathf.Abs(other._rb.position.y - _rb.position.y) < 0.05f) {
				forceBounce = true;
			}
			else if (other._rb.position.y < _rb.position.y) {
				PlayerManager.instance.KillPlayer(_playerId, other);
				PlayClip(_killClip, _killSource);
				forceBounce = true;
			}
		}
		var contact = collision.contacts[0];
		var localContactPoint = contact.point - new Vector2(transform.position.x, transform.position.y);
		Debug.LogError(localContactPoint.y);
		if (localContactPoint.y > 0.05f || forceBounce) {
			var forward = _rb.velocity.normalized;
			if (forward.Equals(Vector2.zero)) {
				var diff = collision.transform.position - transform.position;
				forward = new Vector2(diff.x, diff.y);
			}
			forward.x *= _bounceForce.x;
			forward.y *= _bounceForce.y;
			var dot = Vector3.Dot(contact.normal, (-forward));
			dot *= 2;
			var reflection = contact.normal*dot;
			reflection = reflection + forward;
			_rb.velocity = reflection.normalized*forward.magnitude;
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

	void PlayClip(AudioClip clip, AudioSource _source = null) {
		if (_source == null) {
			_source = _audioSource;
		}
		if (_source.isPlaying) {
			_source.Stop();
		}
		_source.clip = clip;
		_source.Play();
	}

	private void ProcessInput() {
		var yForce = 0f;
		if (_flap && !_flapping) {
			_flapping = true;
			yForce = _moveForce.y;
			PlayClip(_jumpClip);
		}
		_rb.AddForce(new Vector2(_horizontal * _moveForce.x * Mathf.Max(1f, 10f*_accelX), yForce));
	}
}