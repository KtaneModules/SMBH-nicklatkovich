using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KeepCoding;
using KModkit;

using KMAudioRef = KMAudio.KMAudioRef;
using AccretionDiskType = SMBHUtils.AccretionDiskType;
using SMBHBombInfo = BHReflector.SMBHBombInfo;

public class SMBHModule : ModuleScript {
	private class VoltData {
		[JsonProperty("voltage")]
		public string Voltage { get; set; }
	}

	public const int STAGES_COUNT = 12;
	public const int ACCRETION_DISK_VERTICES_COUNT = 64;
	public const float ACCRETION_DISK_RADIUS = 0.05f;
	public const float MAX_RADIUS = 0.05f;
	public const float MIN_ACTIVATION_TIMEOUT = 2f;
	public const string ACTIVATION_SOUND = "Activated";
	public const string FAILURE_SOUND = "Failure";
	public const string INPUT_SOUND = "Input";
	public static readonly Color[] COLORS = SMBHUtils.COLORS;
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
	private static readonly Dictionary<AccretionDiskType, string> TYPE_TO_STRING = new Dictionary<AccretionDiskType, string> {
		{ AccretionDiskType.SOLID, "Single colored" },
		{ AccretionDiskType.SECTORS, "Sectors" },
		{ AccretionDiskType.RINGS, "Rings" },
		{ AccretionDiskType.DYNAMIC, "Dynamic" },
	};

	private float? Voltage {
		get {
			List<string> query = Get<KMBombInfo>().QueryWidgets("volt", "");
			return query.Count != 0 ? (float?)float.Parse(JsonConvert.DeserializeObject<VoltData>(query.First()).Voltage) : null;
		}
	}

	private enum ModuleState { DISABLED, ENABLED, HELD, RELEASED, SOLVED }

	public Shader SingleColoredAccretionDiskShader;
	public Shader RingsColoredAccretionDiskShader;
	public Shader SectorsColoredAccretionDiskShader;
	public GameObject AccretionDisk;
	public TextMesh Text;
	public EventHorizonComponent EventHorizon;
	public KMBombInfo BombInfo;
	public KMBossModule BossModule;
	public KMAudio Audio;

	public bool AccretionDiskActive { get { return State == ModuleState.ENABLED || State == ModuleState.HELD || State == ModuleState.RELEASED; } }

	private bool AccretionDiskActivated = false;
	private int StartingTimeInMinutes;
	private ModuleState State = ModuleState.DISABLED;
	private AccretionDiskType DiskType;
	private int PassedStagesCount = 0;
	private float ActivationTime = 0f;
	private float AveragePassingTime = float.PositiveInfinity;
	private float PrevAccretionDiskAlpha = 0f;
	private float TargetAccretionDiskAlpha = 0f;
	private float ActivationTimeout = 0f;
	private string Input = "";
	private Color[] AccretionDiskColors;
	private HashSet<string> IgnoreList;
	private SMBHBombInfo Info;

	private void Start() {
		CreateAccretionDisk();
		EventHorizon.transform.localScale = 2 * MAX_RADIUS * Vector3.one;
		IgnoreList = new HashSet<string>(BossModule.GetIgnoredModules("SMBH", SMBHUtils.DefaultIgnoredModules));
	}

	private void Update() {
		if (!IsActive) return;
		Mesh accretionDiskMesh = AccretionDisk.GetComponent<MeshFilter>().mesh;
		accretionDiskMesh.vertices = CalculateAccretionDiskVertices();
		if (State == ModuleState.DISABLED) {
			ActivationTimeout -= Time.deltaTime;
			List<string> unsolvedModules = BombInfo.GetUnsolvedModuleNames();
			int unignoredUnsolvedModulesCount = unsolvedModules.Where(m => !IgnoreList.Contains(m)).Count();
			if (ActivationTimeout >= 5f && unignoredUnsolvedModulesCount == 0) OnNoUnsolvedModules();
			else if (ActivationTimeout <= 0f) {
				if (CanActivate()) {
					State = ModuleState.ENABLED;
					ActivateAccretionDisk();
					TargetAccretionDiskAlpha = 1f;
				} else CalculateActivationTime();
			}
		}
		float newAlpha = PrevAccretionDiskAlpha;
		newAlpha += Mathf.Sign(TargetAccretionDiskAlpha - PrevAccretionDiskAlpha) * Time.deltaTime;
		if (PrevAccretionDiskAlpha != newAlpha) {
			Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
			AccretionDiskRenderer.material.SetFloat("_Alpha", newAlpha);
			PrevAccretionDiskAlpha = newAlpha;
		}
	}

