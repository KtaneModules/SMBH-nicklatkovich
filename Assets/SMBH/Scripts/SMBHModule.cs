using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepCoding;

public class SMBHModule : ModuleScript {
	public const int ACCRETION_DISK_VERTICES_COUNT = 64;
	public const float ACCRETION_DISK_RADIUS = 0.05f;
	public const float MAX_RADIUS = 0.05f;
	public static readonly Color[] COLORS = new[] { (Color)new Color32(0xff, 0x80, 0, 0), Color.white, Color.red, Color.yellow, Color.green, Color.blue };
	public static readonly Dictionary<string, float> TWO_RINGS = new Dictionary<string, float> {
		{ "_Color_0_Min", 0.6f },
		{ "_Color_1_Max", 0.4f },
		{ "_Color_1_Min", 0.0f },
	};
	public static readonly Dictionary<string, float> THREE_RINGS = new Dictionary<string, float> {
		{ "_Color_0_Min", 0.7f },
		{ "_Color_1_Max", 0.575f },
		{ "_Color_1_Min", 0.425f },
		{ "_Color_2_Max", 0.3f },
	};
	public static readonly Dictionary<string, float> TWO_SECTORS = new Dictionary<string, float> {
		{ "_Color_0_Min", 0.6f },
		{ "_Color_1_Max", 0.5f },
		{ "_Color_1_Min", 0.1f },
		{ "_Color_2_Max", 0f },
	};
	public static readonly Dictionary<string, float> THREE_SECTORS = new Dictionary<string, float> {
		{ "_Color_0_Min", 0.8f },
		{ "_Color_1_Max", 0.666666f },
		{ "_Color_1_Min", 0.466666f },
		{ "_Color_2_Max", 0.333333f },
		{ "_Color_2_Min", 0.133333f },
	};

	public Shader SingleColoredAccretionDiskShader;
	public Shader RingsColoredAccretionDiskShader;
	public Shader SectorsColoredAccretionDiskShader;
	public GameObject AccretionDisk;
	public Transform EventHorizon;

	private float PrevAccretionDiskAlpha = 0f;
	private float TargetAccretionDiskAlpha = 0f;

	private void Start() {
		CreateAccretionDisk();
		EventHorizon.localScale = 2 * MAX_RADIUS * Vector3.one;
	}

	private void Update() {
		Mesh accretionDiskMesh = AccretionDisk.GetComponent<MeshFilter>().mesh;
		accretionDiskMesh.vertices = CalculateAccretionDiskVertices();
		if (TargetAccretionDiskAlpha <= PrevAccretionDiskAlpha) {
			PrevAccretionDiskAlpha -= Time.deltaTime;
			if (PrevAccretionDiskAlpha <= TargetAccretionDiskAlpha) {
				PrevAccretionDiskAlpha = TargetAccretionDiskAlpha;
				ActivateAccretionDisk();
				TargetAccretionDiskAlpha = 1f;
			}
		} else {
			PrevAccretionDiskAlpha += Time.deltaTime;
			if (PrevAccretionDiskAlpha >= TargetAccretionDiskAlpha) {
				PrevAccretionDiskAlpha = TargetAccretionDiskAlpha;
				TargetAccretionDiskAlpha = 0f;
			}
		}
		Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
		AccretionDiskRenderer.material.SetFloat("_Alpha", PrevAccretionDiskAlpha);
	}

	public override void OnActivate() {
		base.OnActivate();
	}

	private Vector3[] CalculateAccretionDiskVertices() {
		Vector3 localCamPos = EventHorizon.transform.InverseTransformPoint(Camera.main.transform.position);
		Vector2 xzCamPosNorm = new Vector2(localCamPos.x, localCamPos.z).normalized;
		float camAngle = Mathf.Atan2(localCamPos.z, localCamPos.x);
		float camZAngle = localCamPos.y < 0 ? Mathf.PI / 2 : Mathf.PI / 2 - Mathf.Atan2(localCamPos.y, new Vector3(localCamPos.x, 0, localCamPos.z).magnitude);
		float maxAccretionDiskPoint = ACCRETION_DISK_RADIUS + MAX_RADIUS;
		float minAccretionDiskPoint = MAX_RADIUS - 0.001f;
		Mesh accretionDiskMesh = AccretionDisk.GetComponent<MeshFilter>().mesh;
		return Enumerable.Range(0, ACCRETION_DISK_VERTICES_COUNT + 1).SelectMany(i => {
			float vertexAngle = 2 * Mathf.PI * i / ACCRETION_DISK_VERTICES_COUNT;
			Vector3 original = new Vector3(Mathf.Cos(vertexAngle), 0, Mathf.Sin(vertexAngle));
			float angleDiff = Mathf.Abs(Mathf.Deg2Rad * Mathf.DeltaAngle(Mathf.Rad2Deg * camAngle, Mathf.Rad2Deg * vertexAngle));
			Vector3 res = original;
			if (angleDiff > Mathf.PI / 2) res.y = Mathf.Sin((angleDiff - Mathf.PI / 2) * (camZAngle / (Mathf.PI / 2)));
			res = res.normalized;
			return new[] { maxAccretionDiskPoint * res, minAccretionDiskPoint * res };
		}).ToArray();
	}

