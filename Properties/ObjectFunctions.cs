using System;
using System.Collections.Generic;
using System.Text;

namespace FT_ADDON
{
    class ObjectFunctions
    {
        public static void copyMatrixColumns(SAPbouiCOM.Form oSForm, SAPbouiCOM.Matrix oSMatrix, SAPbouiCOM.Form oForm, SAPbouiCOM.Matrix oMatrix)
        {
            try
            {
                string columnname;
                int width = 0;
                string temp = "";
                string title = "";
                SAPbouiCOM.BoFormItemTypes itemtype = SAPbouiCOM.BoFormItemTypes.it_EDIT;
                SAPbouiCOM.Column oSColumn = null;
                SAPbouiCOM.Column oColumn = null;

                SAPbouiCOM.UserDataSource oUds = null;
                int size = 0;
                SAPbouiCOM.BoDataType datatype;
                SAPbouiCOM.LinkedButton oLink;
                SAPbouiCOM.LinkedButton oSLink;
                
                for (int col = 0; col < oSMatrix.Columns.Count; col++)
                {
                    if (oSMatrix.Columns.Item(col).Visible)
                    {
                        oSColumn = oSMatrix.Columns.Item(col);
                        title = oSColumn.Title;//oSMatrix.Columns.Item(col).Title;
                        width = oSColumn.Width;//oSMatrix.Columns.Item(col).Width;
                        columnname = oSColumn.UniqueID.ToString();//oSMatrix.Columns.Item(col).UniqueID.ToString();
                        itemtype = oSColumn.Type; ;//oSMatrix.Columns.Item(col).Type;
                        oColumn = oMatrix.Columns.Add(columnname, itemtype);
                        oColumn.TitleObject.Caption = title;
                        oColumn.Width = width;
                        temp = oSColumn.DataBind.Alias;
                        if (temp == null)
                        {
                            size = 100;
                            datatype = SAPbouiCOM.BoDataType.dt_SHORT_TEXT;
                        }
                        else
                        {
                            size = oSForm.DataSources.UserDataSources.Item(oSColumn.DataBind.Alias).Length;
                            datatype = oSForm.DataSources.UserDataSources.Item(oSColumn.DataBind.Alias).DataType;
                        }
                        oUds = oForm.DataSources.UserDataSources.Add(columnname, datatype, size);
                        oColumn.DataBind.SetBound(true, "", columnname);
                        oColumn.Editable = oSColumn.Editable;
                        if (itemtype == SAPbouiCOM.BoFormItemTypes.it_LINKED_BUTTON)
                        {
                            oSLink = (SAPbouiCOM.LinkedButton)oSColumn.ExtendedObject;
                            oLink = (SAPbouiCOM.LinkedButton)oColumn.ExtendedObject;
                            //SAP.SBOApplication.MessageBox(oSLink.LinkedObject.ToString(), 1, "ok", "", "");
                            oLink.LinkedObject = oSLink.LinkedObject;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "Ok", "", "");
                
            }
       }
        public static void copyMatrixColumnsValues(SAPbouiCOM.Form oSForm, SAPbouiCOM.Matrix oSMatrix, SAPbouiCOM.Form oForm, SAPbouiCOM.Matrix oMatrix)
        {
            try
            {
                string columnname;
                SAPbouiCOM.BoFormItemTypes itemtype = SAPbouiCOM.BoFormItemTypes.it_EDIT;

                string temp = "";

                for (int row = 1; row <= oSMatrix.RowCount; row++)
                {
                    oMatrix.AddRow(1, -1);
                    for (int col = 0; col < oSMatrix.Columns.Count; col++)
                    {
                        if (oSMatrix.Columns.Item(col).Visible)
                        {
                            columnname = oSMatrix.Columns.Item(col).UniqueID.ToString();
                            itemtype = oSMatrix.Columns.Item(col).Type;
                            if (itemtype == SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX)
                            {
                            }
                            else if (itemtype == SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX)
                            {
                                if (((SAPbouiCOM.CheckBox)oSMatrix.Columns.Item(columnname).Cells.Item(row).Specific).Checked)
                                {
                                    oMatrix.Columns.Item(columnname).Cells.Item(row).Click(SAPbouiCOM.BoCellClickType.ct_Regular, 0);
                                }
                            }
                            else if (itemtype == SAPbouiCOM.BoFormItemTypes.it_LINKED_BUTTON)
                            {
                                temp = ((SAPbouiCOM.EditText)oSMatrix.Columns.Item(columnname).Cells.Item(row).Specific).String;
                                ((SAPbouiCOM.EditText)(oMatrix.Columns.Item(columnname).Cells.Item(row).Specific)).String = temp;
                            }
                            else if (itemtype == SAPbouiCOM.BoFormItemTypes.it_EDIT)
                            {
                                temp = ((SAPbouiCOM.EditText)oSMatrix.Columns.Item(columnname).Cells.Item(row).Specific).String;
                                ((SAPbouiCOM.EditText)(oMatrix.Columns.Item(columnname).Cells.Item(row).Specific)).String = temp;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SAP.SBOApplication.MessageBox(Common.ReadException(ex), 1, "Ok", "", "");
                
            }
        }
        public static SAPbouiCOM.BoDataType changeUIFieldsTypeToUIDataType(SAPbouiCOM.BoFieldsType fieldType)
        {
            SAPbouiCOM.BoDataType datatype = SAPbouiCOM.BoDataType.dt_RATE;
            switch (fieldType)
            {
                case SAPbouiCOM.BoFieldsType.ft_NotDefined:
                case SAPbouiCOM.BoFieldsType.ft_AlphaNumeric:
                    datatype = SAPbouiCOM.BoDataType.dt_SHORT_TEXT;
                    break;
                case SAPbouiCOM.BoFieldsType.ft_Date:
                    datatype = SAPbouiCOM.BoDataType.dt_DATE;
                    break;
                case SAPbouiCOM.BoFieldsType.ft_Integer:
                    datatype = SAPbouiCOM.BoDataType.dt_SHORT_NUMBER;
                    break;
                case SAPbouiCOM.BoFieldsType.ft_Text:
                    datatype = SAPbouiCOM.BoDataType.dt_LONG_TEXT;
                    break;
            }
            return datatype;
        }
        public static SAPbouiCOM.BoDataType changeDIFieldTypesToDIDataType(SAPbobsCOM.BoFieldTypes fieldType)
        {
            SAPbouiCOM.BoDataType datatype = SAPbouiCOM.BoDataType.dt_RATE;
            switch (fieldType)
            {
                case SAPbobsCOM.BoFieldTypes.db_Alpha:
                    datatype = SAPbouiCOM.BoDataType.dt_SHORT_TEXT;
                    break;
                case SAPbobsCOM.BoFieldTypes.db_Date:
                    datatype = SAPbouiCOM.BoDataType.dt_DATE;
                    break;
                case SAPbobsCOM.BoFieldTypes.db_Memo:
                    datatype = SAPbouiCOM.BoDataType.dt_LONG_TEXT;
                    break;
                case SAPbobsCOM.BoFieldTypes.db_Numeric:
                    datatype = SAPbouiCOM.BoDataType.dt_SHORT_NUMBER;
                    break;
            }
            return datatype;
        }

    }
}
