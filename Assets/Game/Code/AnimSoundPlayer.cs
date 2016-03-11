using UnityEngine;
using System.Collections;

public class AnimSoundPlayer : MonoBehaviour {

	public AudioClip[] _clips;
	public AudioSource _source;

	private int _clipIndex;

	public void PlayClip(int index) {
		if (_source.isPlaying) {
			_source.Stop();
		}
		_source.clip = _clips[index];
		_source.Play();
		_clipIndex = index;
	}

	public void PlayNextClip() {
		if (++_clipIndex >= _clips.Length) {
			_clipIndex = 0;
		}
		PlayClip(_clipIndex);
	}
}
