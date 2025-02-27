﻿' Created by: Christina Fitzpatrick
' Date: 10/17/2013
' Time: 4:39 PM

'12/11/13: Added fix for blank UOM
'01/22/14: Added fix for invoices with sales tax amount not included in total.
'03/17/14: Changed SQL to pull PO number from line detail first to avoid blank PO numbers when the tax or freight lines are first.
'04/02/14: Added fix for invoices with no unit price to use merchandise amt instead.
'09/05/14: Added try/catch block to skip invoices with invalid file path characters and replaced all "*" in file path 
'10/16/14: Only left-pad POs that do not begin with an alpha character
'10/29/14: Added exception for vendors that are blank or not found
'11/3/14: Added try/catch block to skip invoices with no lines and update the USER_VCHR_CHAR1 to 'L' to identify them

Imports System
Imports System.Configuration
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.OleDb
Imports System.Data.SqlClient
Imports Console = System.Console
Imports System.Xml
Imports System.Text
Imports System.IO
Imports System.Net.Mail
Imports System.Text.RegularExpressions

Module SDI_BRT_HeaderLine
    Sub Main()
        headerData()
    End Sub

    Public Sub headerData()
        'OleDb Connection variables
        Dim connectionString As String
        Dim oledbc As OleDbConnection
        Dim oledbc2 As OleDbConnection
        Dim oledbCmd As OleDbCommand
        Dim oledbCmd2 As OleDbCommand
        Dim oler As OleDbDataReader
        Dim oler2 As OleDbDataReader

        'SQL statements and datasets
        Dim hdrSql As String
        Dim lineSql As String
        Dim hdrInfo As DataSet
        Dim poSql As String
        Dim updateSql As String

        'Output variables
        Dim vchrBldKey As String
        Dim vchrBldKeyC1 As String
        Dim docId As String
        Dim invNo As String
        Dim invDt As String
        Dim invCurr As String
        Dim invSub As Double
        Dim invGross As Double
        Dim invOrigin As String
        Dim busUnit As String
        Dim captureType As String
        Dim vendorId As String
        Dim vendorNm As String
        Dim freight As Double
        Dim salesTax As Double
        Dim vchrId As String
        Dim itemDesc As String
        Dim itemId As String
        Dim poBusUnit As String
        Dim vchrBusUnit As String
        Dim UOM As String
        Dim GLAmt As Double
        Dim GLCode As String
        Dim unitPrice As Double
        Dim vchrPONo As String
        Dim vchrQty As Double
        Dim apxPONo As String
        Dim add1 As String
        Dim add2 As String
        Dim add3 As String
        Dim city As String
        Dim state As String
        Dim postal As String
        Dim country As String
        Dim filePath As String
        Dim i As Integer

        'connectionString = ("Provider=MSDAORA.1;Password=einternet;User ID=einternet;Data Source=devl")
        'connectionString = ("Provider=MSDAORA.1;Password=einternet;User ID=einternet;Data Source=rptg")
        connectionString = ("Provider=MSDAORA.1;Password=einternet;User ID=einternet;Data Source=prod")

        'get header data
        hdrSql = "SELECT UNIQUE(SYSADM8.PS_APXVCHR_HDR_STG.INVOICE_ID), TO_CHAR(SYSADM8.PS_APXVCHR_HDR_STG.INVOICE_DT, 'MM/DD/YYYY'), " & _
                    "SYSADM8.PS_APXVCHR_HDR_STG.TXN_CURRENCY_CD, SYSADM8.PS_APXVCHR_HDR_STG.VENDOR_ID, " & _
                    "SYSADM8.PS_APXVCHR_HDR_STG.VCHR_BLD_KEY_N1, SYSADM8.PS_APXVCHR_HDR_STG.VOUCHER_ID, " & _
                    "SYSADM8.PS_APXVCHR_HDR_STG.ORIGIN , SYSADM8.PS_APXVCHR_HDR_STG.BUSINESS_UNIT, " & _
                    "SYSADM8.PS_APXVCHR_HDR_STG.GROSS_AMT, SYSADM8.PS_VENDOR.NAME1, " & _
                    "SYSADM8.PS_APXVCHR_HDR_STG.SALETX_AMT, SYSADM8.PS_APXVCHR_HDR_STG.FREIGHT_AMT, " & _
                    "SYSADM8.PS_APXVCHR_HDR_STG.VCHR_BLD_KEY_C1 " & _
                    "FROM SYSADM8.PS_APXVCHR_HDR_STG, SYSADM8.PS_VENDOR " & _
                    "WHERE PS_APXVCHR_HDR_STG.VENDOR_ID = PS_VENDOR.VENDOR_ID(+) " & _
                    "AND SYSADM8.PS_APXVCHR_HDR_STG.USER_HDR_CHAR1 = ' '" '& _
        '"AND SYSADM8.PS_APXVCHR_HDR_STG.VCHR_BLD_KEY_N1 IN ('1208100') AND SYSADM8.PS_APXVCHR_HDR_STG.VCHR_BLD_KEY_C1 = 'VOUCHER-IN2'"




        oledbc = New OleDbConnection(connectionString)

        hdrInfo = New DataSet("aps_invoice")

        Try
            oledbc.Open()
            oledbCmd = New OleDbCommand(hdrSql, oledbc)


            oler = oledbCmd.ExecuteReader()


            While oler.Read
                '  filePath = "c:\new\" & Replace(oler.Item(0).ToString, "/", "") & ".xml"
                filePath = "\\APWEB.isacs.com\APxImport\" & Replace(Replace(oler.Item(0).ToString, "/", ""), "*", "") & ".xml"
                'filePath = "\\apdemo.isacs.com\APxImport\" & Replace(oler.Item(0).ToString,"/","") & ".xml"

                Try
                    vchrBldKey = UCase(oler.Item(4).ToString)
                    vchrBldKeyC1 = UCase(oler.Item(12).ToString)

                    If String.IsNullOrEmpty(oler.Item(9).ToString) Then
                        Throw (New VendorException("Vendor Not Found"))
                    End If

                    'create XML document at specified file path
                    hdrInfo.WriteXml(filePath)

                    Dim x As New XmlTextWriter(filePath, System.Text.Encoding.UTF8)
                    x.WriteStartDocument(True)
                    x.Formatting = Formatting.Indented
                    x.Indentation = 4
                    x.WriteStartElement("aps_invoice")


                    invNo = UCase(oler.Item(0).ToString)
                    invDt = UCase(oler.Item(1).ToString)
                    invCurr = UCase(oler.Item(2).ToString)
                    vendorId = UCase(oler.Item(3).ToString)
                    vchrBldKey = UCase(oler.Item(4).ToString)
                    vchrId = UCase(oler.Item(5).ToString)
                    invOrigin = UCase(oler.Item(6).ToString)
                    busUnit = UCase(oler.Item(7).ToString)
                    invGross = Convert.ToDouble(oler.Item(8).ToString)
                    vendorNm = UCase(oler.Item(9).ToString)
                    salesTax = Convert.ToDouble(oler.Item(10).ToString)
                    freight = Convert.ToDouble(oler.Item(11).ToString)
                    vchrBldKeyC1 = UCase(oler.Item(12).ToString)
                    vchrPONo = ""


                    If busUnit = "ISA00" Then salesTax = 0 'Sales tax not paid for ISA invoices
                    invSub = invGross - freight - salesTax
                    If invNo = " " Then invNo = ""
                    If invDt = " " Then invDt = ""
                    If invCurr = " " Then invCurr = "USD"
                    If vendorId = " " Then vendorId = ""
                    If vchrBldKey = " " Then vchrBldKey = ""
                    If vchrId = " " Then vchrId = ""
                    If invOrigin = " " Then invOrigin = ""
                    If busUnit = " " Then busUnit = ""
                    If vendorNm = " " Then vendorNm = ""
                    If vchrBldKey = " " Then vchrBldKey = ""


                    If invGross < 0 Then
                        captureType = "CR"
                    Else
                        captureType = "DR"
                    End If

                    poSql = "SELECT DISTINCT SYSADM8.PS_APXVCHRLINE_STG.PO_ID, SYSADM8.PS_APXVCHRLINE_STG.BUSINESS_UNIT_PO, SYSADM8.PS_APXVCHRLINE_STG.BUSINESS_UNIT " & _
                      "FROM SYSADM8.PS_APXVCHRLINE_STG " & _
                      "WHERE SYSADM8.PS_APXVCHRLINE_STG.VCHR_BLD_KEY_N1 = " & vchrBldKey & _
                      " AND SYSADM8.PS_APXVCHRLINE_STG.VCHR_BLD_KEY_C1 = '" & vchrBldKeyC1 & "'" & _
                      " AND SYSADM8.PS_APXVCHRLINE_STG.PO_ID <> ' '"

                    oledbc2 = New OleDbConnection(connectionString)
                    oledbc2.Open()
                    oledbCmd2 = New OleDbCommand(poSql, oledbc2)
                    oler2 = oledbCmd2.ExecuteReader

                    While oler2.Read
                        vchrPONo = oler2.Item(0).ToString

                        '							If Left(vchrPONo,1) <> "C" And Left(vchrPONo, 3) <> "UPE" And vchrPONo <> "" Then
                        '								vchrPONo = UCase(Right("0000000000" & vchrPONo,10))
                        '							End If

                        'Only left-pad POs that do not begin with an alpha character
                        If Regex.IsMatch(Left(vchrPONo, 1), "[a-zA-Z]") = False Then
                            vchrPONo = UCase(Right("0000000000" & vchrPONo, 10))
                        End If

                        poBusUnit = UCase(oler2.Item(1).ToString)
                        vchrBusUnit = UCase(oler2.Item(2).ToString)

                        If String.Compare(poBusUnit, busUnit) <> 0 Then
                            busUnit = poBusUnit
                        End If
                    End While

                    apxPONo = busUnit & UCase(vchrPONo) 'create APx PO No	

                    oler2.Close()
                    oledbCmd2.Dispose()
                    oledbc2.Close()


                    'get line-level detail
                    lineSql = "SELECT SYSADM8.PS_APXVCHRLINE_STG.PO_ID, SYSADM8.PS_APXVCHRLINE_STG.DESCR, SYSADM8.PS_APXVCHRLINE_STG.INV_ITEM_ID, SYSADM8.PS_APXVCHRLINE_STG.QTY_VCHR, " & _
                        "SYSADM8.PS_APXVCHRLINE_STG.UNIT_OF_MEASURE, SYSADM8.PS_APXVCHRLINE_STG.UNIT_PRICE, SYSADM8.PS_APXVCHRLINE_STG.MERCHANDISE_AMT, " & _
                        "SYSADM8.PS_APXVCHRLINE_STG.VCHR_BLD_KEY_N1, SYSADM8.PS_APXVCHRLINE_STG.BUSINESS_UNIT, SYSADM8.PS_APXVCHRLINE_STG.BUSINESS_UNIT_PO, " & _
                        "SYSADM.PS_LOCATION_TBL.ADDRESS1, SYSADM.PS_LOCATION_TBL.ADDRESS2, SYSADM.PS_LOCATION_TBL.ADDRESS3,  " & _
                        "SYSADM.PS_LOCATION_TBL.CITY, SYSADM.PS_LOCATION_TBL.STATE, SYSADM.PS_LOCATION_TBL.POSTAL, SYSADM.PS_LOCATION_TBL.COUNTRY, SYSADM8.PS_APXVCHRDIST_STG.ACCOUNT " & _
                        "FROM SYSADM8.PS_APXVCHRLINE_STG " & _
                        "LEFT JOIN SYSADM.PS_PO_LINE_SHIP ON SYSADM8.PS_APXVCHRLINE_STG.BUSINESS_UNIT_PO = SYSADM.PS_PO_LINE_SHIP.BUSINESS_UNIT AND SYSADM8.PS_APXVCHRLINE_STG.PO_ID = SYSADM.PS_PO_LINE_SHIP.PO_ID AND SYSADM8.PS_APXVCHRLINE_STG.LINE_NBR = SYSADM.PS_PO_LINE_SHIP.LINE_NBR " & _
                        "LEFT JOIN SYSADM8.PS_APXVCHRDIST_STG ON SYSADM8.PS_APXVCHRLINE_STG.VCHR_BLD_KEY_N1 = SYSADM8.PS_APXVCHRDIST_STG.VCHR_BLD_KEY_N1 " & _
                        "AND SYSADM8.PS_APXVCHRLINE_STG.VOUCHER_LINE_NUM = SYSADM8.PS_APXVCHRDIST_STG.VOUCHER_LINE_NUM " & _
                        "AND SYSADM8.PS_APXVCHRLINE_STG.VCHR_BLD_KEY_C1 = SYSADM8.PS_APXVCHRDIST_STG.VCHR_BLD_KEY_C1 " & _
                        "LEFT JOIN SYSADM.PS_LOCATION_TBL ON SYSADM.PS_PO_LINE_SHIP.SHIPTO_ID = SYSADM.PS_LOCATION_TBL.LOCATION " & _
                        "WHERE SYSADM8.PS_APXVCHRLINE_STG.VCHR_BLD_KEY_N1 = " & vchrBldKey & _
                        " AND SYSADM8.PS_APXVCHRDIST_STG.VCHR_BLD_KEY_C1 = '" & vchrBldKeyC1 & "'" & _
                        " AND ((SYSADM.PS_LOCATION_TBL.EFFDT = (SELECT MAX(A.EFFDT) FROM SYSADM.PS_LOCATION_TBL A WHERE SYSADM.PS_LOCATION_TBL.LOCATION = A.LOCATION) " & _
                        "AND SYSADM.PS_LOCATION_TBL.EFFDT <= SYSDATE) OR SYSADM.PS_LOCATION_TBL.EFFDT IS NULL)" & _
                        " ORDER BY 11"



                    oledbc2 = New OleDbConnection(connectionString)
                    oledbc2.Open()
                    oledbCmd2 = New OleDbCommand(lineSql, oledbc2)
                    oler2 = oledbCmd2.ExecuteReader

                    'create XML node for each existing line
                    i = 0
                    Try

                        While oler2.Read
                            i += 1

                            If i = 1 Then
                                add1 = oler2.Item(10).ToString
                                add2 = oler2.Item(11).ToString
                                add3 = oler2.Item(12).ToString
                                city = oler2.Item(13).ToString
                                state = oler2.Item(14).ToString
                                postal = oler2.Item(15).ToString
                                country = oler2.Item(16).ToString

                                If vchrBldKeyC1 = "VOUCHER-IN" Then 'create unique key
                                    docId = "IN" & vchrBldKey
                                Else
                                    docId = "IN2" & vchrBldKey
                                End If

                                'Write XML header
                                createNode(invNo, invDt, invCurr, vendorId, vchrId, freight.ToString, salesTax.ToString, invOrigin, busUnit, invSub.ToString, invGross.ToString, vendorNm, docId, captureType, add1, add2, add3, city, state, postal, country, x)

                                'Write XML PO info
                                createNodePO(apxPONo, x)
                            End If


                            itemDesc = UCase(oler2.Item(1).ToString)
                            itemId = UCase(oler2.Item(2).ToString)
                            vchrQty = Convert.ToDouble(oler2.Item(3).ToString)
                            UOM = UCase(oler2.Item(4).ToString)
                            unitPrice = Convert.ToDouble(oler2.Item(5).ToString)
                            GLAmt = Convert.ToDouble(oler2.Item(6).ToString)
                            GLCode = oler2.Item(17).ToString

                            If vchrQty = 0 Then
                                vchrQty = 1
                            End If

                            If unitPrice = 0 And GLAmt <> 0 Then
                                unitPrice = GLAmt
                            End If

                            If String.IsNullOrEmpty(UOM.Trim) Then
                                UOM = "EA"
                            End If

                            If String.IsNullOrEmpty(GLCode.ToString.Trim) Then
                                GLAmt = Nothing
                            End If

                            'Write XML detail lines
                            createNodeDetail(apxPONo, itemDesc, itemId, vchrQty.ToString, UOM, unitPrice.ToString, GLAmt, busUnit, invDt, x, GLCode)

                        End While
                        oler2.Close()
                        oledbCmd2.Dispose()
                        oledbc2.Close()
                        x.Close()

                        If i = 0 Then
                            Throw (New VendorException("No invoice lines"))
                        End If
                        'update flag to processed
                        updateSql = "UPDATE SYSADM8.ps_apxvchr_hdr_stg SET user_hdr_char1 = 'Y' WHERE VCHR_BLD_KEY_N1 =" & vchrBldKey
                        updateSql = updateSql & " AND VCHR_BLD_KEY_C1 = '" & vchrBldKeyC1 & "'"
                        oledbCmd2 = New OleDbCommand(updateSql, oledbc)
                        oledbCmd2.ExecuteNonQuery()
                        oledbCmd2.Dispose()
                    Catch ex As Exception
                        updateSql = "UPDATE SYSADM8.ps_apxvchr_hdr_stg SET user_hdr_char1 = 'L' WHERE VCHR_BLD_KEY_N1 =" & vchrBldKey
                        updateSql = updateSql & " AND VCHR_BLD_KEY_C1 = '" & vchrBldKeyC1 & "'"
                        Debug.WriteLine(updateSql)
                        Debug.WriteLine("")
                        oledbCmd2 = New OleDbCommand(updateSql, oledbc)
                        oledbCmd2.ExecuteNonQuery()
                        oledbCmd2.Dispose()
                        Debug.WriteLine("Exception Message: " & ex.Message & " Invoice number " & invNo)
                    End Try
                Catch ex As Exception
                    updateSql = "UPDATE SYSADM8.ps_apxvchr_hdr_stg SET user_hdr_char1 = 'E' WHERE VCHR_BLD_KEY_N1 =" & vchrBldKey
                    updateSql = updateSql & " AND VCHR_BLD_KEY_C1 = '" & vchrBldKeyC1 & "'"
                    Debug.WriteLine(updateSql)
                    Debug.WriteLine("")
                    oledbCmd2 = New OleDbCommand(updateSql, oledbc)
                    oledbCmd2.ExecuteNonQuery()
                    oledbCmd2.Dispose()
                    Debug.WriteLine("Exception Message: " & ex.Message & " Invoice number " & invNo)
                End Try
            End While

            oler.Close()
            oledbCmd.Dispose()
            oledbc.Close()

        Catch ex As Exception
            Debug.WriteLine("Exception Message: " & ex.Message)
        End Try

    End Sub


    Private Sub createNode(ByVal invNo As String, ByVal invDt As String, ByVal invCurr As String, ByVal vendorId As String, ByVal vchrId As String, ByVal freight As String, ByVal salesTax As String, ByVal invOrigin As String, ByVal busUnit As String, ByVal invSub As String, ByVal invGross As String, ByVal vendorNm As String, ByVal vchrBldKey As String, captureType As String, ByVal add1 As String, ByVal add2 As String, ByVal add3 As String, ByVal city As String, ByVal state As String, ByVal postal As String, ByVal country As String, ByVal x As XmlTextWriter)
        x.WriteStartElement("captureDocID")
        x.WriteString(vchrBldKey)
        x.WriteEndElement()
        x.WriteStartElement("invoiceNumber")
        x.WriteString(invNo)
        x.WriteEndElement()
        x.WriteStartElement("invoiceDate")
        x.WriteString(invDt)
        x.WriteEndElement()
        x.WriteStartElement("shippingFreightAmount")
        x.WriteString(freight)
        x.WriteEndElement()
        x.WriteStartElement("taxAmount")
        x.WriteString(salesTax)
        x.WriteEndElement()
        x.WriteStartElement("invoiceCurrency")
        x.WriteString(invCurr)
        x.WriteEndElement()
        x.WriteStartElement("invoiceSubtotal")
        x.WriteString(invSub)
        x.WriteEndElement()
        x.WriteStartElement("invoiceTotal")
        x.WriteValue(invGross)
        x.WriteEndElement()
        x.WriteStartElement("captureDocType")
        x.WriteString(captureType)
        x.WriteEndElement()
        x.WriteStartElement("vendorID")
        x.WriteString(vendorId)
        x.WriteEndElement()
        x.WriteStartElement("vendorName")
        x.WriteString(vendorNm)
        x.WriteEndElement()
        x.WriteStartElement("shipAddress1")
        x.WriteString(add1)
        x.WriteEndElement()
        x.WriteStartElement("shipAddress2")
        x.WriteString(add2)
        x.WriteEndElement()
        x.WriteStartElement("shipAddress3")
        x.WriteString(add3)
        x.WriteEndElement()
        x.WriteStartElement("shipCity")
        x.WriteString(city)
        x.WriteEndElement()
        x.WriteStartElement("shipPOBox")
        x.WriteString("")
        x.WriteEndElement()
        x.WriteStartElement("shipState")
        x.WriteString(state)
        x.WriteEndElement()
        x.WriteStartElement("shipZip")
        x.WriteString(postal)
        x.WriteEndElement()
        x.WriteStartElement("shipCountry")
        x.WriteString(country)
        x.WriteEndElement()
        x.WriteStartElement("customAtt1")
        x.WriteString(vchrId)
        x.WriteEndElement()
        x.WriteStartElement("customAtt2")
        x.WriteString(invOrigin)
        x.WriteEndElement()
        x.WriteStartElement("companyCode")
        x.WriteString(busUnit) '
        x.WriteEndElement()

    End Sub

    Private Sub createNodeDetail(ByVal apxPONo As String, ByVal itemDesc As String, ByVal itemId As String, ByVal invQty As String, ByVal UOM As String, ByVal unitPrice As String, ByVal GLAmt As Double, ByVal invoiceBusUnit As String, ByVal invoiceDt1 As String, ByVal x As XmlTextWriter, ByVal glCode As String)
        x.WriteStartElement("detailLine")
        x.WriteStartElement("description")
        x.WriteString(itemDesc)
        x.WriteEndElement()
        x.WriteStartElement("itemNumber")
        x.WriteString(itemId)
        x.WriteEndElement()
        x.WriteStartElement("quantity")
        x.WriteString(invQty)
        x.WriteEndElement()
        x.WriteStartElement("unitOfMeasure")
        x.WriteString(UOM)
        x.WriteEndElement()

        '
        'GL coding from apx staging tables causes duplicate lines to be exported. Do not uncomment.
        '
        '        If GLAmt > 0 Or GLAmt < 0 Then
        '            x.WriteStartElement("unitPrice")
        '            x.WriteString(unitPrice)
        '            x.WriteEndElement()
        '            x.WriteStartElement("glCode")
        '            x.WriteString(glCode)
        '            x.WriteEndElement()
        '            x.WriteStartElement("glAmount")
        '            x.WriteString(GLAmt.ToString)
        '            x.WriteEndElement()
        '        Else
        x.WriteStartElement("unitPrice")
        x.WriteString(unitPrice)
        x.WriteEndElement()
        '        End If
        x.WriteEndElement()
    End Sub

    Private Sub createNodePO(ByVal apxPONo As String, ByVal x As XmlTextWriter)
        x.WriteStartElement("poNumbers")
        x.WriteStartElement("poNumber")
        x.WriteString(apxPONo)
        x.WriteEndElement()
        x.WriteEndElement()
        x.WriteStartElement("detailLines") 'opening for next tree
    End Sub

    Public Class VendorException : Inherits ApplicationException
        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub
    End Class

End Module