using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KeepCoding;
using KModkit;

using Type = System.Type;

public static class BHReflector {
	public const string BLACK_HOLE_MODULE_ID = "BlackHoleModule";

	public class SMBHBombInfo {
		public HashSet<SMBHModule> Modules;
		public int BHsCount;
		public object BHBombInfo;
		public FieldInfo DigitsEnteredField;
		public List<int> SolutionCode;
		public int? DigitsEntered { get { return DigitsEnteredField.GetValue(BHBombInfo) as int?; } }
		public int LastProcessedDigitsEntered = -1;
	}

	private static Dictionary<string, SMBHBombInfo> BHBombInfos = new Dictionary<string, SMBHBombInfo>();

	public static SMBHBombInfo GetBHBombInfo(SMBHModule smbh) {
		string serial = smbh.BombInfo.GetSerialNumber();
		SMBHBombInfo result;
		if (BHBombInfos.ContainsKey(serial)) {
			result = BHBombInfos[serial];
			result.Modules.Add(smbh);
			return result;
		}
		int modulesCount = smbh.transform.parent.childCount;
		IEnumerable<KMBombModule> modules = Enumerable.Range(0, modulesCount).Select(i => (
			smbh.transform.parent.GetChild(i).GetComponent<KMBombModule>()
		)).Where(m => m != null);
		KMBombModule[] bhs = modules.Where(m => m.ModuleType == BLACK_HOLE_MODULE_ID).ToArray();
		KMBombModule bh = bhs.FirstOrDefault();
		result = ExtractInfoFromBHModule(bh);
		if (result != null) {
			result.Modules = new HashSet<SMBHModule>() { smbh };
			result.BHsCount = bhs.Length;
		}
		BHBombInfos[serial] = result;
		smbh.BombInfo.OnBombExploded += () => BHBombInfos.Remove(serial);
		smbh.BombInfo.OnBombSolved += () => BHBombInfos.Remove(serial);
		return result;
	}

	private static SMBHBombInfo ExtractInfoFromBHModule(KMBombModule bh) {
		if (bh == null) return null;
		Component comp = bh.GetComponent("BlackHoleModule");
		if (comp == null) return null;
		Type type = comp.GetType();
		FieldInfo originalInfoField = type.GetField("_info", BindingFlags.Instance | BindingFlags.NonPublic);
		if (originalInfoField == null) return null;
		object originalInfo = originalInfoField.GetValue(comp);
		if (originalInfo == null) return null;
		Type originalInfoType = originalInfo.GetType();
		FieldInfo digitsEnteredField = originalInfoType.GetField("DigitsEntered", BindingFlags.Public | BindingFlags.Instance);
		if (digitsEnteredField == null) return null;
		FieldInfo solutionCodeField = originalInfoType.GetField("SolutionCode", BindingFlags.Public | BindingFlags.Instance);
		if (solutionCodeField == null) return null;
		List<int> solutionCode = solutionCodeField.GetValue(originalInfo) as List<int>;
		if (solutionCode == null) return null;
		SMBHBombInfo result = new SMBHBombInfo();
		result.BHBombInfo = originalInfo;
		result.DigitsEnteredField = digitsEnteredField;
		result.SolutionCode = solutionCode;
		return result;
	}
}