	public bool CanActivate() {
		if (BombInfo.GetUnsolvedModuleIDs().Count(m => m == BHReflector.BLACK_HOLE_MODULE_ID) > 0 && Info.bhInfo != null) {
			if (Info.bhInfo.DigitsEntered == Info.bhInfo.LastProcessedDigitsEntered) {
				Log("Last processed Black Hole digit already processed. Activation delayed");
				return false;
			}
			bool result = Info.Modules.All(m => !m.AccretionDiskActivated);
			if (!result) Log("Another SMBH waiting for Black Hole digits sum. Activation delayed");
			return result;
		}
		return true;
	}

	public override void OnTimerTick() {
		base.OnTimerTick();
		if (IsSolved) return;
		if (State == ModuleState.DISABLED || State == ModuleState.ENABLED) return;
		Input += "-";
		if (Input.Length > 1 && Input.TakeLast(2).All(c => c == '-') && Input.LastIndexOf('r') >= Input.LastIndexOf('h')) {
			Input = Input.SkipLast(2).Join("");
			int num = SMBHUtils.ParseBHCode(Input, RuleSeed.Seed);
			int expected = CalculateValidAnswer() % 36;
			if (num < 0) {
				Log("Unknown code entered: {0}", Input);
				Text.text = "?";
				State = ModuleState.ENABLED;
				Audio.PlaySoundAtTransform(FAILURE_SOUND, transform);
			} else if (num == expected) OnValidEntry(num);
			else {
				Log("Input: {0} ({1}). Expected: {2} ({3})", Base36ToChar(num), Input, Base36ToChar(expected), SMBHUtils.GetBHSCII(expected, RuleSeed.Seed));
				Text.text = Base36ToChar(num).ToString();
				State = ModuleState.ENABLED;
				Audio.PlaySoundAtTransform(FAILURE_SOUND, transform);
			}
		} else {
			Audio.PlaySoundAtTransform(INPUT_SOUND, transform);
			Text.text = Input;
		}
	}

	private void OnValidEntry(int num) {
		if (Info.bhInfo != null) Info.bhInfo.LastProcessedDigitsEntered = Info.bhInfo.DigitsEntered;
		Log("Valid input: {0} ({1})", Base36ToChar(num), Input);
		Text.text = "";
		float passedTime = Time.time - ActivationTime;
		int passedStagesAddition = passedTime < 60f ? 2 : 1;
		PassedStagesCount += passedStagesAddition;
		Log("Stage passed in {0} seconds. Passed stages +{1} ({2})", Mathf.Ceil(passedTime), passedStagesAddition, PassedStagesCount);
		if (PassedStagesCount >= STAGES_COUNT) {
			State = ModuleState.SOLVED;
			Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
			AccretionDiskRenderer.material.SetColor("_Event_Horizon_Color", Color.green);
			Log("Module solved");
			EventHorizon.Solved = true;
			Solve();
			Audio.PlaySoundAtTransform("Solved", transform);
		} else {
			State = ModuleState.DISABLED;
			CalculateActivationTime();
			TargetAccretionDiskAlpha = 0f;
			Audio.PlaySoundAtTransform("ValidEntry", transform);
		}
	}

	private void OnNoUnsolvedModules() {
		ActivationTimeout = Random.Range(MIN_ACTIVATION_TIMEOUT, 5f);
		Log("No unsolved modules. Next activation in {0} seconds", Mathf.Ceil(ActivationTimeout));
	}