	private void CreateAccretionDisk() {
		Mesh accretionDiskMesh = AccretionDisk.GetComponent<MeshFilter>().mesh;
		accretionDiskMesh.Clear();
		accretionDiskMesh.vertices = CalculateAccretionDiskVertices();
		accretionDiskMesh.uv = Enumerable.Range(0, ACCRETION_DISK_VERTICES_COUNT + 1).SelectMany(i => {
			float x = (float)i / ACCRETION_DISK_VERTICES_COUNT;
			return new[] { new Vector2(x, 0), new Vector2(x, 1) };
		}).ToArray();
		accretionDiskMesh.triangles = Enumerable.Range(0, ACCRETION_DISK_VERTICES_COUNT).SelectMany(i => (
			new[] { 2 * i, 2 * i + 1, 2 * i + 2, 2 * i + 2, 2 * i + 1, 2 * i + 3 }
		)).Select(i => i % (2 * (ACCRETION_DISK_VERTICES_COUNT + 1))).ToArray();
		Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
		AccretionDiskRenderer.material.SetFloat("_Alpha", 0f);
	}

	private void ActivateAccretionDisk() {
		Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
		if (Random.Range(0, 2) == 0) {
			AccretionDiskRenderer.material.shader = SingleColoredAccretionDiskShader;
			AccretionDiskRenderer.material.SetColor("_Color", PickRandomColor());
		} else if (Random.Range(0, 3) != 0) {
			AccretionDiskRenderer.material.shader = SectorsColoredAccretionDiskShader;
			int colorsCount = Random.Range(2, 4);
			Color c0 = PickRandomColor();
			Color c1 = PickRandomColor(c0);
			AccretionDiskRenderer.material.SetColor("_Color_0", c0);
			AccretionDiskRenderer.material.SetColor("_Color_1", c1);
			if (Random.Range(0, 3) == 0) {
				AccretionDiskRenderer.material.SetColor("_Color_2", PickRandomColor(c0, c1));
				foreach (KeyValuePair<string, float> property in THREE_SECTORS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
			} else {
				AccretionDiskRenderer.material.SetColor("_Color_2", c0);
				foreach (KeyValuePair<string, float> property in TWO_SECTORS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
			}
		} else ActivateRingsAccretionDisk();
		AccretionDiskRenderer.material.SetFloat("_Rotation_Speed", Random.Range(1f, 2f) * new[] { 1f, -1f }.PickRandom());
	}

	private void ActivateRingsAccretionDisk() {
		Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
		AccretionDiskRenderer.material.shader = RingsColoredAccretionDiskShader;
		int colorsCount = Random.Range(2, 4);
		Color c0 = COLORS.PickRandom();
		Color c1 = COLORS.Where(c => c != c0).PickRandom();
		AccretionDiskRenderer.material.SetColor("_Color_0", c0);
		AccretionDiskRenderer.material.SetColor("_Color_1", c1);
		if (Random.Range(0, 3) == 0) {
			AccretionDiskRenderer.material.SetColor("_Color_2", PickRandomColor(c0, c1));
			foreach (KeyValuePair<string, float> property in THREE_RINGS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
		} else {
			AccretionDiskRenderer.material.SetColor("_Color_2", c1);
			foreach (KeyValuePair<string, float> property in TWO_RINGS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
		}
	}

	public static Color PickRandomColor(params Color[] except) {
		Color[] possibleColors = COLORS.Where(c => !except.Contains(c)).ToArray();
		if (possibleColors.Length == 0) throw new System.Exception("Nothing to pick");
		int max = Enumerable.Range(0, possibleColors.Length).Sum();
		int rnd = Random.Range(0, max);
		int counter = 0;
		for (int i = 0; i < possibleColors.Length; i++) {
			counter += i;
			if (counter >= rnd) return possibleColors[possibleColors.Length - i - 1];
		}
		throw new System.Exception("Cannot pick random color");
	}
}
