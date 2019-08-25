using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapBuilder : EditorWindow
{
    [SerializeField]
    private List<GameObject> listPrefabTileContent = new List<GameObject>(1);
    private List<Editor> listMeshEditor = new List<Editor>(1);

    // ---- INTERN ----
    private Event currentEvent;
    private float hexagoneSize = 2;
    private int width = 50;
    private int height = 50;
    private int selectedLayer;
    private GameObject tilePrefab;
    private float heightRef;

    private Vector2 scrollPosition = Vector2.zero;

    private Transform mapContainer;
    private Transform whereToPutTheMapTransform { get { return mapContainer; }
        set 
        {
            if (value != null)
            {
                mapContainer = value;
                if (mapContainer.childCount > 0)        // if there is already a map in the Transform, get it
                {
                    tileDictionary.Clear();
                    foreach (Transform t in mapContainer)
                    {
                        Tile currentTile = t.GetComponent<Tile>();
                        if (currentTile != null)
                        {
                            tileDictionary.Add(currentTile.pos, currentTile);
                        }
                    }
                }
            }
        }
    }

    private Vector3 currentMousePosition = Vector3.zero;
    private Vector3 previousMousePosition = Vector3.zero;

    private GameObject objectToBuild;
    private bool isHeightBrushActive = false;
    private bool isBuilding = false;
    private bool isPainting = false;
    private bool isRemoving = false;

    private float brushSize = 5f;
    private float brushHardness = 1f;

    // material
    private Material material;

    // ---- map builder ----
    private float xOffset = 0.866f;
    private float zOffset = 0.75f;
    [HideInInspector]
    [SerializeField]
    private TileDictionary pointToTileStorage = TileDictionary.New<TileDictionary>();
    private Dictionary<Point, Tile> tileDictionary
    {
        get { return pointToTileStorage.dictionary; }
    }


    [MenuItem("Window/HexaTileMapBuilder")]
    public static void ShowWindow()
    {
        GetWindow<MapBuilder>();
    }


    void OnGUI()
    {
        wantsMouseMove = true;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height));

        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 20;
        GUIStyle subTitleStyle = new GUIStyle();
        subTitleStyle.fontSize = 18;

        selectedLayer = EditorGUILayout.LayerField("Tile layer", selectedLayer);

        // ---- Creation ----

        EditorGUILayout.LabelField("Map creator", titleStyle, null);
        EditorGUILayout.Space();

        hexagoneSize = EditorGUILayout.FloatField("Hexagone size", hexagoneSize);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        tilePrefab = (GameObject)EditorGUILayout.ObjectField("Tile Prefab", tilePrefab, typeof(GameObject), false);
        whereToPutTheMapTransform = (Transform)EditorGUILayout.ObjectField("Map container in scene", whereToPutTheMapTransform, typeof(Transform), true);
        if (GUILayout.Button("Build map"))
        {
            BuildMap();
        }
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Reset map"))
        {
            ResetMap();
        }

        GUI.backgroundColor = Color.white;

        // ---- Building ----
        DrawUILine(Color.gray, 4, 20);

        EditorGUILayout.LabelField("Map editor", titleStyle, null);
        EditorGUILayout.Space();
        DrawUILine(Color.gray, 0, 20);
        EditorGUILayout.LabelField("Build", subTitleStyle, null);

        EditorGUILayout.LabelField("List Tile content prefabs");
        int newSize = Mathf.Max(0, EditorGUILayout.IntField("size", listPrefabTileContent.Count));
        
        while (newSize < listPrefabTileContent.Count)
        {
            int removeAt = listPrefabTileContent.Count - 1;
            listPrefabTileContent.RemoveAt(removeAt);
            listMeshEditor.RemoveAt(removeAt);
        }
        while (newSize > listPrefabTileContent.Count)
        {
            listPrefabTileContent.Add(null);
            listMeshEditor.Add(null);
        }

        for (int i = 0; i < listPrefabTileContent.Count; i++)
        {
            listPrefabTileContent[i] = (GameObject)EditorGUILayout.ObjectField("", listPrefabTileContent[i], typeof(GameObject), false);

            GUIStyle bgColor = new GUIStyle();
            bgColor.normal.background = EditorGUIUtility.whiteTexture;
            if (listPrefabTileContent[i] != null)
            {
                if (listMeshEditor[i] == null)
                    listMeshEditor[i] = Editor.CreateEditor(listPrefabTileContent[i]);

                listMeshEditor[i].OnInteractivePreviewGUI(GUILayoutUtility.GetRect(75, 75), bgColor);
                if (GUILayout.Button("select", GUILayout.Width(75)))
                {
                    StopEditing();
                    objectToBuild = listPrefabTileContent[i];
                    isBuilding = true;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Remove tool"))
        {
            StopEditing();
            isRemoving = true;
        }

        EditorGUILayout.Space();

        // ---- Height ----
        DrawUILine(Color.gray, 0, 20);
        EditorGUILayout.LabelField("Height", subTitleStyle, null);

        brushSize = EditorGUILayout.Slider("Brush size", brushSize, 0.2f, 100f);
        brushHardness = EditorGUILayout.Slider("Brush hardness", brushHardness, 0.01f, 3f);

        if (GUILayout.Button("Modify height"))
        {
            StopEditing();
            isHeightBrushActive = true;
        }

        DrawUILine(Color.gray, 0, 20);
        EditorGUILayout.LabelField("Material", subTitleStyle, null);

        material = (Material) EditorGUILayout.ObjectField("Material", material, typeof(Material), false);

        if (material)
        {
            if (GUILayout.Button("Paint material"))
            {
                StopEditing();
                isPainting = true;
            }
        }

        DrawUILine(Color.grey, 2, 20);

        if (isPainting || isBuilding || isHeightBrushActive || isRemoving)
        {
            if (GUILayout.Button("Stop editing (Escape)"))
            {
                StopEditing();
            }
        }

        //GUILayout.EndArea();
        GUILayout.EndScrollView();
    }

    void SceneGUI(SceneView sceneView)
    {
        currentEvent = Event.current;
        //UpdateMousePos(sceneView);
        SceneInput(sceneView);
    }

    private void SceneInput(SceneView sceneView)
    {
        if(currentEvent.type == EventType.KeyDown)
        {
            if(Event.current.keyCode == (KeyCode.Escape))
                StopEditing();
        }

        if (isPainting || isBuilding || isHeightBrushActive || isRemoving)
        {
            // Retrieve the control Id
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            // Start treating your events
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (isHeightBrushActive)
                    {
                        bool touchedBrush = false;
                        RaycastHit hitBrush = getRayCast(sceneView, out touchedBrush);
                        if (touchedBrush)
                        {
                            heightRef = hitBrush.collider.transform.position.y;
                        }
                    }
                    // Tell the UI the event is the main one to use, it override the selection in  the scene view
                    GUIUtility.hotControl = controlId;
                    // use the event
                    Event.current.Use();
                    goto case EventType.MouseDrag;

                case EventType.MouseDrag:
                    bool touched = false;
                    RaycastHit hit = getRayCast(sceneView, out touched);
                    if (touched)
                    {
                        if (isPainting)
                        {
                            PaintTile(hit.collider);
                        }
                        else if (isBuilding)
                        {
                            TryToPlaceTileContent(hit.collider);
                        }
                        else if (isHeightBrushActive)
                        {
                            if (Event.current.shift)
                            {
                                LowerTiles(hit.collider);
                            }
                            else if(Event.current.control)
                            {
                                FlattenTiles(hit.collider, heightRef);
                            }
                            else
                            {
                                RaiseTiles(hit.collider);
                            }
                        }
                        else if (isRemoving)
                        {
                            RemoveTileContent(hit.collider);
                        }
                    }
                    // Tell the UI the event is the main one to use, it override the selection in  the scene view
                    GUIUtility.hotControl = controlId;
                    // use the event
                    Event.current.Use();
                    break;
            }
        }
    }

    private RaycastHit getRayCast(SceneView sceneView, out bool touched)
    {
        Vector3 mousePos = currentEvent.mousePosition;
        float ppp = EditorGUIUtility.pixelsPerPoint;
        mousePos.y = sceneView.camera.pixelHeight - mousePos.y * ppp;
        mousePos.x *= ppp;

        Ray ray = sceneView.camera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        touched = Physics.Raycast(ray, out hit, 1000, 1 << selectedLayer);
        if (touched)
        {
            currentMousePosition = hit.point;
        }

        return hit;
    }


    private void StopEditing()
    {
        isBuilding = false;
        isPainting = false;
        isHeightBrushActive = false;
        isRemoving = false;
    }

    private void TryToPlaceTileContent(Collider collider)
    {
        if (objectToBuild != null)
        {
            Tile tile = collider.gameObject.GetComponent<Tile>();
            if (tile != null && tile.content == null)
            {
                GameObject tileContentGO = (GameObject)Instantiate(objectToBuild, tile.center, tile.transform.rotation, tile.transform);
                tile.content = tileContentGO;
            }
        }
        else
        {
            Debug.LogWarning("No Tile content selected");
        }
    }

    private void RemoveTileContent(Collider collider)
    {
        Tile tile = collider.gameObject.GetComponent<Tile>();
        if (tile != null && tile.content != null)
        {
            DestroyImmediate(tile.content.gameObject);
            tile.content = null;
        }
    }

    private void RaiseTiles(Collider collider)
    {
        Collider[] hitColliders = Physics.OverlapSphere(collider.transform.position, brushSize, 1 << selectedLayer);
        foreach (Collider col in hitColliders)
        {
            Undo.RegisterCompleteObjectUndo(col.gameObject.transform, "moveUp");
            col.transform.Translate(col.transform.up * brushHardness);
        }
    }

    private void LowerTiles(Collider collider)
    {
        Collider[] hitColliders = Physics.OverlapSphere(collider.transform.position, brushSize, 1 << selectedLayer);
        foreach (Collider col in hitColliders)
        {
            Undo.RegisterCompleteObjectUndo(col.gameObject.transform, "moveLower");
            col.transform.Translate(-col.transform.up * brushHardness);
        }
    }

    private void FlattenTiles(Collider collider, float reference)
    {
        Collider[] hitColliders = Physics.OverlapSphere(collider.transform.position, brushSize, 1 << selectedLayer);
        foreach (Collider col in hitColliders)
        {
            Undo.RegisterCompleteObjectUndo(col.gameObject.transform, "moveFlatten");
            col.transform.position = new Vector3(col.transform.position.x, reference, col.transform.position.z);
        }
    }

    private void PaintTile(Collider collider)
    {
        Tile tile = collider.GetComponent<Tile>();
        if (material != null && isPainting && tile != null)
        {
            tile.ChangeMaterial(material);
        }
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += SceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= SceneGUI;
    }

    private void BuildMap()
    {
        CalculOffsets();

        for (int x = 0; x < width; ++x)
        {
            //int logicPosY = x;
            for (int z = 0; z < height; ++z)
            {
                float xPos = x * xOffset;
                if (z % 2 == 1)
                {
                    xPos += xOffset / 2f;
                    //++logicPosY;
                }
                int logicPosX = x + (int)(z * -0.5f);
                Point pos = new Point(logicPosX/*, logicPosY*/, z);

                bool existButNull = (tileDictionary.ContainsKey(pos) && tileDictionary[pos] == null);

                if (!tileDictionary.ContainsKey(pos) || existButNull)
                {
                    if (existButNull)
                    {
                        tileDictionary.Remove(pos);
                    }

                    GameObject tileCloneGO = Instantiate(tilePrefab, new Vector3(xPos, 0, z * zOffset), Quaternion.identity, mapContainer);
                    Tile tileClone = tileCloneGO.GetComponent<Tile>();

                    tileClone.pos = pos;

                    tileDictionary.Add(tileClone.pos, tileClone);

                    tileCloneGO.name = "Tile-" + logicPosX /*+ "_" + logicPosY */+ "_" + z;
                    tileCloneGO.layer = selectedLayer;
                }
            }
        }
    }

    private void ResetMap()
    {
        foreach (Tile t in tileDictionary.Values)
        {
            if (t != null)
            {
                DestroyImmediate(t.gameObject);
            }
        }

        tileDictionary.Clear();
    }

    private void CenterTiles(List<Tile> listTile, int index)
    {
        Vector3 offset = Vector3.zero - listTile[index].transform.localPosition;
        foreach (Tile t in listTile)
        {
            t.transform.localPosition += new Vector3(offset.x, 0f, offset.z);
        }
    }

    /**
     * Set the x and z offsets to put the tiles correctly in world pos
     */
    private void CalculOffsets()
    {
        float rad = 60 * Mathf.PI / 180;
        xOffset = ((hexagoneSize / 4) * Mathf.Tan(rad)) * 2;
        zOffset = hexagoneSize * (3f / 4f);
    }




    public void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

}
