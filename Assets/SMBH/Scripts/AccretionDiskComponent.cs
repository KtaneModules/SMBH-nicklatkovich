using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AccretionDiskType = SMBHUtils.AccretionDiskType;

public class AccretionDiskComponent : MonoBehaviour {
	public const float EVENT_HORIZON_RADIUS = 0.05f;
	public const int ACCRETION_DISK_VERTICES_COUNT = 64;
	public const float ACCRETION_DISK_RADIUS = 0.05f;
	public static readonly AccretionDiskType[] MULTICOLORED_ACCRETION_DISK_TYPES = new[] { AccretionDiskType.RINGS, AccretionDiskType.SECTORS, AccretionDiskType.DYNAMIC };
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
	public static readonly Dictionary<AccretionDiskType, bool> HIDEN_COLOR_REPEAT_FIRST = new Dictionary<AccretionDiskType, bool> {
		{ AccretionDiskType.RINGS, false },
		{ AccretionDiskType.SECTORS, true },
		{ AccretionDiskType.DYNAMIC, true },
	};

	public MeshFilter AccretionDiskMeshFilter;
	public Renderer AccretionDiskRenderer;
	public Shader SolidColorAccretionDiskShader;
	public Shader RingsAccretionDiskShader;
	public Shader SectorsAccretionDiskShader;
	public Shader DynamicAccretionDiskShader;

	public bool Active;
	public AccretionDiskType Type;
	public Color[] Colors;

	private float _alpha = 0f;
	public float Alpha { get { return _alpha; } set { if (_alpha == value) return; _alpha = value; UpdateAlpha(); } }

	private bool _solved = false;
	public bool Solved { get { return _solved; } set { if (_solved == value) return; _solved = value; } }

	private float _eventHorizonGreenness = 0f;
	public float EventHorizonGreenness {
		get { return _eventHorizonGreenness; }
		set { if (_eventHorizonGreenness == value) return; _eventHorizonGreenness = value; UpdateEventHorizonColor(); }
	}

	public bool CW { get { return _rotationSpeed > 0; } }

	// magenta is unused to make first hidden color update requred (cause magenta is not valid accretion disk color)
	private Color _hiddenColor = Color.magenta;
	private Mesh _accretionDiskMesh;
	private float _rotationSpeed = 1f;

	private void Start() {
		Active = false;
		// magenta is unused to make first color always update
		Colors = new[] { Color.magenta };
		// this variable should equals shader type in inspector
		Type = AccretionDiskType.SECTORS;
		_accretionDiskMesh = AccretionDiskMeshFilter.mesh;
		CreateAccretionDisk();
		UpdateAlpha();
	}

	private void Update() {
		Alpha = Mathf.Max(0, Mathf.Min(1, Alpha + (Active ? 1 : -1) * Time.deltaTime));
		_accretionDiskMesh.vertices = CalculateAccretionDiskVertices();
		EventHorizonGreenness = Mathf.Max(0, Mathf.Min(1, EventHorizonGreenness + (Solved ? 1 : -1) * Time.deltaTime));
	}

	public void SetProps(AccretionDiskType type, Color[] colors, bool cw) {
		bool shouldUpdateShader = Type != type;
		if (shouldUpdateShader) {
			Debug.Log(AccretionDiskRenderer.material.shader.name);
			AccretionDiskRenderer.material.shader = GetShaderOfType(type);
			Debug.Log(AccretionDiskRenderer.material.shader.name);
			Type = type;
			UpdateAlpha();
			UpdateEventHorizonColor();
		}
		if (shouldUpdateShader || colors[0] != Colors[0]) AccretionDiskRenderer.material.SetColor("_Color_0", colors[0]);
		if (colors.Length > 1) {
			if (shouldUpdateShader || Colors.Length < 2 || Colors[1] != colors[1]) AccretionDiskRenderer.material.SetColor("_Color_1", colors[1]);
			if (colors.Length > 2) {
				if (shouldUpdateShader || Colors.Length < 3 || Colors[2] != colors[2]) AccretionDiskRenderer.material.SetColor("_Color_2", colors[2]);
			} else {
				Color hidenColor = colors[HIDEN_COLOR_REPEAT_FIRST[type] ? 0 : 1];
				if (shouldUpdateShader || _hiddenColor != hidenColor) AccretionDiskRenderer.material.SetColor("_Color_2", hidenColor);
				_hiddenColor = hidenColor;
			}
		}
		if (shouldUpdateShader || colors.Length != Colors.Length) {
			Dictionary<string, float> props = GetColorsCoordsOfType(type, colors.Length);
			foreach (KeyValuePair<string, float> prop in props) AccretionDiskRenderer.material.SetFloat(prop.Key, prop.Value);
		}
		if (type == AccretionDiskType.DYNAMIC && (shouldUpdateShader || Colors.Length != colors.Length)) {
			AccretionDiskRenderer.material.SetFloat("_Dynamic_Speed", colors.Length == 2 ? 0.5f : 1f / 3);
		}
		Colors = colors;
		_rotationSpeed = Random.Range(1f, 2f) * (cw ? 1 : -1);
		AccretionDiskRenderer.material.SetFloat("_Rotation_Speed", _rotationSpeed);
	}

