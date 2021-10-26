using UnityEngine;

public class EventHorizonComponent : MonoBehaviour {
	public KMSelectable Selectable;
	public Renderer Renderer;

	private bool _solved = false;
	public bool Solved { get { return _solved; } set { if (_solved == value) return; _solved = value; UpdateRenderer(); } }

	private void Start() {
		UpdateRenderer();
	}

	private void UpdateRenderer() {
		Renderer.material.color = Solved ? Color.green : Color.black;
	}
}
