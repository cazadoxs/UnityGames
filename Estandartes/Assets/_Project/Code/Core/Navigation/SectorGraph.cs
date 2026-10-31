using System;
using System.Collections.Generic;

namespace Estandartes.Core.Navigation
{
    /// <summary>
    /// Nivel jerárquico de la navegación (Vol. III §15.2): sectores de N×N celdas unidos por portales.
    /// A* sobre los portales da el corredor de sectores; el flow field se calcula solo en ese corredor.
    /// </summary>
    public sealed class SectorGraph
    {
        public readonly CostGrid Grid;
        public readonly int SectorSize;
        public readonly int SectorsX, SectorsY;

        /// <summary>Un portal une una celda de un sector con la celda vecina de otro.</summary>
        public struct Portal
        {
            public int CellA, CellB;
            public int SectorA, SectorB;
        }

        public readonly List<Portal> Portals = new List<Portal>();
        // Aristas entre nodos (nodo = extremo de portal: 2·i = lado A, 2·i+1 = lado B)
        private readonly List<List<(int to, float cost)>> _edges = new List<List<(int, float)>>();
        private readonly Dictionary<int, List<int>> _nodesBySector = new Dictionary<int, List<int>>();

        public SectorGraph(CostGrid grid, int sectorSize = 64)
        {
            Grid = grid;
            SectorSize = sectorSize;
            SectorsX = (grid.Width + sectorSize - 1) / sectorSize;
            SectorsY = (grid.Height + sectorSize - 1) / sectorSize;
            Rebuild();
        }

        public int SectorOf(int cell) => (cell / Grid.Width) / SectorSize * SectorsX + (cell % Grid.Width) / SectorSize;

        public void Rebuild()
        {
            Portals.Clear();
            _edges.Clear();
            _nodesBySector.Clear();
            // Fronteras verticales (entre sectores en X) y horizontales (en Y)
            for (int sy = 0; sy < SectorsY; sy++)
                for (int sx = 0; sx < SectorsX; sx++)
                {
                    if (sx + 1 < SectorsX) ScanBorder(sx, sy, vertical: true);
                    if (sy + 1 < SectorsY) ScanBorder(sx, sy, vertical: false);
                }
            for (int i = 0; i < Portals.Count * 2; i++) _edges.Add(new List<(int, float)>());
            for (int i = 0; i < Portals.Count; i++)
            {
                var p = Portals[i];
                Link(2 * i, 2 * i + 1, Grid.Costs[p.CellB]);
                Link(2 * i + 1, 2 * i, Grid.Costs[p.CellA]);
                AddNode(p.SectorA, 2 * i);
                AddNode(p.SectorB, 2 * i + 1);
            }
            // Aristas internas: Dijkstra dentro de cada sector entre sus nodos
            foreach (var kv in _nodesBySector)
            {
                var nodes = kv.Value;
                foreach (int a in nodes)
                {
                    var dist = SectorDistances(kv.Key, NodeCell(a));
                    foreach (int b in nodes)
                    {
                        if (a == b) continue;
                        float d = dist[NodeCell(b)];
                        if (!float.IsPositiveInfinity(d)) Link(a, b, d);
                    }
                }
            }
        }

        private void AddNode(int sector, int node)
        {
            if (!_nodesBySector.TryGetValue(sector, out var list)) _nodesBySector[sector] = list = new List<int>();
            list.Add(node);
        }

        private void Link(int a, int b, float cost) => _edges[a].Add((b, cost));

        private int NodeCell(int node) => (node & 1) == 0 ? Portals[node / 2].CellA : Portals[node / 2].CellB;

        private void ScanBorder(int sx, int sy, bool vertical)
        {
            int length = SectorSize;
            int runStart = -1;
            for (int k = 0; k <= length; k++)
            {
                bool open = false;
                int ax = 0, ay = 0, bx = 0, by = 0;
                if (k < length)
                {
                    if (vertical) { ax = sx * SectorSize + SectorSize - 1; ay = sy * SectorSize + k; bx = ax + 1; by = ay; }
                    else { ax = sx * SectorSize + k; ay = sy * SectorSize + SectorSize - 1; bx = ax; by = ay + 1; }
                    open = Grid.Walkable(ax, ay) && Grid.Walkable(bx, by);
                }
                if (open && runStart < 0) runStart = k;
                if (!open && runStart >= 0)
                {
                    int mid = (runStart + k - 1) / 2;
                    int max, may, mbx, mby;
                    if (vertical) { max = sx * SectorSize + SectorSize - 1; may = sy * SectorSize + mid; mbx = max + 1; mby = may; }
                    else { max = sx * SectorSize + mid; may = sy * SectorSize + SectorSize - 1; mbx = max; mby = may + 1; }
                    int ca = Grid.Index(max, may), cb = Grid.Index(mbx, mby);
                    Portals.Add(new Portal { CellA = ca, CellB = cb, SectorA = SectorOf(ca), SectorB = SectorOf(cb) });
                    runStart = -1;
                }
            }
        }

