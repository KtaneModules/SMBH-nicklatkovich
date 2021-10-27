using UnityEngine;

public class EventHorizonComponent : MonoBehaviour {
	public KMSelectable Selectable;
	public Renderer Renderer;

	private bool _solved = false;
	public bool Solved { get { return _solved; } set { if (_solved == value) return; _solved = value; UpdateRenderer(); } }

	private float _greenness = 0f;

	private void Start() {
		UpdateRenderer();
	}

	private void Update() {
		if (Solved) {
			_greenness = Mathf.Min(1, _greenness + Time.deltaTime);
			UpdateRenderer();
		}
	}

	private void UpdateRenderer() {
		Renderer.material.color = new Color(0, _greenness, 0, 1);
	}
}
