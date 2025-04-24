using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class DungeonManager : MonoBehaviour
{
    public int width = 5;
    public int height = 5;

    public int roomWidth = 10;
    public int roomHeight = 10;

    private const int ROOM_GAP = 2;

    private System.Random rng = new();

    public GameObject[] RoomsPrefab;
    public GameObject StartRoom;
    public GameObject EndRoom;
    public GameObject MarkerRoom;

    public GameObject[] Connectors;

    #region Generated
    GameObject startRoom;
    (int x, int y) endRoom = (-1, -1);
    GameObject[,] rooms;

    #endregion Generated


    public void Start()
    {
        if (RoomsPrefab is null)
            throw new System.Exception("RoomsPrefab is null");

        DungeonManagerHelper.Instance.ChangeRooms(RoomsPrefab);
    }

    public void Update()
    {
        if (Input.GetKey(KeyCode.G))
            Gen();
    }


    public void Gen()
    {
        GenRooms();
        GenConnectors();
    }

    private GameObject SpawnRoom(int x, int y, GameObject roomPrefab, string name)
    {
        Vector2 roomPos = new Vector2(x * (roomWidth + ROOM_GAP), y * (roomWidth + ROOM_GAP));
        var room = Instantiate(roomPrefab, roomPos, Quaternion.identity, transform);

        room.transform.localScale = new Vector3(roomWidth, roomHeight, 1);
        room.name = name;

        room.GetComponent<RoomInfo>().position = new Vector2Int(x, y);

        return room;
    }

    void CleanEditor()
    {
        DungeonManagerHelper.Instance.ChangeRooms(RoomsPrefab);
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }
    void CleanPlaying()
    {
        while (transform.childCount > 0)
            Destroy(transform.GetChild(0).gameObject);
    }

    public void GenRooms()
    {
        // todo : remove
        if (!Application.isPlaying)
            CleanEditor();
        else
            CleanPlaying();

        rooms = new GameObject[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                rooms[x, y] = null;

        // starting room
        int middleX = width / 2;
        startRoom = SpawnRoom(middleX, -1, StartRoom, "Starting Room");
        startRoom.GetComponent<RoomInfo>().position = new Vector2Int(middleX, -1);

        // end room
        int endX = rng.Next(0, width);
        int endY = rng.Next(height - 1 - Math.Abs(endX - middleX), height);
        endRoom = (endX, endY);
        var endos = SpawnRoom(endX, endY, EndRoom, "End Room");
        rooms[endX, endY] = endos;

        var shuffledRooms = DungeonManagerHelper.ShuffledRooms();
        var roomSpaces = shuffledRooms.Select(r => r.GetComponent<RoomInfo>()).ToArray();

        bool validPos(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
        bool isRoomValid(int x, int y, RoomInfo roomInfo)
        {
            foreach (var space in roomInfo.SpaceUsed)
            {
                int spaceX = x + space.x;
                int spaceY = y + space.y;
                if (!validPos(spaceX, spaceY)) return false;
                if (rooms[spaceX, spaceY]) return false;
            }

            return true;
        }

        int index = 0;
        int roomCount = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (rooms[x, y]) continue;

                // get valid room
                while (!isRoomValid(x, y, roomSpaces[index]))
                    index = ++index % shuffledRooms.Length;

                var room = SpawnRoom(x, y, shuffledRooms[index], $"Room ({roomCount++})");
                foreach (var a in roomSpaces[index].SpaceUsed)
                    rooms[x + a.x, y + a.y] = room;

                index = ++index % shuffledRooms.Length;
            }

        // neighbours
        Vector2Int[] sides = new Vector2Int[]
        {
            new(0, 1),
            new(1, 0),
            new(0, -1),
            new(-1, 0)
        };
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var room = rooms[x, y].GetComponent<RoomInfo>();
                for (int i = 0; i < 4; i++)
                {
                    if (!validPos(x + sides[i].x, y + sides[i].y)) continue;
                    var neighbour = rooms[x + sides[i].x, y + sides[i].y];
                    if (neighbour != rooms[x, y] && !room.Neighbours.Contains(neighbour))
                    {
                        room.Neighbours.Add(neighbour);

                        // Vector2 selfos = (Vector2)room.transform.position + new Vector2(roomWidth / 2, roomHeight / 2);
                        // Vector2 neighbouros = (Vector2)neighbour.transform.position + new Vector2(roomWidth / 2, roomHeight / 2);
                        // Debug.DrawLine(selfos, neighbouros, Color.red, 10);
                    }
                }
                rng.Shuffle(room.Neighbours);
            }
    }

    public void GenConnectors()
    {
        var path = new List<GameObject>();
        var found = VisitNeighbour(rooms[endRoom.x, endRoom.y], path, new List<GameObject>());

        if (!found)
        {
            Debug.LogError("no path found, tf?");
            return;
        }

        // main path
        var step = new Vector2(roomWidth / 2, roomHeight / 2);
        var lastpos = rooms[endRoom.x, endRoom.y].GetComponent<RoomInfo>();
        for (int i = 1; i < path.Count; i++)
        {
            var temp = path[i].transform.GetComponent<RoomInfo>();
            temp.GetComponent<RoomInfo>().Connected.Add(lastpos.gameObject);
            lastpos = temp;
        }
        startRoom.GetComponent<RoomInfo>().Connected.Add(lastpos.gameObject);

        List<GameObject> roomsList = new();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!roomsList.Contains(rooms[x, y]) && !path.Contains(rooms[x, y]))
                    roomsList.Add(rooms[x, y]);

        int index = 0;
        while (true)
        {
            var temp = roomsList[index];
            foreach (var n in temp.GetComponent<RoomInfo>().Neighbours)
            {
                if (path.Contains(n))
                {
                    n.GetComponent<RoomInfo>().Connected.Add(temp);
                    roomsList.Remove(temp);
                    path.Add(temp);
                    break;
                }
            }
            if (roomsList.Count == 0)
                break;
            index = (index + 1) % roomsList.Count;
        }

        bool isNextTo(Vector2Int uno, Vector2Int dos) =>
            (uno.x == dos.x && uno.y == dos.y + 1) ||
            (uno.x == dos.x + 1 && uno.y == dos.y) ||
            (uno.x == dos.x && uno.y == dos.y - 1) ||
            (uno.x == dos.x - 1 && uno.y == dos.y);

        void SpawnConnector(Vector2Int uno, Vector2Int dos)
        {
            Vector2 start = uno * (roomWidth + ROOM_GAP) + step;
            Vector2 end = dos * (roomWidth + ROOM_GAP) + step;

            var conn = Instantiate(Connectors[0], start + (end - start) / 2, Quaternion.identity, transform);
            conn.transform.localScale = new Vector3(roomWidth, roomHeight, 1);
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var currentRoom = rooms[x, y].GetComponent<RoomInfo>();

                var coord = new Vector2Int(x, y);
                var selfos = coord * (roomWidth + ROOM_GAP) + step;
                foreach (var connectorInfo in currentRoom.Connected.Select(c => c.GetComponent<RoomInfo>()))
                    foreach (var space in connectorInfo.SpaceUsed)
                        if (isNextTo(coord, connectorInfo.position + space))
                        {
                            Debug.DrawLine(selfos, (connectorInfo.position + space) * (roomWidth + ROOM_GAP) + step, Color.red, 0.1f);
                            SpawnConnector(coord, connectorInfo.position + space);
                        }
            }

        var startCoord = new Vector2Int(width / 2, -1);
        var startos = startCoord * (roomWidth + ROOM_GAP) + step;
        foreach (var connectorInfo in startRoom.GetComponent<RoomInfo>().Connected.Select(c => c.GetComponent<RoomInfo>()))
            foreach (var space in connectorInfo.SpaceUsed)
                if (isNextTo(startCoord, connectorInfo.position + space))
                    SpawnConnector(startCoord, connectorInfo.position + space);
        // Debug.DrawLine(startos, (connectorInfo.position + space) * (roomWidth + ROOM_GAP) + step, Color.red, 0.1f);
    }

    bool VisitNeighbour(GameObject currentRoom, List<GameObject> path, List<GameObject> fullyVisited)
    {
        var info = currentRoom.GetComponent<RoomInfo>();
        path.Add(currentRoom);

        foreach (var pos in info.SpaceUsed)
            if (info.position.x + pos.x == width / 2 && info.position.y + pos.y == 0) // above start room => end result
                return true;

        foreach (var neighbour in info.Neighbours)
        {
            if (path.Contains(neighbour)) continue; // dont go back on path
            if (fullyVisited.Contains(neighbour)) continue; // dead end

            var foundPath = VisitNeighbour(neighbour, path, fullyVisited);
            if (foundPath) // found a path
                return true;
        }
        path.RemoveAt(path.Count - 1);
        fullyVisited.Add(currentRoom); // no neighbours (dead end) => remove self
        return false;
    }
}

public class DungeonManagerHelper
{
    public static DungeonManagerHelper Instance = new();
    private GameObject[] rooms = new GameObject[0];
    private static System.Random rng = new();

    public void ChangeRooms(GameObject[] rooms)
    {
        // this.rooms.AddRange(rooms);
        this.rooms = rooms;
    }

    public static GameObject[] ShuffledRooms()
    {
        GameObject[] copy = Instance.rooms.ToArray();
        rng.Shuffle(copy);
        return copy;
    }
}

static class RandomExtensions
{
    public static void Shuffle<T>(this System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (array[k], array[n]) = (array[n], array[k]); // swap
        }
    }

    public static void Shuffle<T>(this System.Random rng, List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (list[k], list[n]) = (list[n], list[k]); // swap
        }
    }
}

[CustomEditor(typeof(DungeonManager))]
public class DungeonManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DungeonManager script = (DungeonManager)target;

        if (GUILayout.Button("full gen"))
        {
            script.Gen();
        }
        if (GUILayout.Button("gen rooms"))
        {
            script.GenRooms();
        }
        if (GUILayout.Button("gen connectors"))
        {
            script.GenConnectors();
        }
    }
}
