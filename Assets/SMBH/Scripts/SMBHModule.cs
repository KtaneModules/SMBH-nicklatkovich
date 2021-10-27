using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KeepCoding;
using KModkit;

using AccretionDiskType = SMBHUtils.AccretionDiskType;
using SMBHBombInfo = BHReflector.SMBHBombInfo;

public class SMBHModule : ModuleScript {
	private class VoltData {
		[JsonProperty("voltage")]
		public string Voltage { get; set; }
	}

	public const int STAGES_COUNT = 12;
	public const float MIN_ACTIVATION_TIMEOUT = 2f;
	public const string ACTIVATION_SOUND = "Activated";
	public const string FAILURE_SOUND = "Failure";
	public const string INPUT_SOUND = "Input";
	public static readonly Color[] COLORS = SMBHUtils.COLORS;
	public static readonly Dictionary<AccretionDiskType, string> TYPE_TO_STRING = new Dictionary<AccretionDiskType, string> {
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

	public EventHorizonComponent EventHorizon;
	public KMBombInfo BombInfo;
	public KMBossModule BossModule;
	public KMAudio Audio;
	public SymbolsContainer Symbols;
	public DigitComponent Digit;
	public AccretionDiskComponent AccretionDisk;

	private int StartingTimeInMinutes;
	private ModuleState State = ModuleState.DISABLED;
	private int PassedStagesCount = 0;
	private float ActivationTime = 0f;
	private float AveragePassingTime = float.PositiveInfinity;
	private float ActivationTimeout = 0f;
	private string Input = "";
	private HashSet<string> IgnoreList;
	private SMBHBombInfo Info;


	private void Start() {
		EventHorizon.transform.localScale = 2 * AccretionDiskComponent.EVENT_HORIZON_RADIUS * Vector3.one;
		IgnoreList = new HashSet<string>(BossModule.GetIgnoredModules("SMBH", SMBHUtils.DefaultIgnoredModules));
	}

	private void Update() {
		if (!IsActive) return;
		if (State == ModuleState.DISABLED) {
			ActivationTimeout -= Time.deltaTime;
			List<string> unsolvedModules = BombInfo.GetUnsolvedModuleNames();
			int unignoredUnsolvedModulesCount = unsolvedModules.Where(m => !IgnoreList.Contains(m)).Count();
			if (ActivationTimeout >= 5f && unignoredUnsolvedModulesCount == 0) OnNoUnsolvedModules();
			else if (ActivationTimeout <= 0f) {
				if (CanActivate()) {
					State = ModuleState.ENABLED;
					ActivateAccretionDisk();
				} else CalculateActivationTime();
			}
		}
	}

	public bool CanActivate() {
		if (BombInfo.GetUnsolvedModuleIDs().Count(m => m == BHReflector.BLACK_HOLE_MODULE_ID) > 0 && Info.bhInfo != null) {
			if (Info.bhInfo.DigitsEntered == Info.bhInfo.LastProcessedDigitsEntered) {
				Log("Last processed Black Hole digit already processed. Activation delayed");
				return false;
			}
			bool result = Info.Modules.All(m => !m.AccretionDisk.Active);
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
				Digit.ProcessNewCharacter('?', false);
				State = ModuleState.ENABLED;
				Strike();
			} else if (num == expected) OnValidEntry(num);
			else {
				Log("Input: {0} ({1}). Expected: {2} ({3})", Base36ToChar(num), Input, Base36ToChar(expected), SMBHUtils.GetBHSCII(expected, RuleSeed.Seed));
				Digit.ProcessNewCharacter(Base36ToChar(num), false);
				State = ModuleState.ENABLED;
				Strike();
			}
			Symbols.Hide = true;
		} else {
			Audio.PlaySoundAtTransform(INPUT_SOUND, transform);
			Symbols.CreateSymbol(Color.white);
		}
	}

	private void OnValidEntry(int num) {
		if (Info.bhInfo != null) Info.bhInfo.LastProcessedDigitsEntered = Info.bhInfo.DigitsEntered;
		char c = Base36ToChar(num);
		Log("Valid input: {0} ({1})", c, Input);
		Digit.ProcessNewCharacter(c, true);
		float passedTime = Time.time - ActivationTime;
		int passedStagesAddition = passedTime < 60f ? 2 : 1;
		PassedStagesCount += passedStagesAddition;
		Log("Stage passed in {0} seconds. Passed stages +{1} ({2})", Mathf.Ceil(passedTime), passedStagesAddition, PassedStagesCount);
		if (PassedStagesCount >= STAGES_COUNT) {
			State = ModuleState.SOLVED;
			Log("Module solved");
			EventHorizon.Solved = true;
			AccretionDisk.Solved = true;
			Solve();
			Audio.PlaySoundAtTransform("Solved", transform);
		} else {
			State = ModuleState.DISABLED;
			CalculateActivationTime();
			AccretionDisk.Active = false;
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
			Symbols.Clear();
			Symbols.Hide = false;
		} else if (State == ModuleState.RELEASED) {
			Input += "h";
		}
		State = ModuleState.HELD;
		Symbols.CreateSymbol(Color.blue);
	}

	private void OnRelease() {
		if (IsSolved) return;
		if (State == ModuleState.HELD) {
			if (Input.Last() == 'h') {
				Input = Input.SkipLast(1).Join("") + "t";
				Symbols.ChangeLastSymbolColor(Color.green);
			} else {
				Input += "r";
				Symbols.CreateSymbol(Color.red);
			}
			State = ModuleState.RELEASED;
		}
	}


	private void ActivateAccretionDisk() {
		Symbols.Clear();
		Audio.PlaySoundAtTransform(ACTIVATION_SOUND, transform);
		ActivationTime = Time.time;
		AccretionDisk.Activate();
		Log("Accretion disk activated. Type: {0}. Rotation: {1}. Colors: {2}", TYPE_TO_STRING[AccretionDisk.Type], AccretionDisk.CW ? "CW" : "CCW",
			AccretionDisk.Colors.Select(c => SMBHUtils.NameOfColor[c]).Join(","));
	}

	private int CalculateValidAnswer() {
		if (BombInfo.GetUnsolvedModuleIDs().Any(m => m == BHReflector.BLACK_HOLE_MODULE_ID) && Info.bhInfo != null) {
			return Info.bhInfo.SolutionCode.Take(Info.bhInfo.DigitsEntered).Sum();
		}
		if (AccretionDisk.Type == AccretionDiskType.SOLID) {
			Color cl = AccretionDisk.Colors[0];
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
		}
		Color[] threeColors = AccretionDisk.Colors.Length == 3
			? AccretionDisk.Colors
			: AccretionDisk.Colors.Concat(new[] { SMBHUtils.GetFirstColorExclude(RuleSeed.Seed, AccretionDisk.Type, AccretionDisk.CW, AccretionDisk.Colors) });
		return SMBHUtils.GetThreeColorValue(threeColors, RuleSeed.Seed);
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

}
