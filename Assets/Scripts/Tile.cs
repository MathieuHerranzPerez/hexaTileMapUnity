using UnityEditor;
using UnityEngine;

// TODO change public to private with Getters

[RequireComponent(typeof(Renderer))]
public class Tile : MonoBehaviour
{
    public Point pos;       // the logic position     
    public Vector3 center { get { return new Vector3(transform.position.x, transform.position.y, transform.position.z); } } // physicPos
    public GameObject content;

    public Tile prev;       // for the pathfinding, to get the previous tile
    public int distance;    // the number of tiles which have been crossed to reach here

    public Renderer rend;

    public bool isWet = false;
    public bool isVisited = false;

    private Material material;

    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    /**
     * Return true if the parameter tile can be LOGICALY considered as a neighbor
     */
    public bool isNeighbor(Tile otherTile)
    {
        int tmpZDiff = pos.z - otherTile.pos.z;
        int tmpXDiff = pos.x - otherTile.pos.x;

        return (otherTile != this && (tmpXDiff <= 1 && tmpXDiff >= -1) && (tmpZDiff <= 1 && tmpZDiff >= -1));
    }

    public void Reset()
    {
        prev = null;
        distance = int.MaxValue;
    }

    public void ChangeMaterial(Material newMaterial)
    {
        GetComponent<Renderer>().sharedMaterial = newMaterial;
    }
}
