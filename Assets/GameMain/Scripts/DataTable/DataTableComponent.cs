using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using DataTable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarForce
{
    public class DataTableComponent : GameFrameworkComponent
    {
        private readonly Dictionary<string, JArray> m_DataTables = new Dictionary<string, JArray>();
        private Tables m_Tables = null;

        public Tables Tables => m_Tables;

        public void AddDataTable(string name, string json)
        {
            m_DataTables[name] = JsonConvert.DeserializeObject(json) as JArray;
        }

        public void LoadTables()
        {
            if (m_Tables != null)
            {
                return;
            }

            m_Tables = new Tables(LoadJson);
        }

        private JArray LoadJson(string file)
        {
            JArray dataTable = null;
            if (m_DataTables.TryGetValue(file, out dataTable))
            {
                return dataTable;
            }

            Log.Error("Can not load data table '{0}'.", file);
            return null;
        }
    }
}