	public void Activate() {
		int colorsCount;
		if (Random.Range(0, 2) == 0) colorsCount = 2;
		else if (Random.Range(0, 3) > 0) colorsCount = 1;
		else colorsCount = 3;
		AccretionDiskType type = colorsCount == 1 ? AccretionDiskType.SOLID : MULTICOLORED_ACCRETION_DISK_TYPES.PickRandom();
		Color[] colors = new Color[colorsCount];
		colors[0] = PickRandomColor();
		if (colorsCount > 1) {
			colors[1] = PickRandomColor(colors[0]);
			if (colorsCount > 2) colors[2] = PickRandomColor(colors.Take(2).ToArray());
			// In case of unfair colors distribution:
			// colors = colors.Shuffle();
		}
		SetProps(type, colors, Random.Range(0, 2) == 0);
		Active = true;
	}

	private Shader GetShaderOfType(AccretionDiskType type) {
		switch (type) {
			case AccretionDiskType.SOLID: return SolidColorAccretionDiskShader;
			case AccretionDiskType.RINGS: return RingsAccretionDiskShader;
			case AccretionDiskType.SECTORS: return SectorsAccretionDiskShader;
			case AccretionDiskType.DYNAMIC: return DynamicAccretionDiskShader;
			default: throw new System.Exception("Unknown accretion disk type");
		}
	}

	private Dictionary<string, float> GetColorsCoordsOfType(AccretionDiskType type, int colorsCount) {
		if (type != AccretionDiskType.SOLID && (colorsCount < 2 || colorsCount > 3)) throw new System.Exception("Invalid colors count");
		switch (type) {
			case AccretionDiskType.SOLID: return new Dictionary<string, float>();
			case AccretionDiskType.RINGS: return colorsCount == 2 ? TWO_RINGS : THREE_RINGS;
			case AccretionDiskType.DYNAMIC:
			case AccretionDiskType.SECTORS: return colorsCount == 2 ? TWO_SECTORS : THREE_SECTORS;
			default: throw new System.Exception("Unknown accretion disk type");
		}
	}

	private void UpdateAlpha() {
		AccretionDiskRenderer.material.SetFloat("_Alpha", Alpha);
	}

	private void CreateAccretionDisk() {
		_accretionDiskMesh.Clear();
		_accretionDiskMesh.vertices = CalculateAccretionDiskVertices();
		_accretionDiskMesh.uv = Enumerable.Range(0, ACCRETION_DISK_VERTICES_COUNT + 1).SelectMany(i => {
			float x = (float)i / ACCRETION_DISK_VERTICES_COUNT;
			return new[] { new Vector2(x, 0), new Vector2(x, 1) };
		}).ToArray();
		_accretionDiskMesh.triangles = Enumerable.Range(0, ACCRETION_DISK_VERTICES_COUNT).SelectMany(i => (
			new[] { 2 * i, 2 * i + 1, 2 * i + 2, 2 * i + 2, 2 * i + 1, 2 * i + 3 }
		)).Select(i => i % (2 * (ACCRETION_DISK_VERTICES_COUNT + 1))).ToArray();
	}

	private Vector3[] CalculateAccretionDiskVertices() {
		Vector3 localCamPos = transform.InverseTransformPoint(Camera.main.transform.position);
		Vector2 xzCamPosNorm = new Vector2(localCamPos.x, localCamPos.z).normalized;
		float camAngle = Mathf.Atan2(localCamPos.z, localCamPos.x);
		float camZAngle = localCamPos.y < 0 ? Mathf.PI / 2 : Mathf.PI / 2 - Mathf.Atan2(localCamPos.y, new Vector3(localCamPos.x, 0, localCamPos.z).magnitude);
		float maxAccretionDiskPoint = ACCRETION_DISK_RADIUS + EVENT_HORIZON_RADIUS;
		float minAccretionDiskPoint = EVENT_HORIZON_RADIUS - 0.001f;
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

	private void UpdateEventHorizonColor() {
		AccretionDiskRenderer.material.SetColor("_Event_Horizon_Color", new Color(0, EventHorizonGreenness, 0, 1));
	}

	public static Color PickRandomColor(params Color[] except) {
		return SMBHUtils.COLORS.Where(c => !except.Contains(c)).PickRandom();
		// Color[] possibleColors = COLORS.Where(c => !except.Contains(c)).ToArray();
		// if (possibleColors.Length == 0) throw new System.Exception("Nothing to pick");
		// int max = Enumerable.Range(0, possibleColors.Length).Sum();
		// int rnd = Random.Range(0, max);
		// int counter = 0;
		// for (int i = 0; i < possibleColors.Length; i++) {
		// 	counter += i;
		// 	if (counter >= rnd) return possibleColors[possibleColors.Length - i - 1];
		// }
		// throw new System.Exception("Cannot pick random color");
	}
}
