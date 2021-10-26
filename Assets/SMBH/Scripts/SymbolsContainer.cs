using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SymbolsContainer : MonoBehaviour {
	public const int SYMBOLS_COUNT = 8;
	public const float SYMBOLS_RADIUS = 0.015f;
	public const float ACTIVE_SCALE = 2f;

	public Texture[] Textures;
	public SymbolComponent SymbolPrefab;
	public bool Hide = true;

	private float _angle;
	public float Angle { get { return _angle; } set { if (_angle == value) return; _angle = value; UpdateAngle(); } }

	private float _scale;
	public float Scale { get { return _scale; } set { if (_scale == value) return; _scale = value; UpdateScale(); } }

	private int _lastSymbolIndex;
	private List<int> _activeSymbols = new List<int>();
	private SymbolComponent[] _symbols;

	private void Start() {
		Angle = Random.Range(0f, 2 * Mathf.PI);
		Scale = 0f;
		_symbols = Enumerable.Range(0, SYMBOLS_COUNT).Select(i => 2 * Mathf.PI * i / SYMBOLS_COUNT).Select(a => {
			SymbolComponent symbol = Instantiate(SymbolPrefab);
			symbol.transform.parent = transform;
			symbol.transform.localPosition = SYMBOLS_RADIUS * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
			symbol.Visible = false;
			return symbol;
		}).ToArray();
	}

	public void CreateSymbol(Color color) {
		int ind;
		if (_activeSymbols.Count >= 8) {
			ind = Enumerable.Range(0, SYMBOLS_COUNT).Where(i => i != _lastSymbolIndex).PickRandom();
			_symbols[ind].Texture = Textures.Where(t => t != _symbols[ind].Texture).PickRandom();
		} else {
			ind = Enumerable.Range(0, SYMBOLS_COUNT).Where(i => !_activeSymbols.Contains(i)).PickRandom();
			_symbols[ind].Visible = true;
			_symbols[ind].Texture = Textures.PickRandom();
			_activeSymbols.Add(ind);
		}
		_lastSymbolIndex = ind;
		_symbols[ind].SymbolColor = color;
		_symbols[ind].Angle = Random.Range(0, 2 * Mathf.PI);
		_symbols[ind].RotationCW = Random.Range(0, 2) == 0;
	}

	public void ChangeLastSymbolColor(Color newColor) {
		_symbols[_lastSymbolIndex].SymbolColor = newColor;
	}

	public void Clear() {
		foreach (SymbolComponent symbol in _symbols) symbol.Visible = false;
		_activeSymbols = new List<int>();
	}

	private void Update() {
		if (Hide) Scale = Mathf.Max(0f, Scale - Time.deltaTime * ACTIVE_SCALE);
		else Scale = ACTIVE_SCALE;
		Angle += 0.2f * Time.deltaTime;
	}

	private void UpdateAngle() {
		transform.localRotation = Quaternion.Euler(0, Mathf.Rad2Deg * _angle, 0);
	}

	private void UpdateScale() {
		transform.localScale = _scale * Vector3.one;
	}
}
