using UnityEngine;
using System;

public class UiManager : MonoBehaviour {
	public static UiManager instance;

	[SerializeField] private RectTransform _playerStatsPanel;
	[SerializeField] private GameObject _playerStatsPrefab;

	public event Action OnRoundStart;
	public event Action OnRoundEnd;
	public event Action<GameState> OnStateChange;

	public enum GameState{
		None = 0,
		Select,
		Game,
	}

	[SerializeField]
	private GameState _state;
	private GameState _prevState;
	public GameState State {
		get { return _state; }
		set { 
			if(_state == value){
				return;
			}
			_prevState = _state;
			_state = value;
			if(OnStateChange != null) {
				OnStateChange(_state);
			}
		}
	}

	private Rect _screenRect;
	private int _numPlayers;
	private int _numLives;
	public GameObject _inputMapper;

	void Awake () {
		instance = this;
		_numPlayers = 2;
		_numLives = 3;
#if UNITY_EDITOR
		_screenRect = new Rect (100, 100, 150, 200);
#else
		_screenRect = new Rect (Screen.width/2f - 75, Screen.height/2f - 100, 150, 200);
#endif
		_prevState = GameState.None;
		OnStateChange += HandleOnStateChange;

		Invoke ("InitState", 0.2f);
	}

	void InitState(){
		var tempState = _state;
		_state = GameState.None;
		State = tempState;
	}

	void HandleOnStateChange (GameState state) {
		if (state == GameState.Game) {
			if (OnRoundStart != null) {
				OnRoundStart ();
			}
			SetupPlayerStats();
		} else if (_prevState == GameState.Game) {
			if (OnRoundEnd != null){
				OnRoundEnd();
			}
		}
	}

	void SetupPlayerStats(){
		foreach (var player in PlayerManager.instance._playerList) {
			var ui = Instantiate(_playerStatsPrefab).GetComponent<PlayerStats>();
			ui.UpdateStats(player.Value);
			ui.transform.SetParent(_playerStatsPanel, false);
		}
	}

	void OnGUI(){
		if (_state != GameState.Select || _inputMapper.activeInHierarchy) {
			return;
		}

		GUILayout.BeginArea (_screenRect, "Player Select");

		ConfigArea ("Players", ref _numPlayers, 1, 8);
		ConfigArea ("Lives", ref _numLives, 1, 99);

		if (GUILayout.Button ("Start")) {
			PlayerManager.instance._playerCount = _numPlayers;
			PlayerManager.instance._startLives = _numLives;
			State = GameState.Game;
		}

		GUILayout.EndArea();
	}

	void ConfigArea(string label, ref int value, int min, int max){
		GUILayout.Label (label + ": " + value);
		GUILayout.BeginHorizontal ();
		
		if (value <= min) {
			GUI.enabled = false;
		}
		if (GUILayout.Button ("-")) {
			--value;
		}
		GUI.enabled = true;
		if (value >= max) {
			GUI.enabled = false;
		}
		if (GUILayout.Button ("+")) {
			++value;
		}
		GUI.enabled = true;
		
		GUILayout.EndHorizontal ();
	}

}