	private void CalculateActivationTime() {
		List<string> unsolvedModules = BombInfo.GetUnsolvedModuleNames();
		int unignoredUnsolvedModulesCount = unsolvedModules.Where(m => !IgnoreList.Contains(m)).Count();
		if (unignoredUnsolvedModulesCount == 0) {
			OnNoUnsolvedModules();
			return;
		}
		int leftStages = STAGES_COUNT - PassedStagesCount;
		float bombTime = BombInfo.GetTime() - 20f;
		float avgLeft = bombTime / leftStages;
		if (AveragePassingTime >= avgLeft) ActivationTimeout = Random.Range(MIN_ACTIVATION_TIMEOUT, Mathf.Max(5f, avgLeft));
		else {
			float avg = (AveragePassingTime * PassedStagesCount + bombTime) / STAGES_COUNT;
			ActivationTimeout = Random.Range(MIN_ACTIVATION_TIMEOUT, Mathf.Max(5f, avg));
		}
		Log("Next activation in {0} seconds", Mathf.Ceil(ActivationTimeout));
	}

	public override void OnActivate() {
		base.OnActivate();
		StartingTimeInMinutes = Mathf.FloorToInt(BombInfo.GetTime() / 60f);
		CalculateActivationTime();
		EventHorizon.Selectable.OnInteract += () => { OnHold(); return false; };
		EventHorizon.Selectable.OnInteractEnded += OnRelease;
		Info = BHReflector.GetBHBombInfo(this);
	}

	private void OnHold() {
		if (IsSolved) return;
		if (State == ModuleState.DISABLED) return;
		if (State == ModuleState.ENABLED) {
			Audio.PlaySoundAtTransform(INPUT_SOUND, transform);
			Input = "h";
			Text.text = Input;
			Text.color = Color.white;
		} else if (State == ModuleState.RELEASED) {
			Input += "h";
			Text.text = Input;
		}
		State = ModuleState.HELD;
	}

