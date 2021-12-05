using UnityEngine;

public class StatusLightsContainer : MonoBehaviour {
	public const float RADIUS = 0.1f;

	public StatusLightComponent StatusLightPrefab;

	private int litLights = 0;
	private StatusLightComponent[] _components;

	private void Start() {
		StatusLightComponent[] components = new StatusLightComponent[12];
		for (int tripletIndex = 0; tripletIndex < 4; tripletIndex++) {
			for (int index = 0; index < 3; index++) {
				float pos = Mathf.PI / 4 + tripletIndex * Mathf.PI / 2 + (index - 1) * Mathf.PI / 32;
				StatusLightComponent comp = Instantiate(StatusLightPrefab);
				comp.transform.parent = transform;
				comp.transform.localPosition = new Vector3(RADIUS * Mathf.Cos(pos), 0, RADIUS * Mathf.Sin(pos));
				comp.transform.localScale = Vector3.one;
				comp.transform.localRotation = Quaternion.identity;
				components[tripletIndex * 3 + index] = comp;
			}
		}
		_components = components.Shuffle();
	}

	public void ChangeLocationForTP() {
		float[] positions = new float[12];
		for (int quadrupletIndex = 0; quadrupletIndex < 3; quadrupletIndex++) {
			for (int index = 0; index < 4; index++) {
				positions[quadrupletIndex * 4 + index] = Mathf.PI / 4 * 3 + quadrupletIndex * Mathf.PI / 2 + (index - 1.5f) * Mathf.PI / 32 / 1.5f;
			}
		}
		positions = positions.Shuffle();
		for (int i = 0; i < 12; i++) _components[i].transform.localPosition = new Vector3(RADIUS * Mathf.Cos(positions[i]), 0, RADIUS * Mathf.Sin(positions[i]));
	}

	public void Lit() {
		if (litLights >= 12) return;
		_components[litLights++].LightColor = Color.green;
	}
}
