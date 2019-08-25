using System;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField]
    private Transform tilesContainer = default;

    // ---- INTERN ----
    private Dictionary<Point, Tile> tilesDictionary = new Dictionary<Point, Tile>();
    private Dictionary<Point, Tile> emptyTileDictionary = new Dictionary<Point, Tile>();

    private void Awake()
    {
        BuildDictionary();
    }

    public Tile GetTileAtPos(Point pos)
    {
        return tilesDictionary.ContainsKey(pos) ? tilesDictionary[pos] : null;
    }

    private void BuildDictionary()
    {
        tilesDictionary.Clear();
        foreach (Transform tileChild in tilesContainer)
        {
            Tile t = tileChild.gameObject.GetComponent<Tile>();
            if (t != null)
            {
                tilesDictionary.Add(t.pos, t);
                if (t.content == null)
                {
                    emptyTileDictionary.Add(t.pos, t);
                }
            }
        }
    }


    /**
     * ------------------------------------
     * ----------- PATH FINDING -----------
     * ------------------------------------ 
     */

    public bool AreTilesReashableFrom(List<Tile> listTileToReach, Tile startPosTile)
    {
        LaunchWave(startPosTile);

        bool res = true;
        int i = 0;
        while (res && i < listTileToReach.Count)
        {
            if (!listTileToReach[i].isWet)
                res = false;
            ++i;
        }

        DryAll();

        return res;
    }

    public Stack<Tile> GetShortestPath(Tile from, Tile to)
    {
        Search(from, ExpandSearchWalk);

        Stack<Tile> path = new Stack<Tile>();
        Tile next = to;
        while (next != null)
        {
            path.Push(next);
            next = next.prev;
        }

        return path;
    }


    private List<Tile> Search(Tile start, Func<Tile, Tile, bool> addTile)
    {
        List<Tile> retValue = new List<Tile>();
        retValue.Add(start);

        ClearSearch();
        //We need two lists, one for the tiles we need to check and one for the next tiles
        Queue<Tile> checkNext = new Queue<Tile>();
        Queue<Tile> checkNow = new Queue<Tile>();

        start.distance = 0;
        checkNow.Enqueue(start);

        while (checkNow.Count > 0)
        {
            Tile tile = checkNow.Dequeue();
            for (int i = 0; i < 6; ++i)
            {
                Tile next = GetNeighbour(tile, (Neighbour)i);

                if (next != null && next.distance > tile.distance + 1) // exists or we already pass it with bigger distance
                {
                    if (addTile(tile, next))
                    {
                        next.distance = tile.distance + 1;
                        next.prev = tile;

                        checkNext.Enqueue(next);
                        retValue.Add(next);
                    }
                }
            }

            if (checkNow.Count == 0)
                SwapReference(ref checkNow, ref checkNext);
        }

        return retValue;
    }

    private void LaunchWave(Tile from)
    {
        DryAll();
        Queue<Tile> queue = new Queue<Tile>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            Tile currentTile = queue.Dequeue();
            foreach (Neighbour n in (Neighbour[])Enum.GetValues(typeof(Neighbour)))
            {
                Tile neighbourTile = GetNeighbour(currentTile, n);
                if (neighbourTile != null && !neighbourTile.isWet &&
                    (neighbourTile.content == null))
                {
                    neighbourTile.isWet = true;
                    queue.Enqueue(neighbourTile);
                }
            }
        }
    }

    private void DryAll()
    {
        foreach (Tile t in tilesDictionary.Values)
        {
            t.isWet = false;
        }
    }

    private void UnvisiteAll()
    {
        foreach (Tile t in tilesDictionary.Values)
        {
            t.isVisited = false;
        }
    }

    private void SwapReference(ref Queue<Tile> a, ref Queue<Tile> b)
    {
        Queue<Tile> temp = a;
        a = b;
        b = temp;
    }

    private void ClearSearch()
    {
        foreach (Tile tile in tilesDictionary.Values)
        {
            tile.Reset();
        }
    }

    private bool ExpandSearchWalk(Tile from, Tile to)
    {
        // Skip if the tile is occuped by something
        return (to.content == null);
    }

    private Tile GetNeighbour(Tile tile, Neighbour neighbour)
    {
        Tile res = null;
        Point pos = tile.pos;
        Point newPos = new Point();
        switch (neighbour)
        {
            case Neighbour.TopRight:
                newPos = pos + new Point(0, 1);
                break;

            case Neighbour.Right:
                newPos = pos + new Point(1, 0);
                break;

            case Neighbour.BottomRight:
                newPos = pos + new Point(1, -1);
                break;

            case Neighbour.BottomLeft:
                newPos = pos + new Point(0, -1);
                break;

            case Neighbour.Left:
                newPos = pos + new Point(-1, 0);
                break;

            case Neighbour.TopLeft:
                newPos = pos + new Point(-1, 1);
                break;
        }

        if (tilesDictionary.ContainsKey(newPos))
            res = tilesDictionary[newPos];

        return res;
    }

    private enum Neighbour
    {
        TopRight,
        Right,
        BottomRight,
        BottomLeft,
        Left,
        TopLeft,
    }
}
