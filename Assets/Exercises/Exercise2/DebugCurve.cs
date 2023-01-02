using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace AfGD
{
    [ExecuteInEditMode]
    public class DebugCurve : MonoBehaviour
    {

        // TODO exercise 2.3
        // you will want to have more than one CurveSegment when creating a cyclic path
        // you can consider a List<CurveSegment>. 
        // You may also want to add more control points, and "lock" the CurveType, since 
        // different curve types make curves in different ranges 
        // (e.g. Catmull-rom and B-spline make a curve from cp2 to cp3, Hermite and Bezier from cp1 to cp4)
        List<CurveSegment> curveSegments;
        // must be assigned in the inspector
        [Tooltip("curve control points/vectors")]
        public Transform cp1, cp2, cp3, cp4;
        [Tooltip("Set the curve type")]
        public CurveType curveType = CurveType.BEZIER;
        public bool isCyclic;

        // these variables are only used for visualization
        [Header("Debug varaibles")]
        [Range(2, 100)]
        public int debugSegments = 20;
        public bool drawPath = true;
        public Color pathColor = Color.magenta;
        public bool drawTangents = true;
        public Color tangentColor = Color.green;

        public float MaxLength
        {
            get
            {
                if(_cumulativeLengths == null)
                    InitCumulativeLengths();

                return _maxLength;
            }
        }
        
        private float _maxLength = float.NaN;
        private float[] _cumulativeLengths;

        bool Init()
        {
            // initialize curve if all control points are valid
            if (cp1 == null || cp2 == null || cp3 == null || cp4 == null)
                return false;

            if (isCyclic)
            {
                Vector3 cp1Pos = cp1.position;
                Vector3 cp2Pos = cp2.position;
                Vector3 cp3Pos = cp3.position;
                Vector3 cp4Pos = cp4.position;

                switch (curveType)
                {
                    case CurveType.CATMULLROM:
                    case CurveType.BSPLINE:
                        curveSegments = new List<CurveSegment>
                        {
                            new CurveSegment(cp1Pos, cp2Pos, cp3Pos, cp4Pos, curveType),
                            new CurveSegment(cp2Pos, cp3Pos, cp4Pos, cp1Pos, curveType),
                            new CurveSegment(cp3Pos, cp4Pos, cp1Pos, cp2Pos, curveType),
                            new CurveSegment(cp4Pos, cp1Pos, cp2Pos, cp3Pos, curveType)
                        };
                        break;
                    case CurveType.BEZIER:
                        curveSegments = new List<CurveSegment>(); // Can't be cyclic
                        Debug.LogWarning("Bezier curve can't be cyclic");
                        break;
                    case CurveType.HERMITE:
                        curveSegments = new List<CurveSegment>
                        {
                            new CurveSegment(cp1Pos, cp2Pos, cp3Pos, cp4Pos, curveType),
                            new CurveSegment(cp3Pos, cp4Pos, cp1Pos, cp2Pos, curveType)
                        };
                        break;
                }
            }
            else
                curveSegments = new List<CurveSegment>
                {
                    new CurveSegment(cp1.position, cp2.position, cp3.position, cp4.position, curveType)
                };

            _cumulativeLengths = null;
            
            return true;
        }

        public Vector4 Evaluate(float u)
        {
            int segment = Mathf.FloorToInt(u);

            float t = u - segment;

            if (segment >= curveSegments.Count)
                return curveSegments[curveSegments.Count - 1].Evaluate(1f);

            return curveSegments[segment].Evaluate(t);
        }
        
        public Vector4 EvaluateDv(float u)
        {
            int segment = Mathf.FloorToInt(u);

            float t = u - segment;
            
            if (segment >= curveSegments.Count)
                return curveSegments[curveSegments.Count - 1].EvaluateDv(1f);
            
            return curveSegments[segment].EvaluateDv(t);
        }
        
        public Vector4 EvaluateDv2(float u)
        {
            int segment = Mathf.FloorToInt(u);

            float t = u - segment;
            
            if (segment >= curveSegments.Count)
                return curveSegments[curveSegments.Count - 1].EvaluateDv2(1f);

            return curveSegments[segment].EvaluateDv2(t);
        }

        public float ArcLength(float length)
        {
            if(_cumulativeLengths == null)
                InitCumulativeLengths();

            int nSegments = curveSegments.Count;
            float deltaU = nSegments / (float) debugSegments;

            int lastIndex = Array.FindLastIndex(_cumulativeLengths, distance => distance < length);

            if (lastIndex < 0)
                return 0;
            
            float a = _cumulativeLengths[lastIndex];
            float b = lastIndex == _cumulativeLengths.Length - 1 ? _maxLength : _cumulativeLengths[lastIndex + 1];
            
            float invL = Mathf.InverseLerp(a, b, length);
            
            return (lastIndex + invL) * deltaU;
        }

        private void InitCumulativeLengths()
        {
            _cumulativeLengths = new float[debugSegments];
            
            int nSegments = curveSegments.Count;

            float deltaU = nSegments / (float) debugSegments;

            Vector4 pCurrent = Vector4.zero;

            for (int i = 0; i <= _cumulativeLengths.Length; i++)
            {
                Vector4 pPrevious = pCurrent;
                
                pCurrent = Evaluate(i * deltaU);
                
                if(i == _cumulativeLengths.Length)
                    _maxLength = _cumulativeLengths[i -1] + Vector3.Distance(pPrevious, pCurrent);
                else if(i > 0)
                    _cumulativeLengths[i] = _cumulativeLengths[i -1] + Vector3.Distance(pPrevious, pCurrent);
            }
        }
        
        public static void DrawCurveSegments(List<CurveSegment> curveSegments,
            Color color, int segments = 50)
        {
            // TODO exercise 2.2
            // evaluate the curve from start to end (range [0, 1])
            // and you draw a number of line segments between 
            // consecutive points

            float stride = 1.0f / segments;

            Vector4 pCurrent = Vector4.zero;

            foreach (CurveSegment curve in curveSegments)
            {
                Vector4 pPrevious = curve.Evaluate(0);
                for (int i = 1; i <= segments; i++)
                {
                    if (i > 1)
                        pPrevious = pCurrent;

                    pCurrent = curve.Evaluate(i * stride);
                
                    Debug.DrawLine(pPrevious, pCurrent, color);
                }
            }
        }

        public static void DrawTangents(List<CurveSegment> curveSegments,
            Color color, int segments = 50, float scale = 0.1f)
        {
            // TODO exercise 2.2
            // evaluate the curve and tangent from start to end (range [0, 1])
            // and draw the tangent as a line from the current curve point
            // to the current point + the tangent vector 

            float stride = 1.0f / segments;
            
            Vector4 p = Vector4.zero;
            Vector4 v = Vector4.zero;
            
            foreach (CurveSegment curve in curveSegments)
            {
                for (int i = 0; i <= segments; i++)
                {
                    p = curve.Evaluate(i * stride);
                    v = scale * curve.EvaluateDv(i * stride);
                
                    Debug.DrawLine(p, p + v, color);
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        // Update is called once per frame
        void Update()
        {
            if (Application.isEditor)
            {
                // reinitialize if we change somethign while not playing
                // this is here so we can update the debug draw of the curve
                // while in edit mode
                if (!Init())
                    return;
            }

            if(curveType == CurveType.HERMITE)
            {
                // Hermite spline has control vectors besides start and end points
                Debug.DrawLine(cp1.position, cp2.position);
                Debug.DrawLine(cp4.position, cp3.position);
            }
            else
            {
                // line connecting control points
                Debug.DrawLine(cp1.position, cp2.position);
                Debug.DrawLine(cp2.position, cp3.position);
                Debug.DrawLine(cp3.position, cp4.position);
            }

            // draw the debug shapes
            if (drawPath)
                DrawCurveSegments(curveSegments, pathColor, debugSegments);
            if (drawTangents)
                DrawTangents(curveSegments, tangentColor, debugSegments);

        }
    }
}