	private void OnRelease() {
		if (IsSolved) return;
		if (State == ModuleState.HELD) {
			if (Input.Last() == 'h') Input = Input.SkipLast(1).Join("") + "t";
			else Input += "r";
			State = ModuleState.RELEASED;
			Text.text = Input;
		}
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
		Audio.PlaySoundAtTransform(ACTIVATION_SOUND, transform);
		ActivationTime = Time.time;
		Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
		float rotationSpeed = Random.Range(1f, 2f) * new[] { 1f, -1f }.PickRandom();
		if (Random.Range(0, 2) == 0) {
			DiskType = AccretionDiskType.SOLID;
			AccretionDiskRenderer.material.shader = SingleColoredAccretionDiskShader;
			Color cl = PickRandomColor();
			AccretionDiskColors = new[] { cl };
			AccretionDiskRenderer.material.SetColor("_Color", cl);
		} else if (Random.Range(0, 3) != 0) {
			DiskType = AccretionDiskType.SECTORS;
			AccretionDiskRenderer.material.shader = SectorsColoredAccretionDiskShader;
			int colorsCount = Random.Range(2, 4);
			Color c0 = PickRandomColor();
			Color c1 = PickRandomColor(c0);
			AccretionDiskRenderer.material.SetColor("_Color_0", c0);
			AccretionDiskRenderer.material.SetColor("_Color_1", c1);
			if (Random.Range(0, 3) == 0) {
				Color c2 = PickRandomColor(c0, c1);
				AccretionDiskColors = new[] { c0, c1, c2 };
				AccretionDiskRenderer.material.SetColor("_Color_2", c2);
				foreach (KeyValuePair<string, float> property in THREE_SECTORS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
			} else {
				AccretionDiskColors = new[] { c0, c1, SMBHUtils.GetFirstColorExclude(RuleSeed.Seed, AccretionDiskType.SECTORS, rotationSpeed > 0, c0, c1) };
				AccretionDiskRenderer.material.SetColor("_Color_2", c0);
				foreach (KeyValuePair<string, float> property in TWO_SECTORS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
			}
		} else ActivateRingsAccretionDisk(rotationSpeed > 0);
		AccretionDiskRenderer.material.SetFloat("_Rotation_Speed", rotationSpeed);
		Log("Accretion disk activated. Type: {0}. Rotation: {1}. Colors: {2}", TYPE_TO_STRING[DiskType], rotationSpeed < 0 ? "CCW" : "CW", AccretionDiskColors.Select(c => (
			SMBHUtils.NameOfColor[c]
		)).Join(","));
	}

	private void ActivateRingsAccretionDisk(bool cw) {
		DiskType = AccretionDiskType.RINGS;
		Renderer AccretionDiskRenderer = AccretionDisk.GetComponent<Renderer>();
		AccretionDiskRenderer.material.shader = RingsColoredAccretionDiskShader;
		int colorsCount = Random.Range(2, 4);
		Color c0 = COLORS.PickRandom();
		Color c1 = COLORS.Where(c => c != c0).PickRandom();
		AccretionDiskRenderer.material.SetColor("_Color_0", c0);
		AccretionDiskRenderer.material.SetColor("_Color_1", c1);
		if (Random.Range(0, 3) == 0) {
			Color c2 = PickRandomColor(c0, c1);
			AccretionDiskColors = new[] { c0, c1, c2 };
			AccretionDiskRenderer.material.SetColor("_Color_2", c2);
			foreach (KeyValuePair<string, float> property in THREE_RINGS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
		} else {
			AccretionDiskColors = new[] { c0, c1, SMBHUtils.GetFirstColorExclude(RuleSeed.Seed, AccretionDiskType.RINGS, cw, c0, c1) };
			AccretionDiskRenderer.material.SetColor("_Color_2", c1);
			foreach (KeyValuePair<string, float> property in TWO_RINGS) AccretionDiskRenderer.material.SetFloat(property.Key, property.Value);
		}
	}

	private int CalculateValidAnswer() {
		if (BombInfo.GetUnsolvedModuleIDs().Any(m => m == BHReflector.BLACK_HOLE_MODULE_ID) && Info.bhInfo != null) {
			return Info.bhInfo.SolutionCode.Take(Info.bhInfo.DigitsEntered).Sum();
		}
		if (DiskType == AccretionDiskType.SOLID) {
			Color cl = AccretionDiskColors[0];
			if (cl == SMBHUtils.ORANGE) return BombInfo.GetSolvedModuleIDs().Count;
			else if (cl == Color.white) return BombInfo.GetStrikes() + BombInfo.GetTwoFactorCodes().Select(c => c % 10).Sum();
			else if (cl == Color.red) return CharToBase36(BombInfo.GetSerialNumber()[(BombInfo.GetPortCount(Port.Serial) + BombInfo.GetPortCount(Port.Parallel)) % 6]);
			else if (cl == Color.yellow) return Voltage != null ? Mathf.FloorToInt(Voltage.Value) : BombInfo.GetSerialNumberNumbers().Sum() + BombInfo.GetModuleIDs().Count;
			else if (cl == Color.green) return BombInfo.GetPortCount();
			else if (cl == Color.blue) {
				int[] codes = BombInfo.GetTwoFactorCodes().ToArray();
				if (codes.Length == 0) return StartingTimeInMinutes;
				return StartingTimeInMinutes + Mathf.Max(codes) % 10;
			} else throw new System.Exception("Unknown disk color");
		} else return SMBHUtils.GetThreeColorValue(AccretionDiskColors, RuleSeed.Seed);
	}

	public static int CharToBase36(char c) {
		if (c >= '0' && c <= '9') return c - '0';
		if (c >= 'A' && c <= 'Z') return c - 'A' + 10;
		throw new System.Exception("Unable to cast char to base36 integer");
	}

	public static char Base36ToChar(int v) {
		if (v >= 0 && v < 10) return (char)('0' + v);
		if (v >= 10 && v < 36) return (char)('A' + v - 10);
		throw new System.Exception("Unable to cast base36 integer to char");
	}

	public static Color PickRandomColor(params Color[] except) {
		return COLORS.Where(c => !except.Contains(c)).PickRandom();
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
