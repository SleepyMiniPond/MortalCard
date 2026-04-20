using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class CustomLineRenderer : Graphic
{
    public record PathPoint(Vector2 Position, float CumulativeLength, Color Color);

    public enum LineType
    {
        Solid,
        Dashed,
        Dotted
    }

    [Header("目標與繪製設定")]
    [SerializeField]
    private Vector3 _startWorldPosition;
    [SerializeField]
    private RectTransform _targetRect;

    [Header("曲線設定")]
    [Range(2, 1000)]
    [SerializeField]
    private int _segments;
    [SerializeField]
    private float _curveHeight;

    [Header("線條外觀")]
    [SerializeField]
    private LineType _lineType = LineType.Solid;
    [SerializeField]
    private float _thickness;

    [Header("虛線/點狀線設定")]
    [SerializeField]
    [Range(1, 100)]
    private float _dashLength;
    [SerializeField]
    [Range(1, 100)]
    private float _gapLength;

    [Header("流動")]
    [SerializeField]
    private bool _enableFlowAnimation = false;
    [SerializeField]
    [Range(-100, 100)]
    private float _flowSpeed;

    [Header("箭頭外觀")]
    [SerializeField]
    private float _arrowHeadLength;
    [SerializeField]
    private float _arrowHeadWidth;
    [SerializeField]
    private float _lineIntersectOffset;

    [Header("漸變色 (Gradient)")]
    [SerializeField]
    private Color _startColor = Color.white;
    [SerializeField]
    private Color _endColor = Color.black;

    private RectTransform _canvasRect;

    private float animationPhase = 0f;

    protected override void OnValidate()
    {
        base.OnValidate();        
        SetVerticesDirty();
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        // 確保每次啟用都從頭開始
        animationPhase = 0;
    }

    protected void Update()
    {
        if (_enableFlowAnimation)
        {
            float patternLength = _dashLength + _gapLength;
            if(patternLength > 0)
            {
                // 更新相位，並使用 % 運算符使其循環
                animationPhase = (animationPhase - Time.deltaTime * _flowSpeed) % patternLength;
            }
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_targetRect == null) return;
        if (_canvasRect == null) _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        Vector2 startPoint = rectTransform.InverseTransformPoint(_startWorldPosition);
        Vector2 endPoint = rectTransform.InverseTransformPoint(_targetRect.position);
        Vector2 controlPoint = (startPoint + endPoint) / 2 + Vector2.Perpendicular(endPoint - startPoint).normalized * _curveHeight;


        int resolution = _segments * 3;
        var paths = _GeneratePathWithArcLength(startPoint, controlPoint, endPoint, resolution);
        
        if (paths.Count < 2) return;

        var totalLength = paths[paths.Count - 1].CumulativeLength;
        var arrowBaseDistance = Mathf.Max(0, totalLength - _arrowHeadLength + _lineIntersectOffset);

        switch (_lineType)
        {
            case LineType.Solid:
                _DrawSolidLine(vh, paths, arrowBaseDistance);
                break;
            case LineType.Dashed:
                _DrawDashedLine(vh, paths, _dashLength, _gapLength, animationPhase, arrowBaseDistance);
                break;
            case LineType.Dotted:
                _DrawDashedLine(vh, paths, _thickness, _gapLength + (_dashLength - _thickness), animationPhase, arrowBaseDistance);
                break;
        }
        
        _DrawArrowHead(vh, paths);
    }
        
    private List<PathPoint> _GeneratePathWithArcLength(Vector2 start, Vector2 curvePoint, Vector2 end, int resolution)
    {
        var points = new List<PathPoint>();
        float cumulativeLength = 0;
        Vector2 prevPoint = GetBezierPoint(start, curvePoint, end, 0);
        
        points.Add(new PathPoint(prevPoint, 0, GradientColor(0)));
        for (int i = 1; i <= resolution; i++)
        {
            var ratio = (float)i / resolution;
            Vector2 currentPoint = GetBezierPoint(start, curvePoint, end, ratio);
            cumulativeLength += Vector2.Distance(prevPoint, currentPoint);
            
            points.Add(new PathPoint(currentPoint, cumulativeLength, GradientColor(ratio)));
            
            prevPoint = currentPoint;
        }
        return points;     

        Vector2 GetBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float ratio)
        {
            ratio = Mathf.Clamp01(ratio); 
            var oneMinusRatio = 1f - ratio;
            return oneMinusRatio * oneMinusRatio * p0 + 2f * oneMinusRatio * ratio * p1 + ratio * ratio * p2;
        }

        Color GradientColor(float ratio)
            => Color.Lerp(_startColor, _endColor, ratio) * this.color;   
    }
    
    private void _DrawSolidLine(VertexHelper vh, List<PathPoint> pathPoints, float maxLength)
    {
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            var p1 = pathPoints[i];
            var p2 = pathPoints[i + 1];

            if (p1.CumulativeLength >= maxLength) 
                break;
            
            if (p2.CumulativeLength > maxLength)
            {
                p2 = _GetPointAtDistance(pathPoints, maxLength);
                _DrawQuadBetweenPoints(vh, p1, p2);
                break;
            }
            
            _DrawQuadBetweenPoints(vh, p1, p2);
        }
    }
    
    private void _DrawDashedLine(VertexHelper vh, List<PathPoint> pathPoints, float dashLength, float gapLength, float phase, float maxLength)
    {
        var patternLength = dashLength + gapLength;
        if (patternLength <= 0)
        {
            _DrawSolidLine(vh, pathPoints, maxLength);
            return;
        }

        float fmod(float a, float b) => a - b * Mathf.Floor(a / b);
        var normalizedPhase = fmod(phase, patternLength);
        var currentDistance = 0f;
        
        while (currentDistance < maxLength)
        {
            var effectiveDistance = currentDistance + normalizedPhase;
            var positionInPattern = fmod(effectiveDistance, patternLength);

            bool isDrawingDash = positionInPattern < dashLength;            
            if (isDrawingDash)
            {
                var remainingInDash = dashLength - positionInPattern;
                var endDashDistance = currentDistance + remainingInDash;
                
                var clampedEnd = Mathf.Min(endDashDistance, maxLength);
                
                var startPoint = _GetPointAtDistance(pathPoints, currentDistance);
                var endPoint = _GetPointAtDistance(pathPoints, clampedEnd);
                _DrawQuadBetweenPoints(vh, startPoint, endPoint);
                
                currentDistance = endDashDistance;
            }
            else
            {
                currentDistance += patternLength - positionInPattern;
            }
        }
    }
    
    private PathPoint _GetPointAtDistance(List<PathPoint> pathPoints, float distance)
    {
        distance = Mathf.Clamp(distance, 0, pathPoints[pathPoints.Count - 1].CumulativeLength);
        
        int low = 0, high = pathPoints.Count - 1, index = 0;
        while(low <= high)
        {
            int mid = low + (high - low) / 2;
            if (pathPoints[mid].CumulativeLength < distance)
            {
                index = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1; 
            }
        }
        
        if (index >= pathPoints.Count - 1) return pathPoints[pathPoints.Count - 1];

        var p1 = pathPoints[index];
        var p2 = pathPoints[index + 1];

        if (p1.CumulativeLength == p2.CumulativeLength) return p1;
        float ratio = (distance - p1.CumulativeLength) / (p2.CumulativeLength - p1.CumulativeLength);
        
        return new PathPoint(Vector2.Lerp(p1.Position, p2.Position, ratio), distance, Color.Lerp(p1.Color, p2.Color, ratio));
    }

    private void _DrawQuadBetweenPoints(VertexHelper vh, PathPoint p1, PathPoint p2)
    {
        Vector2 direction = (p2.Position - p1.Position);
        if (direction == Vector2.zero) return;
        direction.Normalize();

        Vector2 normal = Vector2.Perpendicular(direction) * (_thickness / 2);
        
        var vert1 = new UIVertex { position = p1.Position - normal, color = p1.Color };
        var vert2 = new UIVertex { position = p1.Position + normal, color = p1.Color };
        var vert3 = new UIVertex { position = p2.Position + normal, color = p2.Color };
        var vert4 = new UIVertex { position = p2.Position - normal, color = p2.Color };

        int startIndex = vh.currentVertCount;
        vh.AddVert(vert1);
        vh.AddVert(vert2);
        vh.AddVert(vert3); 
        vh.AddVert(vert4);
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    private void _DrawArrowHead(VertexHelper vh, List<PathPoint> path)
    {
        if (path.Count < 2) return;

        var tipPoint = path[path.Count - 1];
        var baseDistance = Mathf.Max(0, tipPoint.CumulativeLength - _arrowHeadLength);
        PathPoint basePoint = _GetPointAtDistance(path, baseDistance);
        Vector2 direction = (tipPoint.Position - basePoint.Position).normalized;

        if (direction == Vector2.zero)
        {
            direction = (path[path.Count - 1].Position - path[path.Count - 2].Position).normalized; 
        }
        Vector2 normal = Vector2.Perpendicular(direction);
        Vector2 p1 = tipPoint.Position;
        Vector2 p2 = tipPoint.Position - direction * _arrowHeadLength + normal * (_arrowHeadWidth / 2);
        Vector2 p3 = tipPoint.Position - direction * _arrowHeadLength - normal * (_arrowHeadWidth / 2);
        Color headColor = _endColor * this.color;

        var vert1 = new UIVertex { position = p1, color = headColor };
        var vert2 = new UIVertex { position = p2, color = headColor };
        var vert3 = new UIVertex { position = p3, color = headColor };
        var startIndex = vh.currentVertCount;
        vh.AddVert(vert1);
        vh.AddVert(vert2); 
        vh.AddVert(vert3);
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
    }    
    
    public void SetLineProperty(
        Vector3 startWorldPosition,
        RectTransform targetRect,
        LineType? lineType = null,
        float? curveHeight = null,
        bool? enableFlowAnimation = null,
        float? flowSpeed = null,
        float? thickness = null,
        float? dashLength = null,
        float? gapLength = null,
        Color? startColor = null,
        Color? endColor = null)
    {
        _startWorldPosition = startWorldPosition;
        _targetRect = targetRect;
        _lineType = lineType ?? _lineType;
        _curveHeight = curveHeight ?? _curveHeight;

        _enableFlowAnimation = enableFlowAnimation ?? _enableFlowAnimation;
        _flowSpeed = flowSpeed ?? _flowSpeed;
        _thickness = thickness ?? _thickness;
        _dashLength = dashLength ?? _dashLength;
        _gapLength = gapLength ?? _gapLength;

        _startColor = startColor ?? _startColor;
        _endColor = endColor ?? _endColor;

        SetVerticesDirty();
    }
}