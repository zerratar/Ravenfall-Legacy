using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace RavenNest.SDK.Endpoints
{
    public class Update<T>
    {
        public T Data;
        public DateTime Updated;
        public Update(T data)
        {
            Data = data;
            Updated = DateTime.UtcNow;
        }
    }

    public class Update<T, T2>
    {
        public T UpdateData;
        public T2 KnowledgeBase;
        public DateTime Updated;
        public Update(T data, T2 knowledgeBase)
        {
            UpdateData = data;

            // We have to make a copy of the knowledge base
            // otherwise we cannot ensure its integrity
            // I really don't like this but its a sacrifice I'm willing to make.

            KnowledgeBase = JsonConvert.DeserializeObject<T2>(JsonConvert.SerializeObject(knowledgeBase));
            Updated = DateTime.UtcNow;
        }
    }
}