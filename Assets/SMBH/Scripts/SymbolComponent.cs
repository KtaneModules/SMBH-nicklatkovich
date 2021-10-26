using UnityEngine;

public class SymbolComponent : MonoBehaviour {
	public Renderer SelfRenderer;
	public bool RotationCW;

	private Texture _texture = null;
	public Texture Texture { get { return _texture; } set { if (_texture == value) return; _texture = value; UpdateTexture(); } }

	private Color _color = Color.white;
	public Color SymbolColor { get { return _color; } set { if (_color == value) return; _color = value; UpdateColor(); } }

	private float _angle;
	public float Angle { get { return _angle; } set { if (_angle == value) return; _angle = value; UpdateAngle(); } }

	private bool _visible;
	public bool Visible { get { return _visible; } set { if (_visible == value) return; _visible = value; UpdateScale(); } }

	private void Start() {
		UpdateTexture();
		UpdateColor();
		UpdateScale();
		Angle = Random.Range(0, 2 * Mathf.PI);
	}

	private void Update() {
		Angle += Time.deltaTime * (RotationCW ? 1 : -1);
	}

	private void UpdateTexture() {
		SelfRenderer.material.SetTexture("_Texture", _texture);
	}

	private void UpdateColor() {
		SelfRenderer.material.SetColor("_Color", _color);
	}

	private void UpdateAngle() {
		transform.localRotation = Quaternion.Euler(0, Mathf.Rad2Deg * _angle, 0);
	}

	private void UpdateScale() {
		transform.localScale = Visible ? Vector3.one : Vector3.zero;
	}
}
