using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour {

	[SerializeField] private TextMeshProUGUI _textBox;
	private string _startText;
	private int _playerId;

	void Awake () {
		_startText = _textBox.text;
		PlayerManager.instance.OnPlayerKill += OnPlayerKill;
	}

	private void OnPlayerKill(PlayerManager.Player killer, PlayerManager.Player victim) {
		var player = _playerId == killer.id ? killer : (_playerId == victim.id ? victim : null);
		if (player != null) {
			UpdateStats(player);
		}
	}

	public void UpdateStats(PlayerManager.Player player) {
		_playerId = player.id;

		_textBox.text = string.Format(_startText, _playerId + 1, player.lives, player.kills);
	}
}
