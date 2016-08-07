using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ICFP16
{
    public class GraphEdge
    {
        public int TargetNode;
        public int FaceRight;
        public int FaceLeft;
        public BigRational? Length;
    }

    public class Graph
    {
        public List<Tuple<Point, List<GraphEdge>>> Nodes;
        public Dictionary<int, Tuple<BigRational, List<int>>> Faces;

        public Dictionary<int, int> map90 = new Dictionary<int, int>();
        public Dictionary<int, int> map180 = new Dictionary<int, int>();
        bool? Rotation90Symmetric_cache = null;
        bool? Rotation180Symmetric_cache = null;
        private void RotationSymmetric_Calc()
        {
            var minX = Nodes.Min(x => x.Item1.X);
            var minY = Nodes.Min(x => x.Item1.Y);
            var maxX = Nodes.Max(x => x.Item1.X);
            var maxY = Nodes.Max(x => x.Item1.Y);
            var midX = (minX + maxX) * BigRational.OneHalf();
            var midY = (minY + maxY) * BigRational.OneHalf();

            var nodes = Nodes.Select(x => new Tuple<Point, List<GraphEdge>>(
                new Point(x.Item1.X - midX, x.Item1.Y - midY),
                x.Item2
                )).ToList();

            bool rot90 = true;
            bool rot180 = true;

            // CHECK
            for (int i = 0; i < nodes.Count; ++i)
            {
                var n = nodes[i];

                var np90 = n.Item1.Rot90();
                var np180 = n.Item1.Rot180();
                if (rot90)
                {
                    var in90 = nodes.FindIndex(x => x.Item1.Equals(np90));
                    if (in90 == -1)
                        rot90 = false;
                    else
                    {
                        map90[i] = in90;
                        var n90 = nodes[in90];
                        if (n90 == null || n.Item2.Count != n90.Item2.Count)
                            rot90 = false;
                        else
                        {
                            var ep1 = n.Item2.Select(x => nodes[x.TargetNode].Item1.Rot90()).ToList();
                            var ep2 = n90.Item2.Select(x => nodes[x.TargetNode].Item1).ToList();
                            rot90 &= ep1.All(ep => ep2.Contains(ep));
                        }
                    }
                }
                if (rot180)
                {
                    var in180 = nodes.FindIndex(x => x.Item1.Equals(np180));
                    if (in180 == -1)
                        rot180 = false;
                    else
                    {
                        map180[i] = in180;
                        var n180 = nodes[in180];
                        if (n180 == null || n.Item2.Count != n180.Item2.Count)
                            rot180 = false;
                        else
                        {
                            var ep1 = n.Item2.Select(x => nodes[x.TargetNode].Item1.Rot180()).ToList();
                            var ep2 = n180.Item2.Select(x => nodes[x.TargetNode].Item1).ToList();
                            rot180 &= ep1.All(ep => ep2.Contains(ep));
                        }
                    }
                }
            }

            Rotation90Symmetric_cache = rot90;
            Rotation180Symmetric_cache = rot180;
        }
        public bool RotationSymmetric90()
        {
            if (!Rotation90Symmetric_cache.HasValue)
                RotationSymmetric_Calc();
            return Rotation90Symmetric_cache.Value;
        }
        public bool RotationSymmetric180()
        {
            if (!Rotation180Symmetric_cache.HasValue)
                RotationSymmetric_Calc();
            return Rotation180Symmetric_cache.Value;
        }

        public int ComingFromIndex(int from, int to)
        {
            return Nodes[to].Item2.FindIndex(x => x.TargetNode == from);
        }
        public int ComingFromIndexCW(int from, int to)
        {
            var node = Nodes[to].Item2;
            var index = ComingFromIndex(from, to) + 1;
            return index % node.Count;
        }
        public int ComingFromIndexCWW(int from, int to)
        {
            var node = Nodes[to].Item2;
            var index = ComingFromIndex(from, to) + node.Count - 1;
            return index % node.Count;
        }

        private IEnumerable<List<int>> EnumDistanceXPaths(int current, BigRational len, Stack<int> state)
        {
            state.Push(current);
            if (len.A.Sign >= 0)
            {
                if (len.IsZero())
                    yield return state.ToList();
                else
                {
                    var currentNode = Nodes[current];
                    var currentPos = currentNode.Item1;
                    foreach (var edge in currentNode.Item2
                        .Where(x => x.Length.HasValue)
                        .OrderByDescending(x => x.Length.Value))
                    {
                        //if (edge.FaceLeft == 0)
                        //    continue;
                        var next = edge.TargetNode;
                        BigRational? dist = edge.Length;
                        foreach (var xx in EnumDistanceXPaths(next, len - dist.Value, state))
                            yield return xx;
                    }
                }
            }

            state.Pop();
        }
        public IEnumerable<List<int>> EnumDistance1Paths(int from)
        {
            return EnumDistanceXPaths(from, BigRational.One(), new Stack<int>()).Where(x => x.First() >= from);
        }
        public IEnumerable<TResult> scatter<TResult>(IEnumerable<IEnumerable<TResult>> es)
        {
            var enumerators = new Queue<IEnumerator<TResult>>(es.Select(x => x.GetEnumerator()));
            while (enumerators.Count > 0)
            {
                var e = enumerators.Dequeue();
                if (e.MoveNext())
                {
                    enumerators.Enqueue(e);
                    yield return e.Current;
                }
            }
        }
        public IEnumerable<List<int>> EnumDistance1Paths()
        {
            return scatter(Enumerable.Range(0, Nodes.Count).Select(x => EnumDistance1Paths(x)));
        }
        private IEnumerable<Dictionary<int, int>> EnumFaceTheoriesForArea(IEnumerable<Tuple<int, BigRational>> faces, BigRational area, int maxCompl)
        {
            if (area.A.IsZero)
                yield return new Dictionary<int, int>();
            if (area.A.Sign <= 0 || !faces.Any() || area.Complexity() > maxCompl)
                yield break;
            var face = faces.First();
            faces = faces.Skip(1);
            var max = area / face.Item2;
            var maxxI = Math.Min((int)(max.A / max.B), Constants.MaxFacetsPerTypeCount);
            var m = 8;
            for (var maxI = maxxI / m * m; maxI >= 0; maxI -= m)
            {
                var res = EnumFaceTheoriesForArea(faces, area - new BigRational(maxI, 1) * face.Item2, maxCompl);
                foreach (var d in res)
                {
                    d[face.Item1] = maxI;
                    yield return d;
                }
            }
        }
        public IEnumerable<Dictionary<int, int>> EnumFaceTheories()
        {
            var faces = Faces
                .Select(x => new Tuple<int, BigRational>(x.Key, x.Value.Item1))
                .OrderByDescending(x => x.Item2)
                .ToList();
            var area = BigRational.One();
            foreach (var f in faces) area -= f.Item2;

            var maxCompl = faces.Max(x => x.Item2.Complexity());
            Console.WriteLine("COMPL: " + maxCompl);
            foreach (var d in EnumFaceTheoriesForArea(faces, area, maxCompl + 1))
            {
                foreach (var f in faces.Select(f => f.Item1))
                    d[f] = (d.ContainsKey(f) ? d[f] : 0) + 1;
                yield return d;
            }
        }

        public IEnumerable<World> FullSearch()
        {
            Console.WriteLine("FULL SEARCH: |F|=" + this.Faces.Count + " |V|=" + this.Nodes.Count);
            Console.WriteLine("MaxComplexity = " + MaxComplexity);
            Console.WriteLine(string.Join("\n", this.Nodes.Select(x => "\t" + x.Item1)));
            
            var edges = Enumerable.Range(0, this.Nodes.Count)
                .SelectMany(i => this.Nodes[i].Item2.Select(e => new { l = e.Length, a = i, b = e.TargetNode }))
                .Where(e => e.l.HasValue)
                .OrderByDescending(e => FacesAt(e.a, e.b).Average(x => Faces[x].Item1.ToFloat()))
                //.OrderByDescending(e => e.l)
                .ToList();

            var sw = Stopwatch.StartNew();

            for(var i = 0; i < edges.Count; ++i)
            {
                Console.Title = i + " / " + edges.Count;

                var e = edges[i];
                World w = new World(new List<int> { e.a, e.b }, this);
                foreach (var sol in w.Solutions())
                    yield return sol;

                if (sw.ElapsedMilliseconds > Constants.MaxTimePerGraphMS)
                {
                    Console.WriteLine("TIMELIMIT EXCEEDED");
                    break;
                }
            }
        }

        void AssignFaces(int faceID, int start, int nextIndex)
        {
            if (Nodes[start].Item2[nextIndex].FaceLeft != -1)
                return;
            if (faceID != 0)
                Faces[faceID] = new Tuple<BigRational, List<int>>(BigRational.Zero(), new List<int>());

            int current = start;
            do
            {
                if (faceID != 0)
                    Faces[faceID].Item2.Add(current);

                Nodes[current].Item2[nextIndex].FaceLeft = faceID;
                var next = Nodes[current].Item2[nextIndex].TargetNode;
                Nodes[next].Item2[ComingFromIndex(current, next)].FaceRight = faceID;
                nextIndex = ComingFromIndexCWW(current, next);
                current = next;
            } while (current != start);
        }

        public int MaxComplexity;
        internal void InitFaces(Func<Point, bool> isInside)
        {
            Faces = new Dictionary<int, Tuple<BigRational, List<int>>>();
            int faceID = 0;
            for (int startIndex = 0; startIndex < Nodes.Count; ++startIndex)
                for (var nextIndex = 0; nextIndex < Nodes[startIndex].Item2.Count; ++nextIndex)
                {
                    var src = Nodes[startIndex].Item1;
                    var dst = Nodes[Nodes[startIndex].Item2[nextIndex].TargetNode].Item1;
                    var probe = Point.LeftOf(src, dst);
                    AssignFaces(isInside(probe) ? (++faceID) : 0, startIndex, nextIndex);
                }

            // areas
            foreach (var id in Faces.Keys.ToList())
            {
                var Outline = Faces[id].Item2;
                var Area = BigRational.Zero();
                for (int i = 1; i < Outline.Count - 1; ++i)
                {
                    var A = Nodes[Outline[0]].Item1;
                    var B = Nodes[Outline[i]].Item1;
                    var C = Nodes[Outline[i + 1]].Item1;
                    var AreaPart = Point.Cross(B - A, C - A) * BigRational.OneHalf();
                    Area += AreaPart;
                }
                Faces[id] = new Tuple<BigRational, List<int>>(Area, Outline);
            }

            // MaxComplexity
            MaxComplexity = this.Nodes.Max(x => x.Item1.Complexity());
        }

        public IEnumerable<int> FacesAt(int v1, int v2)
        {
            var node = Nodes[v2];
            var nextIndex = ComingFromIndex(v1, v2);
            var edge = node.Item2[nextIndex];
            if (edge.FaceLeft != 0)
                yield return edge.FaceLeft;
            if (edge.FaceRight != 0)
                yield return edge.FaceRight;
        }

    }
}
