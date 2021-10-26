using System.Collections.Generic;
using UnityEngine;

public class BlackHoleModule : MonoBehaviour {
	sealed class BlackHoleBombInfo {
		public List<BlackHoleModule> Modules = new List<BlackHoleModule>();
		public List<int> SolutionCode;
		public int DigitsEntered = 0;
		public int DigitsExpected;
		public BlackHoleModule LastDigitEntered;
		public Event[][] Gestures;
	}

	public KMBombModule Module;
	public KMSelectable MockButton;
	public TextMesh Text;

	private bool _solved = false;
	private BlackHoleBombInfo _info = new BlackHoleBombInfo();

	public BlackHoleModule() : base() {
		_info.SolutionCode = new List<int>();
		_info.DigitsExpected = 7;
	}

	private void Start() {
		Module.OnActivate += OnActivate;
	}

	public void OnActivate() {
		MockButton.OnInteract += () => { EnterDigit(); return false; };
	}

	public void EnterDigit() {
		if (_solved) return;
		_info.SolutionCode.Add(Random.Range(0, 5));
		_info.DigitsEntered += 1;
		Text.text = _info.SolutionCode.Join("");
		if (_info.DigitsEntered >= _info.DigitsExpected) {
			Module.HandlePass();
			_solved = true;
		}
	}
}
