using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
    }

    private GameObject SpawnRoom(int x, int y, GameObject roomPrefab, string name)
    {
        Vector2 roomPos = new Vector2(x * (roomWidth + ROOM_GAP), y * (roomWidth + ROOM_GAP));
        var room = Instantiate(roomPrefab, roomPos, Quaternion.identity, transform);

        room.transform.localScale = new Vector3(roomWidth, roomHeight, 1);
        room.name = name;

        return room;
    }

    public void GenRooms()
    {
        // todo : remove
        if (!Application.isPlaying)
        {
            DungeonManagerHelper.Instance.ChangeRooms(RoomsPrefab);
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);
        }
        else
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

        // var marker = Instantiate(MarkerRoom, new Vector3(0, 0, 10), Quaternion.identity, transform);
        // marker.transform.localScale = new Vector2(width * roomWidth + (width - 1) * ROOM_GAP, height * roomHeight + (height - 1) * ROOM_GAP);
        // marker.name = "Marker background";

        // starting room
        int middleX = width / 2;
        SpawnRoom(middleX, -1, StartRoom, "Starting Room");

        // end room
        int endX = rng.Next(0, width);
        int endY = rng.Next(height - 1 - Math.Abs(endX - middleX), height);
        SpawnRoom(endX, endY, EndRoom, "End Room");

        var shuffledRooms = DungeonManagerHelper.ShuffledRooms();
        var roomSpaces = shuffledRooms.Select(r => r.GetComponent<RoomInfo>()).ToArray();

        GameObject[,] roomSpacesAvailable = new GameObject[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                roomSpacesAvailable[x, y] = null;
        roomSpacesAvailable[endX, endY] = EndRoom;

        bool validPos(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
        bool isRoomValid(int x, int y, RoomInfo roomInfo)
        {
            foreach (var space in roomInfo.SpaceUsed)
            {
                int spaceX = x + space.x;
                int spaceY = y + space.y;
                if (!validPos(spaceX, spaceY)) return false;
                if (roomSpacesAvailable[spaceX, spaceY]) return false;
            }

            return true;
        }

        int index = 0;
        int roomCount = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (roomSpacesAvailable[x, y]) continue;

                // get valid room
                while (!isRoomValid(x, y, roomSpaces[index]))
                    index = ++index % shuffledRooms.Length;

                var room = SpawnRoom(x, y, shuffledRooms[index], $"Room ({roomCount++})");
                foreach (var a in roomSpaces[index].SpaceUsed)
                    roomSpacesAvailable[x + a.x, y + a.y] = room;

                index = ++index % shuffledRooms.Length;
            }
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
    }
}
