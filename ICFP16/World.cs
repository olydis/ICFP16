using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ICFP16
{
    public class World
    {
        Graph g;
        Dictionary<Point, int> field;
        List<Point> baseLine;
        List<Tuple<int, Tuple<bool, List<Point>>>> facets;
        BigRational area;

        private World() { }
        public World(List<int> baseLine, Graph g)
        {
            this.g = g;
            this.baseLine = new List<Point>();
            this.field = new Dictionary<Point, int>();
            this.facets = new List<Tuple<int, Tuple<bool, List<Point>>>>();
            this.area = BigRational.Zero();
            var origin = new Point(BigRational.Zero(), BigRational.Zero());
            for (var i = 0; i < baseLine.Count; ++i)
            {
                this.baseLine.Add(origin);
                field[origin] = baseLine[i];

                if (i < baseLine.Count - 1)
                {
                    var a = g.Nodes[baseLine[i]].Item1;
                    var b = g.Nodes[baseLine[i + 1]].Item1;
                    var len = (a - b).Length();
                    origin.Y += len.Value;
                }
            }
        }

        public bool Solved
        {
            get { return area.IsOne(); }
        }

        public World Clone
        {
            get
            {
                return new World
                {
                    g = g,
                    baseLine = baseLine.ToList(),
                    field = new Dictionary<Point, int>(field),
                    facets = facets.ToList(),
                    area = area
                };
            }
        }

        internal void Render()
        {
            var target = Form1.RenderTarget;

            Rectangle bounds = target.ClientRectangle;
            if (bounds.Width < bounds.Height)
                bounds.Inflate(0, (bounds.Width - bounds.Height) / 2);
            if (bounds.Width > bounds.Height)
                bounds.Inflate((bounds.Height - bounds.Width) / 2, 0);

            var gg = target.CreateGraphics();
            BufferedGraphics bg = BufferedGraphicsManager.Current.Allocate(gg, bounds);
            var g = bg.Graphics;

            g.Clear(Color.White);
            Brush brush = new SolidBrush(Color.FromArgb(150, Color.CornflowerBlue));
            Pen pen = new Pen(Color.Red, 5);
            PointF[] poly;
            Func<Point, PointF> transform = p => new PointF(p.X.ToFloat() * bounds.Width + bounds.X, p.Y.ToFloat() * bounds.Height + bounds.Y);
            foreach (var f in facets)
            {
                poly = f.Item2.Item2.Select(transform).ToArray();
                g.FillPolygon(brush, poly);
                g.DrawPolygon(Pens.CornflowerBlue, poly);
            }
            // baseline
            poly = baseLine.Select(transform).ToArray();
            if (poly.Length > 1)
                g.DrawLines(pen, poly);

            // field
            foreach (var f in field)
            {
                var p = transform(f.Key);
                g.FillEllipse(Brushes.Black, p.X - 1, p.Y - 1, 3, 3);
            }

            bg.Render();
        }

        public bool Valid
        {
            get
            {
                // cover all fasets (at least once?)
                foreach (var f in g.Faces.Keys)
                    if (!facets.Any(x => x.Item1 == f))
                        return false;
                // baseline gone?
                if (this.baseLine.Count > 1)
                    return false;
                return true;
            }
        }

        private bool AddPoint(int i, Point p)
        {
            if (field.ContainsKey(p) && field[p] != -1)
                return field[p] == i || i == -1;
            field[p] = i;
            return true;
        }

        private bool BaseLineCheckSimpl()
        {
            if (area > BigRational.One())
                return false;

            // validate Y range
            if (baseLine.Any(x => x.Y > BigRational.One() || x.Y < BigRational.Zero()))
                return false;
            // validate X range
            if (baseLine.Any(x => x.X > BigRational.One() || x.X < BigRational.Zero()))
                return false;

            // cut ends
            while (baseLine.Count > 1 && baseLine[0].Y.IsZero() && baseLine[1].Y.IsZero())
                baseLine.RemoveAt(0);
            while (baseLine.Count > 1 && baseLine[0].X.IsOne() && baseLine[1].X.IsOne())
                baseLine.RemoveAt(0);
            while (baseLine.Count > 1 && baseLine[0].Y.IsOne() && baseLine[1].Y.IsOne())
                baseLine.RemoveAt(0);
            while (baseLine.Count > 1 && baseLine[0].X.IsZero() && baseLine[1].X.IsZero())
                baseLine.RemoveAt(0);
            while (baseLine.Count > 1 && baseLine[baseLine.Count - 1].X.IsZero() && baseLine[baseLine.Count - 2].X.IsZero())
                baseLine.RemoveAt(baseLine.Count - 1);
            while (baseLine.Count > 1 && baseLine[baseLine.Count - 1].Y.IsOne() && baseLine[baseLine.Count - 2].Y.IsOne())
                baseLine.RemoveAt(baseLine.Count - 1);
            while (baseLine.Count > 1 && baseLine[baseLine.Count - 1].X.IsOne() && baseLine[baseLine.Count - 2].X.IsOne())
                baseLine.RemoveAt(baseLine.Count - 1);
            while (baseLine.Count > 1 && baseLine[baseLine.Count - 1].Y.IsZero() && baseLine[baseLine.Count - 2].Y.IsZero())
                baseLine.RemoveAt(baseLine.Count - 1);

            while (baseLine.Count > 1
                && baseLine[0].Equals(baseLine[baseLine.Count - 1])
                && baseLine[1].Equals(baseLine[baseLine.Count - 2]))
            {
                baseLine.RemoveAt(0);
                baseLine.RemoveAt(baseLine.Count - 1);
            }

            // remove inner points
            for (int i = 1; i < baseLine.Count - 1; ++i)
                if (baseLine[i - 1].Equals(baseLine[i + 1]))
                {
                    baseLine.RemoveAt(i);
                    baseLine.RemoveAt(i);
                    i -= 2;
                    if (i < 1) i = 1;
                }

            // self intersection
            // only checking new part!
            return true;
        }

        public IEnumerable<World> Solutions()
        {
            var state = new Stack<World>();
            state.Push(this);

            var sw = Stopwatch.StartNew();
            BigRational maxArea = BigRational.Zero();
            while (state.Count > 0)
            {
                var x = state.Pop();

                if (x.facets.Count > Constants.MaxFacetsCount)
                {
                    Console.WriteLine("hit max. facets count");
                    continue;
                }
                
                if (sw.ElapsedMilliseconds > Constants.MaxTimePerWorldMS)
                {
                    Console.WriteLine("BREAK current world: TIMEOUT");
                    yield break;
                }
                // DEBUG
                if (Form1.SleepBetweenSteps > 0 || x.area >= maxArea)
                {
                    maxArea = x.area;
                    x.Render();
                    Application.DoEvents();
                    Thread.Sleep(Form1.SleepBetweenSteps);
                }
                // DEBUG

                if (x.Solved)
                {
                    if (x.Valid)
                        yield return x;
                }
                else
                    foreach (var xx in x.Extend().OrderByDescending(y => y.area))
                        state.Push(xx);
            }
        }

        private IEnumerable<World> Extend()
        {
            return ExtendAt(0);

            var bestIndex = -1;
            float bestScore = -1;
            List<int> options = Enumerable.Range(0, baseLine.Count - 1).ToList();
            foreach (var i in options)
            {
                var p1 = baseLine[i];
                var p2 = baseLine[i + 1];

                if (Point.AtBorder(p1, p2))
                    continue;
                if (baseLine.Where((x, j) => i != j && x.Equals(p1)).Any() &&
                    baseLine.Where((x, j) => i + 1 != j && x.Equals(p2)).Any())
                    continue;

                if (Point.CloseToBorder(p1, p2))
                    return ExtendAt(i);

                var v1 = field[p1];
                var v2 = field[p2];

                var faces = g.FacesAt(v1, v2).ToList();

                //if (faces.Count == 1)
                //    return ExtendAt(i);

                // SCORING
                float choicesScore = 1f / faces.Count;
                float sizeScore = faces.Sum(x => g.Faces[x].Item1.ToFloat()) / faces.Count;
                float posScore = 1 - Math.Min(p1.Y.ToFloat(), p2.Y.ToFloat());
                float relSizeScore = sizeScore / new Edge(p1,p2).GetLengthSquared().ToFloat();

                float score = sizeScore * posScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex == -1)
                return Enumerable.Empty<World>();
            
            return ExtendAt(bestIndex);
        }

        private IEnumerable<World> ExtendAt(int baselineIndex)
        {
            var p1 = baseLine[baselineIndex];
            var p2 = baseLine[baselineIndex + 1];
            var v1 = field[p1];
            var v2 = field[p2];

            var faces = g.FacesAt(v1, v2).ToList();
            foreach (var face in faces
                .OrderByDescending(x => g.Faces[x].Item1))
            {
                var facet = g.Faces[face];
                var facetOutline = facet.Item2;
                // find orientation
                bool flip = false;
                var temp = facetOutline.Concat(facetOutline).ToList();
                if (temp[temp.IndexOf(v1) + 1] != v2)
                    flip = true;

                if (flip)
                    facetOutline = facetOutline.AsEnumerable().Reverse().ToList();
                int index = facetOutline.IndexOf(v1);
                // shift
                while (index-- > 0)
                {
                    var front = facetOutline[0];
                    facetOutline.RemoveAt(0);
                    facetOutline.Add(front);
                }

                // coords
                var facetOutlineCoords = facetOutline.Select(x => g.Nodes[x].Item1).ToList();
                if (!flip)
                    facetOutlineCoords = facetOutlineCoords.Select(x => new Point(-x.X, x.Y)).ToList();
                // normalize coords
                var tempOrigin = facetOutlineCoords[0];
                facetOutlineCoords = facetOutlineCoords.Select(x => x - tempOrigin).ToList();

                var dTarg = p2 - p1;
                var dSrc = facetOutlineCoords[1];
                var dSrcLenSq = dSrc.X * dSrc.X + dSrc.Y * dSrc.Y;
                BigRational xx = (dTarg.X * dSrc.X + dTarg.Y * dSrc.Y) / dSrcLenSq;
                BigRational yy = (dTarg.Y * dSrc.X - dTarg.X * dSrc.Y) / dSrcLenSq;
                //dTarg.X = xx * dSrc.X - yy * dSrc.Y;
                //dTarg.Y = yy * dSrc.X + xx * dSrc.Y;

                // rotate
                facetOutlineCoords = facetOutlineCoords.Select(x => new Point(
                    xx * x.X - yy * x.Y,
                    yy * x.X + xx * x.Y
                )).ToList();

                // translate back
                facetOutlineCoords = facetOutlineCoords.Select(x => x + p1).ToList();




                bool valid = true;

                // check for self-intersection
                for (int i = 1; i < facetOutlineCoords.Count; ++i)
                {
                    var ne = new Edge(facetOutlineCoords[i - 1], facetOutlineCoords[i]);
                    for (var j = 1; j < baseLine.Count; ++j)
                    {
                        var me = new Edge(baseLine[j - 1], baseLine[j]);
                        if (Edge.Intersects(ne, me))
                            valid = false;
                    }
                }
                // check for sneaky self-intersection using min. X/Y
                var mmminX = baseLine.Min(x => x.X);
                var mmminY = baseLine.Min(x => x.Y);
                if (facetOutlineCoords.Any(x => x.X < mmminX))
                    valid = false;
                if (facetOutlineCoords.Any(x => x.Y < mmminY))
                    valid = false;
                // check for field occupation
                //foreach (var newP in facetOutlineCoords)
                //    if (field.ContainsKey(newP) && field[newP] != -1)
                //        if (!baseLine.Contains(newP))
                //            valid = false;
                // check for facet field-point containment
                if (Environment.TickCount % 10 == 0)
                {
                    var tempFacet = new Facet { Outline = facetOutlineCoords };
                    foreach (var pp in field.Keys)
                        if (!tempFacet.IsOnOutline(pp) && tempFacet.IsInside(pp) ||
                            tempFacet.OutlineEdges().Any(e => e.Contains(pp)))
                            valid = false;
                }
                // check max complexity
                foreach (var foc in facetOutlineCoords)
                    if (foc.Complexity() > g.MaxComplexity + Constants.MaxComplexityBonus)
                        valid = false;

                var w = Clone;
                // INSERT into baseline
                w.baseLine.InsertRange(baselineIndex + 1, facetOutlineCoords.Skip(2).Reverse());
                // INSERT into field
                for (int i = 2; i < facetOutline.Count; ++i)
                {
                    var foi = facetOutline[i];
                    valid &= w.AddPoint(foi, facetOutlineCoords[i]);
                    //assume diago axis symmetry
                    //if (false)
                    {
                        //valid &= w.AddPoint(foi, new Point(facetOutlineCoords[i].Y, facetOutlineCoords[i].X));
                        //valid &= w.AddPoint(foi, facetOutlineCoords[i].Rot180SQ());
                        //valid &= w.AddPoint(foi, new Point(facetOutlineCoords[i].Y, facetOutlineCoords[i].X).Rot180SQ());
                    }
                    if (false)
                    {
                        valid &= w.AddPoint(-1, facetOutlineCoords[i].Rot90SQ());
                        valid &= w.AddPoint(-1, facetOutlineCoords[i].Rot180SQ());
                        valid &= w.AddPoint(-1, facetOutlineCoords[i].Rot180SQ().Rot90SQ());
                    }
                    if (g.RotationSymmetric90())
                    {
                        valid &= w.AddPoint(g.map90[foi], facetOutlineCoords[i].Rot90SQ());
                        valid &= w.AddPoint(g.map180[foi], facetOutlineCoords[i].Rot180SQ());
                        valid &= w.AddPoint(g.map90[g.map180[foi]], facetOutlineCoords[i].Rot180SQ().Rot90SQ());
                    }
                    //if (g.RotationSymmetric180())
                    //{
                    //    //valid &= w.AddPoint(-1, facetOutlineCoords[i].Rot180SQ());
                    //    valid &= w.AddPoint(g.map180[foi], facetOutlineCoords[i].Rot180SQ());
                    //}
                }
                // update FACETS and area
                w.area += facet.Item1;
                w.facets.Add(new Tuple<int, Tuple<bool, List<Point>>>(
                    face, 
                    new Tuple<bool, List<Point>>(flip, facetOutlineCoords)));
                // return
                if (valid && w.BaseLineCheckSimpl())
                    yield return w;
            }
        }

        public void OptimizeSolution()
        {
            // check for removable shared edges
        redo:
            for(int i = 0; i < facets.Count; ++i)
            {
                var facet = facets[i].Item2;
                var candid = facets
                    .Where(f => f.Item2 != facet)
                    .Where(f => f.Item2.Item1 == facet.Item1)
                    .Where(f => f.Item2.Item2.Count(p => facet.Item2.Contains(p)) == 2)
                    .FirstOrDefault();
                if (candid == null)
                    continue;

                var face1 = facet.Item2;
                var face2 = candid.Item2.Item2;

                var j = facets.IndexOf(candid);
                var schnitt = face1.Where(p => face2.Contains(p)).ToList();
                var a = schnitt[0];
                var b = schnitt[1];
                while (!face2[0].Equals(a))
                {
                    face2.Add(face2[0]);
                    face2.RemoveAt(0);
                }
                while (!face1[0].Equals(b))
                {
                    face1.Add(face1[0]);
                    face1.RemoveAt(0);
                }
                face2.RemoveAt(0);
                face2.RemoveAt(face2.Count - 1);
                face1.AddRange(face2);
                
                facets.RemoveAt(j);
                goto redo;
            }
        }

        public override string ToString()
        {
            var fieldPoints = field.ToList();

            var s = new StringBuilder();
            s.AppendLine(field.Count.ToString());
            foreach (var f in fieldPoints)
                s.AppendLine(f.Key.ToString());
            s.AppendLine(facets.Count.ToString());
            foreach (var facet in facets)
            {
                var points = facet.Item2.Item2;
                s.AppendLine(points.Count + " " + string.Join(" ", points.Select(p => fieldPoints.FindIndex(x => x.Key.Equals(p)))));
            }
            foreach (var f in fieldPoints)
                s.AppendLine(g.Nodes[f.Value].Item1.ToString());

            return s.ToString();
        }

    }
}
