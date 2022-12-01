using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FT_ADDON
{
    class UserPermissionTree
    {
        SAPbobsCOM.UserPermissionTree upt;

        public static bool TryGet(string id, out UserPermissionTree oupt)
        {
            oupt = null;
            var temp = Common.createSAPObject<SAPbobsCOM.UserPermissionTree>(SAPbobsCOM.BoObjectTypes.oUserPermissionTree);

            if (!temp.GetByKey(id)) return false;

            oupt = new UserPermissionTree(temp);
            return true;
        }

        public UserPermissionTree(SAPbobsCOM.UserPermissionTree _upt)
        {
            upt = _upt;
        }

        public UserPermissionTree(string id)
        {
            upt = Common.createSAPObject<SAPbobsCOM.UserPermissionTree>(SAPbobsCOM.BoObjectTypes.oUserPermissionTree);

            if (!upt.GetByKey(id)) SAP.SBOApplication.MessageBox(FT_ADDON.SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
        }

        public UserPermissionTree(string id, string name, bool hasRead)
        {
            upt = Common.createSAPObject<SAPbobsCOM.UserPermissionTree>(SAPbobsCOM.BoObjectTypes.oUserPermissionTree);

            if (upt.GetByKey(id)) return;

            SAP.SBOCompany.StartTransaction();

            try
            {
                initialize(id, name, hasRead);
                upt.ParentID = "";

                if (upt.Add() != 0 || !upt.GetByKey(id))
                {
                    SAP.SBOApplication.MessageBox(FT_ADDON.SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                    return;
                }

                SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
            }
            finally
            {
                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
            }
        }

        public UserPermissionTree(string id, string name, bool hasRead, UserPermissionTree parent)
        {
            upt = Common.createSAPObject<SAPbobsCOM.UserPermissionTree>(SAPbobsCOM.BoObjectTypes.oUserPermissionTree);

            if (upt.GetByKey(id)) return;

            SAP.SBOCompany.StartTransaction();

            try
            {
                initialize(id, name, hasRead);
                upt.ParentID = parent.upt.PermissionID;

                if (upt.Add() != 0 || !upt.GetByKey(id))
                {
                    SAP.SBOApplication.MessageBox(FT_ADDON.SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
                    return;
                }

                SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
            }
            finally
            {
                if (SAP.SBOCompany.InTransaction) SAP.SBOCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
            }
        }

        ~UserPermissionTree()
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(upt);
            upt = null;
        }

        private void initialize(string id, string name, bool hasRead)
        {
            upt = Common.createSAPObject<SAPbobsCOM.UserPermissionTree>(SAPbobsCOM.BoObjectTypes.oUserPermissionTree);
            upt.PermissionID = id;
            upt.Name = name;
            upt.Options = hasRead ? SAPbobsCOM.BoUPTOptions.bou_FullReadNone : SAPbobsCOM.BoUPTOptions.bou_FullNone;
            upt.IsItem = SAPbobsCOM.BoYesNoEnum.tNO;
        }

        public void addForm(string formId)
        {
            upt.UserPermissionForms.FormType = formId;
            upt.UserPermissionForms.Add();
        }

        public void update()
        {
            if (upt.Update() != 0) SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
        }

        public void delete()
        {
            if (upt.Remove() != 0) SAP.SBOApplication.MessageBox(SAP.SBOCompany.GetLastErrorDescription(), 1, "&Ok", "", "");
        }
    }
}
