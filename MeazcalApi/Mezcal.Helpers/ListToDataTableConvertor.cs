﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Mezcal.Helper
{
    public class ListToDataTableConvertor
    {

        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// This extension converts an enumerable set to a Dapper TVP
        /// </summary>
        /// <typeparam name="T">type of enumerbale</typeparam>
        /// <param name="enumerable">list of values</param>
        /// <param name="typeName">database type name</param>
        /// <param name="orderedColumnNames">if more than one column in a TVP, 
        /// columns order must mtach order of columns in TVP</param>
        /// <returns>a custom query parameter</returns>
        public static SqlMapper.ICustomQueryParameter AsTableValuedParameter<T>
            (this IEnumerable<T> enumerable, string typeName, IEnumerable<string> orderedColumnNames = null)
        {
            var dataTable = new DataTable();
            if (typeof(T).IsValueType || typeof(T).FullName.Equals("System.String"))
            {
                dataTable.Columns.Add(orderedColumnNames == null ? "NONAME" : orderedColumnNames.First(), typeof(T));
                foreach (T obj in enumerable)
                {
                    dataTable.Rows.Add(obj);
                }
            }
            else
            {
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo[] readableProperties = properties.Where(w => w.CanRead).ToArray();
                if (readableProperties.Length > 1 && orderedColumnNames == null)
                    throw new ArgumentException("Ordered list of column names must be provided when TVP contains more than one column");
                var columnNames = (orderedColumnNames ??
                    readableProperties.Select(s => s.Name)).ToArray();
                foreach (string name in columnNames)
                {
                    dataTable.Columns.Add(name, readableProperties.Single(s => s.Name.Equals(name)).PropertyType);
                }
                foreach (T obj in enumerable)
                {
                    dataTable.Rows.Add(
                        columnNames.Select(s => readableProperties.Single(s2 => s2.Name.Equals(s)).GetValue(obj)).ToArray());
                }
            }
            return dataTable.AsTableValuedParameter(typeName);
        }
    }
}
