using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour {
	public static PlayerManager instance;

	public GameObject _playerPrefab;
	public float _respawnTime;
	public int _startLives;
	public int _playerCount;
	public Transform[] _spawnPoints;

	public Dictionary<int, Player> _playerList;
	public event Action<Player, Player> OnPlayerKill;
	public event Action<Player> OnPlayerSpawn;

	private int _deadPlayers;

	public class Player {
		public int id;
		public int lives;
		public int kills;
		public PlayerController controller;
	}

	void Awake() {
		instance = this;
	}

	void Start(){
		UiManager.instance.OnRoundEnd += EndRound;
		UiManager.instance.OnRoundStart += StartRound;
	}

	void OnDestroy() {
		UiManager.instance.OnRoundEnd -= EndRound;
		UiManager.instance.OnRoundStart -= StartRound;
	}

	public void EndRound(){
		for (var i = 0; i < _playerList.Count; i++) {
			var controller = _playerList[i].controller;
			if(controller != null && controller.gameObject != null){
				Destroy(controller.gameObject);
			}
			_playerList[i] = null;
		}
		_playerList.Clear ();
	}

	public void StartRound(){
		_deadPlayers = 0;
		_playerList = new Dictionary<int, Player>();
		for (var i = 0; i < _playerCount; i++) {
			StartCoroutine(RespawnPlayer(i, 0f, i));
		}
	}

	public void AddPlayer(PlayerController player) {
		if (_playerList.ContainsKey(player._playerId) == false) {
			_playerList.Add(player._playerId, new Player() {
				id = player._playerId,
				lives = _startLives, 
				controller = player, 
				kills = 0
			});
		}
		else {
			_playerList[player._playerId].controller = player;
		}
	}


	public void KillPlayer(int killerId, PlayerController player) {
		_playerList[killerId].kills++;

		var lives = --_playerList[player._playerId].lives;
		_playerList[player._playerId].controller = null;

		var roundEnd = false;
		if (lives > 0) {
			StartCoroutine (RespawnPlayer (player._playerId, _respawnTime));
		} else {
			if(++_deadPlayers >= _playerCount - 1){
				roundEnd = true;
			}
		}

		if (OnPlayerKill != null) {
			OnPlayerKill(_playerList[killerId], _playerList[player._playerId]);
		}
		StartCoroutine (player.DeathAnimation ());

		if (roundEnd) {
			UiManager.instance.State = UiManager.GameState.Select;
		}
	}

	Vector3 GetSpawn(int overwriteIndex = -1) {
		if (overwriteIndex >= 0) {
			return _spawnPoints[overwriteIndex].position;
		}
		return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)].position;
	}

	IEnumerator RespawnPlayer(int id, float delay, int spawnIndex = -1) {
		if (delay > 0) {
			yield return new WaitForSeconds(delay);
		}
		var newPlayer = Instantiate(_playerPrefab).GetComponent<PlayerController>();
		newPlayer._playerId = id;
		newPlayer.transform.position = GetSpawn(spawnIndex);
		AddPlayer(newPlayer);

		if (OnPlayerSpawn != null) {
			OnPlayerSpawn(_playerList[id]);
		}
		yield return 1;
	}
}
