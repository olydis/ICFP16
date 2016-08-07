using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ICFP16
{
    public struct BigRational : IComparable<BigRational>
    {
        public BigRational(BigInteger A, BigInteger B)
        {
            if (B.Sign < 0)
            {
                A *= -1;
                B *= -1;
            }
            var gcd = BigInteger.GreatestCommonDivisor(A, B);
            this.A = A / gcd;
            this.B = B / gcd;
        }

        public int Complexity()
        {
            return B.ToByteArray().Length;
        }

        public BigInteger A;
        public BigInteger B;

        public bool IsOne()
        {
            return A.IsOne && B.IsOne;
        }

        public bool IsZero()
        {
            return A.IsZero;
        }

        public override string ToString()
        {
            if (B.IsOne)
                return A.ToString();
            return A.ToString() + "/" + B.ToString();
        }

        public static BigRational operator *(BigRational a, BigRational b)
        {
            return new BigRational(a.A * b.A, a.B * b.B);
        }
        public static BigRational operator /(BigRational a, BigRational b)
        {
            return new BigRational(a.A * b.B, a.B * b.A);
        }
        public static BigRational operator +(BigRational a, BigRational b)
        {
            return new BigRational(a.A * b.B + b.A * a.B, a.B * b.B);
        }
        public static BigRational operator -(BigRational a, BigRational b)
        {
            return new BigRational(a.A * b.B - b.A * a.B, a.B * b.B);
        }
        public static BigRational operator -(BigRational a)
        {
            return new BigRational(-a.A, a.B);
        }
        public static bool operator >(BigRational a, BigRational b)
        {
            return (a - b).A.Sign > 0;
        }
        public static bool operator <(BigRational a, BigRational b)
        {
            return (a - b).A.Sign < 0;
        }
        public static bool operator >=(BigRational a, BigRational b)
        {
            return (a - b).A.Sign >= 0;
        }
        public static bool operator <=(BigRational a, BigRational b)
        {
            return (a - b).A.Sign <= 0;
        }

        public static BigRational Parse(string s)
        {
            var parts = s.Split('/');
            if (parts.Length > 2)
                throw new FormatException();
            var a = BigInteger.Parse(parts[0]);
            var b = parts.Length == 1
                ? BigInteger.One
                : BigInteger.Parse(parts[1]);
            return new BigRational(a, b);
        }

        internal static BigRational Epsilon()
        {
            return new BigRational(1, new BigInteger(int.MaxValue));
        }

        internal static BigRational Zero()
        {
            return new BigRational(0, 1);
        }
        internal static BigRational One()
        {
            return new BigRational(1, 1);
        }
        internal static BigRational OneHalf()
        {
            return new BigRational(1, 2);
        }

        public int CompareTo(BigRational other)
        {
            return (this - other).A.Sign;
        }

        static BigInteger? SqrtInt(BigInteger i)
        {
            BigInteger lower = 0;
            BigInteger upper = i;
            while (upper - lower > 0)
            {
                var probe = (lower + upper) / 2;
                var sqr = probe * probe;
                if (sqr == i)
                    return probe;
                if (sqr > i)
                    upper = probe - 1;
                if (sqr < i)
                    lower = probe + 1;
            }
            if (lower * lower != i)
                return null;
            return lower;
        }

        internal BigRational? Sqrt()
        {
            var a = SqrtInt(A);
            var b = SqrtInt(B);
            if (!a.HasValue || !b.HasValue)
                return null;
            return new BigRational(a.Value, b.Value);
        }

        internal float ToFloat()
        {
            int prec = 100000;
            return (float)(prec * this.A / this.B) / prec;
        }
    }

    public struct Point
    {
        public static Point Parse(string s)
        {
            var parts = s.Split(',');
            if (parts.Length != 2)
                throw new FormatException();
            return new Point
                (
                    BigRational.Parse(parts[0]),
                    BigRational.Parse(parts[1])
                );
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }
        public static Point operator +(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }
        public static Point operator /(Point a, BigRational b)
        {
            return new Point(a.X / b, a.Y / b);
        }
        public static Point operator *(Point a, BigRational b)
        {
            return new Point(a.X * b, a.Y * b);
        }
        public static bool operator >(Point a, Point b)
        {
            if (a.X > b.X) return true;
            if (a.X.Equals(b.X) && a.Y > b.Y) return true;
            return false;
        }
        public static bool operator <(Point a, Point b)
        {
            return !(a > b) && !a.Equals(b);
        }

        public static BigRational Cross(Point d1, Point d2)
        {
            return d1.X * d2.Y - d2.X * d1.Y;
        }

        public static int CCW(Point a, Point b, Point c)
        {
            return CCWD(b - a, c - b);
        }

        public static int CCWD(Point d1, Point d2)
        {
            return Cross(d1, d2).A.Sign;
        }

        public static Point LeftOf(Point src, Point dst)
        {
            return LeftOf(src, dst, BigRational.Epsilon());
        }
        public static Point LeftOf(Point src, Point dst, BigRational amount)
        {
            var delta = dst - src;
            delta = new Point(-delta.Y, delta.X) * amount;
            return (src + dst) * BigRational.OneHalf() + delta;
        }
        public static Point RightOf(Point src, Point dst)
        {
            return LeftOf(dst, src);
        }
        public static Point RightOf(Point src, Point dst, BigRational amount)
        {
            return LeftOf(dst, src, amount);
        }
        internal static bool AtBorder(Point p1, Point p2)
        {
            return !RightOf(p1, p2).InBounds();
        }

        internal static bool CloseToBorder(Point p1, Point p2)
        {
            return !RightOf(p1, p2, new BigRational(1, 10)).InBounds();
        }

        private bool InBounds()
        {
            var b0 = BigRational.Zero();
            var b1 = BigRational.One();
            return b0 <= X && X <= b1
                && b0 <= Y && Y <= b1;
        }

        public Point(BigRational x, BigRational y)
        {
            this.X = x;
            this.Y = y;
        }

        public int Complexity()
        {
            return Math.Max(X.Complexity(), Y.Complexity());
        }

        public BigRational X;
        public BigRational Y;

        public override string ToString()
        {
            return X + "," + Y;
        }

        internal BigRational? Length()
        {
            var lenSq = X * X + Y * Y;
            return lenSq.Sqrt();
        }

        internal BigRational Angle()
        {
            BigRational res = BigRational.Zero();
            var x = X;
            var y = Y;
            if (y.A.Sign < 0)
            {
                x *= new BigRational(-1, 1);
                y *= new BigRational(-1, 1);
                res += new BigRational(2, 1);
            }
            if (x.A.Sign < 0)
            {
                var tmp = y;
                y = x * new BigRational(-1, 1);
                x = tmp;
                res += BigRational.One();
            }
            res += y / (x + y);
            return res;
        }

        internal System.Drawing.PointF ToPointF()
        {
            return new System.Drawing.PointF(this.X.ToFloat(), this.Y.ToFloat());
        }

        internal Point Rot90()
        {
            return new Point(-Y, X);
        }
        internal Point Rot180()
        {
            return new Point(-X, -Y);
        }

        static Point halfHalf = new Point(BigRational.OneHalf(), BigRational.OneHalf());
        internal Point Rot90SQ()
        {
            return (this - halfHalf).Rot90() + halfHalf;
        }
        internal Point Rot180SQ()
        {
            return new Point(BigRational.One() - X, BigRational.One() - Y);
        }
    }

    struct PointAngleComparer : IComparer<Point>
    {
        public int Compare(Point x, Point y)
        {
            var up = new Point(BigRational.Zero(), BigRational.One());
            int ox = Point.CCWD(up, x);
            int oy = Point.CCWD(up, y);
            if (ox > oy) return 1;
            if (ox < oy) return -1;
            return Point.CCWD(y, x);
        }
    }

    public class Facet
    {
        public static Facet Parse(StringReader reader)
        {
            var num = int.Parse(reader.ReadLine());
            var res = new Facet();
            for (int i = 0; i < num; ++i)
                res.Outline.Add(Point.Parse(reader.ReadLine()));
            return res;
        }

        public Facet()
        {
            Outline = new List<Point>();
        }

        public int GetOrientation()
        {
            var l = Outline.Concat(Outline.Take(2)).ToArray();
            var os = Enumerable
                .Range(0, Outline.Count)
                .Select(x => Point.CCW(Outline[x], Outline[x + 1], Outline[x + 2]))
                .ToList();
            if (os.All(x => x == 1))
                return 1;
            if (os.All(x => x == -1))
                return -1;
            return 0;
        }

        public List<Point> Outline;
        public IEnumerable<Edge> OutlineEdges()
        {
            return Outline.Select((x, i) => new Edge(x, Outline[(i + 1) % Outline.Count]));
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Outline.Count.ToString());
            foreach (var o in Outline)
                builder.AppendLine(o.ToString());
            return builder.ToString();
        }

        internal bool IsInside(Point p)
        {
            var rightMostCoord = Outline.Max(x => x.X) + BigRational.One();
            var ex = new Edge(p, new Point(rightMostCoord, p.Y + BigRational.Epsilon()));
            var importantEdges = OutlineEdges()
                .Where(e => Edge.Intersects(
                    e,
                    ex)).ToList();
            return importantEdges.Count % 2 == 1;
        }
        internal bool IsOnOutline(Point p)
        {
            return OutlineEdges().Any(x => x.Contains(p) || x.A.Equals(p) || x.B.Equals(p));
        }

        private bool? IsPositive_cache = null;
        public bool IsPositive()
        {
            if (!IsPositive_cache.HasValue)
            {
                var edge = OutlineEdges().First();
                var probe = edge.LeftOf();
                var res = IsInside(probe);
                IsPositive_cache = res;
            }
            return IsPositive_cache.Value;
        }
    }

    public struct Edge
    {
        public static Edge Parse(string s)
        {
            var parts = s.Split();
            if (parts.Length != 2)
                throw new FormatException();
            return new Edge(Point.Parse(parts[0]), Point.Parse(parts[1]));
        }

        public Edge(Point a, Point b)
        {
            A = a;
            B = b;
        }

        public Point A;
        public Point B;

        public BigRational GetLengthSquared()
        {
            var d = B - A;
            return d.X * d.X + d.Y * d.Y;
        }

        public BigRational? GetLength()
        {
            return GetLengthSquared().Sqrt();
        }

        public override string ToString()
        {
            return A + " " + B;
        }

        internal static bool Intersects(Edge e1, Edge e2)
        {
            var e1i = Point.CCWD(e2.B - e2.A, e2.B - e1.B) * Point.CCWD(e2.B - e2.A, e2.B - e1.A);
            var e2i = Point.CCWD(e1.B - e1.A, e1.B - e2.B) * Point.CCWD(e1.B - e1.A, e1.B - e2.A);
            return e1i == -1 && e2i == -1;
        }

        public bool Contains(Point p)
        {
            var d = B - A;
            var dx = p - A;
            var t = d.X.IsZero()
                ? dx.Y / d.Y
                : dx.X / d.X;
            d *= t;
            if (!dx.Equals(d))
                return false;
            return t > BigRational.Zero() && t < BigRational.One();
        }

        internal IEnumerable<Edge> SplitAt(Point p)
        {
            if (Contains(p))
            {
                yield return new Edge(A, p);
                yield return new Edge(p, B);
            }
            else
                yield return this;
        }
        internal IEnumerable<Edge> SplitAt(Edge e)
        {
            if (Edge.Intersects(this, e))
            {
                var D = B - A;
                var eD = e.B - e.A;
                // t * B.X + (1 - t) * A.X = s * e.B.X + (1 - s) * e.A.X
                // t * B.Y + (1 - t) * A.Y = s * e.B.Y + (1 - s) * e.A.Y
                // t * D.X + A.X = s * eD.X + e.A.X
                // t * D.Y + A.Y = s * eD.Y + e.A.Y
                // A.X * D.Y - A.Y * D.X = s * (D.Y * eD.X - D.X * eD.Y) + e.A.X * D.Y - e.A.Y * D.X
                var s = (A.X * D.Y - A.Y * D.X + e.A.Y * D.X - e.A.X * D.Y) / (D.Y * eD.X - D.X * eD.Y);
                // var t = (e.A.X * eD.Y - e.A.Y * eD.X + A.Y * eD.X - A.X * eD.Y) / (eD.Y * D.X - eD.X * D.Y);

                var p = e.B * s + e.A * (BigRational.One() - s);

                yield return new Edge(A, p);
                yield return new Edge(p, B);
            }
            else
                yield return this;
        }
        internal IEnumerable<Edge> SplitAt(List<Point> p)
        {
            var resultSet = new List<Edge> { this };
            foreach (var po in p)
                resultSet = resultSet.SelectMany(e => e.SplitAt(po)).ToList();
            return resultSet;
        }
        internal IEnumerable<Edge> SplitAt(List<Edge> es)
        {
            var resultSet = new List<Edge> { this };
            foreach (var x in es)
                resultSet = resultSet.SelectMany(e => e.SplitAt(x)).ToList();
            return resultSet;
        }

        public Point LeftOf()
        {
            return Point.LeftOf(A, B);
        }
        public Point RightOf()
        {
            return Point.RightOf(A, B);
        }
    }

    public class Problem
    {
        public static Problem Parse(string s)
        {
            var res = new Problem();

            StringReader reader = new StringReader(s);
            int numF = int.Parse(reader.ReadLine());
            for (int i = 0; i < numF; ++i)
                res.Facets.Add(Facet.Parse(reader));
            int numE = int.Parse(reader.ReadLine());

            if (!Constants.TryWithoutEdges)
                for (int i = 0; i < numE; ++i)
                    res.Edges.Add(Edge.Parse(reader.ReadLine()));
            else if (res.Facets.Sum(x => x.Outline.Count) > 4)
                return null;
            else // add synthetic fractions
            {
                if (res.Facets[0].Outline.Count == 4)
                {
                    var width = res.Facets[0].Outline[0].X - res.Facets[0].Outline[2].X;
                    if (width < BigRational.Zero()) width *= new BigRational(-1, 1);
                    var height = res.Facets[0].Outline[0].Y - res.Facets[0].Outline[2].Y;
                    if (height < BigRational.Zero()) height *= new BigRational(-1, 1);
                    var remainderWidth = BigRational.One();
                    while (remainderWidth >= width)
                        remainderWidth -= width;
                    var remainderHeight = BigRational.One();
                    while (remainderHeight >= height)
                        remainderHeight -= height;
                    if (!remainderWidth.IsZero())
                    {
                        var x = res.Facets[0].Outline[0].X < res.Facets[0].Outline[2].X
                            ? res.Facets[0].Outline[0].X + remainderWidth
                            : res.Facets[0].Outline[2].X + remainderWidth;
                        res.Edges.Add(new Edge(
                            new Point(x, res.Facets[0].Outline[0].Y),
                            new Point(x, res.Facets[0].Outline[2].Y)));
                    }
                    if (!remainderHeight.IsZero())
                    {
                        var y = res.Facets[0].Outline[0].Y < res.Facets[0].Outline[2].Y
                            ? res.Facets[0].Outline[0].Y + remainderHeight
                            : res.Facets[0].Outline[2].Y + remainderHeight;
                        res.Edges.Add(new Edge(
                            new Point(res.Facets[0].Outline[0].X, y),
                            new Point(res.Facets[0].Outline[2].X, y)));
                    }
                }
            }

            foreach (var f in res.Facets)
                res.Edges.AddRange(f.OutlineEdges());

            // normalize
            var p = res.Edges.SelectMany(x => new[] { x.A, x.B }).Distinct().ToList();
            //  split at endpoints
            res.Edges = res.Edges
                .SelectMany(e => e.SplitAt(p))
                .ToList();
            //  split at edges
            res.Edges = res.Edges
                .SelectMany(e => e.SplitAt(res.Edges))
                .ToList();
            // order
            res.Edges = res.Edges.Select(e =>
            {
                return e.A < e.B
                    ? e
                    : new Edge(e.B, e.A);
            }).ToList();
            // filter duplicates
            res.Edges = res.Edges.Distinct().ToList();
            return res;
        }

        public Problem()
        {
            Facets = new List<Facet>();
            Edges = new List<Edge>();
        }

        public List<Facet> Facets;
        public List<Edge> Edges;

        public bool IsInside(Point p)
        {
            var inside = Facets.Any(f => f.IsPositive() && f.IsInside(p));
            var outside = Facets.Any(f => !f.IsPositive() && f.IsInside(p));
            return inside && !outside;
        }

        private IEnumerable<Point> Points()
        {
            return Edges.SelectMany(e => new[] { e.A, e.B }).Distinct();
        }

        public void Render()
        {
            Control target = Form1.RenderTarget2;

            var points = Points().ToArray();

            var g = target.CreateGraphics();
            var bounds = target.ClientRectangle;
            var myBounds = RectangleF.FromLTRB(
                points.Min(c => c.X.ToFloat()),
                points.Min(c => c.Y.ToFloat()),
                points.Max(c => c.X.ToFloat()),
                points.Max(c => c.Y.ToFloat())
                );

            float ar = (float)bounds.Width / bounds.Height;
            var mbWidth = myBounds.Height * ar;
            var mbHeight = myBounds.Width / ar;

            if (myBounds.Width < mbWidth)
                myBounds.Inflate((mbWidth - myBounds.Width) / 2, 0);
            if (myBounds.Height < mbHeight)
                myBounds.Inflate(0, (mbHeight - myBounds.Height) / 2);

            Func<Point, PointF> transform = p =>
            {
                var x = p.X.ToFloat() - myBounds.X;
                var y = p.Y.ToFloat() - myBounds.Y;
                x /= myBounds.Width;
                y /= myBounds.Height;
                x *= bounds.Width;
                y *= bounds.Height;
                x += bounds.X;
                y += bounds.Y;

                return new PointF(x, y);
            };


            g.Clear(Color.White);
            // Facets
            // pos
            foreach (var f in Facets.Where(f => f.IsPositive()))
                g.FillPolygon(Brushes.CornflowerBlue, f.Outline.Select(transform).ToArray());
            // neg
            foreach (var f in Facets.Where(f => !f.IsPositive()))
                g.FillPolygon(Brushes.LightGray, f.Outline.Select(transform).ToArray());

            // Edges
            foreach (var e in Edges)
                g.DrawLine(Pens.Black, transform(e.A), transform(e.B));
        }

        public Graph CreateGraph()
        {
            var edgesx = Edges.Concat(Edges.Select(ed => new Edge(ed.B, ed.A))).ToList();
            var points = edgesx.Select(x => x.A).Distinct().ToList();

            Graph res = new Graph();
            res.Nodes = points.Select(x => new Tuple<Point, List<GraphEdge>>(x, new List<GraphEdge>())).ToList();
            foreach (var ed in edgesx)
                res.Nodes[points.IndexOf(ed.A)].Item2.Add(new GraphEdge
                {
                    FaceLeft = -1,
                    FaceRight = -1,
                    TargetNode = points.IndexOf(ed.B),
                    Length = ed.GetLength()
                });
            res.Nodes = res.Nodes
                .Select(x => new Tuple<Point, List<GraphEdge>>(
                    x.Item1, 
                    x.Item2.OrderBy(e => (points[e.TargetNode] - x.Item1).Angle()).ToList()))
                .ToList();

            res.InitFaces(IsInside);

            return res;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Facets.Count.ToString());
            foreach (var f in Facets)
                builder.Append(f.ToString());
            builder.AppendLine(Edges.Count.ToString());
            foreach (var e in Edges)
                builder.AppendLine(e.ToString());
            return builder.ToString();
        }
    }
}
