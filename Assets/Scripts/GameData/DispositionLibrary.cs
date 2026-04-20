using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public record DispositionData(
    string ID,
    int Range,
    int RecoverEnergyPoint,
    int DrawCardCount
);

public class DispositionLibrary
{
    // key: upper bound 
    public Dictionary<int, DispositionData> _dispositions { get; }
    // Range: [2][3] means data will be [0-1][2-4] 
    public int MaxDisposition => _dispositions.Values.Sum(x => x.Range) - 1;
    
    public DispositionLibrary(IReadOnlyCollection<DispositionData> dispositionDatas)
    {
        _dispositions = new Dictionary<int, DispositionData>();

        var upperBound = 0;
        foreach (var dispositionData in dispositionDatas)
        {
            upperBound += dispositionData.Range;
            _dispositions.Add(upperBound, dispositionData);
        }
    }

    public string GetDispositionId(int value)
    {
        return _GetDispositionData(value).ID;
    }

    public int GetRecoverEnergyPoint(int value)
    {
        return _GetDispositionData(value).RecoverEnergyPoint;
    }

    public int GetDrawCardCount(int value)
    {
        return _GetDispositionData(value).DrawCardCount;
    }

    private DispositionData _GetDispositionData(int value)
    {
        foreach (var disposition in _dispositions)
        {
            if (value < disposition.Key)
            {
                return disposition.Value;
            }
        }

        return _dispositions.Values.Last();
    }
}
