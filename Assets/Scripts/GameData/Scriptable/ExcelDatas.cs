using System;
using System.Collections.Generic;
using UnityEngine;

[ExcelAsset(ExcelName = "MortalGames", AssetPath = "ScriptableObjects")]
public class ExcelDatas : ScriptableObject
{
	public List<ConstExcelData> Constant;
	public List<DispositionExcelData> Disposition;
	public List<LocalizeExcelTitleData> LocalizeCard;
	public List<LocalizeExcelTitleData> LocalizeCardBuff;
	public List<LocalizeExcelTitleData> LocalizeKeyWord;
	public List<LocalizeExcelTitleData> LocalizePlayerBuff;
	public List<LocalizeExcelTitleData> LocalizePlayer;
	public List<LocalizeExcelData> LocalizeUI;
}

[Serializable]

public class ConstExcelData
{
	public string Id;
	public string Value;
}

[Serializable]
public class DispositionExcelData
{
	public string Id;
	public int Range;
	public int RecoverEnergyPoint;
	public int DrawCardCount;
}

[Serializable]
public class LocalizeExcelTitleData
{
	public string Id;
	public string Title;
	public string Info;
}

[Serializable]
public class LocalizeExcelData
{
	public string Id;
	public string Info;
}