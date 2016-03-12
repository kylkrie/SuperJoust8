using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioController : MonoBehaviour {

	[System.Serializable]
	public class Clip {
		public AudioSource _source;
		public AudioClip _clip;
		[Range(0f, 1f)]
		public float _volume;
		public string _name;
	}

	public Dictionary<string, Clip> _clipDict;
	public Clip[] _clips;

	void Awake() {
		_clipDict = new Dictionary<string, Clip>();
		foreach (var clip in _clips) {
			_clipDict.Add (clip._name, clip);
		}
	}

	private Clip GetClip(string name){
		if (_clipDict.ContainsKey (name)) {
			return _clipDict[name];
		} else {
			Debug.LogError("Invlaid Clip Name: " + name);
			return null;
		}
	}

	public void PlayClip(string name){
		var clip = GetClip (name);
		if (clip == null) {
			return;
		}

		var source = clip._source;
		if(source.isPlaying){
			source.Stop ();
		}
		source.clip = clip._clip;
		source.volume = clip._volume;
		source.Play();
	}
	
	public void StopClip(string name){
		var clip = GetClip (name);
		if (clip == null) {
			return;
		}

		var source = clip._source;
		if (source.isPlaying && source.clip == clip._clip) {
			source.Stop();
		}
	}
}
