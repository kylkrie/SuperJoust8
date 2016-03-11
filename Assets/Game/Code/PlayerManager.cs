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

	public class Player {
		public int id;
		public int lives;
		public int kills;
		public PlayerController controller;
	}

	void Awake() {
		instance = this;
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

		if (lives > 0) {
			StartCoroutine(RespawnPlayer(player._playerId, _respawnTime));
		}

		if (OnPlayerKill != null) {
			OnPlayerKill(_playerList[killerId], _playerList[player._playerId]);
		}
		Destroy(player.gameObject);
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
