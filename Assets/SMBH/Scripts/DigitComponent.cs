using UnityEngine;

public class DigitComponent : MonoBehaviour {
	public const float ROTATION_SPEED = 0.5f;

	public TextMesh Text;

	private float _scale = 0f;
	public float Scale { get { return _scale; } set { if (_scale == value) return; _scale = value; UpdateScale(); } }

	private float _angle;
	public float Angle { get { return _angle; } set { if (_angle == value) return; _angle = value; UpdateAngle(); } }

	private float _fade;
	public float Fade { get { return _fade; } set { if (_fade == value) return; _fade = value; UpdateColor(); } }

	private bool _valid;
	public bool Valid { get { return _valid; } set { if (_valid == value) return; _valid = value; UpdateColor(); } }

	private float _scaleSpeed;
	private float _angleSpeed;
	private float _fadeSpeed;

	private void Start() {
		UpdateScale();
		UpdateAngle();
	}

	public void ProcessNewCharacter(char c, bool valid) {
		_valid = valid;
		_angleSpeed = valid ? ROTATION_SPEED : 0;
		Angle = valid ? -ROTATION_SPEED / 2f : 0f;
		_fadeSpeed = valid ? 0 : -1f;
		Fade = 1f;
		_scaleSpeed = valid ? -1 : 0;
		Scale = 1f;
		Text.text = "" + c;
	}

	private void Update() {
		Angle += _angleSpeed * Time.deltaTime;
		Fade = Mathf.Max(0, Mathf.Min(1, Fade + _fadeSpeed * Time.deltaTime));
		Scale = Fade == 0 ? 0f : Mathf.Max(0, Scale + _scaleSpeed * Time.deltaTime);
	}

	private void UpdateScale() {
		transform.localScale = Scale * Vector3.one;
	}

	private void UpdateAngle() {
		transform.localRotation = Quaternion.Euler(0, Mathf.Rad2Deg * Angle, 0);
	}

	private void UpdateColor() {
		Text.color = _valid ? new Color(0, 1f * _fade, 0, 1f) : new Color(1f * _fade, 0, 0, 1f);
	}
}
