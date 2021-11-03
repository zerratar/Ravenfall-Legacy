using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tessera;
using UnityEngine;
using UnityEngine.AI;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private DungeonRoomController[] availableRooms;


    private static readonly Vector3 gizmosSize = Vector3.one * .1f;

    private DungeonRoomController start;
    private DungeonRoomController exit;

    public Vector3 BoundsOffset = Vector3.zero;

    public int MinRoomCount = 2;
    public int MaxRoomCount = 10;
    public int MaxLSequence = 1;

    public int MaxRSequence = 2;

    public Vector3 StartingRoomPosition;

    private bool validDungeon;
    private bool generatingDungeon;
    private List<Bounds> addedSurfaces = new List<Bounds>();
    private List<DungeonRoom> addedRooms = new List<DungeonRoom>();
    private bool interrupted;



    // Start is called before the first frame update
    void Start()
    {
        if (availableRooms == null || availableRooms.Length == 0)
            return;

        //GenerateDungeon();
    }

    //public void Update()
    //{
    //    if (!generatingDungeon)
    //    {
    //        foreach (var bound in addedSurfaces)
    //        {
    //            if (RoomIntersects(bound, out _))
    //            {
    //                GenerateDungeon();
    //                return;
    //            }
    //        }
    //    }
    //}

    public void GoodDungeon()
    {
        GameManager.Log(string.Join(",", addedRooms.Select((x, i) => i.ToString()).ToArray()));
    }


    public void Interrupt()
    {
        interrupted = true;
    }


    // Update is called once per frame
    public void GenerateDungeon()
    {
        interrupted = false;
        generatingDungeon = true;
        validDungeon = false;
        StartCoroutine(GenerateRooms(UnityEngine.Random.Range(MinRoomCount, MaxRoomCount + 1)));
    }


    private static Vector3 Multiply(Vector3Int v3i, Vector3 v3)
    {
        return new Vector3(v3.x * v3i.x, v3.y * v3i.y, v3.z * v3i.z);
    }

    private static Vector3 Multiply(Vector3 v3i, Vector3 v3)
    {
        return new Vector3(v3.x * v3i.x, v3.y * v3i.y, v3.z * v3i.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        CheckForCollisions();

        //foreach (var b in addedBounds)
        //{
        //    if (addedBounds.Any(x => x.Intersects(b) && b != x))
        //    {
        //        Gizmos.color = Color.red;
        //    }
        //    else
        //    {
        //        Gizmos.color = Color.green;
        //    }
        //    //Gizmos.DrawWireCube(b.center, b.size);
        //    DrawGizmosFor(b);
        //}


        //foreach (var bound in addedSurfaces)
        //{
        //    if (RoomIntersects(bound, out _))
        //    {
        //        Gizmos.color = Color.red;
        //    }
        //    else
        //    {
        //        Gizmos.color = Color.white;
        //    }

        //    //Gizmos.DrawWireCube(bound.center, bound.size);
        //    DrawGizmosFor(bound);
        //}
    }

    private bool CheckForCollisions()
    {
        var tileSize = (Vector3.one * 5f);
        var addedBounds = new List<Bounds>();
        for (var i = 0; i < this.transform.childCount; ++i)
        {
            var room = this.transform.GetChild(i);
            var rot = room.rotation;
            var pos = room.position;
            var tile = room.GetComponent<TesseraTile>();
            //var bounds = tile.GetBounds();

            for (var j = 0; j < tile.offsets.Count; ++j)
            {
                var offset = Multiply(Multiply(tile.offsets[j], Vector3.one), tileSize);
                var bounds = new Bounds(pos + (rot * (offset + tile.center)), rot * (tileSize * 0.99f));
                addedBounds.Add(bounds);
            }

            //Gizmos.DrawWireCube(pos + (rot * (bounds.center)), rot * (Multiply(bounds.size, tile.tileSize)));
        }


        for (var a = 0; a < addedBounds.Count; ++a)
        {
            var boundsA = addedBounds[a];
            Gizmos.color = Color.green;
            for (var b = 0; b < addedBounds.Count; ++b)
            {
                if (b == a)
                {
                    continue;
                }

                var minA = boundsA.min;
                var maxA = boundsA.max;
                var minB = new Vector3(maxA.x, minA.y, maxA.z);
                var maxB = new Vector3(minA.x, maxA.y, minA.z);

                var boundsB = addedBounds[b];
                if (boundsA.Intersects(boundsB)
                    || boundsB.Intersects(boundsA)
                    || boundsB.Contains(minA)
                    || boundsB.Contains(maxA)
                    || boundsB.Contains(minB)
                    || boundsB.Contains(maxB))
                {
                    //Gizmos.color = Color.red;
                    //break;
                    return true;
                }
            }
            //DrawGizmosFor(boundsA);
        }
        return false;
    }

    public static void DrawGizmosFor(Bounds B)
    {
        var xVals = new[] {
            B.max.x, B.min.x
        };
        var yVals = new[] {
            B.max.y, B.min.y
        };
        var zVals = new[] {
            B.max.z, B.min.z
        };

        for (int i = 0; i < xVals.Length; i++)
        {
            var x = xVals[i];
            for (int j = 0; j < yVals.Length; j++)
            {
                var y = yVals[j];
                for (int k = 0; k < zVals.Length; k++)
                {
                    var z = zVals[k];

                    var point = new Vector3(x, y, z);
                    Gizmos.DrawCube(point, gizmosSize);

                    if (i == 0)
                    {
                        Gizmos.DrawLine(point, new Vector3(xVals[1], y, z));
                    }
                    if (j == 0)
                    {
                        Gizmos.DrawLine(point, new Vector3(x, yVals[1], z));
                    }
                    if (k == 0)
                    {
                        Gizmos.DrawLine(point, new Vector3(x, y, zVals[1]));
                    }

                }
            }
        }
    }

    bool Bounds2dIntersect(Bounds a, Bounds b)
    {
        return a.Intersects(b);
        //return ((Math.Abs((a.min.x + a.max.x / 2) - (b.min.x + b.max.x / 2)) * 2 < (a.max.x + b.max.x)) &&
        //       (Math.Abs((a.min.z + a.max.z / 2) - (b.min.z + b.max.z / 2)) * 2 < (a.max.z + b.max.z)));
        //         ||((Math.Abs(a.center.x - b.center.x) * 2 < (a.size.x + b.size.x)) &&
        //       (Math.Abs(a.center.z - b.center.z) * 2 < (a.size.z + b.size.z)));
    }

    private bool RoomIntersects(Bounds bound)
    {
        return CheckForCollisions();

        //var collisionIndices = new List<int>();

        //for (int i = 0; i < addedSurfaces.Count; i++)
        //{
        //    Bounds s = addedSurfaces[i];
        //    if (s.GetHashCode() == bound.GetHashCode())
        //        continue;

        //    //if ((s.center - bound.center) == Vector3.zero)
        //    //    continue;

        //    if (Bounds2dIntersect(bound, s))
        //    {
        //        collisionIndices.Add(i);
        //    }
        //}

        //var y = bound.extents.y;
        //var min = bound.min;
        //var max = bound.max;

        //var m0 = new Vector3(max.x, y, max.z);
        //var m1 = new Vector3(max.x, y, min.z);
        //var m2 = new Vector3(min.x, y, max.z);
        //var m3 = new Vector3(min.x, y, min.z);
        //var collisions = addedSurfaces.Where((x, i) =>
        //{
        //    if ((x.center - bound.center) != Vector3.zero && (
        //            x.Intersects(bound) || bound.Intersects(x) ||
        //            x.Contains(m0) || x.Contains(m1) || x.Contains(m2) || x.Contains(m3)))
        //    {
        //        collisionIndices.Add(i);
        //        return true;
        //    }
        //    return false;
        //}).ToList();

        //indices = collisionIndices.ToArray();
        //return collisionIndices.Count > 0;
    }


    //private bool CollidesWithOtherRoom(Bounds roomBounds, out int index)
    //{
    //    index = -1;
    //    for (int i1 = 0; i1 < addedSurfaces.Count; i1++)
    //    {
    //        Bounds e = addedSurfaces[i1];
    //        if (roomBounds.Intersects(e))
    //        {
    //            index = i1;
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    public void ClearChildren()
    {
        var childCount = this.transform.childCount;

        for (var i = childCount - 1; i >= 0; --i)
        {
            var child = this.transform.GetChild(i);
            if (child)
            {
                if (!Application.isPlaying || Application.isEditor)
                {
                    DestroyImmediate(child.gameObject);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private int dummy = 0;
    public void Step()
    {
        ++dummy;
    }

    private IEnumerator GenerateRooms(int roomCount)
    {
        //Start:

        ClearChildren();

        yield return null;

        addedSurfaces = new List<Bounds>();
        addedRooms = new List<DungeonRoom>();

        if (interrupted)
        {
            interrupted = false;
            yield break;
        }

        GenerateStartingRoom();

        // Rules to make this work
        // 1. The L Shape cannot be used 3 times in a row
        // 2. The Boss Room connector cannot be used after the L Shape
        //    So the last shape must be the cube

        var rooms = availableRooms.Where(x => x.RoomType == DungeonRoomType.Room && !x.BossConnector).ToList();
        var lastWasL = false;
        var lastExitPoint = start.ExitPoint.position;
        var lastRotation = Vector3.zero;
        var rotation = Vector3.zero;
        var lSequence = 0;
        var rSequence = 0;

        // Add rooms
        for (var i = 0; i < roomCount;)
        {

            if (interrupted)
            {
                interrupted = false;
                yield break;
            }

            yield return null;

            var room = rooms[UnityEngine.Random.Range(0, rooms.Count)];
            var isL = room.RoomShape == DungeonRoomShape.L;

            if (isL && lSequence >= MaxLSequence)
            {
                continue;
            }

            if (!isL && rSequence >= MaxRSequence)
            {
                continue;
            }

            var position = lastExitPoint;
            rotation = lastRotation + new Vector3(0, room.RoomRotation, 0);

            var rot = Quaternion.Euler(rotation);
            var roomSurface = room.GetComponent<NavMeshSurface>();
            var surfaceBounds = roomSurface.navMeshData.sourceBounds;
            var roomBounds = new Bounds(position + rot * surfaceBounds.center, rot * (surfaceBounds.size + BoundsOffset));


            if (RoomIntersects(roomBounds))
            {
                //UnityEngine.Debug.LogError("New [" + addedRooms.Count + "] room (" + room.name + ") intersects with " + addedRooms[i1[0]].RoomController.name + " [" + i1[0] + "]");
                //goto Start;
                yield return GenerateRooms(roomCount);
                yield break;
                //++i;
                //Destroy(addedRooms[i1].gameObject);
                //addedRooms.RemoveAt(i1);
                //addedSurfaces.RemoveAt(i1);
                //continue;
            }

            var obj = GameObject.Instantiate(room.gameObject, position, rot, this.transform);
            var r = obj.GetComponent<DungeonRoomController>();

            lastExitPoint = r.ExitPoint.position;
            lastRotation = rotation;
            lastWasL = isL;


            AddRoom(r);
            addedSurfaces.Add(roomBounds);

            if (isL)
            {
                ++lSequence;
                rSequence = 0;
            }
            else
            {
                ++rSequence;
                lSequence = 0;
            }
            ++i;
        }

        // Add Boss Room Connector
        yield return null;
        {
            var bossConnector = availableRooms.FirstOrDefault(x => x.BossConnector);
            rotation = lastWasL ? lastRotation : lastRotation + new Vector3(0, bossConnector.RoomRotation, 0);
            {
                var rot = Quaternion.Euler(rotation);
                var roomSurface = bossConnector.GetComponent<NavMeshSurface>();
                var surfaceBounds = roomSurface.navMeshData.sourceBounds;
                var roomBounds = new Bounds(lastExitPoint + rot * surfaceBounds.center, rot * (surfaceBounds.size + BoundsOffset));

                if (RoomIntersects(roomBounds))
                {
                    //UnityEngine.Debug.LogError("New [" + addedRooms.Count + "] room (" + bossConnector.name + ") intersects with " + addedRooms[i1[0]].RoomController.name + " [" + i1[0] + "]");
                    yield return GenerateRooms(roomCount);
                    yield break;
                }

                addedSurfaces.Add(roomBounds);
            }

            var bcobj = GameObject.Instantiate(bossConnector.gameObject, lastExitPoint, Quaternion.Euler(rotation), this.transform);
            var bcr = bcobj.GetComponent<DungeonRoomController>();
            lastExitPoint = bcr.ExitPoint.position;
            AddRoom(bcr);
        }

        // Add Boss Room
        yield return null;
        {
            var boss = availableRooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Boss);
            rotation = rotation + new Vector3(0, boss.RoomRotation, 0);

            {
                var rot = Quaternion.Euler(rotation);
                var roomSurface = boss.GetComponent<NavMeshSurface>();
                var surfaceBounds = roomSurface.navMeshData.sourceBounds;
                var roomBounds = new Bounds(lastExitPoint + rot * surfaceBounds.center, rot * (surfaceBounds.size + BoundsOffset));

                if (RoomIntersects(roomBounds))
                {
                    //UnityEngine.Debug.LogError("New [" + addedRooms.Count + "] room (" + boss.name + ") intersects with " + addedRooms[i1[0]].RoomController.name + " [" + i1[0] + "]");
                    yield return GenerateRooms(roomCount);
                    yield break;
                }

                addedSurfaces.Add(roomBounds);
            }
            var bobj = GameObject.Instantiate(boss.gameObject, lastExitPoint, Quaternion.Euler(rotation), this.transform);
            var br = bobj.GetComponent<DungeonRoomController>();
            AddRoom(br);
        }


        yield return null;
        var intersected = false;
        foreach (var bound in addedSurfaces)
        {
            if (RoomIntersects(bound))
            {
                intersected = true;
                break;
            }
        }
        if (intersected)
        {
            GenerateDungeon();
        }
        else
        {
            generatingDungeon = false;
        }
    }

    private void GenerateStartingRoom()
    {
        var sr = availableRooms.FirstOrDefault(x => x.RoomType == DungeonRoomType.Start);
        {
            var boxCollider = sr.GetComponent<BoxCollider>();
            var roomBounds = new Bounds();
            if (boxCollider)
            {
                roomBounds = new Bounds(StartingRoomPosition + boxCollider.center, (boxCollider.size + BoundsOffset));
            }
            else
            {
                var roomSurface = sr.GetComponent<NavMeshSurface>();
                var surfaceBounds = roomSurface.navMeshData.sourceBounds;
                roomBounds = new Bounds(StartingRoomPosition + surfaceBounds.center, (surfaceBounds.size + BoundsOffset));
            }
            addedSurfaces.Add(roomBounds);
        }

        start = GameObject.Instantiate(sr.gameObject, StartingRoomPosition, Quaternion.identity, this.transform)
            .GetComponent<DungeonRoomController>();

        AddRoom(start);
    }

    private void AddRoom(DungeonRoomController room)
    {
        var tile = room.transform.GetComponent<TesseraTile>();
        addedRooms.Add(new DungeonRoom { RoomController = room, Tile = tile });
    }

    public void SaveGeneratedAsPrefab()
    {

    }

    public class DungeonRoom
    {
        public DungeonRoomController RoomController;
        public TesseraTile Tile;
    }
}
