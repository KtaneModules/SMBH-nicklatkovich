using UnityEngine;

public class StatusLightComponent : MonoBehaviour {
	public Renderer BulbRenderer;

	private Color _color = Color.black;
	public Color LightColor { get { return _color; } set { if (_color == value) return; _color = value; UpdateColor(); } }

	private void Start() {
		UpdateColor();
	}

	private void UpdateColor() {
		BulbRenderer.material.color = _color;
	}
}
