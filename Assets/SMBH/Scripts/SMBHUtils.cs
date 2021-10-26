using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SMBHUtils {
	public enum AccretionDiskType { SOLID, SECTORS, RINGS, DYNAMIC }

	public static readonly Color ORANGE = (Color)new Color32(0xff, 0x80, 0, 0);
	public static readonly Color[] COLORS = new[] { ORANGE, Color.white, Color.red, Color.yellow, Color.green, Color.blue };

	public static readonly Dictionary<Color, string> NameOfColor = new Dictionary<Color, string> {
		{ ORANGE, "Orange" },
		{ Color.white, "White" },
		{ Color.red, "Red" },
		{ Color.yellow, "Yellow" },
		{ Color.green, "Green" },
		{ Color.blue, "Blue" },
	};

	public static readonly string[] DefaultIgnoredModules = new[] {
		"+",
		"14",
		"42",
		"501",
		"A>N<D",
		"Amnesia",
		"B-Machine",
		"Bamboozling Time Keeper",
		"Black Arrows",
		"Brainf---",
		"Busy Beaver",
		"Button Messer",
		"Concentration",
		"Cookie Jars",
		"Cube Synchronization",
		"Divided Squares",
		"Don't Touch Anything",
		"Doomsday Button",
		"Duck Konundrum",
		"Encryption Bingo",
		"Floor Lights",
		"Forget Any Color",
		"Forget Enigma",
		"Forget Everything",
		"Forget Infinity",
		"Forget It Not",
		"Forget Maze Not",
		"Forget Me Later",
		"Forget Me Not",
		"Forget Perspective",
		"Forget The Colors",
		"Forget Them All",
		"Forget This",
		"Forget Us Not",
		"Four-Card Monte",
		"Gemory",
		"Hogwarts",
		"Iconic",
		"Keypad Directionality",
		"Kugelblitz",
		"Multitask",
		"Mystery Module",
		"OmegaDestroyer",
		"OmegaForget",
		"Organization",
		"Out of Time",
		"Password Destroyer",
		"Pow",
		"Purgatory",
		"RPS Judging",
		"SMBH",
		"Scrabble Scramble",
		"Security Council",
		"Shoddy Chess",
		"Simon",
		"Simon Forgets",
		"Simon's Stages",
		"Soulscream",
		"Souvenir",
		"Tallordered Keys",
		"Tetrahedron",
		"The Board Walk",
		"The Heart",
		"The Klaxon",
		"The Swan",
		"The Time Keeper",
		"The Troll",
		"The Twin",
		"The Very Annoying Button",
		"Timing is Everything",
		"Turn The Key",
		"Turn The Keys",
		"Ultimate Custom Night",
		"Whiteout",
		"Zener Cards",
		"Übermodule",
	};

	public static string GetBHSCII(int num, int rndSeend) {
		string[] alternatives = new[] { "h-r-t", "h-r" };
		if (BHSCII.ContainsKey(rndSeend)) return BHSCII[rndSeend][num];
		MonoRandom rnd = new MonoRandom(rndSeend);
		for (int i = rnd.Next(0, 10); i > 0; i--) rnd.NextDouble();
		string[] pool = new[] { "t-t", "h-rh-r", "t-h-r", "h--r", alternatives[rnd.Next(0, alternatives.Length)], "t--t" }.OrderBy(x => rnd.NextDouble()).ToArray();
		string c = new[] { "h-rt", "tt", "th-r" }[rnd.Next(0, 3)];
		string[] bhAB = pool.Take(5).Concat(Enumerable.Range(0, 7).Select(_ => "")).Concat(new[] { c }).Concat(Enumerable.Range(0, 23).Select(_ => "")).ToArray();
		string[] codes = GenerateBHCodes(rnd, bhAB).OrderBy(x => rnd.NextDouble()).ToArray();
		GenerateTwoColorTable(rnd);
		GenerateThreeColorTable(rnd);
		int lastTakeIndex = codes.Length;
		for (int i = 0; i < bhAB.Length; i++) {
			if (bhAB[i].Length > 0) continue;
			lastTakeIndex -= 1;
			bhAB[i] = codes[lastTakeIndex];
		}
		BHSCII[rnd.Seed] = bhAB;
		return bhAB[num];
	}

	public static int GetThreeColorValue(Color[] colors, int rndSeed) {
		GetBHSCII(0, rndSeed);
		char[] tmp = colors.Select(c => NameOfColor[c][0]).ToArray();
		System.Array.Sort(tmp);
		string clr = tmp.Join("");
		return ThreeColorTable[rndSeed][clr];
	}

	private static Dictionary<int, string[]> BHSCII = new Dictionary<int, string[]>();
	private static Dictionary<int, Dictionary<AccretionDiskType, Color[]>> CWAddColors = new Dictionary<int, Dictionary<AccretionDiskType, Color[]>>();
	private static Dictionary<int, Dictionary<AccretionDiskType, Color[]>> CCWAddColors = new Dictionary<int, Dictionary<AccretionDiskType, Color[]>>();
	private static Dictionary<int, Dictionary<string, int>> ThreeColorTable = new Dictionary<int, Dictionary<string, int>>();

	private static string[] GenerateBHCodes(MonoRandom rnd, string[] bhAB) {
		Dictionary<int, List<string>> variants = new Dictionary<int, List<string>>();
		AddVariant(variants, "t");
		AddVariant(variants, "h-r");
		AddVariant(variants, "h--r");
		int minVariant = 0;
		List<string> codes = new List<string>();
		int limit = 100;
		while (true) {
			if (limit-- == 0) {
				throw new System.Exception(string.Format("Unable to build BHSCII Table for seed {0}. Built codes count: {1}", rnd.Seed, codes.Count));
			}
			if (!variants.ContainsKey(minVariant) || variants[minVariant].Count == 0) {
				minVariant += 1;
				continue;
			}
			List<string> vars = variants[minVariant];
			int variantIndex = rnd.Next(0, vars.Count);
			string prevCode = vars[variantIndex];
			if (prevCode.Length > 1 && !bhAB.Contains(prevCode)) codes.Add(prevCode);
			if (codes.Count >= bhAB.Length - 6) return codes.ToArray();
			vars[variantIndex] = vars[vars.Count - 1];
			vars.RemoveAt(vars.Count - 1);
			int lastTickIndex = prevCode.LastIndexOf("-");
			AddVariant(variants, prevCode + "-t");
			AddVariant(variants, prevCode + "-h-r");
			AddVariant(variants, prevCode + "-h--r");
			if (prevCode.Length - lastTickIndex < 3) {
				AddVariant(variants, prevCode + "t");
				AddVariant(variants, prevCode + "h-r");
				AddVariant(variants, prevCode + "h--r");
			}
		}
	}

	private static void GenerateTwoColorTable(MonoRandom rnd) {
		CWAddColors[rnd.Seed] = new Dictionary<AccretionDiskType, Color[]>();
		CCWAddColors[rnd.Seed] = new Dictionary<AccretionDiskType, Color[]>();
		Color[] topRowColors = COLORS.OrderBy(x => rnd.NextDouble()).ToArray();
		int columnIndex = 0;
		foreach (AccretionDiskType type in new[] { AccretionDiskType.RINGS, AccretionDiskType.SECTORS, AccretionDiskType.DYNAMIC }) {
			Color topColor;
			topColor = topRowColors[columnIndex++];
			CWAddColors[rnd.Seed][type] = new[] { topColor }.Concat(COLORS.Where(c => c != topColor).OrderBy(x => rnd.NextDouble())).ToArray();
			topColor = topRowColors[columnIndex++];
			CCWAddColors[rnd.Seed][type] = new[] { topColor }.Concat(COLORS.Where(c => c != topColor).OrderBy(x => rnd.NextDouble())).ToArray();
		}
	}

	private static void GenerateThreeColorTable(MonoRandom rnd) {
		ThreeColorTable[rnd.Seed] = new Dictionary<string, int>();
		int[] chars = Enumerable.Range(0, 36).OrderBy(x => rnd.NextDouble()).ToArray().ToArray();
		int lastIndex = chars.Length;
		char[] cs = COLORS.Select(c => NameOfColor[c][0]).ToArray();
		for (int i1 = 0; i1 < cs.Length; i1++) {
			for (int i2 = i1 + 1; i2 < cs.Length; i2++) {
				for (int i3 = i2 + 1; i3 < cs.Length; i3++) {
					char[] temp = new[] { i1, i2, i3 }.Select(i => cs[i]).ToArray();
					System.Array.Sort(temp);
					string clr = temp.Join("");
					lastIndex -= 1;
					ThreeColorTable[rnd.Seed][clr] = chars[lastIndex];
				}
			}
		}
	}

	public static int ParseBHCode(string code, int rndSeed) {
		GetBHSCII(0, rndSeed);
		return BHSCII[rndSeed].IndexOf((str) => str == code);
	}

	public static Color GetFirstColorExclude(int rndSeed, AccretionDiskType type, bool cw, params Color[] except) {
		GetBHSCII(0, rndSeed);
		return (cw ? CWAddColors : CCWAddColors)[rndSeed][type].First(c => !except.Contains(c));
	}

	private static void AddVariant(Dictionary<int, List<string>> variants, string code) {
		if (variants.ContainsKey(code.Length)) variants[code.Length].Add(code);
		else variants[code.Length] = new List<string> { code };
	}
}
