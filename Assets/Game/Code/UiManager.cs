using UnityEngine;

public class UiManager : MonoBehaviour {
	public static UiManager instance;

	[SerializeField] private RectTransform _playerStatsPanel;
	[SerializeField] private GameObject _playerStatsPrefab;

	void Awake () {
		instance = this;
	}

	void Start() {
		foreach (var player in PlayerManager.instance._playerList) {
			var ui = Instantiate(_playerStatsPrefab).GetComponent<PlayerStats>();
			ui.UpdateStats(player.Value);
			ui.transform.SetParent(_playerStatsPanel, false);
		}
	}

}
