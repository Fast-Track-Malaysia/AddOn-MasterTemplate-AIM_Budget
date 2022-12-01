using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class RecordSet : IDisposable
    {
        SAPbobsCOM.Recordset rc;
        bool disposed = false;

        public bool EoF { get => rc.EoF; }
        public bool BoF { get => rc.BoF; }
        public int RecordCount { get => rc.RecordCount; }
        /// <summary>
        /// Same as RecordCount
        /// </summary>
        public int Count { get => rc.RecordCount; }

        public RecordSet()
        {
            rc = (SAPbobsCOM.Recordset)SAP.SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
        }

        ~RecordSet()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (disposed) return;

                disposed = true;
            }

            if (rc == null) return;

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(rc);
            rc = null;
            GC.Collect();
        }

        public void DoQuery(string query, object obj = null) => rc.DoQuery(query, obj);

        public object GetValue(object field) => rc.Fields.Item(field).Value;

        public void MoveFirst() => rc.MoveFirst();

        public void MoveLast() => rc.MoveLast();

        public void MoveNext() => rc.MoveNext();

        public void MovePrevious() => rc.MovePrevious();

        public RecordSet GetRecord(object index, object filter)
        {
            rc.GetRecord(index, filter);
            return this;
        }

        public IEnumerable<T> Query<T>(string query) => rc.Query<T>(query);
    }
}
