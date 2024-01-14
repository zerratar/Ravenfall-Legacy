using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    [CreateAssetMenu(fileName = "New Player Skin", menuName = "Ravenfall/ScriptableObjects/Player Skin", order = 1)]
    public class PlayerSkinObject : ScriptableObject
    {
        public string Name;
        public GameObject SkinMeshObject;
    }
}