        /// <summary>Distancias desde una celda limitadas a su sector (Dijkstra).</summary>
        private DefaultDict SectorDistances(int sector, int fromCell)
        {
            var dist = new Dictionary<int, float>();
            var heap = new MinHeap();
            dist[fromCell] = 0f;
            heap.Push(0f, fromCell);
            while (heap.Count > 0)
            {
                var (d, idx) = heap.Pop();
                if (d > dist[idx]) continue;
                int x = idx % Grid.Width, y = idx / Grid.Width;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + CostGrid.DX[k], ny = y + CostGrid.DY[k];
                    if (!Grid.Walkable(nx, ny)) continue;
                    if ((k & 1) == 1 && (!Grid.Walkable(nx, y) || !Grid.Walkable(x, ny))) continue;
                    int n = Grid.Index(nx, ny);
                    if (SectorOf(n) != sector) continue;
                    float nd = d + Grid.Costs[n] * ((k & 1) == 1 ? SimMath.Sqrt2 : 1f);
                    if (!dist.TryGetValue(n, out float old) || nd < old) { dist[n] = nd; heap.Push(nd, n); }
                }
            }
            return new DefaultDict(dist);
        }

        private sealed class DefaultDict : Dictionary<int, float>
        {
            public DefaultDict(Dictionary<int, float> src) : base(src) { }
            public new float this[int key] => TryGetValue(key, out float v) ? v : float.PositiveInfinity;
        }

        /// <summary>Corredor de sectores de <paramref name="startCell"/> a <paramref name="goalCell"/> (null si no hay camino).</summary>
        public List<int> FindCorridor(int startCell, int goalCell)
        {
            int startSector = SectorOf(startCell), goalSector = SectorOf(goalCell);
            var corridor = new List<int>();
            if (startSector == goalSector)
            {
                var d = SectorDistances(startSector, startCell);
                if (!float.IsPositiveInfinity(d[goalCell])) { corridor.Add(startSector); return corridor; }
            }
            // Nodos virtuales: N = inicio, N+1 = destino
            int n = Portals.Count * 2, start = n, goal = n + 1;
            var extra = new Dictionary<int, List<(int, float)>>();
            var fromStart = SectorDistances(startSector, startCell);
            var startEdges = new List<(int, float)>();
            if (_nodesBySector.TryGetValue(startSector, out var sNodes))
                foreach (int node in sNodes) { float c = fromStart[NodeCell(node)]; if (!float.IsPositiveInfinity(c)) startEdges.Add((node, c)); }
            var toGoal = SectorDistances(goalSector, goalCell);
            var goalLinks = new Dictionary<int, float>();
            if (_nodesBySector.TryGetValue(goalSector, out var gNodes))
                foreach (int node in gNodes) { float c = toGoal[NodeCell(node)]; if (!float.IsPositiveInfinity(c)) goalLinks[node] = c; }

            var gScore = new Dictionary<int, float> { [start] = 0f };
            var came = new Dictionary<int, int>();
            var heap = new MinHeap();
            heap.Push(0f, start);
            Vec2 goalPos = CellPos(goalCell);
            while (heap.Count > 0)
            {
                var (_, cur) = heap.Pop();
                if (cur == goal) break;
                IEnumerable<(int to, float cost)> neighbors = cur == start ? startEdges : (IEnumerable<(int, float)>)_edges[cur];
                var list = new List<(int to, float cost)>(neighbors);
                if (cur < n && goalLinks.TryGetValue(cur, out float gc)) list.Add((goal, gc));
                foreach (var (to, cost) in list)
                {
                    float ng = gScore[cur] + cost;
                    if (gScore.TryGetValue(to, out float old) && ng >= old) continue;
                    gScore[to] = ng;
                    came[to] = cur;
                    Vec2 p = to == goal ? goalPos : CellPos(NodeCell(to));
                    heap.Push(ng + Vec2.Distance(p, goalPos), to);
                }
            }
            if (!came.ContainsKey(goal)) return null;
            var nodes = new List<int>();
            for (int c = goal; c != start; c = came[c]) nodes.Add(c);
            nodes.Reverse();
            corridor.Add(startSector);
            foreach (int node in nodes)
            {
                int sector = node == goal ? goalSector : SectorOf(NodeCell(node));
                if (corridor[corridor.Count - 1] != sector) corridor.Add(sector);
            }
            return corridor;
        }

        /// <summary>Máscara de celdas de los sectores del corredor, para <see cref="FlowField.Compute"/>.</summary>
        public bool[] CorridorMask(IReadOnlyList<int> sectors)
        {
            var mask = new bool[Grid.Width * Grid.Height];
            var set = new HashSet<int>(sectors);
            for (int i = 0; i < mask.Length; i++) mask[i] = set.Contains(SectorOf(i));
            return mask;
        }

        private Vec2 CellPos(int cell) => new Vec2(cell % Grid.Width, cell / Grid.Width);
    }
}
