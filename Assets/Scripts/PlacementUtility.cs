using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class PlacementUtility
{
    public static void PlaceOnGround(this GameObject obj)
    {
        var pos = obj.transform.position += Vector3.up * 10f;
        var ray = new Ray(obj.transform.position, Vector3.down);
        var hits = Physics.RaycastAll(ray, 100f);
        foreach (var hit in hits.OrderBy(x => x.distance))
        {
            if (hit.collider.name.IndexOf("env_ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("env_dirt", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("env_pond", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("env_pound", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("generic_ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("prop_dock", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("bld_dock", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                hit.collider.name.IndexOf("terrain", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                obj.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                break;
            }
        }
    }
}

