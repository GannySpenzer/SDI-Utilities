Imports System.Xml
Imports System.IO
Imports System.Data.OleDb
Imports System.Web.Mail
Imports System.Web.UI
Imports System.Text
Imports System.Configuration
Imports System.Linq
Imports System.Collections.Generic
Imports System.Drawing.Color
Imports System.Net
Imports Newtonsoft.Json
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Threading.Tasks
Imports System.Web.Script.Serialization
Imports System.Web
Imports System.Globalization

Module Module1

    Dim objStreamWriter As StreamWriter
    Dim objGenerallLogStreamWriter As StreamWriter
    Dim objWalmartSC As StreamWriter
    Dim objWalSCComments As StreamWriter
    Dim rootDir As String = ConfigurationManager.AppSettings("rootDir")
    Dim logpath As String = ConfigurationManager.AppSettings("logpath") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim Generallogpath As String = ConfigurationManager.AppSettings("Generallogpath") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim WalmartSC As String = ConfigurationManager.AppSettings("WalmartSC") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim WalmartSCComments As String = ConfigurationManager.AppSettings("WalmartSCComments") & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim connectOR As New OleDbConnection(Convert.ToString(ConfigurationManager.AppSettings("OLEDBconString")))
    'Added timings for order status summary email--  WW-287 & WAL-632 Poornima S
    Dim SumryStartLngTime As String = ConfigurationManager.AppSettings("SummaryStartLongTime")
    Dim SumryEndLngTime As String = ConfigurationManager.AppSettings("SummaryEndLongTime")
    Dim SumryMailTime As String = ConfigurationManager.AppSettings("SummaryMailTime")
    Dim ImageUrl As String = ConfigurationManager.AppSettings("Image")

    Sub Main()

        Console.WriteLine("Start StatChg Email send")
        Console.WriteLine("")

        objStreamWriter = File.CreateText(logpath)
        objStreamWriter.WriteLine("  Send emails out " & Now())

        objGenerallLogStreamWriter = File.CreateText(Generallogpath)
        objGenerallLogStreamWriter.WriteLine("Started Writing the Logs " & Now())

        objWalmartSC = File.CreateText(WalmartSC)
        objWalmartSC.WriteLine("Started Walmart Service Channel " & Now())

        objWalSCComments = File.CreateText(WalmartSCComments)
        objWalSCComments.WriteLine("Start Walmart Service Channel Comments " & Now())

        'GetNotes() 'Mythili - WAL-534, Updating all the supplier portal and expedite comments to service channel

        Dim bolError As Boolean = buildstatchgout()

        If bolError = True Then
            SendEmail()
        End If


        objStreamWriter.WriteLine("  End of StatChg Email send " & Now())
        objGenerallLogStreamWriter.WriteLine("Ends " & Now())
        objWalmartSC.WriteLine("Ends " & Now())
        objWalSCComments.WriteLine("Ends " & Now())

        objStreamWriter.Flush()
        objStreamWriter.Close()
        objGenerallLogStreamWriter.Flush()
        objGenerallLogStreamWriter.Close()

        objWalmartSC.Flush()
        objWalmartSC.Close()

        objWalSCComments.Flush()
        objWalSCComments.Close()

    End Sub

    Private Function buildstatchgout() As Boolean
        ' get XML file of sites that require email
        Dim strXMLDir As String = rootDir & "\EmailSites.xml"
        Dim xmldata As New XmlDocument
        Dim sr As System.IO.StreamReader
        Dim XMLContent As String
        Dim jobNode As XmlNode
        sr = New System.IO.StreamReader(strXMLDir)
        XMLContent = sr.ReadToEnd()
        sr.Close()
        xmldata.LoadXml(XMLContent)
        Dim jj As XmlNode = xmldata.ChildNodes(2)
        jobNode = xmldata.ChildNodes(1)
        Dim dsRows As New DataSet
        dsRows.ReadXml(New XmlNodeReader(jobNode))

        Dim I As Integer
        Dim bolErrorSomeWhere As Boolean

        Dim connectionString As String = ConfigurationManager.AppSettings("OLEDBconString")
        connectOR = New OleDbConnection(connectionString)


        ' check stock
        For I = 0 To dsRows.Tables(0).Rows.Count - 1
            If dsRows.Tables(0).Rows(I).Item("SITESTK") = "Y" Then
                Console.WriteLine(Convert.ToString(I + 1) + ".Verifying/Updating StatChg Email for Stock - BU: " + Convert.ToString(dsRows.Tables(0).Rows(I).Item("SITEBU")) + "")
                objGenerallLogStreamWriter.WriteLine(Convert.ToString(I + 1) + ".Verifying/Updating StatChg Email for Stock - BU: " + Convert.ToString(dsRows.Tables(0).Rows(I).Item("SITEBU")) + " " & Now())
                objStreamWriter.WriteLine("--------------------------------------------------------------------------------------")
                objStreamWriter.WriteLine("  StatChg Email send stock emails for " & dsRows.Tables(0).Rows(I).Item("SITEBU") & " " & Now())
                buildstatchgout = checkStock(dsRows.Tables(0).Rows(I).Item("SITEBU"), dsRows.Tables(0).Rows(I).Item("SITESTART"))
                'buildstatchgout = False
                If buildstatchgout = True Then
                    bolErrorSomeWhere = True
                End If
            End If
        Next

        ' check non-stock
        For I = 0 To dsRows.Tables(0).Rows.Count - 1
            If dsRows.Tables(0).Rows(I).Item("SITENSTK") = "Y" Then
                Console.WriteLine(Convert.ToString(I + 1) + ".Verifying/Updating StatChg Email for Non-Stock - BU: " + Convert.ToString(dsRows.Tables(0).Rows(I).Item("SITEBU")) + "")
                objGenerallLogStreamWriter.WriteLine(Convert.ToString(I + 1) + ".Verifying/Updating StatChg Email for Non-Stock - BU: " + Convert.ToString(dsRows.Tables(0).Rows(I).Item("SITEBU")) + " " & Now())
                objStreamWriter.WriteLine("--------------------------------------------------------------------------------------")
                objStreamWriter.WriteLine("  StatChg Email send nonstock emails for " & dsRows.Tables(0).Rows(I).Item("SITEBU") & " " & Now())
                buildstatchgout = checkNonStock(dsRows.Tables(0).Rows(I).Item("SITEBU"), dsRows.Tables(0).Rows(I).Item("SITESTART"))
                If buildstatchgout = True Then
                    bolErrorSomeWhere = True
                End If
            End If
        Next


        For I = 0 To dsRows.Tables(0).Rows.Count - 1
            Console.WriteLine(Convert.ToString(I + 1) + ".Order Status Email Completed for BU: " + Convert.ToString(dsRows.Tables(0).Rows(I).Item("SITEBU")) + "")
            objGenerallLogStreamWriter.WriteLine(Convert.ToString(I + 1) + ".Order Status Email Completed for BU: " + Convert.ToString(dsRows.Tables(0).Rows(I).Item("SITEBU")) + " " & Now())
            objStreamWriter.WriteLine("--------------------------------------------------------------------------------------")
            objStreamWriter.WriteLine("  StatChg Email send allstatus emails for XML Site : " & dsRows.Tables(0).Rows(I).Item("SITEBU") & " " & Now())
            buildstatchgout = checkAllStatus_7(dsRows.Tables(0).Rows(I).Item("SITEBU"), dsRows.Tables(0).Rows(I).Item("SITESTART"))
            If buildstatchgout = True Then
                bolErrorSomeWhere = True
            End If
        Next


        '' Blocked the email sending for the checkStock,checkNonStock to all sites.It's only for the XML Sites
        Dim dsBU As DataSet
        dsBU = GetBU()

        If Not dsBU Is Nothing Then
            objGenerallLogStreamWriter.WriteLine("Total BU going to Process " + Convert.ToString(dsBU.Tables(0).Rows.Count()))

            Console.WriteLine("----------------------------------- New Profile Status Emails ------------------------------------------------------------")
            objGenerallLogStreamWriter.WriteLine("-------------------------------------------------------------------------------")
            For I = 0 To dsBU.Tables(0).Rows.Count - 1

                Dim time As DateTime = DateTime.Now
                Dim format As String = "HH:mm:ss"
                Dim TimeNow As String = time.ToString(format)

                'Added checkAllStatusWAL() by checking I0W01 BU, Email time for order status summary email--  WW-287 & WAL-632 Poornima S
                'SP-316 EMCOR order status summary- Dhamotharan
                'If (dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT") = "I0W01" Or dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT") = "I0631") And ((TimeNow > SumryStartLngTime) And (TimeNow < SumryEndLngTime)) Then
                '    Try
                '        Console.WriteLine(Convert.ToString(I + 1) + ".Order Status Email Completed for BU: " + Convert.ToString(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT")) + "")
                '        objGenerallLogStreamWriter.WriteLine(Convert.ToString(I + 1) + ".Order Status Email Completed for BU: " + Convert.ToString(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT")) + " " & Now())
                '        objStreamWriter.WriteLine("--------------------------------------------------------------------------------------")
                '        objStreamWriter.WriteLine("  StatChg Email send allstatus emails for Enterprise BU : " & dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT") & " " & Now())
                '        buildstatchgout = checkAllStatusWAL(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT"))
                '    Catch ex As Exception

                '    End Try

                'End If
                Console.WriteLine(Convert.ToString(I + 1) + ".Order Status Email Completed for BU: " + Convert.ToString(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT")) + "")
                objGenerallLogStreamWriter.WriteLine(Convert.ToString(I + 1) + ".Order Status Email Completed for BU: " + Convert.ToString(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT")) + " " & Now())
                objStreamWriter.WriteLine("--------------------------------------------------------------------------------------")
                objStreamWriter.WriteLine("  StatChg Email send allstatus emails for Enterprise BU : " & dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT") & " " & Now())
                buildstatchgout = checkAllStatusNew(dsBU.Tables(0).Rows(I).Item("BUSINESS_UNIT"))

                If buildstatchgout = True Then
                    bolErrorSomeWhere = True
                End If
            Next
        Else

        End If
        '7 is stock
        'R is non-stock
        objGenerallLogStreamWriter.WriteLine("-------------------------------------------------------------------------------")
        objGenerallLogStreamWriter.WriteLine("Start buildNotifyReceiver for Stock " & Now())
        bolErrorSomeWhere = buildNotifyReceiver("7")
        objGenerallLogStreamWriter.WriteLine("End buildNotifyReceiver for Stock " & Now())
        objGenerallLogStreamWriter.WriteLine("-------------------------------------------------------------------------------")
        objGenerallLogStreamWriter.WriteLine("Start buildNotifyReceiver for Non-Stock " & Now())
        bolErrorSomeWhere = buildNotifyReceiver("R")
        objGenerallLogStreamWriter.WriteLine("End buildNotifyReceiver for Non-Stock " & Now())
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try

        Return bolErrorSomeWhere

    End Function

    Private Function GetBU() As DataSet
        Dim ds As System.Data.DataSet = New System.Data.DataSet
        Try
            '' To get teh list of BU if the privilage was set to any site
            Dim getBuQuery As String = "SELECT DISTINCT(E.ISA_BUSINESS_UNIT) AS BUSINESS_UNIT from PS_ISA_USERS_PRIVS P,PS_ISA_ENTERPRISE E  where E.ISA_BUSINESS_UNIT = P.BUSINESS_UNIT AND " & vbCrLf &
                "P.ISA_IOL_OP_NAME in ('EMAILCRE01','EMAILQTW02','EMAILQTC03','EMAILQTS04','EMAILCST05','EMAILVND06','EMAILAPR07','EMAILQTA08'," & vbCrLf &
                "'EMAILQTR09','EMAILRFA10','EMAILRFR11','EMAILRFC12','EMAILRCF13','EMAILRCP14','EMAILCNC15','EMAILDLF16','EMAILPKA17','EMAILASN18')"

            Dim Command As OleDbCommand = New OleDbCommand(getBuQuery, connectOR)
            If connectOR.State = ConnectionState.Open Then
                'do nothing
            Else
                connectOR.Open()
            End If

            Dim dataAdapter As OleDbDataAdapter =
                        New OleDbDataAdapter(Command)
            Try
                dataAdapter.Fill(ds)
                connectOR.Close()
            Catch ex As Exception

            End Try
            If Not ds Is Nothing Then
                If ds.Tables(0).Rows.Count() > 0 Then
                    Return ds
                Else
                    Return Nothing
                End If
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Function checkNonStock(ByVal strBU As String, ByVal dtrStartDate As String) As Boolean

        Dim dteStrDate As DateTime
        Dim dteEndDate As DateTime

        dteStrDate = Now.AddDays(-1).ToString("MM/dd/yyyy")
        'dteStrDate = Today.AddHours(-12)
        'dteEndDate = Now
        'we could run this twice a day noon and midnight.

        Dim strSiteBU As String
        Dim strSQLstring As String
        Dim Command As OleDbCommand

        strSQLstring = "SELECT /*+ USE_NL(A B C D E) */ A.ORDER_NO, B.ISA_INTFC_LN AS LINE_NBR, E.RECEIVER_ID," & vbCrLf &
                " E.RECV_LN_NBR, A.BUSINESS_UNIT_OM, B.ISA_EMPLOYEE_ID AS EMPLID, E.DESCR254_MIXED, A.Origin" & vbCrLf &
                " FROM PS_ISA_ORD_INTF_HD A, PS_ISA_ORD_INTF_LN B," & vbCrLf &
                " PS_ISA_USERS_TBL C, PS_PO_LINE_DISTRIB D," & vbCrLf &
                " PS_RECV_LN_SHIP E" & vbCrLf &
                " WHERE A.BUSINESS_UNIT_OM = '" & strBU & "'" & vbCrLf &
                " AND A.ADD_DTTM > TO_DATE('" & dteStrDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                " AND A.ADD_DTTM > TO_DATE('" & dtrStartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                " AND A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                " AND A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                " AND A.BUSINESS_UNIT_OM = C.BUSINESS_UNIT" & vbCrLf &
                " AND upper(B.ISA_EMPLOYEE_ID) = upper(C.ISA_EMPLOYEE_ID)" & vbCrLf &
                " AND A.ORDER_NO = D.REQ_ID" & vbCrLf &
                " AND B.ISA_INTFC_LN = D.REQ_LINE_NBR" & vbCrLf &
                " AND D.BUSINESS_UNIT = E.BUSINESS_UNIT" & vbCrLf &
                " AND D.PO_ID = E.PO_ID" & vbCrLf &
                " AND D.LINE_NBR = E.LINE_NBR" & vbCrLf &
                " AND E.QTY_SH_ACCPT > 0" & vbCrLf &
                " AND NOT EXISTS (SELECT 'X'" & vbCrLf &
                " FROM PS_RTV_LN F" & vbCrLf &
                " WHERE F.BUSINESS_UNIT_PO = D.BUSINESS_UNIT" & vbCrLf &
                " AND F.PO_ID = D.PO_ID" & vbCrLf &
                " AND F.LINE_NBR = D.LINE_NBR" & vbCrLf &
                " AND F.QTY_LN_RETRN_SUOM = E.QTY_SH_ACCPT" & vbCrLf &
                " AND F.RETURN_REASON = 'MTX')" & vbCrLf &
                " AND NOT EXISTS (SELECT /*+ USE_NL(A G) */ 'X'" & vbCrLf &
                " FROM PS_ISA_ORDSTAT_EML G" & vbCrLf &
                " WHERE A.BUSINESS_UNIT_OM = G.BUSINESS_UNIT_OM" & vbCrLf &
                " AND A.ORDER_NO = G.ORDER_NO" & vbCrLf &
                " AND B.ISA_INTFC_LN = G.LINE_NBR" & vbCrLf &
                " AND E.RECEIVER_ID = G.RECEIVER_ID" & vbCrLf &
                " AND E.RECV_LN_NBR = G.RECV_LN_NBR" & vbCrLf &
                " AND G.ISA_LINE_STATUS = 'RET')" & vbCrLf &
                " ORDER BY ORDER_NO, LINE_NBR, RECEIVER_ID, RECV_LN_NBR"

        objStreamWriter.WriteLine("  CheckNonStock (2): " & strSQLstring)

        Command = New OleDbCommand(strSQLstring, connectOR)
        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        Dim dataAdapter As OleDbDataAdapter
        Dim st As New Stopwatch()
        Dim ds As System.Data.DataSet = New System.Data.DataSet
        Dim ts As TimeSpan
        Dim elapsedTime As String
        Try
            st.Start()
            dataAdapter = New OleDbDataAdapter(Command)
            st.Stop()
            ts = st.Elapsed
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
            objStreamWriter.WriteLine("Query Execution Time " + elapsedTime)
        Catch ex As Exception
            dataAdapter = New OleDbDataAdapter(Command)
            objStreamWriter.WriteLine("Query Execution Time " + Now())
        End Try
        Try
            dataAdapter.Fill(ds)
            connectOR.Close()
        Catch ex As Exception
            objStreamWriter.WriteLine("  StatChg Email NSTK send select orders for " & strBU & " " & Now())
            connectOR.Close()
            Return True
        End Try
        ' don't process UNCC status change emails - done in another program UNCCSTATUSCHANGEEMAIL
        If ds.Tables(0).Rows.Count = 0 Or strBU = "I0256" Then
            Console.WriteLine("Fetched Datas:0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas:0")
            objStreamWriter.WriteLine("  StatChg Email NSTK send select orders = 0 for" & strBU & " " & Now())
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try
            Return False
        Else
            Console.WriteLine("Fetched Datas:" + Convert.ToString(ds.Tables(0).Rows.Count()))
            objGenerallLogStreamWriter.WriteLine("Fetched Datas:" + Convert.ToString(ds.Tables(0).Rows.Count()))
        End If

        'insert into the PS_ISA_ORDSTAT_EML table

        Dim I As Integer

        connectOR.Open()
        For I = 0 To ds.Tables(0).Rows.Count - 1
            Dim strBUSINESSUNITOM As String = ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM")
            Dim strORDERNO As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
            Dim strINTFCLINENUM As String = ds.Tables(0).Rows(I).Item("LINE_NBR")
            Dim strRECEIVERID As String = ds.Tables(0).Rows(I).Item("RECEIVER_ID")
            Dim strRECVLNNBR As String = ds.Tables(0).Rows(I).Item("RECV_LN_NBR")
            Dim strEMPLID As String = ds.Tables(0).Rows(I).Item("EMPLID")
            Dim strDesc254 As String = ds.Tables(0).Rows(I).Item("DESCR254_MIXED")
            Dim strorigin As String = ds.Tables(0).Rows(I).Item("Origin")

            Dim ChkExistsLog As Boolean = ChkExistnLog(strBUSINESSUNITOM, strORDERNO, strINTFCLINENUM, "0", "0", strEMPLID, "RET")
            connectOR.Open()

            If Not ChkExistsLog Then
                strSQLstring = "INSERT INTO PS_ISA_ORDSTAT_EML" & vbCrLf &
                        " VALUES ('" & strBUSINESSUNITOM & "'," & vbCrLf &
                        " '" & strORDERNO & "'," & vbCrLf &
                        " '" & strINTFCLINENUM & "'," & vbCrLf &
                        " '0'," & vbCrLf &
                        " '0'," & vbCrLf &
                        " '" & strRECEIVERID & "'," & vbCrLf &
                        " '" & strRECVLNNBR & "'," & vbCrLf &
                        " '" & strEMPLID & "'," & vbCrLf &
                        " 'RET', '')" & vbCrLf


                Dim command1 As OleDbCommand
                command1 = New OleDbCommand(strSQLstring, connectOR)
                Try
                    Dim rowsaffected As Integer
                    rowsaffected = command1.ExecuteNonQuery
                    If Not rowsaffected = 1 Then
                        objStreamWriter.WriteLine("  StatChg Email NSTK send insert orders for " & ds.Tables(0).Rows(I).Item("ORDER_NO") & " " &
                         ds.Tables(0).Rows(I).Item("LINE_NBR") & " " &
                                            ds.Tables(0).Rows(I).Item("RECEIVER_ID") & " " &
                                            ds.Tables(0).Rows(I).Item("RECV_LN_NBR") & " " & Now())
                        checkNonStock = True
                    End If
                    command1.Dispose()
                Catch ex As Exception
                    objStreamWriter.WriteLine("  StatChg Email NSTK send insert error for " & ds.Tables(0).Rows(I).Item("ORDER_NO") & " " &
                        ds.Tables(0).Rows(I).Item("LINE_NBR") & " " &
                        ds.Tables(0).Rows(I).Item("RECEIVER_ID") & " " &
                        ds.Tables(0).Rows(I).Item("RECV_LN_NBR") & " " & Now())
                    objStreamWriter.WriteLine(ex.ToString)
                    checkNonStock = True
                End Try
            Else
                objStreamWriter.WriteLine("  StatChg Email NSTK send insert already exists in PS_ISA_ORDSTAT_EML table for order no " & ds.Tables(0).Rows(I).Item("ORDER_NO") & " " &
                        ds.Tables(0).Rows(I).Item("LINE_NBR") & " " &
                        ds.Tables(0).Rows(I).Item("RECEIVER_ID") & " " &
                        ds.Tables(0).Rows(I).Item("RECV_LN_NBR") & " " & Now())
                checkNonStock = True
            End If
        Next
        objStreamWriter.WriteLine("  StatChg Email NSTK send select orders = " & ds.Tables(0).Rows.Count & " for" & strBU & " " & Now())

        Try
            connectOR.Close()
        Catch ex As Exception

        End Try


    End Function

    Private Function checkStock(ByVal strbu As String, ByVal dtrStartDate As String) As Boolean
        ' the union all in the sql below - diferences between top and bot
        ' bot " AND E.CONFIRMED_FLAG = 'Y'" & vbCrLf & _
        ' 
        Dim dteStrDate As DateTime
        dteStrDate = Now.AddDays(-1).ToString("MM/dd/yyyy")
        Dim strSQLstring As String
        strSQLstring = "SELECT /*+ index(D,PSWIN_DEMAND) */ B.ORDER_NO, B.ISA_INTFC_LN AS INTFC_LINE_NUM, B.ISA_INTFC_LN AS ORDER_INT_LINE_NO, D.DEMAND_LINE_NO," & vbCrLf &
                " B.BUSINESS_UNIT_OM, B.ISA_EMPLOYEE_ID AS EMPLID, E.DESCR60, E.INV_ITEM_ID, A.Origin" & vbCrLf &
                " FROM PS_ISA_ORD_INTF_HD A, PS_ISA_ORD_INTF_LN B," & vbCrLf &
                " PS_ISA_USERS_TBL C, SYSADM8.PS_IN_DEMAND D," & vbCrLf &
                " PS_MASTER_ITEM_TBL E" & vbCrLf &
                " WHERE A.BUSINESS_UNIT_OM = '" & strbu & "'" & vbCrLf &
                " AND A.ADD_DTTM > TO_DATE('" & dteStrDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                " AND A.ADD_DTTM > TO_DATE('" & dtrStartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                " AND A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                " AND A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                " AND B.INV_ITEM_ID <> ' '" & vbCrLf &
                " AND A.BUSINESS_UNIT_OM = C.BUSINESS_UNIT" & vbCrLf &
                " AND UPPER(B.ISA_EMPLOYEE_ID) =UPPER(C.ISA_EMPLOYEE_ID)" & vbCrLf &
                " AND B.ISA_INTFC_LN = D.ORDER_INT_LINE_NO" & vbCrLf &
                " AND B.ORDER_NO = D.ORDER_NO" & vbCrLf &
                " AND E.SETID = 'MAIN1'" & vbCrLf &
                " AND D.INV_ITEM_ID = E.INV_ITEM_ID" & vbCrLf &
                " AND D.IN_FULFILL_STATE IN ('60','50')" & vbCrLf &
                " AND D.DEMAND_SOURCE = 'OM'" & vbCrLf &
                " AND D.QTY_PICKED > 0" & vbCrLf &
                " AND NOT EXISTS (SELECT 'X'" & vbCrLf &
                " FROM PS_ISA_ORDSTAT_EML F" & vbCrLf &
                " WHERE F.BUSINESS_UNIT_OM = D.SOURCE_BUS_UNIT" & vbCrLf &
                " AND F.ORDER_NO = D.ORDER_NO" & vbCrLf &
                " AND F.DEMAND_LINE_NO = D.DEMAND_LINE_NO" & vbCrLf &
                " AND F.ORDER_INT_LINE_NO = D.ORDER_INT_LINE_NO" & vbCrLf &
                " AND F.ISA_LINE_STATUS IN ('PKP', 'PKF'))" & vbCrLf &
                " ORDER BY ORDER_NO, INTFC_LINE_NUM, DEMAND_LINE_NO"

        objStreamWriter.WriteLine("  CheckStock: " & strSQLstring)

        Dim Command As OleDbCommand = New OleDbCommand(strSQLstring, connectOR)
        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If

        Dim dataAdapter As OleDbDataAdapter
        Dim st As New Stopwatch()
        Dim ds As System.Data.DataSet = New System.Data.DataSet
        Dim ts As TimeSpan
        Dim elapsedTime As String
        Try
            st.Start()
            dataAdapter = New OleDbDataAdapter(Command)
            st.Stop()
            ts = st.Elapsed
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
            objStreamWriter.WriteLine("Query Execution Time " + elapsedTime)
        Catch ex As Exception
            dataAdapter = New OleDbDataAdapter(Command)
            objStreamWriter.WriteLine("Query Execution Time " + Now())
        End Try
        Try
            dataAdapter.Fill(ds)
            connectOR.Close()
        Catch ex As Exception
            objStreamWriter.WriteLine("Error in StatChg Email send select orders for " & strbu & " " & Now())
            connectOR.Close()
            Return True
        End Try

        If ds.Tables(0).Rows.Count = 0 Then
            Console.WriteLine("Fetched Datas:0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas:0")
            objStreamWriter.WriteLine("  StatChg Email send select orders = 0 for " & strbu & " " & Now())
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try

            Return False
        Else
            Console.WriteLine("Fetched Datas:" + Convert.ToString(ds.Tables(0).Rows.Count()))
            objGenerallLogStreamWriter.WriteLine("Fetched Datas:" + Convert.ToString(ds.Tables(0).Rows.Count()) & " " & Now())
        End If

        'insert into the PS_ISA_ORDSTAT_EML table

        Dim I As Integer

        connectOR.Open()
        For I = 0 To ds.Tables(0).Rows.Count - 1
            Dim strBUSINESSUNITOM As String = ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM")
            Dim strORDERNO As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
            Dim strINTFCLINENUM As String = ds.Tables(0).Rows(I).Item("INTFC_LINE_NUM")
            Dim strORDERINTLINENO As String = ds.Tables(0).Rows(I).Item("ORDER_INT_LINE_NO")
            Dim strDEMANDLINENO As String = ds.Tables(0).Rows(I).Item("DEMAND_LINE_NO")
            Dim strEMPLID As String = ds.Tables(0).Rows(I).Item("EMPLID")
            Dim strDesc254S As String = ds.Tables(0).Rows(I).Item("DESCR60")
            Dim strorigin As String = ds.Tables(0).Rows(I).Item("Origin")

            Dim ChkExistsLog As Boolean = ChkExistnLog(strBUSINESSUNITOM, strORDERNO, strINTFCLINENUM, strORDERINTLINENO, strDEMANDLINENO, strEMPLID, "7")
            connectOR.Open()
            If Not ChkExistsLog Then
                strSQLstring = "INSERT INTO PS_ISA_ORDSTAT_EML" & vbCrLf &
                        " VALUES ('" & strBUSINESSUNITOM & "'," & vbCrLf &
                        " '" & strORDERNO & "'," & vbCrLf &
                        " '" & strINTFCLINENUM & "'," & vbCrLf &
                        " '" & strORDERINTLINENO & "'," & vbCrLf &
                        " '" & strDEMANDLINENO & "'," & vbCrLf &
                        " ' ',0," & vbCrLf &
                        " '" & strEMPLID & "'," & vbCrLf &
                         " '7', '')" & vbCrLf

                Dim command1 As OleDbCommand
                command1 = New OleDbCommand(strSQLstring, connectOR)
                Try
                    Dim rowsaffected As Integer
                    rowsaffected = command1.ExecuteNonQuery
                    If Not rowsaffected = 1 Then
                        objStreamWriter.WriteLine("  StatChg Email send insert error for " & ds.Tables(0).Rows(I).Item("ORDER_NO") & " " &
                        ds.Tables(0).Rows(I).Item("INTFC_LINE_NUM") & " " &
                        ds.Tables(0).Rows(I).Item("ORDER_INT_LINE_NO") & " " &
                        ds.Tables(0).Rows(I).Item("DEMAND_LINE_NO") & " " & Now())
                        checkStock = True
                    End If
                    command1.Dispose()
                Catch ex As Exception
                    objStreamWriter.WriteLine("  StatChg Email send insert orders for " & ds.Tables(0).Rows(I).Item("ORDER_NO") & " " &
                        ds.Tables(0).Rows(I).Item("INTFC_LINE_NUM") & " " &
                        ds.Tables(0).Rows(I).Item("ORDER_INT_LINE_NO") & " " &
                        ds.Tables(0).Rows(I).Item("DEMAND_LINE_NO") & " " & Now())
                    checkStock = True
                End Try
            Else
                objStreamWriter.WriteLine("  StatChg Email send insert already exists in PS_ISA_ORDSTAT_EML table for order no " & ds.Tables(0).Rows(I).Item("ORDER_NO") & " " &
                        ds.Tables(0).Rows(I).Item("INTFC_LINE_NUM") & " " &
                        ds.Tables(0).Rows(I).Item("ORDER_INT_LINE_NO") & " " &
                        ds.Tables(0).Rows(I).Item("DEMAND_LINE_NO") & " " & Now())
                checkStock = True
            End If
        Next

        objStreamWriter.WriteLine("  StatChg Email STK send select orders = " & ds.Tables(0).Rows.Count & " for" & strbu & " " & Now())
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try

    End Function

    Private Function ChkExistnLog(ByVal strbu As String, ByVal strordno As String, ByVal ln_no As String, ByVal ordintln As String, ByVal dmnd_ln As String, ByVal strEmpId As String,
                                  ByVal ln_status As String) As Boolean
        Dim strquery As String = "SELECT * FROM PS_ISA_ORDSTAT_EML WHERE business_unit_om = '" + strbu + "' AND ORDER_NO='" + strordno + "' AND LINE_NBR = " + ln_no + " AND ORDER_INT_LINE_NO = " + ordintln + " AND DEMAND_LINE_NO = " + dmnd_ln + " AND EMPLID='" + strEmpId + "' AND ISA_LINE_STATUS = '" + ln_status + "'"
        Dim ChkExists As Boolean = False
        Try
            'If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            '    connectOR.Close()
            'End If

            Dim dSet As New DataSet
            dSet = ORDBAccess.GetAdapter(strquery, connectOR)

            If Not dSet Is Nothing Then
                If dSet.Tables(0).Rows.Count > 0 Then
                    ChkExists = True
                Else

                End If
            Else

            End If
            'If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            '    connectOR.Close()
            'End If
        Catch ex As Exception

        End Try
        Return ChkExists
    End Function

    Private Function buildNotifyReceiver(ByVal strOrderStatus As String) As Boolean

        Select Case strOrderStatus
            Case "7"
                objGenerallLogStreamWriter.WriteLine("Stock Notification StatEmail")
                buildNotifyReceiver = buildNotifySTKReady()
            Case "R"
                objGenerallLogStreamWriter.WriteLine("Non-Stock Notification StatEmail")
                buildNotifyReceiver = buildNotifyNSTKReady()
        End Select
    End Function

    Private Function buildNotifyNSTKReady() As Boolean

        Dim strSQLString As String
        Dim bolErrorR As Boolean

        ' get all stock lines that have been picked
        strSQLString = "SELECT A.BUSINESS_UNIT_OM, A.ORDER_NO, L.ISA_WORK_ORDER_NO as WORK_ORDER_NO, A.LINE_NBR," & vbCrLf &
                " A.EMPLID, A.ISA_LINE_STATUS AS ISA_ORDER_STATUS," & vbCrLf &
                " B.INV_ITEM_ID, B.QTY_SH_ACCPT AS QTY_LN_ACCPT," & vbCrLf &
                " B.DESCR254_MIXED , D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH, B.PO_ID, H.Origin " & vbCrLf &
                " FROM PS_ISA_ORDSTAT_EML A," & vbCrLf &
                " PS_RECV_LN_SHIP B," & vbCrLf &
                " PS_ISA_USERS_TBL D," & vbCrLf &
                " ps_isa_ord_intf_hD H ," & vbCrLf &
                " ps_isa_ord_intf_lN L" & vbCrLf &
                " WHERE A.EMAIL_DATETIME Is NULL " & vbCrLf &
                " and H.order_no=A.order_no " & vbCrLf &
                " and H.business_unit_om=A.BUSINESS_UNIT_OM " & vbCrLf &
                " and H.business_unit_om=D.BUSINESS_UNIT " & vbCrLf &
                " and H.business_unit_om = L.business_unit_om " & vbCrLf &
                " and H.order_no = L.order_no " & vbCrLf &
                " AND A.ISA_LINE_STATUS = 'RET'" & vbCrLf &
                " AND A.RECEIVER_ID = B.RECEIVER_ID" & vbCrLf &
                " AND A.RECV_LN_NBR = B.RECV_LN_NBR" & vbCrLf &
                " and L.ISA_INTFC_LN = B.RECV_LN_NBR" & vbCrLf &
                " and L.ISA_INTFC_LN = A.RECV_LN_NBR" & vbCrLf &
                " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                " AND UPPER(A.EMPLID) = UPPER(D.ISA_EMPLOYEE_ID)" & vbCrLf &
                " "

        Dim Command = New OleDbCommand(strSQLString, connectOR)
        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        Dim dataAdapter As OleDbDataAdapter =
                    New OleDbDataAdapter(Command)
        Dim ds As System.Data.DataSet = New System.Data.DataSet
        Try
            dataAdapter.Fill(ds)
            connectOR.Close()
        Catch ex As Exception
            objStreamWriter.WriteLine("  StatChg Email NSTK send PS_ISA_ORDSTAT_EML R error")
            connectOR.Close()
            buildNotifyNSTKReady = True
            bolErrorR = True
        End Try

        If bolErrorR = False And ds.Tables(0).Rows.Count > 0 Then
            Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
            objGenerallLogStreamWriter.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
            connectOR.Open()
            Dim strPreOrderno As String
            Dim I As Integer
            Dim X As Integer

            Dim dsEmail As New DataTable
            Dim dr As DataRow
            dsEmail.Columns.Add("Order No.")
            dsEmail.Columns.Add("Line Number")
            dsEmail.Columns.Add("Description")
            dsEmail.Columns.Add("Qty")
            dsEmail.Columns.Add("Work Order Number")
            dsEmail.Columns.Add("P.O. #: ")
            dsEmail.Columns.Add("Line Notes")
            For I = 0 To ds.Tables(0).Rows.Count - 1

                dr = dsEmail.NewRow()
                dr.Item(0) = ds.Tables(0).Rows(I).Item("ORDER_NO")
                dr.Item(1) = ds.Tables(0).Rows(I).Item("LINE_NBR")
                dr.Item(2) = ds.Tables(0).Rows(I).Item("DESCR254_MIXED")
                dr.Item(3) = ds.Tables(0).Rows(I).Item("QTY_LN_ACCPT")
                dr.Item(4) = ds.Tables(0).Rows(I).Item("WORK_ORDER_NO")
                dr.Item(5) = ds.Tables(0).Rows(I).Item("PO_ID")
                'WORK_ORDER_NO

                Dim ln_notes As String = ""
                ln_notes = GetLineNotes(Convert.ToString(ds.Tables(0).Rows(I).Item("ORDER_NO")), Convert.ToString(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM")), Convert.ToString(ds.Tables(0).Rows(I).Item("LINE_NBR")))
                dr.Item(6) = ln_notes
                connectOR.Open()
                dsEmail.Rows.Add(dr)
                'take this code down below so u can have multiple Order num's per email.
                Dim strEmail_test As String = ";tom.rapp@sdi.com"

                Dim strEmailTo As String = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_EMAIL")
                Dim strOrderNo As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
                Dim strBu As String = ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM")

                '' check is processed order is ASCEND order
                'Dim bIsAscend As Boolean = False
                'If Trim(strBu) <> "" Then
                '    bIsAscend = IsBuAscend(strBu)
                'End If

                'If bIsAscend Then
                '    Dim strAscendEmail As String = GetAscendEmailAddress(strBu, strOrderNo, connectOR)
                '    If Not strAscendEmail Is Nothing Then
                '        If Trim(strAscendEmail) <> "" Then
                '            strEmailTo = strAscendEmail
                '        End If
                '    End If

                'End If

                If I = ds.Tables(0).Rows.Count - 1 Then
                    sendCustEmail(dsEmail,
                        strOrderNo,
                        ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                        ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                        strEmailTo,
                        strBu, ds.Tables(0).Rows(I).Item("Origin"))

                    dsEmail.Clear()
                    If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                        connectOR.Close()
                    End If
                    connectOR.Open()
                    buildNotifyNSTKReady = updateSendEmailTbl(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM"),
                        ds.Tables(0).Rows(I).Item("ORDER_NO"),
                        ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"), ds.Tables(0).Rows(I).Item("Origin"))

                ElseIf ds.Tables(0).Rows(I + 1).Item("BUSINESS_UNIT_OM") _
                   & ds.Tables(0).Rows(I + 1).Item("ORDER_NO") <>
                   ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM") _
                   & ds.Tables(0).Rows(I).Item("ORDER_NO") Then
                    sendCustEmail(dsEmail,
                        strOrderNo,
                        ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                        ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                        strEmailTo,
                        strBu, ds.Tables(0).Rows(I).Item("Origin"))
                    dsEmail.Clear()
                    If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                        connectOR.Close()
                    End If
                    connectOR.Open()
                    buildNotifyNSTKReady = updateSendEmailTbl(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM"),
                        ds.Tables(0).Rows(I).Item("ORDER_NO"),
                        ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"), ds.Tables(0).Rows(I).Item("Origin"))
                End If
                strPreOrderno = ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM") _
                   & ds.Tables(0).Rows(I).Item("ORDER_NO")
                Try
                    connectOR.Close()
                Catch ex As Exception

                End Try
            Next
            objStreamWriter.WriteLine("  StatChg Build Notify Non STK, total orders = " & ds.Tables(0).Rows.Count)
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try
        Else
            Console.WriteLine("Fetched Datas 0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
        End If
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try
    End Function

    Private Function buildNotifySTKReady() As Boolean

        Dim strSQLString As String
        Dim bolError7 As Boolean
        Dim bolErrorR As Boolean
        'zzzzzzzzzzzzzzzzzzzzzzzzzzz
        ' get all stock lines that have been picked
        strSQLString = "SELECT A.BUSINESS_UNIT_OM, A.ORDER_NO,L.ISA_WORK_ORDER_NO as WORK_ORDER_NO, A.LINE_NBR," & vbCrLf &
                " A.EMPLID, A.ISA_LINE_STATUS AS ISA_ORDER_STATUS," & vbCrLf &
                " B.INV_ITEM_ID, B.QTY_PICKED, B.QTY_BACKORDER," & vbCrLf &
                " C.DESCR60, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH," & vbCrLf &
                " C.INV_ITEM_ID, H.Origin " & vbCrLf &
                " FROM PS_ISA_ORDSTAT_EML A," & vbCrLf &
                " SYSADM8.PS_IN_DEMAND B," & vbCrLf &
                " PS_MASTER_ITEM_TBL C," & vbCrLf &
                " PS_ISA_USERS_TBL D," & vbCrLf &
                " ps_isa_ord_intf_hD H ," & vbCrLf &
                " ps_isa_ord_intf_lN L " & vbCrLf &
                " WHERE A.EMAIL_DATETIME Is NULL" & vbCrLf &
                " AND A.ISA_LINE_STATUS = 'PKF'" & vbCrLf &
                " AND B.DEMAND_SOURCE = 'OM'" & vbCrLf &
                 " and H.order_no=A.order_no " & vbCrLf &
                " and H.business_unit_om=A.BUSINESS_UNIT_OM " & vbCrLf &
                " and H.business_unit_om=D.BUSINESS_UNIT " & vbCrLf &
                " and H.business_unit_om = L.business_unit_om " & vbCrLf &
                " and H.order_no = L.order_no " & vbCrLf &
                " AND B.SOURCE_BUS_UNIT = A.BUSINESS_UNIT_OM" & vbCrLf &
                " AND A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                " AND A.ORDER_INT_LINE_NO = B.ORDER_INT_LINE_NO" & vbCrLf &
                " AND A.DEMAND_LINE_NO = B.DEMAND_LINE_NO" & vbCrLf &
                " AND C.SETID = 'MAIN1'" & vbCrLf &
                " AND B.INV_ITEM_ID = C.INV_ITEM_ID" & vbCrLf &
                " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                " and L.ISA_INTFC_LN = A.line_nbr" & vbCrLf &
                " AND (UPPER(A.EMPLID)) = UPPER(D.ISA_EMPLOYEE_ID)" & vbCrLf &
                " AND B.IN_FULFILL_STATE IN ('60','50')" & vbCrLf &
                " AND B.DEMAND_SOURCE = 'OM'" & vbCrLf &
                " AND B.QTY_PICKED > 0" & vbCrLf

        Dim Command As OleDbCommand = New OleDbCommand(strSQLString, connectOR)
        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        Dim dataAdapter As OleDbDataAdapter =
                    New OleDbDataAdapter(Command)
        Dim ds As System.Data.DataSet = New System.Data.DataSet
        Try
            dataAdapter.Fill(ds)
            connectOR.Close()
        Catch ex As Exception
            objStreamWriter.WriteLine("  StatChg Email send PS_ISA_ORDSTAT_EML 7 error")
            connectOR.Close()
            buildNotifySTKReady = True
            bolError7 = True
        End Try

        If bolError7 = False And ds.Tables(0).Rows.Count > 0 Then
            objGenerallLogStreamWriter.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
            Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
            connectOR.Open()
            Dim strPreOrderno As String
            Dim I As Integer
            Dim X As Integer
            Dim decQtyOrdered As Decimal
            Dim decQtyShipped As Decimal

            Dim dsEmail As New DataTable
            Dim dr As DataRow
            dsEmail.Columns.Add("Item ID")
            dsEmail.Columns.Add("Description")
            dsEmail.Columns.Add("Picked Qty.")
            dsEmail.Columns.Add("Back Ordered")
            dsEmail.Columns.Add("Order Number")
            dsEmail.Columns.Add("Work Order Number")
            dsEmail.Columns.Add("Line Notes")
            For I = 0 To ds.Tables(0).Rows.Count - 1

                dr = dsEmail.NewRow()
                dr.Item(0) = ds.Tables(0).Rows(I).Item("INV_ITEM_ID")
                dr.Item(1) = ds.Tables(0).Rows(I).Item("DESCR60")
                dr.Item(2) = ds.Tables(0).Rows(I).Item("QTY_PICKED")
                dr.Item(3) = ds.Tables(0).Rows(I).Item("QTY_BACKORDER")
                dr.Item(4) = ds.Tables(0).Rows(I).Item("ORDER_NO")
                dr.Item(5) = ds.Tables(0).Rows(I).Item("WORK_ORDER_NO")
                Dim ln_notes As String = ""
                ln_notes = GetLineNotes(Convert.ToString(ds.Tables(0).Rows(I).Item("ORDER_NO")), Convert.ToString(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM")), Convert.ToString(ds.Tables(0).Rows(I).Item("LINE_NBR")))
                dr.Item(6) = ln_notes
                connectOR.Open()

                decQtyOrdered = getQtyOrdered(ds.Tables(0).Rows(I).Item("ORDER_NO"),
                                              ds.Tables(0).Rows(I).Item("LINE_NBR"),
                                              connectOR)

                If (decQtyOrdered > 0) And
                    decQtyOrdered <> (ds.Tables(0).Rows(I).Item("QTY_PICKED") +
                                    ds.Tables(0).Rows(I).Item("QTY_BACKORDER")) Then
                    decQtyShipped = getQtyShipped(ds.Tables(0).Rows(I).Item("ORDER_NO"),
                                              ds.Tables(0).Rows(I).Item("LINE_NBR"),
                                              connectOR)
                    If decQtyShipped > 0 Then
                        dr.Item(3) = Format((decQtyOrdered - decQtyShipped), "0.####")
                    End If

                End If
                dsEmail.Rows.Add(dr)
                Dim strEmail_test As String = ";tom.rapp@sdi.com"

                Dim strEmailTo As String = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_EMAIL")
                Dim strOrderNo As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
                Dim strBu As String = ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM")

                If I = ds.Tables(0).Rows.Count - 1 Then
                    sendCustEmail(dsEmail,
                        strOrderNo,
                        ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                        ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                        strEmailTo,
                        strBu,
                        ds.Tables(0).Rows(I).Item("Origin"))
                    dsEmail.Clear()
                    If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                        connectOR.Close()
                    End If
                    connectOR.Open()
                    buildNotifySTKReady = updateSendEmailTbl(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM"),
                        ds.Tables(0).Rows(I).Item("ORDER_NO"),
                        ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"), ds.Tables(0).Rows(I).Item("Origin"))

                ElseIf ds.Tables(0).Rows(I + 1).Item("BUSINESS_UNIT_OM") _
                   & ds.Tables(0).Rows(I + 1).Item("ORDER_NO") <>
                   ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM") _
                   & ds.Tables(0).Rows(I).Item("ORDER_NO") Then
                    sendCustEmail(dsEmail,
                        strOrderNo,
                        ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                        ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                        strEmailTo,
                        strBu,
                        ds.Tables(0).Rows(I).Item("Origin"))
                    dsEmail.Clear()
                    If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                        connectOR.Close()
                    End If
                    connectOR.Open()
                    buildNotifySTKReady = updateSendEmailTbl(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM"),
                        ds.Tables(0).Rows(I).Item("ORDER_NO"),
                        ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"), ds.Tables(0).Rows(I).Item("Origin"))
                    'buildNotifySTKReady = updateSendEmailTbl(ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM"), _
                    '                       ds.Tables(0).Rows(I).Item("ORDER_NO"), _
                    '                       ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"))
                    Try
                        connectOR.Close()
                    Catch ex As Exception

                    End Try
                End If
                strPreOrderno = ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM") _
                   & ds.Tables(0).Rows(I).Item("ORDER_NO")
            Next
            objStreamWriter.WriteLine("  StatChg Build Notify STK, total orders = " & ds.Tables(0).Rows.Count)
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
        Else
            Console.WriteLine("Fetched Datas 0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
        End If
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try
    End Function

    Private Function getQtyOrdered(ByVal strOrderNo As String,
                                ByVal intLineNbr As Integer,
                                ByVal connectOR As OleDb.OleDbConnection) As Decimal

        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If
        connectOR.Open()
        Dim strSQLstring As String
        strSQLstring = "SELECT SUM( B.QTY_ORDERED)" & vbCrLf &
                " FROM PS_ISA_ORD_INTF_LN A, PS_ORD_LINE B" & vbCrLf &
                " WHERE A.ORDER_NO = '" & strOrderNo & "'" & vbCrLf &
                " AND A.ISA_INTFC_LN = " & intLineNbr & vbCrLf &
                " AND A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                " AND A.ISA_INTFC_LN = B.ORDER_INT_LINE_NO"

        Try
            getQtyOrdered = ORDBAccess.GetScalar(strSQLstring, connectOR)
        Catch ex As Exception
            getQtyOrdered = 0
        End Try

    End Function

    Private Function getQtyShipped(ByVal strOrderNo As String,
                                ByVal intLineNbr As Integer,
                                ByVal connectOR As OleDb.OleDbConnection) As Decimal

        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If
        connectOR.Open()
        Dim strSQLstring As String
        strSQLstring = "SELECT SUM( B.QTY_PICKED)" & vbCrLf &
                " FROM PS_ISA_ORD_INTF_LN A, SYSADM8.PS_IN_DEMAND B" & vbCrLf &
                " WHERE A.ORDER_NO = '" & strOrderNo & "'" & vbCrLf &
                " AND A.ISA_INTFC_LN = " & intLineNbr & vbCrLf &
                " AND A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                " AND A.ISA_INTFC_LN = B.ORDER_INT_LINE_NO" & vbCrLf &
                " AND B.IN_FULFILL_STATE IN ('60','50')" & vbCrLf &
                " AND B.DEMAND_SOURCE = 'OM'" & vbCrLf &
                " AND B.QTY_PICKED > 0" & vbCrLf &
                ""
        Try
            getQtyShipped = ORDBAccess.GetScalar(strSQLstring, connectOR)
        Catch ex As Exception
            getQtyShipped = 0
        End Try

    End Function

    Private Sub sendCustEmail(ByVal dsEmail As DataTable,
                        ByVal strOrderNo As String,
                        ByVal strFirstName As String,
                        ByVal strLastName As String,
                        ByVal strEmail As String,
                        ByVal strbu As String,
                        ByVal strOrigin As String)

        Dim SDIEmailService As SDiEmailUtilityService.EmailServices = New SDiEmailUtilityService.EmailServices()
        Dim MailAttachmentName As String()
        Dim MailAttachmentbytes As New List(Of Byte())()
        Dim strbodyhead As String
        Dim strbodydetl As String
        Dim txtBody As String
        Dim txtHdr As String
        Dim txtMsg As String
        Dim dataGridHTML As String
        Dim SBnstk As New StringBuilder
        Dim SWnstk As New StringWriter(SBnstk)
        Dim htmlTWnstk As New HtmlTextWriter(SWnstk)
        Dim bolSelectItem As Boolean

        Dim Mailer As MailMessage = New MailMessage
        Dim strccfirst As String = "erwin.bautista"   '  "pete.doyle"
        Dim strcclast As String = "sdi.com"
        'SP-316 get from email from table - Dhamotharan
        Dim fromMail As String = ""
        'SDI-40628 Changing Mail id as walmartpurchasing@sdi.com from sdiexchange@sdi.com for Walmart BU.
        'If strbu = "I0W01" Then
        '    Mailer.From = "WalmartPurchasing@sdi.com"
        'Else
        '    Mailer.From = "SDIExchange@SDI.com"  '  "Insiteonline@SDI.com"
        'End If
        Try
            Dim BU As String = ""
            If strbu = "I0W01" Then
                BU = "WAL00"
                fromMail = getFromMail(BU, connectOR)
            ElseIf strbu = "I0631" Then
                BU = "EMC00"
                fromMail = getFromMail(BU, connectOR)
            Else
                BU = "ISA00"
                fromMail = getFromMail(BU, connectOR)
            End If
        Catch ex As Exception

        End Try


        Mailer.From = fromMail
        Mailer.Cc = ""
        Mailer.Bcc = strccfirst & "@" & strcclast
        'strbodyhead = "<center><span style='font-family:Arial;font-size:X-Large;width:256px;'>SDI Marketplace</span></center>" & vbCrLf
        'strbodyhead = strbodyhead & "<center><span >SDiExchange - Order Status</span></center>"
        'strbodyhead = strbodyhead & "&nbsp;" & vbCrLf
        strbodyhead = "<table width='100%'><tbody><tr><td><img src='http://www.sdiexchange.com/images/SDILogo_Email.png' alt='SDI' width='98px' height='82px' vspace='0' hspace='0' /></td><td width='100%'><br /><br /><br /><br /><br /><br /><center><span style='font-family: Arial; font-size: x-large; text-align: center;'>SDI Marketplace</span></center><center><span style='text-align: center; margin: 0px auto;'>SDiExchange - Order Status</span></center></td></tr></tbody></table>"
        strbodyhead = strbodyhead & "<HR width='100%' SIZE='1'>"
        strbodyhead = strbodyhead & "&nbsp;" & vbCrLf
        Dim dtgEmail As WebControls.DataGrid
        dtgEmail = New WebControls.DataGrid

        dtgEmail.DataSource = dsEmail
        dtgEmail.DataBind()
        dtgEmail.BorderColor = Gray
        dtgEmail.HeaderStyle.BackColor = System.Drawing.Color.LightGray
        dtgEmail.HeaderStyle.Font.Bold = True
        dtgEmail.HeaderStyle.ForeColor = Black
        WebControls.Unit.Percentage(90)
        dtgEmail.CellPadding = 3
        'dtgEmail.Width.Percentage(90)

        'dtgPO.Columns(9).ItemStyle.HorizontalAlign = HorizontalAlign.Center
        dtgEmail.RenderControl(htmlTWnstk)
        dataGridHTML = SBnstk.ToString()

        ''Get Order Notes here
        Dim Ord_notes As String = ""
        Ord_notes = GetOrderNotes(strOrderNo, strbu)

        Dim strPurchaserName As String = strFirstName &
            " " & strLastName
        Dim ted As String = ";erwin.bautista@sdi.com"  '  ";pete.doyle@sdi.com"
        Dim strPurchaserEmail As String = strEmail
        'dim strPurchaserEmail As String = strEmail
        strbodydetl = "&nbsp;" & vbCrLf
        strbodydetl = strbodydetl & "<div>"
        strbodydetl = strbodydetl & "<p >Hello " & strPurchaserName & ",<br>"
        strbodydetl = strbodydetl & "&nbsp;<BR>"
        If strbu = "I0260" Or strbu = "I0206" Then
            If Not strOrigin = "MIS" Then
                strbodydetl = strbodydetl & "Your SDiExchange Order Number " & strOrderNo & " has been Processed and Delivered.<br>"
            Else
                strbodydetl = strbodydetl & "Your SDiExchange Order Number " & strOrderNo & " has been Picked and is Ready for Pickup at the SDI Storeroom.<br>"
            End If
        Else
            strbodydetl = strbodydetl & "Your SDiExchange Order Number " & strOrderNo & " has been Processed and Ready for Pickup.<br>"
        End If
        strbodydetl = strbodydetl & "&nbsp;<BR>"
        If Not Ord_notes Is Nothing Then
            If Not (String.IsNullOrEmpty(Ord_notes.Trim())) Then
                strbodydetl = strbodydetl & "Customer Notes: " & Ord_notes & " <br> "
            End If
        End If
        strbodydetl = strbodydetl & "Order contents:<br>"
        strbodydetl = strbodydetl & "&nbsp;<BR>"

        strbodydetl = strbodydetl & "&nbsp;</p>"
        strbodydetl = strbodydetl & "<TABLE cellSpacing='1' cellPadding='1' width='100%' border='0'>" & vbCrLf
        strbodydetl = strbodydetl + "<TR><TD Class='DetailRow' width='100%'>" & dataGridHTML & "</TD></TR>"
        strbodydetl = strbodydetl + "<TR><TD Class='DetailRow'>&nbsp;</TD></TR>"
        strbodydetl = strbodydetl & "</TABLE>" & vbCrLf

        strbodydetl = strbodydetl & "&nbsp;<br>"
        strbodydetl = strbodydetl & "Sincerely,<br>"
        strbodydetl = strbodydetl & "&nbsp;<br>"
        strbodydetl = strbodydetl & "SDI Customer Care<br>"
        strbodydetl = strbodydetl & "&nbsp;<br>"
        strbodydetl = strbodydetl & "</p>"
        strbodydetl = strbodydetl & "</div>"
        strbodydetl = strbodydetl & "<HR width='100%' SIZE='1'>" & vbCrLf
        strbodydetl = strbodydetl & "<img src='http://www.sdiexchange.com/Images/SDIFooter_Email.png' />" & vbCrLf
        Mailer.Body = strbodyhead & strbodydetl
        If Not getDBName() Then
            Mailer.To = "webdev@sdi.com"
        Else
            Mailer.To = strPurchaserEmail
        End If
        Mailer.BodyFormat = System.Web.Mail.MailFormat.Html
        If strbu = "I0260" Or strbu = "I0206" Then
            If Not strOrigin = "MIS" Then
                If Not getDBName() Then
                    Mailer.Subject = "<<TEST SITE>>SDiExchange - Order Status " & strOrderNo & " has been Delivered"
                Else
                    Mailer.Subject = "SDiExchange - Order Status " & strOrderNo & " has been Delivered"
                End If
            Else
                If Not getDBName() Then
                    Mailer.Subject = "<<TEST SITE>>SDiExchange - Order Status " & strOrderNo & " Picked & Ready for Pickup @ SDI Storeroom"
                Else
                    Mailer.Subject = "SDiExchange - Order Status " & strOrderNo & " Picked & Ready for Pickup @ SDI Storeroom"
                End If

            End If
            Try
                SDIEmailService.EmailUtilityServices("MailandStore", Mailer.From, Mailer.To, Mailer.Subject, String.Empty, "webdev@sdi.com", Mailer.Body, "StatusChangeEmail0", MailAttachmentName, MailAttachmentbytes.ToArray())
            Catch ex As Exception

            End Try

        Else
            If Not getDBName() Then
                Mailer.Subject = "<<TEST SITE>>SDiExchange - Order Status " & strOrderNo & " is Ready for Pickup"
            Else
                Mailer.Subject = "SDiExchange - Order Status " & strOrderNo & " is Ready for Pickup"
            End If
            Try
                SDIEmailService.EmailUtilityServices("MailandStore", Mailer.From, Mailer.To, Mailer.Subject, String.Empty, "webdev@sdi.com", Mailer.Body, "StatusChangeEmail0", MailAttachmentName, MailAttachmentbytes.ToArray())
                objStreamWriter.WriteLine("  Generated Email for the order number " & strOrderNo & " " & Now())
            Catch ex As Exception

            End Try

        End If
    End Sub

    Private Sub SendEmail()

        Dim SDIEmailService As SDiEmailUtilityService.EmailServices = New SDiEmailUtilityService.EmailServices()
        Dim MailAttachmentName As String()
        Dim MailAttachmentbytes As New List(Of Byte())()
        Dim email As New MailMessage

        'The email address of the sender
        email.From = "TechSupport@sdi.com"

        'The email address of the recipient. 
        email.To = "webdev@sdi.com"  '  "pete.doyle@sdi.com"

        'The subject of the email
        email.Subject = "Error in the 'Status Change Email' Console App."

        'The Priority attached and displayed for the email
        email.Priority = MailPriority.High

        email.BodyFormat = MailFormat.Html

        email.Body = "<html><body><table><tr><td>StatusChangeEmail has completed with errors, review log (C:\StatChg) </td></tr>"

        'email.Body = email.Body & "<tr><td></td><a href='\\BDougherty_XP-l\logs'>\\BDougherty_XP-l\logs\</a></tr></table></body></html>"

        'Send the email and handle any error that occurs
        Try
            'UpdEmailOut.UpdEmailOut.UpdEmailOut(email.Subject, email.From, "sriram.s@avasoft.biz", "", "", "Y", email.Body, connectOR)
            SDIEmailService.EmailUtilityServices("MailandStore", email.From, email.To, email.Subject, String.Empty, String.Empty, email.Body, "StatusChangeEmail0", MailAttachmentName, MailAttachmentbytes.ToArray())
        Catch
            objStreamWriter.WriteLine("     Error - the email was not sent")
        End Try

    End Sub

    'Private Sub sendemail(ByVal mailer As MailMessage)

    '    Try
    '        SmtpMail.Send(mailer)
    '    Catch ex As Exception
    '        objStreamWriter.WriteLine("     Error - in the sendemail to customer SUB")
    '    End Try
    'End Sub

    Private Function updateSendEmailTbl(ByVal strBU As String, ByVal strOrderNo As String, ByVal strOrderStatus As String, ByVal strorigin As String) As Boolean

        Dim strSQLstring As String
        Dim rowsaffected As Integer
        strSQLstring = "UPDATE PS_ISA_ORDSTAT_EML" & vbCrLf &
                       " SET EMAIL_DATETIME = TO_DATE('" & Now() & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                       " WHERE BUSINESS_UNIT_OM = '" & strBU & "'" & vbCrLf &
                       " AND ORDER_NO = '" & strOrderNo & "'" & vbCrLf &
                       " AND ISA_LINE_STATUS = '" & strOrderStatus & "'"

        Dim Command1 As OleDbCommand
        Command1 = New OleDbCommand(strSQLstring, connectOR)
        Try
            rowsaffected = Command1.ExecuteNonQuery
            If rowsaffected = 0 Then
                objStreamWriter.WriteLine("**")
                objStreamWriter.WriteLine("     Error - 0 PS_ISA_ORDSTAT_EML tbl for order " & strOrderNo)
                objStreamWriter.WriteLine("**")
                updateSendEmailTbl = True
            End If
        Catch OleDBExp As OleDbException
            objStreamWriter.WriteLine("**")
            objStreamWriter.WriteLine("     Error - updating PS_ISA_ORDSTAT_EML tbl for order " & strOrderNo)
            objStreamWriter.WriteLine("**")
            updateSendEmailTbl = True
        End Try
        Command1.Dispose()
    End Function

    Private Function IsBuAscend(ByVal strBu As String) As Boolean

        ' check is processed order is ASCEND order
        Dim bIsAscend As Boolean = False
        Dim strAscendBuList As String = "I0440,I0441,I0442,I0443,I0444"
        If Trim(strBu) <> "" Then
            Try
                If strAscendBuList.IndexOf(strBu.Trim().ToUpper()) > -1 Then
                    bIsAscend = True
                End If
            Catch ex As Exception
                bIsAscend = False
            End Try
        End If

        Return bIsAscend

    End Function

    Private Function checkAllStatus_7(ByVal strBU As String, ByVal dtrStartDate As String) As Boolean
        Dim strSQLstring As String
        Dim dteEndDate As DateTime = Now

        Dim format As New System.Globalization.CultureInfo("en-US", True)
        strSQLstring = "SELECT" & vbCrLf &
            " to_char(MAX( A.DTTM_STAMP), 'MM/DD/YY HH24:MI:SS') as MAXDATE" & vbCrLf &
            " FROM PS_ISAORDSTATUSLOG A" & vbCrLf &
             " WHERE A.BUSINESS_UNIT_OM = '" & strBU & "' "

        Dim dr As OleDbDataReader = Nothing

        Try
            objStreamWriter.WriteLine("  CheckAllStatus_7 (1): " & strSQLstring)

            Dim command As OleDbCommand
            command = New OleDbCommand(strSQLstring, connectOR)
            If connectOR.State = ConnectionState.Open Then
                'do nothing
            Else
                connectOR.Open()
            End If
            Dim st As New Stopwatch()
            Dim ts As TimeSpan
            Dim elapsedTime As String
            Try
                st.Start()
                dr = command.ExecuteReader
                st.Stop()
                ts = st.Elapsed
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
                objStreamWriter.WriteLine("Query Execution Time " + elapsedTime)
            Catch ex As Exception
                dr = command.ExecuteReader
                objStreamWriter.WriteLine("Query Execution Time " + Now())
            End Try
            Try

                If dr.Read Then
                    dteEndDate = (dr.Item("MAXDATE"))
                Else
                    dteEndDate = Now.ToString
                End If
            Catch ex As Exception
                dteEndDate = Now.ToString
            End Try

            dr.Close()
            connectOR.Close()

        Catch OleDBExp As OleDbException
            Try
                dr.Close()
                connectOR.Close()
            Catch exOR As Exception

            End Try
            objStreamWriter.WriteLine("     Error - error reading end date FROM PS_ISAORDSTATUSLOG A" & " " & Now())
            Return True
        End Try

        connectOR.Open()
        Dim objEnterprise As New clsEnterprise(strBU, connectOR)
        Dim dteCustID As String = objEnterprise.CustID
        Dim dteCompanyID As String = objEnterprise.CompanyID
        Dim dteStartDate As DateTime = objEnterprise.SendStartDate
        Dim dteSiteEmail As String = objEnterprise.SiteEmail
        Dim dteSTKREQEmail As String = objEnterprise.STKREQEmail
        Dim dteNONSKREQEmail As String = objEnterprise.NONSKREQEmail



        Try
            connectOR.Close()
        Catch ex As Exception

        End Try

        Dim ds As New DataSet
        Dim bolerror1 As Boolean

        ' check is processed order is ASCEND order
        Dim bIsAscend As Boolean = False
        If Trim(strBU) <> "" Then
            bIsAscend = IsBuAscend(strBU)
        End If

        dteEndDate.AddSeconds(1)

        ' stock items will get item id from the ps_isa_ord_intfc_l table  but description from the PS_MASTER_ITEM_TB
        ' non-stock items  has no item-id num and gets description from the ps_isa_ord_intfc_l
        ' PS_ISAORDSTATUSLOG the line number points to the line number in ps_isa_ord_intfc_l
        ' DO NOT SELECT G.ISA_ORDER_STATUS = '6'  WE ARE GETTING IT UP TOP.
        '         '  

        strSQLstring = "SELECT H.ISA_IOL_OP_NAME as STATUS_CODE, TBL.* FROM (SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                 " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE," & vbCrLf &
                 " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, " & vbCrLf   '  & _


        strSQLstring += "  G.ISA_LINE_STATUS AS ISA_ORDER_STATUS, DECODE(G.ISA_LINE_STATUS,'CRE','1','NEW','2','DSP','3','ORD','3','RSV','3','PKA','4','PKP','4','DLP','5','RCP','5','RCF','6','PKQ','5','DLO','5','DLF','6','PKF','7','CNC','C','QTS','Q','QTW','W','1') AS OLD_ORDER_STATUS," & vbCrLf &
                     " (SELECT E.XLATLONGNAME" & vbCrLf &
                                    " FROM XLATTABLE E" & vbCrLf &
                                    " WHERE E.EFFDT =" & vbCrLf &
                                    " (SELECT MAX(E_ED.EFFDT) FROM XLATTABLE E_ED" & vbCrLf &
                                    " WHERE(E.FIELDNAME = E_ED.FIELDNAME)" & vbCrLf &
                                    " AND E.FIELDVALUE = E_ED.FIELDVALUE" & vbCrLf &
                                    " AND E_ED.EFFDT <= SYSDATE)" & vbCrLf &
                                    " AND E.FIELDNAME = 'ISA_LINE_STATUS'" & vbCrLf &
                                    " AND E.FIELDVALUE = G.ISA_LINE_STATUS) as ORDER_STATUS_DESC, " & vbCrLf &
                     " B.DESCR254 As NONSTOCK_DESCRIPTION, C.DESCR60 as STOCK_DESCRIPTION, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                     " B.INV_ITEM_ID as INV_ITEM_ID," & vbCrLf &
                     " B.QTY_REQUESTED,B.QTY_RECEIVED,B.UNIT_OF_MEASURE," & vbCrLf &
                     " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH" & vbCrLf &
                     " ,A.origin, LD.PO_ID, SH.ISA_ASN_TRACK_NO" & vbCrLf &
                     " FROM ps_isa_ord_intf_HD A," & vbCrLf  '   & _

        strSQLstring += " ps_isa_ord_intf_LN B," & vbCrLf &
                     " PS_MASTER_ITEM_TBL C," & vbCrLf &
                     " PS_ISA_USERS_TBL D," & vbCrLf &
                     " PS_ISAORDSTATUSLOG G, PS_ISA_ASN_SHIPPED SH, PS_PO_LINE_DISTRIB LD" & vbCrLf &
                     " where G.BUSINESS_UNIT_OM = '" & strBU & "' " & vbCrLf &
                     " AND G.BUSINESS_UNIT_OM = A.BUSINESS_UNIT_OM " & vbCrLf &
                     " AND G.BUSINESS_UNIT_OM = D.BUSINESS_UNIT " & vbCrLf     '   & _

        strSQLstring += "  and A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                     " and A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                     " and C.SETID (+) = 'MAIN1'" & vbCrLf &
                     " and C.INV_ITEM_ID(+) = B.INV_ITEM_ID " & vbCrLf &
                     " AND G.ORDER_NO = A.ORDER_NO " & vbCrLf &
                     " AND B.ISA_INTFC_LN = G.ISA_INTFC_LN" & vbCrLf &
                     " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                     " AND SH.PO_ID (+) = LD.PO_ID And SH.LINE_NBR (+) = LD.LINE_NBR And SH.SCHED_NBR (+) = LD.SCHED_NBR And LD.Req_id (+) = B.order_no AND LD.REQ_LINE_NBR (+) = B.ISA_INTFC_LN" & vbCrLf &
                     " AND G.DTTM_STAMP > TO_DATE('" & dteStartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                     " AND G.DTTM_STAMP <= TO_DATE('" & dteEndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                     " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID)) TBL, PS_ISA_USERS_PRIVS H " & vbCrLf &
                     " WHERE H.BUSINESS_UNIT = TBL.BUSINESS_UNIT " & vbCrLf &
                     " AND TBL.EMPLID = H.ISA_EMPLOYEE_ID " & vbCrLf &
                     " AND SUBSTR(H.ISA_IOL_OP_NAME,10) = TBL.OLD_ORDER_STATUS " & vbCrLf &
                     " AND H.ISA_IOL_OP_VALUE = 'Y' " & vbCrLf &
                      " ORDER BY ORDER_NO, LINE_NBR, DTTM_STAMP"
        ' this is set up in the user priveleges when giving out the status code priveleges in ISOL under Add/Change User
        ' matches the orserstatus emails set up for with the order status in PS_ISAORDSTATUSLOG
        ' the tenth byte of isa_iol_op_name has the one character g.isa_order_status code
        ' example: substr(emlsubmit1,10) = '1'   order status code 1
        ' We are going to check for priveleges in the upd_email_out program that sends the emails out.

        Try
            objStreamWriter.WriteLine("  CheckAllStatus_7 (2) Q1: " & strSQLstring)
            Dim st As New Stopwatch()
            Dim ts As TimeSpan
            Dim elapsedTime As String
            Try
                st.Start()
                ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                st.Stop()
                ts = st.Elapsed
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
                objStreamWriter.WriteLine("Query Execution Time " + elapsedTime)
            Catch ex As Exception
                ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                objStreamWriter.WriteLine("Query Execution Time " + Now())
            End Try


        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            objStreamWriter.WriteLine("     Error - error reading transaction FROM PS_ISAORDSTATUSLOG A" & " " & Now())
            Return True
        End Try

        If IsDBNull(ds.Tables(0).Rows.Count) Or (ds.Tables(0).Rows.Count) = 0 Then
            Console.WriteLine("Fetched Datas 0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
            objStreamWriter.WriteLine("     Warning - no status changes to process at this time for All Statuses" & " " & Now())
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try
            Return False
        Else
            Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
            objGenerallLogStreamWriter.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
        End If

        Dim rowsaffected As Integer
        Dim tmpOrderNo As String

        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        Dim strPreOrderno As String
        Dim I As Integer
        Dim X As Integer
        Dim dsEmail As New DataTable
        Dim dr1 As DataRow

        'SDI - 23457 added qty ordered, qty received and UOM column for order status email
        dsEmail.Columns.Add("Order No.")
        dsEmail.Columns.Add("Status")
        dsEmail.Columns.Add("Non-Stock Item Description")
        dsEmail.Columns.Add("Stock Item Description")
        dsEmail.Columns.Add("Item ID")
        dsEmail.Columns.Add("Line Number")
        dsEmail.Columns.Add("Time")
        dsEmail.Columns.Add("Status Code")
        dsEmail.Columns.Add("Work Order Number")
        dsEmail.Columns.Add("PO #")
        dsEmail.Columns.Add("Tracking No")
        dsEmail.Columns.Add("Line Notes")
        dsEmail.Columns.Add("Qty Ordered")
        dsEmail.Columns.Add("Qty Received")
        dsEmail.Columns.Add("UOM")

        Dim strdescription As String = " "
        Dim strEmailTo As String = " "

        For I = 0 To ds.Tables(0).Rows.Count - 1
            Dim strStatus_code As String = " "
            Try
                strStatus_code = ds.Tables(0).Rows(I).Item("STATUS_CODE")
                strStatus_code = strStatus_code.Substring(9)

            Catch ex As Exception
                strStatus_code = " "
            End Try
            Dim strSiteBU As String
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            Dim Command As OleDbCommand

            strSQLstring = "SELECT A.BUSINESS_UNIT" & vbCrLf &
                    " FROM PS_REQ_LOADER_DFL A" & vbCrLf &
                    " WHERE A.LOADER_BU = '" & strBU & "'" & vbCrLf

            objStreamWriter.WriteLine("  CheckAllStatus_7 (3): " & strSQLstring & " " & Now())

            Command = New OleDbCommand(strSQLstring, connectOR)
            connectOR.Open()
            Try
                strSiteBU = Command.ExecuteScalar
                connectOR.Close()
            Catch ex As Exception
                objStreamWriter.WriteLine("  StatChg Email NSTK send select siteBU for " & strBU & " " & Now())
                connectOR.Close()
                strSiteBU = "ISA00"
            End Try

            dr1 = dsEmail.NewRow()
            Dim stroderno As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
            Dim strlineno As String = ds.Tables(0).Rows(I).Item("LINE_NBR")
            dr1.Item(0) = ds.Tables(0).Rows(I).Item("ORDER_NO")
            dr1.Item(1) = ds.Tables(0).Rows(I).Item("ORDER_STATUS_DESC")
            dr1.Item(2) = ds.Tables(0).Rows(I).Item("NONSTOCK_DESCRIPTION")
            dr1.Item(3) = ds.Tables(0).Rows(I).Item("STOCK_DESCRIPTION")
            dr1.Item(4) = ds.Tables(0).Rows(I).Item("INV_ITEM_ID")
            dr1.Item(5) = ds.Tables(0).Rows(I).Item("LINE_NBR")
            Dim ln_notes As String = ""
            ln_notes = GetLineNotes(stroderno, strBU, strlineno)
            If ln_notes Is Nothing OrElse ln_notes = "(null)" OrElse ln_notes.Trim() = "" Then
                ln_notes = "-"
            End If
            dr1.Item(11) = ln_notes
            connectOR.Open()
            dr1.Item(6) = ds.Tables(0).Rows(I).Item("DTTM_STAMP")
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            Dim strpo_id As String = getpo_id(stroderno, strlineno, strBU, strSiteBU)
            'dr1.Item(7) = ds.Tables(0).Rows(I).Item("STATUS_CODE")
            'just get the last character
            dr1.Item(7) = strStatus_code
            dr1.Item(8) = ds.Tables(0).Rows(I).Item("WORK_ORDER_NO")
            dr1.Item(9) = strpo_id
            Dim trackingNo As String = ""
            Try
                trackingNo = ds.Tables(0).Rows(I).Item("ISA_ASN_TRACK_NO")
            Catch ex As Exception
                trackingNo = ""
            End Try


            If Not String.IsNullOrEmpty(trackingNo) Then
                If trackingNo.Contains("1Z") Then
                    Dim URL As String = "http://wwwapps.ups.com/WebTracking/processInputRequest?HTMLVersion=5.0&sort_by=status&term_warn=yes&tracknums_displayed=5&TypeOfInquiryNumber=T&loc=en_US&InquiryNumber1=" & trackingNo & "&InquiryNumber2=&InquiryNumber3=&InquiryNumber4=&InquiryNumber5=&AgreeToTermsAndConditions=yes&track.x=25&track.y=9','','"
                    Dim m_cURL1 As String = "<a href=""" & URL & """ target=""_blank"">" & trackingNo & "</a>"
                    dr1.Item(10) = m_cURL1
                Else
                    Dim URL As String = "https://www.fedex.com/apps/fedextrack/?action=track&trackingnumber=" & trackingNo & "&cntry_code=us&locale=en_US"
                    Dim m_cURL1 As String = "<a href=""" & URL & """ target=""_blank"">" & trackingNo & "</a>"
                    dr1.Item(10) = m_cURL1
                End If
            Else
                dr1.Item(10) = "-"
            End If
            Try
                dr1.Item(12) = ds.Tables(0).Rows(I).Item("QTY_REQUESTED")
            Catch ex As Exception
                dr1.Item(12) = ""
            End Try
            Try
                dr1.Item(13) = ds.Tables(0).Rows(I).Item("QTY_RECEIVED")
            Catch ex As Exception
                dr1.Item(13) = ""
            End Try
            Try
                dr1.Item(14) = ds.Tables(0).Rows(I).Item("UNIT_OF_MEASURE")
            Catch ex As Exception
                dr1.Item(14) = ""
            End Try
            dsEmail.Rows.Add(dr1)

            ' "R" nonstock
            ' "7" stock

            If ds.Tables(0).Rows(I).Item("Origin") = "MIS" And strBU = "I0206" Then
                strdescription = "PICKED"
            Else
                Try
                    strdescription = ds.Tables(0).Rows(I).Item("ORDER_STATUS_DESC")
                Catch ex As Exception
                    strdescription = "Err_line_" & I.ToString()
                End Try

            End If
            strEmailTo = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_EMAIL")

            If I = ds.Tables(0).Rows.Count - 1 Then

                sendCustEmail1(dsEmail,
                ds.Tables(0).Rows(I).Item("ORDER_NO"),
                dteCompanyID,
                dteCustID,
                ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"),
                strdescription,
                ds.Tables(0).Rows(I).Item("INV_ITEM_ID"),
                ds.Tables(0).Rows(I).Item("LINE_NBR"),
                ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                strEmailTo,
                ds.Tables(0).Rows(I).Item("Origin"),
                strBU)

                dsEmail.Clear()
                If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                    connectOR.Close()
                End If
            ElseIf ds.Tables(0).Rows(I + 1).Item("BUSINESS_UNIT_OM") _
                          & ds.Tables(0).Rows(I + 1).Item("ORDER_NO") <>
                          ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM") _
                          & ds.Tables(0).Rows(I).Item("ORDER_NO") Then

                sendCustEmail1(dsEmail,
               ds.Tables(0).Rows(I).Item("ORDER_NO"),
               dteCompanyID,
               dteCustID,
               ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"),
               strdescription,
               ds.Tables(0).Rows(I).Item("INV_ITEM_ID"),
               ds.Tables(0).Rows(I).Item("LINE_NBR"),
               ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
               ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
               strEmailTo,
               ds.Tables(0).Rows(I).Item("Origin"),
               strBU)

                dsEmail.Clear()
                If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                    connectOR.Close()
                End If
            End If
        Next

        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If

        'If strBU <> "I0256" Then
        bolerror1 = updateEnterprise(strBU, dteEndDate)
        'End If
    End Function
    'Below Method is to get order details of Walmart users for order status summary email--WW-287 & WAL-632 Poornima S
    Private Function checkAllStatusWAL(ByVal strBU As String) As Boolean
        Dim strSQLstring As String
        Dim ds As New DataSet


        '********Commenting UpdateWalmartSourceCode() method since the same method will be called order wise in checkAllStatusNew() for I0W01 BU*******

        'Dim dteEndDate As DateTime = Now

        'Dim format As New System.Globalization.CultureInfo("en-US", True)
        'strSQLstring = "SELECT" & vbCrLf &
        '    " to_char(MAX( A.DTTM_STAMP), 'MM/DD/YY HH24:MI:SS') as MAXDATE" & vbCrLf &
        '    " FROM PS_ISAORDSTATUSLOG A" & vbCrLf &
        '     " WHERE A.BUSINESS_UNIT_OM = '" & strBU & "' "

        'Dim dr As OleDbDataReader = Nothing

        'Try
        '    objStreamWriter.WriteLine("  checkAllStatusWAL (1): " & strSQLstring)

        '    Dim command As OleDbCommand
        '    command = New OleDbCommand(strSQLstring, connectOR)
        '    If connectOR.State = ConnectionState.Open Then
        '        'do nothing
        '    Else
        '        connectOR.Open()
        '    End If
        '    dr = command.ExecuteReader
        '    Try

        '        If dr.Read Then
        '            dteEndDate = (dr.Item("MAXDATE"))
        '        Else
        '            dteEndDate = Now.ToString
        '        End If
        '    Catch ex As Exception
        '        dteEndDate = Now.ToString
        '    End Try

        '    dr.Close()
        '    connectOR.Close()

        'Catch OleDBExp As OleDbException
        '    Try
        '        dr.Close()
        '        connectOR.Close()
        '    Catch exOR As Exception

        '    End Try
        '    objStreamWriter.WriteLine("     Error - error reading end date FROM PS_ISAORDSTATUSLOG A")
        '    Return True
        'End Try

        'connectOR.Open()
        'Dim objEnterprise As New clsEnterprise(strBU, connectOR)
        'Dim dteCustID As String = objEnterprise.CustID
        'Dim dteCompanyID As String = objEnterprise.CompanyID
        'Dim dteStartDate As DateTime = objEnterprise.SendStartDate
        'Dim dteSiteEmail As String = objEnterprise.SiteEmail
        'Dim dteSTKREQEmail As String = objEnterprise.STKREQEmail
        'Dim dteNONSKREQEmail As String = objEnterprise.NONSKREQEmail

        'Try
        '    connectOR.Close()
        'Catch ex As Exception

        'End Try

        ' check is processed order is ASCEND order

        'dteEndDate.AddSeconds(1)

        'Try
        '    UpdateWalmartSourceCode(dteStartDate, dteEndDate, strBU)
        'Catch

        'End Try

        '********End of Commenting UpdateWalmartSourceCode()*********

        ' stock items will get item id from the ps_isa_ord_intfc_l table  but description from the PS_MASTER_ITEM_TB
        ' non-stock items  has no item-id num and gets description from the ps_isa_ord_intfc_l
        ' PS_ISAORDSTATUSLOG the line number points to the line number in ps_isa_ord_intfc_l
        ' DO NOT SELECT G.ISA_ORDER_STATUS = '6'  WE ARE GETTING IT UP TOP.
        '         '  

        '4/26/2022 Walmart order's status change emails alone won't be sent based on selected email privileges in user profile, removed that condition from below query - Poornima S

        strSQLstring = "( SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                 " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE,B.OPRID_ENTERED_BY, B.SHIPTO_ID as SHIPTO,B.ISA_USER2 as STORE," & vbCrLf &
                 " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, B.ISA_PRIORITY_FLAG As IsPriority, B.ISA_REQUIRED_BY_DT,B.VENDOR_ID," & vbCrLf &
                 "  G.ISA_LINE_STATUS AS ISA_ORDER_STATUS," & vbCrLf


        strSQLstring += " (SELECT E.XLATLONGNAME" & vbCrLf &
                                " FROM XLATTABLE E" & vbCrLf &
                                " WHERE E.EFFDT =" & vbCrLf &
                                " (SELECT MAX(E_ED.EFFDT) FROM XLATTABLE E_ED" & vbCrLf &
                                " WHERE(E.FIELDNAME = E_ED.FIELDNAME)" & vbCrLf &
                                " AND E.FIELDVALUE = E_ED.FIELDVALUE" & vbCrLf &
                                " AND E_ED.EFFDT <= SYSDATE)" & vbCrLf &
                                " AND E.FIELDNAME = 'ISA_LINE_STATUS'" & vbCrLf &
                                " AND E.FIELDVALUE = G.ISA_LINE_STATUS) as ORDER_STATUS_DESC, " & vbCrLf &
                 " B.DESCR254 As NONSTOCK_DESCRIPTION, C.DESCR60 as STOCK_DESCRIPTION, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                 " B.INV_ITEM_ID as INV_ITEM_ID," & vbCrLf &
                 " B.QTY_REQUESTED,B.QTY_RECEIVED,B.UNIT_OF_MEASURE," & vbCrLf &
        " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH" & vbCrLf &
                 " ,A.origin, LD.PO_ID, (select Tracking_Number(B.ORDER_NO,B.ISA_INTFC_LN,B.BUSINESS_UNIT_OM) from dual) as ISA_ASN_TRACK_NO," & vbCrLf &
                 "(SELECT MAX(FDX.DESCR80) FROM PS_ISA_FEDEX_STG FDX WHERE LD.PO_ID=FDX.PO_ID AND LD.REQ_ID=B.ORDER_NO AND FDX.PROCESS_STATUS='C') AS SHIP_STATUS" & vbCrLf & '[1/25/2023]WAL-781 Added shipment status fetching query with order summary query
                 " FROM ps_isa_ord_intf_HD A," & vbCrLf  '   & _
        '[11-28-2022]WW-554 Changed the query to get tracking num from Tracking_Number function since the inventory orders are not available in PS_ISA_ASN_SHIPPED table -- Poornima S
        strSQLstring += " ps_isa_ord_intf_LN B," & vbCrLf &
                 " PS_MASTER_ITEM_TBL C," & vbCrLf &
                 " PS_ISA_USERS_TBL D," & vbCrLf &
                 " PS_ISAORDSTATUSLOG G, PS_PO_LINE_DISTRIB LD" & vbCrLf &
                 " where G.BUSINESS_UNIT_OM = '" & strBU & "' " & vbCrLf &
                 " AND G.BUSINESS_UNIT_OM = A.BUSINESS_UNIT_OM " & vbCrLf &
                 " AND G.BUSINESS_UNIT_OM = D.BUSINESS_UNIT " & vbCrLf     '   & _

        strSQLstring += "  and A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                 " and A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                 " and C.SETID (+) = 'MAIN1'" & vbCrLf &
                 " and C.INV_ITEM_ID(+) = B.INV_ITEM_ID " & vbCrLf &
                 " AND G.ORDER_NO = A.ORDER_NO " & vbCrLf &
                 " AND B.ISA_INTFC_LN = G.ISA_INTFC_LN" & vbCrLf &
                 " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                 " And LD.Req_id (+) = B.order_no AND LD.REQ_LINE_NBR (+) = B.ISA_INTFC_LN" & vbCrLf

        strSQLstring += " AND G.DTTM_STAMP > (TRUNC(sysdate -1) + '" & SumryMailTime & "'/24)" & vbCrLf &
             " AND G.DTTM_STAMP <= (TRUNC(sysdate) + '" & SumryMailTime & "'/24)" & vbCrLf

        strSQLstring += "AND EXISTS (SELECT 'X' FROM PS_ISA_WO_STATUS I " & vbCrLf &
                  "WHERE B.BUSINESS_UNIT_OM = I.BUSINESS_UNIT_OM " & vbCrLf &
                  "AND   B.ISA_WORK_ORDER_NO = I.ISA_WORK_ORDER_NO " & vbCrLf &
                  "AND   I.ISA_WO_STATUS <> 'COMPLETED')" & vbCrLf

        strSQLstring += " AND B.ISA_LINE_STATUS IN ('CRE','QTW','QTC','QTS','CST','VND','APR','QTA','RCF','RCP','DLF','PKA','ASN')" & vbCrLf 'WW-644_PC_SUMMARYMAIL_02 (it should send mail for this status so changed  'G' to 'B'-> from PS_ISA_ORD_INTF_LN table ) (3/3/2023)-Aparna

        strSQLstring += " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID)" & vbCrLf


        'WW-644_PC_SUMMARYMAIL_01 (to get the mail daily for 'ready for pickup' orders until the status get changed) (3/3/2023)-Aparna
        'reusing the query to union the table which will get the 'PUR' status without date limit
        strSQLstring += " UNION SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                 " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE,B.OPRID_ENTERED_BY, B.SHIPTO_ID as SHIPTO,B.ISA_USER2 as STORE," & vbCrLf &
                 " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, B.ISA_PRIORITY_FLAG As IsPriority, B.ISA_REQUIRED_BY_DT,B.VENDOR_ID," & vbCrLf &
                 "  G.ISA_LINE_STATUS AS ISA_ORDER_STATUS," & vbCrLf


        strSQLstring += " (SELECT E.XLATLONGNAME" & vbCrLf &
                                " FROM XLATTABLE E" & vbCrLf &
                                " WHERE E.EFFDT =" & vbCrLf &
                                " (SELECT MAX(E_ED.EFFDT) FROM XLATTABLE E_ED" & vbCrLf &
                                " WHERE(E.FIELDNAME = E_ED.FIELDNAME)" & vbCrLf &
                                " AND E.FIELDVALUE = E_ED.FIELDVALUE" & vbCrLf &
                                " AND E_ED.EFFDT <= SYSDATE)" & vbCrLf &
                                " AND E.FIELDNAME = 'ISA_LINE_STATUS'" & vbCrLf &
                                " AND E.FIELDVALUE = G.ISA_LINE_STATUS) as ORDER_STATUS_DESC, " & vbCrLf &
                 " B.DESCR254 As NONSTOCK_DESCRIPTION, C.DESCR60 as STOCK_DESCRIPTION, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                 " B.INV_ITEM_ID as INV_ITEM_ID," & vbCrLf &
                 " B.QTY_REQUESTED,B.QTY_RECEIVED,B.UNIT_OF_MEASURE," & vbCrLf &
        " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH" & vbCrLf &
                 " ,A.origin, LD.PO_ID, (select Tracking_Number(B.ORDER_NO,B.ISA_INTFC_LN,B.BUSINESS_UNIT_OM) from dual) as ISA_ASN_TRACK_NO," & vbCrLf &
                 "(SELECT MAX(FDX.DESCR80) FROM PS_ISA_FEDEX_STG FDX WHERE LD.PO_ID=FDX.PO_ID AND LD.REQ_ID=B.ORDER_NO AND FDX.PROCESS_STATUS='C') AS SHIP_STATUS" & vbCrLf & '[1/25/2023]WAL-781 Added shipment status fetching query with order summary query
                 " FROM ps_isa_ord_intf_HD A," & vbCrLf  '   & _
        strSQLstring += " ps_isa_ord_intf_LN B," & vbCrLf &
                 " PS_MASTER_ITEM_TBL C," & vbCrLf &
                 " PS_ISA_USERS_TBL D," & vbCrLf &
                 " PS_ISAORDSTATUSLOG G, PS_PO_LINE_DISTRIB LD" & vbCrLf &
                 " where G.BUSINESS_UNIT_OM = '" & strBU & "' " & vbCrLf &
                 " AND G.BUSINESS_UNIT_OM = A.BUSINESS_UNIT_OM " & vbCrLf &
                 " AND G.BUSINESS_UNIT_OM = D.BUSINESS_UNIT " & vbCrLf     '   & _

        strSQLstring += "  and A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                 " and A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                 " and C.SETID (+) = 'MAIN1'" & vbCrLf &
                 " and C.INV_ITEM_ID(+) = B.INV_ITEM_ID " & vbCrLf &
                 " AND G.ORDER_NO = A.ORDER_NO " & vbCrLf &
                 " AND B.ISA_INTFC_LN = G.ISA_INTFC_LN" & vbCrLf &
                 " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                 " And LD.Req_id (+) = B.order_no AND LD.REQ_LINE_NBR (+) = B.ISA_INTFC_LN" & vbCrLf
        'reusing the query to union the table which will get the 'PUR' status without date limit
        'strSQLstring += " AND G.DTTM_STAMP > (TRUNC(sysdate -1) + '" & SumryMailTime & "'/24)" & vbCrLf &
        '     " AND G.DTTM_STAMP <= (TRUNC(sysdate) + '" & SumryMailTime & "'/24)" & vbCrLf

        strSQLstring += " AND EXISTS (SELECT 'X' FROM PS_ISA_WO_STATUS I " & vbCrLf &
                  "WHERE B.BUSINESS_UNIT_OM = I.BUSINESS_UNIT_OM " & vbCrLf &
                  "AND   B.ISA_WORK_ORDER_NO = I.ISA_WORK_ORDER_NO " & vbCrLf &
                  "AND   I.ISA_WO_STATUS <> 'COMPLETED')" & vbCrLf

        strSQLstring += " AND ( ((B.ISA_LINE_STATUS IN ('ASN','DSP')) AND (B.USER1='P' OR B.USER_CHAR2='PU') OR B.ISA_LINE_STATUS IN ('PUR','RPU')))" & vbCrLf 'to get the 'PUR' orders

        strSQLstring += " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID) )" & vbCrLf &
                  " ORDER BY ORDER_NO, LINE_NBR, DTTM_STAMP" & vbCrLf
        ' this is set up in the user priveleges when giving out the status code priveleges in ISOL under Add/Change User
        ' matches the orserstatus emails set up for with the order status in PS_ISAORDSTATUSLOG
        ' the tenth byte of isa_iol_op_name has the one character g.isa_order_status code
        ' example: substr(emlsubmit1,10) = '1'   order status code 1
        ' We are going to check for priveleges in the upd_email_out program that sends the emails out.

        Try
            objStreamWriter.WriteLine("  checkAllStatusWAL (2) Q1: " & strSQLstring)
            Try
                Dim st As New Stopwatch()
                st.Start()
                ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                st.Stop()
                Dim ts As TimeSpan = st.Elapsed
                Dim elapsedTime As String = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
                objStreamWriter.WriteLine("Query Execution Time " + elapsedTime)
            Catch ex As Exception
                ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                objStreamWriter.WriteLine("Query Execution Time " + Now())

            End Try

        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            objStreamWriter.WriteLine("     Error - error reading the order details" & " " & Now())
            Return True
        End Try

        If Not ds Is Nothing Then
            If ds.Tables.Count > 0 Then
                If IsDBNull(ds.Tables(0).Rows.Count) Or (ds.Tables(0).Rows.Count) = 0 Then
                    Console.WriteLine("Fetched Datas 0")
                    objGenerallLogStreamWriter.WriteLine("Fetched Datas 0" & " " & Now())
                    objStreamWriter.WriteLine("     Warning - no status changes to process at this time for All Statuses" & " " & Now())
                    Try
                        connectOR.Close()
                    Catch ex As Exception
                        objStreamWriter.WriteLine("Warning Error in fetching data- " & " " & Now())

                    End Try
                    Return False
                Else
                    Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
                    objStreamWriter.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()) & " " & Now())
                    objGenerallLogStreamWriter.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()) & " " & Now())
                End If
            Else
                Console.WriteLine("Fetched Datas 0")
                objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
                objStreamWriter.WriteLine(" Tables does not exist")
                Try
                    connectOR.Close()
                Catch ex As Exception

                End Try
                Return False
            End If
        Else
            Console.WriteLine("Fetched Datas 0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
            objStreamWriter.WriteLine("    Dataset is nothing")
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try
            Return False
        End If
        'Dim rowsaffected As Integer
        'Dim tmpOrderNo As String

        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        'Dim strPreOrderno As String
        Dim I As Integer
        'Dim X As Integer
        Dim dsEmail As New DataTable
        Dim dr1 As DataRow
        Dim dsShipTo As DataSet

        'SDI - 23457 added qty ordered, qty received and UOM column for order status email
        dsEmail.Columns.Add("Order No.")
        dsEmail.Columns.Add("Status")
        dsEmail.Columns.Add("Non-Stock Item Description")
        dsEmail.Columns.Add("Stock Item Description")
        dsEmail.Columns.Add("Item ID")
        dsEmail.Columns.Add("Line Number")
        dsEmail.Columns.Add("Time")
        dsEmail.Columns.Add("Status Code")
        dsEmail.Columns.Add("Work Order Number")
        dsEmail.Columns.Add("PO #")
        dsEmail.Columns.Add("Line Notes")
        dsEmail.Columns.Add("Tracking No")
        dsEmail.Columns.Add("Qty Ordered")
        dsEmail.Columns.Add("Qty Received")
        dsEmail.Columns.Add("UOM")
        dsEmail.Columns.Add("STORE")
        dsEmail.Columns.Add("First Name")
        dsEmail.Columns.Add("Last Name")
        dsEmail.Columns.Add("IsPriority")
        dsEmail.Columns.Add("Ship To")
        dsEmail.Columns.Add("Delivery Date")
        dsEmail.Columns.Add("Supplier Name")
        dsEmail.Columns.Add("Shipment Status")
        Try
            strSQLstring = "Select DESCR, SHIPTO_ID FROM PS_SHIPTO_TBL"
            dsShipTo = ORDBAccess.GetAdapter(strSQLstring, connectOR)
        Catch
        End Try
        Dim strdescription As String = " "
        Dim strEmailTo As String = " "
        Dim strEmpID As String = ""
        Dim OrderStatusURL As String = ConfigurationManager.AppSettings("OrderStatusURL")
        Dim OrderStatusToken As String = ConfigurationManager.AppSettings("OrderStatusToken")
        Dim lstOfString As List(Of String) = New List(Of String)

        Dim EmpIDArr As String() = ds.Tables(0).AsEnumerable().[Select](Function(r) r.Field(Of String)("ISA_EMPLOYEE_ID")).Distinct().ToArray()

        For Each EmpID As String In EmpIDArr
            Try
                Dim OrderDetailDT As New DataTable

                OrderDetailDT = (From C In ds.Tables(0).AsEnumerable Where C.Field(Of String)("ISA_EMPLOYEE_ID") = EmpID).CopyToDataTable()
                objGenerallLogStreamWriter.WriteLine("Reading order details of Employee: " + EmpID)
                objStreamWriter.WriteLine("Reading order details of Employee:" + EmpID & " " & Now())

                For I = 0 To OrderDetailDT.Rows.Count - 1
                    Dim strStatus_code As String = " "
                    Try
                        strStatus_code = OrderDetailDT.Rows(I).Item("STATUS_CODE")
                        strStatus_code = strStatus_code.Substring(9)

                    Catch ex As Exception
                        strStatus_code = " "
                    End Try
                    Dim strSiteBU As String
                    If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                        connectOR.Close()
                    End If
                    Dim Command As OleDbCommand

                    strSQLstring = "SELECT A.BUSINESS_UNIT" & vbCrLf &
                        " FROM PS_REQ_LOADER_DFL A" & vbCrLf &
                        " WHERE A.LOADER_BU = '" & strBU & "'" & vbCrLf

                    objStreamWriter.WriteLine("  CheckAllStatusWAL (3): " & strSQLstring & " " & Now())

                    Command = New OleDbCommand(strSQLstring, connectOR)
                    connectOR.Open()
                    Try
                        strSiteBU = Command.ExecuteScalar
                        connectOR.Close()
                    Catch ex As Exception
                        objStreamWriter.WriteLine("  StatChg Email NSTK send select siteBU for " & strBU & " " & Now())
                        connectOR.Close()
                        strSiteBU = "ISA00"
                    End Try

                    dr1 = dsEmail.NewRow()
                    objStreamWriter.WriteLine("Setting details of order " + OrderDetailDT.Rows(I).Item("ORDER_NO") + "to email datatset" & " " & Now())
                    Dim Dtformat As String = "MM/dd/yyyy"
                    Dim stroderno As String = OrderDetailDT.Rows(I).Item("ORDER_NO")
                    Dim strlineno As String = OrderDetailDT.Rows(I).Item("LINE_NBR")
                    dr1.Item(0) = OrderDetailDT.Rows(I).Item("ORDER_NO")
                    dr1.Item(1) = OrderDetailDT.Rows(I).Item("ORDER_STATUS_DESC")
                    dr1.Item(2) = OrderDetailDT.Rows(I).Item("NONSTOCK_DESCRIPTION")
                    dr1.Item(3) = OrderDetailDT.Rows(I).Item("STOCK_DESCRIPTION")
                    dr1.Item(4) = OrderDetailDT.Rows(I).Item("INV_ITEM_ID")
                    dr1.Item(5) = OrderDetailDT.Rows(I).Item("LINE_NBR")
                    Dim ln_notes As String = ""
                    ln_notes = GetLineNotes(stroderno, strBU, strlineno)
                    dr1.Item(10) = ln_notes
                    connectOR.Open()
                    dr1.Item(6) = OrderDetailDT.Rows(I).Item("DTTM_STAMP")
                    If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                        connectOR.Close()
                    End If

                    'just get the last character
                    dr1.Item(7) = strStatus_code
                    dr1.Item(8) = OrderDetailDT.Rows(I).Item("WORK_ORDER_NO")
                    dr1.Item(9) = OrderDetailDT.Rows(I).Item("PO_ID")
                    Dim trackingNo As String = ""
                    Try
                        trackingNo = OrderDetailDT.Rows(I).Item("ISA_ASN_TRACK_NO")
                    Catch ex As Exception
                        trackingNo = ""
                    End Try

                    If Not String.IsNullOrEmpty(trackingNo) Then
                        If trackingNo.Contains("1Z") Then
                            Dim URL As String = "http://wwwapps.ups.com/WebTracking/processInputRequest?HTMLVersion=5.0&sort_by=status&term_warn=yes&tracknums_displayed=5&TypeOfInquiryNumber=T&loc=en_US&InquiryNumber1=" & trackingNo & "&InquiryNumber2=&InquiryNumber3=&InquiryNumber4=&InquiryNumber5=&AgreeToTermsAndConditions=yes&track.x=25&track.y=9','','"
                            Dim m_cURL1 As String = "<a href=""" & URL & """ target=""_blank"">" & trackingNo & "</a>"
                            dr1.Item(11) = m_cURL1
                        Else
                            Dim URL As String = "https://www.fedex.com/apps/fedextrack/?action=track&trackingnumber=" & trackingNo & "&cntry_code=us&locale=en_US"
                            Dim m_cURL1 As String = "<a href=""" & URL & """ target=""_blank"">" & trackingNo & "</a>"
                            dr1.Item(11) = m_cURL1
                        End If
                    Else
                        dr1.Item(11) = "-"
                    End If
                    Try
                        dr1.Item(12) = OrderDetailDT.Rows(I).Item("QTY_REQUESTED")
                    Catch ex As Exception
                        dr1.Item(12) = ""
                    End Try

                    Try
                        dr1.Item(13) = OrderDetailDT.Rows(I).Item("QTY_RECEIVED")
                    Catch ex As Exception
                        dr1.Item(13) = ""
                    End Try
                    Try
                        dr1.Item(14) = OrderDetailDT.Rows(I).Item("UNIT_OF_MEASURE")
                    Catch ex As Exception
                        dr1.Item(14) = ""
                    End Try

                    Try
                        dr1.Item(15) = OrderDetailDT.Rows(I).Item("STORE")
                    Catch ex As Exception
                        dr1.Item(15) = ""
                    End Try

                    Try
                        dr1.Item(16) = OrderDetailDT.Rows(I).Item("FIRST_NAME_SRCH")
                    Catch ex As Exception
                        dr1.Item(16) = ""
                    End Try

                    Try
                        dr1.Item(17) = OrderDetailDT.Rows(I).Item("LAST_NAME_SRCH")
                    Catch ex As Exception
                        dr1.Item(17) = ""
                    End Try

                    Try
                        dr1.Item(18) = OrderDetailDT.Rows(I).Item("IsPriority")
                    Catch ex As Exception
                        dr1.Item(18) = ""
                    End Try


                    If OrderDetailDT.Rows(I).Item("SHIPTO").ToString <> "" Then
                        Try
                            Dim Descr As String = dsShipTo.Tables(0).AsEnumerable().
     Where(Function(r) Convert.ToString(r.Field(Of String)("SHIPTO_ID")) = OrderDetailDT.Rows(I).Item("SHIPTO").ToString).
     Select(Function(r) Convert.ToString(r.Field(Of String)("DESCR"))).FirstOrDefault()
                            dr1.Item(19) = Descr + "_" + OrderDetailDT.Rows(I).Item("SHIPTO").ToString
                        Catch
                            dr1.Item(19) = ""
                        End Try

                    End If

                    Try
                        dr1.Item(20) = Convert.ToDateTime(OrderDetailDT.Rows(I).Item("ISA_REQUIRED_BY_DT")).ToString(Dtformat)
                    Catch ex As Exception
                        dr1.Item(20) = "-"
                    End Try

                    Dim vendor As String = OrderDetailDT.Rows(I).Item("VENDOR_ID")
                    Dim Cmd As OleDbCommand
                    Dim vendorName As String

                    strSQLstring = "Select NAME1 from PS_Vendor where VENDOR_ID= '" & vendor & "'"

                    objStreamWriter.WriteLine("  CheckAllStatusWAL (4): " & strSQLstring & " " & Now())


                    Cmd = New OleDbCommand(strSQLstring, connectOR)
                    connectOR.Open()
                    Try
                        vendorName = Cmd.ExecuteScalar
                        dr1.Item(21) = OrderDetailDT.Rows(I).Item("VENDOR_ID") + "- " + vendorName
                        connectOR.Close()
                    Catch ex As Exception
                        objStreamWriter.WriteLine("  Exception in fetching vendor ID " & " " & Now())
                        connectOR.Close()
                        dr1.Item(21) = ""
                    End Try

                    Try
                        If OrderDetailDT.Rows(I).Item("SHIP_STATUS").Trim() <> "" Then
                            dr1.Item(22) = OrderDetailDT.Rows(I).Item("SHIP_STATUS")
                        Else
                            dr1.Item(22) = "-"
                        End If
                    Catch ex As Exception
                        dr1.Item(22) = "-"
                    End Try

                    dsEmail.Rows.Add(dr1)
                    ' "R" nonstock
                    ' "7" stock

                    strEmailTo = OrderDetailDT.Rows(I).Item("ISA_EMPLOYEE_EMAIL")
                    strEmpID = OrderDetailDT.Rows(I).Item("ISA_EMPLOYEE_ID")

                Next

                sendCustEmailWal(dsEmail, strEmpID, strEmailTo, strBU)

                dsEmail.Clear()
            Catch ex As Exception

            End Try
        Next


        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If

    End Function
    'Madhu-INC0027259-Status change utility - Optimisation
    Private Function checkAllStatusNew(ByVal strBU As String) As Boolean
        Dim strSQLstring As String
        Dim dteEndDate As DateTime = Now

        Dim format As New System.Globalization.CultureInfo("en-US", True)
        strSQLstring = "SELECT" & vbCrLf &
            " to_char(MAX( A.DTTM_STAMP), 'MM/DD/YY HH24:MI:SS') as MAXDATE" & vbCrLf &
            " FROM PS_ISAORDSTATUSLOG A" & vbCrLf &
             " WHERE A.BUSINESS_UNIT_OM = '" & strBU & "' "

        Dim dr As OleDbDataReader = Nothing

        Try
            objStreamWriter.WriteLine("  checkAllStatusNew (1): " & strSQLstring & " " & Now())

            Dim command As OleDbCommand
            command = New OleDbCommand(strSQLstring, connectOR)
            If connectOR.State = ConnectionState.Open Then
                'do nothing
            Else
                connectOR.Open()
            End If
            dr = command.ExecuteReader
            Try

                If dr.Read Then
                    dteEndDate = (dr.Item("MAXDATE"))
                    dteEndDate = dteEndDate.AddMinutes(+1)
                Else
                    dteEndDate = Now.ToString
                End If
            Catch ex As Exception
                dteEndDate = Now.ToString
            End Try

            dr.Close()
            connectOR.Close()

        Catch OleDBExp As OleDbException
            Try
                dr.Close()
                connectOR.Close()
            Catch exOR As Exception

            End Try
            objStreamWriter.WriteLine("     Error - error reading end date FROM PS_ISAORDSTATUSLOG A" & " " & Now())
            Return True
        End Try

        connectOR.Open()
        Dim objEnterprise As New clsEnterprise(strBU, connectOR)
        Dim dteCustID As String = objEnterprise.CustID
        Dim dteCompanyID As String = objEnterprise.CompanyID
        Dim dteStartDate As DateTime = objEnterprise.SendStartDate
        Dim dteSiteEmail As String = objEnterprise.SiteEmail
        Dim dteSTKREQEmail As String = objEnterprise.STKREQEmail
        Dim dteNONSKREQEmail As String = objEnterprise.NONSKREQEmail

        'If strBU = "I0970" Then
        '    dteEndDate = dteEndDate.AddMinutes(+1)
        '    'dteStartDate = dteStartDate.AddHours(-36)          
        'End If

        Try
            connectOR.Close()
        Catch ex As Exception

        End Try

        Dim ds As New DataSet
        Dim bolerror1 As Boolean

        ' check is processed order is ASCEND order
        Dim bIsAscend As Boolean = False
        If Trim(strBU) <> "" Then
            bIsAscend = IsBuAscend(strBU)
        End If

        dteEndDate.AddSeconds(1)
        'Madhu-WAL-1203-Commenting this part of code since we have separated the utility

        'If strBU = "I0W01" Then
        '    Try
        '        UpdateWalmartSourceCode(dteStartDate, dteEndDate, strBU)
        '    Catch

        '    End Try
        'End If

        ' stock items will get item id from the ps_isa_ord_intfc_l table  but description from the PS_MASTER_ITEM_TB
        ' non-stock items  has no item-id num and gets description from the ps_isa_ord_intfc_l
        ' PS_ISAORDSTATUSLOG the line number points to the line number in ps_isa_ord_intfc_l
        ' DO NOT SELECT G.ISA_ORDER_STATUS = '6'  WE ARE GETTING IT UP TOP.
        '         '  

        '4/26/2022 Walmart order's status change emails alone won't be sent based on selected email privileges in user profile, removed that condition from below query - Poornima S
        If strBU = "I0W01" Then
            strSQLstring = "SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                 " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE,B.OPRID_ENTERED_BY, B.SHIPTO_ID as SHIPTO,B.ISA_USER2 as STORE," & vbCrLf &
                 " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, " & vbCrLf   '  & _

            strSQLstring += "  G.ISA_LINE_STATUS AS ISA_ORDER_STATUS," & vbCrLf
        Else
            strSQLstring = "SELECT H.ISA_IOL_OP_NAME as STATUS_CODE, TBL.* FROM (SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                 " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE,B.OPRID_ENTERED_BY, B.SHIPTO_ID as SHIPTO,B.ISA_USER2 as STORE," & vbCrLf &
                 " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, " & vbCrLf   '  & _

            strSQLstring += "  G.ISA_LINE_STATUS AS ISA_ORDER_STATUS, DECODE(G.ISA_LINE_STATUS,'CRE','01','QTW','02','QTC','03','QTS','04','CST','05','VND','06','APR','07','QTA','08','QTR','09','RFA','10','RFR','11','RFC','12','RCF','13','RCP','14','CNC','15','DLF','16','PKA','17','ASN','18') AS OLD_ORDER_STATUS," & vbCrLf
        End If

        strSQLstring += " (SELECT E.XLATLONGNAME" & vbCrLf &
                                " FROM XLATTABLE E" & vbCrLf &
                                " WHERE E.EFFDT =" & vbCrLf &
                                " (SELECT MAX(E_ED.EFFDT) FROM XLATTABLE E_ED" & vbCrLf &
                                " WHERE(E.FIELDNAME = E_ED.FIELDNAME)" & vbCrLf &
                                " AND E.FIELDVALUE = E_ED.FIELDVALUE" & vbCrLf &
                                " AND E_ED.EFFDT <= SYSDATE)" & vbCrLf &
                                " AND E.FIELDNAME = 'ISA_LINE_STATUS'" & vbCrLf &
                                " AND E.FIELDVALUE = G.ISA_LINE_STATUS) as ORDER_STATUS_DESC, " & vbCrLf &
                 " B.DESCR254 As NONSTOCK_DESCRIPTION, C.DESCR60 as STOCK_DESCRIPTION, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                 " B.INV_ITEM_ID as INV_ITEM_ID," & vbCrLf &
                 " B.QTY_REQUESTED,B.QTY_RECEIVED,B.UNIT_OF_MEASURE," & vbCrLf &
                 " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH" & vbCrLf &
                 " ,A.origin, LD.PO_ID," & vbCrLf
        If strBU = "I0W01" Then
            '[11-28-2022]WW-554 Changed the query to get tracking num from Tracking_Number function since the inventory orders are not available in PS_ISA_ASN_SHIPPED table -- Poornima S
            strSQLstring += "(select Tracking_Number(B.ORDER_NO,B.ISA_INTFC_LN,B.BUSINESS_UNIT_OM) from dual) as ISA_ASN_TRACK_NO FROM" & vbCrLf
        Else
            strSQLstring += "SH.ISA_ASN_TRACK_NO" & vbCrLf &
                " FROM PS_ISA_ASN_SHIPPED SH," & vbCrLf
        End If

        strSQLstring += " ps_isa_ord_intf_HD A," & vbCrLf &
                 " ps_isa_ord_intf_LN B," & vbCrLf &
                 " PS_MASTER_ITEM_TBL C," & vbCrLf &
                 " PS_ISA_USERS_TBL D," & vbCrLf &
                 " PS_ISAORDSTATUSLOG G, PS_PO_LINE_DISTRIB LD" & vbCrLf &
                 " where G.BUSINESS_UNIT_OM = '" & strBU & "' " & vbCrLf &
                 " AND G.BUSINESS_UNIT_OM = A.BUSINESS_UNIT_OM " & vbCrLf


        strSQLstring += "  and A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                 " and A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                 " and C.SETID (+) = 'MAIN1'" & vbCrLf &
                 " and C.INV_ITEM_ID(+) = B.INV_ITEM_ID " & vbCrLf &
                 " AND G.ORDER_NO = A.ORDER_NO " & vbCrLf &
                 " AND B.ISA_INTFC_LN = G.ISA_INTFC_LN" & vbCrLf &
                " AND B.ISA_LINE_STATUS = G.ISA_LINE_STATUS " & vbCrLf

        If strBU <> "I0W01" Then
            strSQLstring += " AND SH.PO_ID (+) = LD.PO_ID And SH.LINE_NBR (+) = LD.LINE_NBR And SH.SCHED_NBR (+) = LD.SCHED_NBR" & vbCrLf
        End If

        strSQLstring += " And LD.Req_id (+) = B.order_no AND LD.REQ_LINE_NBR (+) = B.ISA_INTFC_LN" & vbCrLf &
        " AND G.DTTM_STAMP > TO_DATE('" & dteStartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
        " AND G.DTTM_STAMP <= TO_DATE('" & dteEndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf

        If strBU = "I0W01" Then
            strSQLstring += " AND EXISTS (SELECT 'X' FROM PS_ISA_WO_STATUS I " & vbCrLf &
                 " WHERE B.BUSINESS_UNIT_OM = I.BUSINESS_UNIT_OM " & vbCrLf &
                 " AND B.ISA_WORK_ORDER_NO = I.ISA_WORK_ORDER_NO " & vbCrLf &
                 " AND I.ISA_WO_STATUS <> 'COMPLETED')" & vbCrLf

            strSQLstring += "AND G.ISA_LINE_STATUS IN ('CRE','QTW','QTC','QTS','CST','VND','APR','QTA','RCF','RCP','DLF','PKA','ASN')" & vbCrLf

            strSQLstring += " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID)" & vbCrLf &
                  " ORDER BY ORDER_NO, LINE_NBR, DTTM_STAMP" & vbCrLf
        Else
            strSQLstring += " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID)) TBL, PS_ISA_USERS_PRIVS H " & vbCrLf &
                 " WHERE TBL.EMPLID = H.ISA_EMPLOYEE_ID " & vbCrLf &
                 "AND TBL.G_BUS_UNIT = H.BUSINESS_UNIT" & vbCrLf &
                 " AND SUBSTR(H.ISA_IOL_OP_NAME,9) = TBL.OLD_ORDER_STATUS " & vbCrLf &
                 " AND H.ISA_IOL_OP_VALUE = 'Y' " & vbCrLf &
                  " ORDER BY ORDER_NO, LINE_NBR, DTTM_STAMP"

        End If
        ' this is set up in the user priveleges when giving out the status code priveleges in ISOL under Add/Change User
        ' matches the orserstatus emails set up for with the order status in PS_ISAORDSTATUSLOG
        ' the tenth byte of isa_iol_op_name has the one character g.isa_order_status code
        ' example: substr(emlsubmit1,10) = '1'   order status code 1
        ' We are going to check for priveleges in the upd_email_out program that sends the emails out.

        Try
            objStreamWriter.WriteLine("  checkAllStatusNew (2) Q1: " & strSQLstring)
            Dim st As New Stopwatch()
            st.Start()
            ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
            st.Stop()
            Dim ts As TimeSpan = st.Elapsed
            Dim elapsedTime As String = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
            objStreamWriter.WriteLine("Query Execution Time " + elapsedTime)
        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            objStreamWriter.WriteLine("     Error - error reading transaction FROM PS_ISAORDSTATUSLOG A" & " " & Now())
            Return True
        End Try

        If Not ds Is Nothing Then
            If ds.Tables.Count > 0 Then
                If IsDBNull(ds.Tables(0).Rows.Count) Or (ds.Tables(0).Rows.Count) = 0 Then
                    Console.WriteLine("Fetched Datas 0")
                    objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
                    objStreamWriter.WriteLine("     Warning - no status changes to process at this time for All Statuses")
                    Try
                        connectOR.Close()
                    Catch ex As Exception

                    End Try
                    Return False
                Else
                    Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
                    objGenerallLogStreamWriter.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
                End If
            Else
                Console.WriteLine("Fetched Datas 0")
                objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
                objStreamWriter.WriteLine(" Tables does not exist")
                Try
                    connectOR.Close()
                Catch ex As Exception

                End Try
                Return False
            End If
        Else
            Console.WriteLine("Fetched Datas 0")
            objGenerallLogStreamWriter.WriteLine("Fetched Datas 0")
            objStreamWriter.WriteLine("    Dataset is nothing")
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try
            Return False
        End If
        Dim rowsaffected As Integer
        Dim tmpOrderNo As String

        If connectOR.State = ConnectionState.Open Then
            'do nothing
        Else
            connectOR.Open()
        End If
        Dim strPreOrderno As String
        Dim I As Integer
        Dim X As Integer
        Dim dsEmail As New DataTable
        Dim dr1 As DataRow
        Dim dsShipTo As DataSet

        'SDI - 23457 added qty ordered, qty received and UOM column for order status email
        dsEmail.Columns.Add("Order No.")
        dsEmail.Columns.Add("Status")
        dsEmail.Columns.Add("Non-Stock Item Description")
        dsEmail.Columns.Add("Stock Item Description")
        dsEmail.Columns.Add("Item ID")
        dsEmail.Columns.Add("Line Number")
        dsEmail.Columns.Add("Time")
        dsEmail.Columns.Add("Status Code")
        dsEmail.Columns.Add("Work Order Number")
        dsEmail.Columns.Add("PO #")
        dsEmail.Columns.Add("Line Notes")
        dsEmail.Columns.Add("Tracking No")
        dsEmail.Columns.Add("Qty Ordered")
        dsEmail.Columns.Add("Qty Received")
        dsEmail.Columns.Add("UOM")
        If strBU = "I0W01" Then
            dsEmail.Columns.Add("Ship To")
            Try
                strSQLstring = "SELECT DESCR,SHIPTO_ID FROM PS_SHIPTO_TBL"
                dsShipTo = ORDBAccess.GetAdapter(strSQLstring, connectOR)
            Catch
            End Try
        End If
        Dim strdescription As String = " "
        Dim strEmailTo As String = " "
        Dim strEmpID As String = ""
        Dim OrderStatusURL As String = ConfigurationManager.AppSettings("OrderStatusURL")
        Dim OrderStatusToken As String = ConfigurationManager.AppSettings("OrderStatusToken")
        Dim lstOfString As List(Of String) = New List(Of String)
        For I = 0 To ds.Tables(0).Rows.Count - 1
            Dim strStatus_code As String = " "
            Try
                strStatus_code = ds.Tables(0).Rows(I).Item("STATUS_CODE")
                strStatus_code = strStatus_code.Substring(9)

            Catch ex As Exception
                strStatus_code = " "
            End Try
            Dim strSiteBU As String
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            Dim Command As OleDbCommand

            strSQLstring = "SELECT A.BUSINESS_UNIT" & vbCrLf &
                    " FROM PS_REQ_LOADER_DFL A" & vbCrLf &
                    " WHERE A.LOADER_BU = '" & strBU & "'" & vbCrLf

            objStreamWriter.WriteLine("  CheckAllStatusNew (3): " & strSQLstring)

            Command = New OleDbCommand(strSQLstring, connectOR)
            connectOR.Open()
            Try
                strSiteBU = Command.ExecuteScalar
                connectOR.Close()
            Catch ex As Exception
                objStreamWriter.WriteLine("  StatChg Email NSTK send select siteBU for " & strBU)
                connectOR.Close()
                strSiteBU = "ISA00"
            End Try

            dr1 = dsEmail.NewRow()
            Dim stroderno As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
            Dim strlineno As String = ds.Tables(0).Rows(I).Item("LINE_NBR")
            dr1.Item(0) = ds.Tables(0).Rows(I).Item("ORDER_NO")
            dr1.Item(1) = ds.Tables(0).Rows(I).Item("ORDER_STATUS_DESC")
            dr1.Item(2) = ds.Tables(0).Rows(I).Item("NONSTOCK_DESCRIPTION")
            dr1.Item(3) = ds.Tables(0).Rows(I).Item("STOCK_DESCRIPTION")
            dr1.Item(4) = ds.Tables(0).Rows(I).Item("INV_ITEM_ID")
            dr1.Item(5) = ds.Tables(0).Rows(I).Item("LINE_NBR")
            Dim ln_notes As String = ""
            ln_notes = GetLineNotes(stroderno, strBU, strlineno)
            If ln_notes Is Nothing OrElse ln_notes = "(null)" OrElse ln_notes.Trim() = "" Then
                ln_notes = "-"
            End If
            dr1.Item(10) = ln_notes
            connectOR.Open()
            dr1.Item(6) = ds.Tables(0).Rows(I).Item("DTTM_STAMP")
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            'Dim strpo_id As String = getpo_id(stroderno, strlineno, strBU, strSiteBU)
            'dr1.Item(7) = ds.Tables(0).Rows(I).Item("STATUS_CODE")
            'just get the last character
            dr1.Item(7) = strStatus_code
            dr1.Item(8) = ds.Tables(0).Rows(I).Item("WORK_ORDER_NO")
            dr1.Item(9) = ds.Tables(0).Rows(I).Item("PO_ID")
            Dim trackingNo As String = ""
            Try
                trackingNo = ds.Tables(0).Rows(I).Item("ISA_ASN_TRACK_NO")
            Catch ex As Exception
                trackingNo = ""
            End Try


            If Not String.IsNullOrEmpty(trackingNo) Then
                If trackingNo.Contains("1Z") Then
                    Dim URL As String = "http://wwwapps.ups.com/WebTracking/processInputRequest?HTMLVersion=5.0&sort_by=status&term_warn=yes&tracknums_displayed=5&TypeOfInquiryNumber=T&loc=en_US&InquiryNumber1=" & trackingNo & "&InquiryNumber2=&InquiryNumber3=&InquiryNumber4=&InquiryNumber5=&AgreeToTermsAndConditions=yes&track.x=25&track.y=9','','"
                    Dim m_cURL1 As String = "<a href=""" & URL & """ target=""_blank"">" & trackingNo & "</a>"
                    dr1.Item(11) = m_cURL1
                Else
                    Dim URL As String = "https://www.fedex.com/apps/fedextrack/?action=track&trackingnumber=" & trackingNo & "&cntry_code=us&locale=en_US"
                    Dim m_cURL1 As String = "<a href=""" & URL & """ target=""_blank"">" & trackingNo & "</a>"
                    dr1.Item(11) = m_cURL1
                End If
            Else
                dr1.Item(11) = "-"
            End If
            Try
                dr1.Item(12) = ds.Tables(0).Rows(I).Item("QTY_REQUESTED")
            Catch ex As Exception
                dr1.Item(12) = ""
            End Try
            Try
                dr1.Item(13) = ds.Tables(0).Rows(I).Item("QTY_RECEIVED")
            Catch ex As Exception
                dr1.Item(13) = ""
            End Try
            Try
                dr1.Item(14) = ds.Tables(0).Rows(I).Item("UNIT_OF_MEASURE")
            Catch ex As Exception
                dr1.Item(14) = ""
            End Try


            If strBU = "I0W01" Then
                If ds.Tables(0).Rows(I).Item("SHIPTO").ToString <> "" Then
                    Try
                        Dim Descr As String = dsShipTo.Tables(0).AsEnumerable().
 Where(Function(r) Convert.ToString(r.Field(Of String)("SHIPTO_ID")) = ds.Tables(0).Rows(I).Item("SHIPTO").ToString).
 Select(Function(r) Convert.ToString(r.Field(Of String)("DESCR"))).FirstOrDefault()
                        dr1.Item(15) = Descr + "_" + ds.Tables(0).Rows(I).Item("SHIPTO").ToString
                    Catch
                        dr1.Item(15) = ""
                    End Try

                End If
            End If
            dsEmail.Rows.Add(dr1)
            ' "R" nonstock
            ' "7" stock
            If ds.Tables(0).Rows(I).Item("Origin") = "MIS" And strBU = "I0206" Then
                strdescription = "PICKED"
            Else
                Try
                    strdescription = ds.Tables(0).Rows(I).Item("ORDER_STATUS_DESC")
                Catch ex As Exception
                    strdescription = "Err_line_" & I.ToString()
                End Try

            End If
            strEmailTo = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_EMAIL")
            strEmpID = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_ID")
            Dim Store As String = String.Empty
            Try
                Store = ds.Tables(0).Rows(I).Item("STORE")
                Store = Store.Split("-").LastOrDefault().Trim()

            Catch ex As Exception
            End Try
            'INC0022437 - Scott: We are being told by our NYC DOE - Dhamotharan P
            Try

                Dim StrShipTo As String = ds.Tables(0).Rows(I).Item("SHIPTO")
                Dim strLineSts As String = ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS")


                If ((StrShipTo <> "L0260-01") Or (StrShipTo = "L0260-01" And strLineSts <> "RCP" And strLineSts <> "RCF")) Then

                    If I = ds.Tables(0).Rows.Count - 1 Then

                        sendCustEmail1(dsEmail,
                            ds.Tables(0).Rows(I).Item("ORDER_NO"),
                            dteCompanyID,
                            dteCustID,
                            ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"),
                            strdescription,
                            ds.Tables(0).Rows(I).Item("INV_ITEM_ID"),
                            ds.Tables(0).Rows(I).Item("LINE_NBR"),
                            ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                            ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                            strEmailTo,
                            ds.Tables(0).Rows(I).Item("Origin"),
                            strBU, strEmpID,
                            ds.Tables(0).Rows(I).Item("WORK_ORDER_NO"),
                            Store
                                )

                        dsEmail.Clear()
                        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                            connectOR.Close()
                        End If
                    ElseIf ds.Tables(0).Rows(I + 1).Item("BUSINESS_UNIT_OM") _
                                      & ds.Tables(0).Rows(I + 1).Item("ORDER_NO") <>
                                      ds.Tables(0).Rows(I).Item("BUSINESS_UNIT_OM") _
                                      & ds.Tables(0).Rows(I).Item("ORDER_NO") Then

                        sendCustEmail1(dsEmail,
                           ds.Tables(0).Rows(I).Item("ORDER_NO"),
                           dteCompanyID,
                           dteCustID,
                           ds.Tables(0).Rows(I).Item("ISA_ORDER_STATUS"),
                           strdescription,
                           ds.Tables(0).Rows(I).Item("INV_ITEM_ID"),
                           ds.Tables(0).Rows(I).Item("LINE_NBR"),
                           ds.Tables(0).Rows(I).Item("FIRST_NAME_SRCH"),
                           ds.Tables(0).Rows(I).Item("LAST_NAME_SRCH"),
                           strEmailTo,
                           ds.Tables(0).Rows(I).Item("Origin"),
                           strBU, strEmpID,
                           ds.Tables(0).Rows(I).Item("WORK_ORDER_NO"),
                           Store
                           )

                        dsEmail.Clear()
                        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                            connectOR.Close()
                        End If
                    End If
                End If
            Catch ex As Exception

            End Try


        Next

        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If

        'If strBU <> "I0256" Then
        bolerror1 = updateEnterprise(strBU, dteEndDate)
        'End If
    End Function
    'Below Method is to send order staus summary email for Walmart users -- WW-287 & WAL-632 Poornima S
    Private Sub sendCustEmailWal(ByVal dtEmail As DataTable, ByVal EmpID As String, ByVal EmailTo As String, ByVal strBU As String)
        Dim SDIEmailService As SDiEmailUtilityService.EmailServices = New SDiEmailUtilityService.EmailServices()
        Dim MailAttachmentName As String()
        Dim MailAttachmentbytes As New List(Of Byte())()
        Dim strbodyhead1 As String
        Dim strbodydet1 As String
        Dim dataGridHTML1 As String
        Dim SBnstk1 As New StringBuilder
        Dim SWnstk1 As New StringWriter(SBnstk1)
        Dim htmlTWnstk1 As New HtmlTextWriter(SWnstk1)

        Dim Mailer1 As MailMessage = New MailMessage
        Dim strccfirst1 As String = "erwin.bautista"  '  "pete.doyle"
        Dim strcclast1 As String = "sdi.com"
        'SP-316 get from email from table - Dhamotharan
        'SDI-40628 Changing Mail id as walmartpurchasing@sdi.com from sdiexchange@sdi.com for Walmart BU.
        'If strBU = "I0W01" Then
        '    Mailer1.From = "WalmartPurchasing@sdi.com"
        'Else
        '    Mailer1.From = "SDIExchange@SDI.com"  '  "Insiteonline@SDI.com"
        'End If
        Try
            Dim BU As String = ""
            If strBU = "I0W01" Then
                BU = "WAL00"
                Mailer1.From = getFromMail(BU, connectOR)
            ElseIf strBU = "I0631" Then
                BU = "EMC00"
                Mailer1.From = getFromMail(BU, connectOR)
            Else
                BU = "ISA00"
                Mailer1.From = getFromMail(BU, connectOR)
            End If
            'Mythili -- INC0023448 Adding CC emails
            Mailer1.Cc = getToMail(BU, connectOR)
        Catch ex As Exception

        End Try


        Mailer1.Bcc = strccfirst1 & "@" & strcclast1
        strbodyhead1 = "<table width='100%' bgcolor='black'><tbody><tr><td><img data-imagetype='External' src='https://www.sdizeus.com/images/SDNewLogo_Email.png' alt='SDI' vspace='0' hspace='0'></td><td width='100%'><center><span style='font-family:Calibri; font-size:x-large; text-align: center; color:White' > SDI Marketplace</span></center><center><span style='text-align: center; margin:0px auto; color:White'>SDiExchange - Order Status</span></center></td></tr></tbody></table>"
        'strbodyhead1 = "<table width='100%'><tbody><tr><td><img src='http://www.sdiexchange.com/images/SDILogo_Email.png' alt='SDI' width='98px' height='82px' vspace='0' hspace='0' /></td><td width='100%'><br /><br /><br /><br /><br /><br /><center><span style='font-family: Calibri; font-size: 32px; text-align: center;font-weight:bold'>SDI Marketplace</span></center><center style='font-size:18px'><span style='text-align: center; margin: 0px auto; font-family:Calibri;'>SDiExchange - Order Status</span></center></td></tr></tbody></table>"
        strbodyhead1 = strbodyhead1 & "<HR width='100%' SIZE='1'>"
        strbodyhead1 = strbodyhead1 & "&nbsp;" & vbCrLf


        Dim strPurchaserName As String = dtEmail(0).Item("First Name") & " " & dtEmail(0).Item("Last Name")
        Dim strPurchaserEmail As String = EmailTo

        strbodydet1 = "&nbsp;" & vbCrLf
        strbodydet1 = strbodydet1 & "<div>"
        strbodydet1 = strbodydet1 & "<p style='font-family:Calibri;font-size:18px'>Hello " & strPurchaserName & ",</p>"

        Dim dtgEmail1 As WebControls.DataGrid
        dtgEmail1 = New WebControls.DataGrid
        Dim IsPrioAvail As Boolean = False
        Dim IsNonPrioAvail As Boolean = False
        Dim DtCount As Integer
        Dim I As Integer
        Dim dtPrioOrders As New DataTable
        Dim dtNonPrio As New DataTable

        Try
            dtPrioOrders = (From C In dtEmail.AsEnumerable Where C.Field(Of String)("IsPriority") = "R" Or C.Field(Of String)("IsPriority") = "E").CopyToDataTable()
        Catch ex As Exception
        End Try

        Try
            dtNonPrio = (From C In dtEmail.AsEnumerable Where C.Field(Of String)("IsPriority") <> "R" And C.Field(Of String)("IsPriority") <> "E").CopyToDataTable()
        Catch ex As Exception
        End Try

        If ((Not IsDBNull(dtPrioOrders.Rows.Count)) And (Not dtPrioOrders.Rows.Count = 0)) And ((Not IsDBNull(dtNonPrio.Rows.Count)) And (Not dtNonPrio.Rows.Count = 0)) Then
            IsPrioAvail = True
            IsNonPrioAvail = True
            DtCount = 2
        ElseIf ((Not IsDBNull(dtPrioOrders.Rows.Count)) And (Not dtPrioOrders.Rows.Count = 0)) Then
            IsPrioAvail = True
            DtCount = 1
        ElseIf ((Not IsDBNull(dtNonPrio.Rows.Count)) And (Not dtNonPrio.Rows.Count = 0)) Then
            IsNonPrioAvail = True
            DtCount = 1
        End If

        For I = 1 To DtCount
            Dim StoreNumDT As New DataTable
            If (IsPrioAvail = True) Then
                StoreNumDT = dtPrioOrders
            ElseIf (IsNonPrioAvail = True) Then
                StoreNumDT = dtNonPrio
            End If

            If IsPrioAvail Then
                strbodydet1 = strbodydet1 & "<span style='font-family:Calibri;font-size: 21px;margin-bottom:10px;width:100%;float:left'><B>PRIORITY ORDERS</B></span>"
            ElseIf IsNonPrioAvail Then
                strbodydet1 = strbodydet1 & "<span style='font-family:Calibri;font-size: 21px;margin-bottom:10px;width:100%;float:left'><B>STANDARD ORDERS</B></span>"
            End If

            Dim dateAsString As String = DateTime.Now.ToString("MM/dd/yyyy")
            Dim IsProdDB As Boolean = False

            If Not getDBName() Then
                Mailer1.To = "webdev@sdi.com"
                Mailer1.Subject = "<<TEST SITE>>Order Status Summary - " & dateAsString & ""
            Else
                Mailer1.To = EmailTo
                Mailer1.Subject = "Order Status Summary - " & dateAsString & ""
                IsProdDB = True
            End If

            Dim K As Integer = 0
            Dim StoreNumArr As String() = StoreNumDT.AsEnumerable().[Select](Function(r) r.Field(Of String)("STORE")).Distinct().ToArray()
            Array.Sort(StoreNumArr)

            For Each StoreNum As String In StoreNumArr

                'SP-316 No store number for EMCOR - Dhamotharan
                Dim NewStoreNumDT As New DataTable
                Try

                    NewStoreNumDT = (From C In StoreNumDT.AsEnumerable Where C.Field(Of String)("STORE") = StoreNum).CopyToDataTable()
                    If strBU <> "I0631" Then
                        objGenerallLogStreamWriter.WriteLine("Reading order details of Store Num:" + StoreNum & " " & Now())
                        objStreamWriter.WriteLine("Reading order details of Store Num:" + StoreNum & " " & Now())

                        strbodydet1 = strbodydet1 & "<div style='float:left;width:100%;margin-bottom:30px'>"
                        If Not (String.IsNullOrEmpty(StoreNum.Trim())) Then
                            strbodydet1 = strbodydet1 & "<p><span style='background-color:#dbf4f7;padding:10px 15px;border-radius:36px;float:Left();font-size:16.5px;margin-bottom:5px;float:left'><span style='font-weight:bold;font-family:Calibri;color: #0505af;'> Install Store:</span> <span style='font-family:Calibri;color: #0505af;'>&nbsp;" & StoreNum & "</span></span></p>"
                        End If
                    End If
                Catch ex As Exception

                End Try



                Dim WOArr As String() = NewStoreNumDT.AsEnumerable().[Select](Function(r) r.Field(Of String)("Work Order Number")).Distinct().ToArray()

                For Each WONum As String In WOArr
                    Dim WONumDetails As New DataTable
                    WONumDetails = (From C In NewStoreNumDT.AsEnumerable Where C.Field(Of String)("Work Order Number") = WONum).CopyToDataTable()
                    strbodydet1 = strbodydet1 & "<div style='float:Left()'>"
                    strbodydet1 = strbodydet1 & "<p style='width: 100%;font-size:16.5px;font-weight: bold;float:left;margin-bottom:5px'><span style='font-weight:bold; font-family:Calibri'> Work Order Num:</span> <span style='font-family:Calibri;'>&nbsp;" & WONum & "</span></p>"

                    Dim Ordernum As String() = WONumDetails.AsEnumerable().[Select](Function(r) r.Field(Of String)("Order No.")).Distinct().ToArray()

                    For Each orderno As String In Ordernum
                        objStreamWriter.WriteLine("  Reading order details of order " & orderno & " " & Now())
                        Dim OrderDetails As New DataTable

                        If connectOR.State = ConnectionState.Open Then
                            'do nothing
                        Else
                            connectOR.Open()
                        End If

                        Try
                            Dim strQuery As String = "SELECT SYSADM8.ORD_STAT_SUMMARY('" & orderno & "') from dual"
                            Dim OrderStatus As String = ORDBAccess.GetScalar(strQuery, connectOR)

                            Dim OrdStatusArr() As String
                            Dim statusImg As String
                            OrdStatusArr = OrderStatus.Split("^")
                            If OrdStatusArr(3) = 1 Then
                                statusImg = "'https://walmarttest.sdi.com/images/chain0.png'"
                            ElseIf OrdStatusArr(3) = 2 Then
                                statusImg = "'https://walmarttest.sdi.com/images/chain1.png'"
                            ElseIf OrdStatusArr(3) = 3 Then
                                statusImg = "'https://walmarttest.sdi.com/images/chain2.png'"
                            ElseIf OrdStatusArr(3) = 4 Then
                                statusImg = "'https://walmarttest.sdi.com/images/chain3.png'"
                            ElseIf OrdStatusArr(3) = 5 Then
                                statusImg = "'https://walmarttest.sdi.com/images/chain4.png'"
                            Else
                                statusImg = "'https://walmarttest.sdi.com/images/chain6.png'"
                            End If

                            Dim bgColor As String = ""
                            Dim Color As String = ""
                            Dim borderColor As String = ""

                            If OrdStatusArr(4) = "YELLOW" Then
                                bgColor = "Yellow"
                                Color = "dimgrey"
                                borderColor = "Yellow"
                            ElseIf OrdStatusArr(4) = "RED" Then
                                bgColor = "Red"
                                Color = "white"
                                borderColor = "Red"
                            ElseIf OrdStatusArr(4) = "GREEN" Then
                                bgColor = "forestgreen"
                                Color = "white"
                                borderColor = "forestgreen"
                            ElseIf OrdStatusArr(4) = "GRAY" Then
                                bgColor = "darkgray"
                                Color = "white"
                                borderColor = "darkgray"
                            End If

                            OrderDetails = (From C In WONumDetails.AsEnumerable Where C.Field(Of String)("Order No.") = orderno).CopyToDataTable()
                            strbodydet1 = strbodydet1 & "<div style='float:left;width:100%'>"

                            strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin-bottom:9px !important;'><span style='font-weight:bold;font-family:Calibri;'> Order Number:</span> <span style='font-family:Calibri;'>&nbsp;" & orderno & "</span></p> "
                            strbodydet1 = strbodydet1 & "<div style='float: left;width: 100%;padding-left: 17px;margin-bottom:9px !important;'><span><img src =" & statusImg & " alt='SDI' width='50%' height='5%' vspace='0' hspace='0' style='width:280px' /></span>&nbsp;&nbsp;&nbsp;</div><br>"
                            strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin-bottom:9px !important;margin-top:0px'><span style='font-Size:16px; background-color:" & bgColor & ";color: " & Color & ";font-family:Calibri;border-radius:50px; text-align:center;padding:2px 8px 2px 2px;'>&nbsp; " & OrdStatusArr(1) & "</span></p>"

                            Try
                                OrderDetails = OrderDetails.AsEnumerable().GroupBy(Function(row) row.Field(Of String)("Line Number")).Select(Function(group) group.First()).CopyToDataTable()
                            Catch ex As Exception

                            End Try

                            For K = 0 To OrderDetails.Rows.Count - 1

                                If K = 0 Then
                                    If strBU <> "I0631" Then
                                        strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin-bottom:9px !important;margin-top:0px'><span style='font-weight:bold;font-family:Calibri;'> Ship-to Store:</span> <span style='font-family:Calibri;'>&nbsp;" & OrderDetails.Rows(K).Item("Ship To") & "</span></p> "
                                    End If
                                    strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin-bottom:9px !important;margin-top:0px'><span style='font-weight:bold;font-family:Calibri;'> Items Ordered:</span></p> "
                                End If
                                strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin-bottom:1px;margin-top:0px'><span style='font-family:Calibri;font-weight:bold;'> &nbsp;&nbsp; Qty:</span> <span style='font-family:Calibri;'>" & OrderDetails.Rows(K).Item("Qty Ordered") & "</span><span style='font-family:Calibri;'>,&nbsp; " & OrderDetails.Rows(K).Item("Non-Stock Item Description") & "</span></p> "

                                strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin:1px !important;'><span style='font-weight:bold;font-family:Calibri;'> &nbsp;&nbsp; Tracking Number:</span> <span style='font-family:Calibri;'>&nbsp;" & OrderDetails.Rows(K).Item("Tracking No") & "</span></p> "
                                strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin:1px !important;'><span style='font-weight:bold;font-family:Calibri;'> &nbsp;&nbsp; Delivery Date:</span> <span style='font-family:Calibri;'>&nbsp;" & OrderDetails.Rows(K).Item("Delivery Date") & "</span></p> "
                                strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin:1px !important;'><span style='font-weight:bold;font-family:Calibri;'> &nbsp;&nbsp; Supplier Name:</span> <span style='font-family:Calibri;'>&nbsp;" & OrderDetails.Rows(K).Item("Supplier Name") & "</span></p> "
                                strbodydet1 = strbodydet1 & "<p style='float: left;width: 100%;padding-left: 17px;margin-top:1px !important;margin-bottom:20px'><span style='font-weight:bold;font-family:Calibri;'> &nbsp;&nbsp; Shipment Status:</span> <span style='font-family:Calibri;'>&nbsp;" & OrderDetails.Rows(K).Item("Shipment Status") & "</span></p> "

                            Next
                        Catch ex As Exception


                        End Try
                        strbodydet1 = strbodydet1 & "</div>"
                    Next

                    strbodydet1 = strbodydet1 & "</div>"

                Next

                strbodydet1 = strbodydet1 & "</div>"
            Next
            If (IsPrioAvail = True) Then
                IsPrioAvail = False
            ElseIf (IsNonPrioAvail = True) Then
                IsNonPrioAvail = False
            End If

        Next

        strbodydet1 = strbodydet1 & "&nbsp;<br>"
        strbodydet1 = strbodydet1 & "<span style='font-family:Calibri;font-size:18px'>Sincerely,</span><br>"
        strbodydet1 = strbodydet1 & "&nbsp;<br>"
        strbodydet1 = strbodydet1 & "<span style='font-family:Calibri;font-size:18px'>SDI Customer Care</span><br>"
        strbodydet1 = strbodydet1 & "&nbsp;<br>"
        strbodydet1 = strbodydet1 & "</p>"
        strbodydet1 = strbodydet1 & "</div>"
        strbodydet1 = strbodydet1 & "<HR width='100%' SIZE='1'>" & vbCrLf
        strbodydet1 = strbodydet1 & "<img src='http://www.sdiexchange.com/Images/SDIFooter_Email.png'/>" & vbCrLf

        Mailer1.Body = strbodyhead1 & strbodydet1

        Mailer1.BodyFormat = System.Web.Mail.MailFormat.Html
        Try
            objGenerallLogStreamWriter.WriteLine("Sending order summary email to " + Mailer1.To)
            objStreamWriter.WriteLine("Sending order summary email to " + Mailer1.To & " " & Now())
            'Mythili -- INC0023448 Adding CC emails
            SDIEmailService.EmailUtilityServices("MailandStore", Mailer1.From, Mailer1.To, Mailer1.Subject, Mailer1.Cc, "webdev@sdi.com", Mailer1.Body, "StatusChangeEmail1", MailAttachmentName, MailAttachmentbytes.ToArray())
        Catch ex As Exception
            objGenerallLogStreamWriter.WriteLine("Error in sending order summary email to " + Mailer1.To)
            objStreamWriter.WriteLine("Error in sending order summary email to " + Mailer1.To & " " & Now())
            objStreamWriter.WriteLine("  Generated Email for the order number " & " " & Now())
        End Try
        objStreamWriter.WriteLine("  Generated Email for the order number " & " " & Now())

    End Sub

    Private Function UpdateWalmartSourceCode(ByVal dteStartDate As Date, ByVal dteEndDate As Date, ByVal strBU As String)
        Try
            Dim ds As New DataSet
            Dim strSQLstring As String = String.Empty
            strSQLstring = "SELECT distinct G.BUSINESS_UNIT_OM, G.BUSINESS_UNIT_OM AS G_BUS_UNIT, D.BUSINESS_UNIT, D.ISA_EMPLOYEE_ID, A.ORDER_NO,B.ISA_WORK_ORDER_NO As WORK_ORDER_NO, B.ISA_INTFC_LN AS line_nbr," & vbCrLf &
                " B.ISA_EMPLOYEE_ID AS EMPLID, B.ISA_LINE_STATUS as ORDER_TYPE,B.OPRID_ENTERED_BY," & vbCrLf &
                " TO_CHAR(G.DTTM_STAMP, 'MM/DD/YYYY HH:MI:SS AM') as DTTM_STAMP, " & vbCrLf   '  & _

            'Mythili - WAL-824 Need Service Channel API change to map PUR (Ready for Pickup) from In Progress / Parts on Order to new Service Channel Extended Status �In Progress / Parts Ready for Pickup
            strSQLstring += "  G.ISA_LINE_STATUS AS ISA_ORDER_STATUS, DECODE(G.ISA_LINE_STATUS,'CRE','01','QTW','02','QTC','03','QTS','04','CST','05','VND','06','APR','07','QTA','08','QTR','09','RFA','10','RFR','11','RFC','12','RCF','13','RCP','14','CNC','15','DLF','16','PUR','17') AS OLD_ORDER_STATUS," & vbCrLf &
                     " (SELECT E.XLATLONGNAME" & vbCrLf &
                                    " FROM XLATTABLE E" & vbCrLf &
                                    " WHERE E.EFFDT =" & vbCrLf &
                                    " (SELECT MAX(E_ED.EFFDT) FROM XLATTABLE E_ED" & vbCrLf &
                                    " WHERE(E.FIELDNAME = E_ED.FIELDNAME)" & vbCrLf &
                                    " AND E.FIELDVALUE = E_ED.FIELDVALUE" & vbCrLf &
                                    " AND E_ED.EFFDT <= SYSDATE)" & vbCrLf &
                                    " AND E.FIELDNAME = 'ISA_LINE_STATUS'" & vbCrLf &
                                    " AND E.FIELDVALUE = G.ISA_LINE_STATUS) as ORDER_STATUS_DESC, " & vbCrLf &
                     " B.DESCR254 As NONSTOCK_DESCRIPTION, C.DESCR60 as STOCK_DESCRIPTION, D.ISA_EMPLOYEE_EMAIL," & vbCrLf &
                     " B.INV_ITEM_ID as INV_ITEM_ID," & vbCrLf &
            " D.FIRST_NAME_SRCH, D.LAST_NAME_SRCH" & vbCrLf &
                     " ,A.origin, LD.PO_ID, SH.ISA_ASN_TRACK_NO" & vbCrLf &
                     " FROM ps_isa_ord_intf_HD A," & vbCrLf  '   & _

            strSQLstring += " ps_isa_ord_intf_LN B," & vbCrLf &
                     " PS_MASTER_ITEM_TBL C," & vbCrLf &
                     " PS_ISA_USERS_TBL D," & vbCrLf &
                     " PS_ISAORDSTATUSLOG G, PS_ISA_ASN_SHIPPED SH, PS_PO_LINE_DISTRIB LD" & vbCrLf &
                     " where G.BUSINESS_UNIT_OM = '" & strBU & "' " & vbCrLf &
                     " AND G.BUSINESS_UNIT_OM = A.BUSINESS_UNIT_OM " & vbCrLf &
                     " AND G.BUSINESS_UNIT_OM = D.BUSINESS_UNIT " & vbCrLf     '   & _

            strSQLstring += "  and A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf &
                     " and A.ORDER_NO = B.ORDER_NO" & vbCrLf &
                     " and C.SETID (+) = 'MAIN1'" & vbCrLf &
                     " and C.INV_ITEM_ID(+) = B.INV_ITEM_ID " & vbCrLf &
                     " AND G.ORDER_NO = A.ORDER_NO " & vbCrLf &
                     " AND B.ISA_INTFC_LN = G.ISA_INTFC_LN" & vbCrLf &
                     " AND A.BUSINESS_UNIT_OM = D.BUSINESS_UNIT" & vbCrLf &
                     " AND SH.PO_ID (+) = LD.PO_ID And SH.LINE_NBR (+) = LD.LINE_NBR And SH.SCHED_NBR (+) = LD.SCHED_NBR And LD.Req_id (+) = B.order_no AND LD.REQ_LINE_NBR (+) = B.ISA_INTFC_LN" & vbCrLf &
                     " AND G.DTTM_STAMP > TO_DATE('" & dteStartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                     " AND G.DTTM_STAMP <= TO_DATE('" & dteEndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                     " AND UPPER(B.ISA_EMPLOYEE_ID) = UPPER(D.ISA_EMPLOYEE_ID)" & vbCrLf &
                      " ORDER BY ORDER_NO, LINE_NBR, DTTM_STAMP"

            Try
                objWalmartSC.WriteLine("  UpdateWalmartSourceCode Q1New: " & strSQLstring)
                Try
                    Dim st As New Stopwatch()
                    st.Start()
                    ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                    st.Stop()
                    Dim ts As TimeSpan = st.Elapsed
                    Dim elapsedTime As String = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)
                    objWalmartSC.WriteLine("Query Execution Time " + elapsedTime)
                Catch ex As Exception
                    ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
                    objWalmartSC.WriteLine("Query Execution Time " + Now())
                End Try

                Dim I As Integer
                Dim lstOfString As List(Of String) = New List(Of String)
                For I = 0 To ds.Tables(0).Rows.Count - 1

                    Try
                        Dim OrderNo As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
                        If OrderNo.ToUpper.Substring(0, 1) = "W" Then
                            If Not lstOfString.Contains(OrderNo) Then
                                objWalmartSC.WriteLine("Order No: " + Convert.ToString(OrderNo))
                                lstOfString.Add(OrderNo)
                                Dim WorkOrder As String = ds.Tables(0).Rows(I).Item("WORK_ORDER_NO")
                                objWalmartSC.WriteLine("WorkOrder No: " + Convert.ToString(WorkOrder))
                                Dim EnteredBy As String = ds.Tables(0).Rows(I).Item("OPRID_ENTERED_BY")
                                If Not String.IsNullOrEmpty(WorkOrder) Then
                                    Dim strSQLQuery As String = "select THIRDPARTY_COMP_ID from SDIX_USERS_TBL where ISA_EMPLOYEE_ID='" & EnteredBy & "' "
                                    Dim dsUser As DataSet = ORDBAccess.GetAdapter(strSQLQuery, connectOR)
                                    Dim Order As String()
                                    If dsUser.Tables.Count > 0 Then
                                        Dim THIRDPARTY_COMP_ID As String = String.Empty
                                        Try
                                            THIRDPARTY_COMP_ID = dsUser.Tables(0).Rows(0).Item("THIRDPARTY_COMP_ID")
                                            objWalmartSC.WriteLine("THIRDPARTY_COMP_ID: " + Convert.ToString(THIRDPARTY_COMP_ID))
                                        Catch ex As Exception
                                            THIRDPARTY_COMP_ID = "0"
                                            objWalmartSC.WriteLine("Catch-THIRDPARTY_COMP_ID: " + Convert.ToString(THIRDPARTY_COMP_ID))
                                        End Try
                                        Dim OrderStatusDetail As New OrderStatusDetail
                                        Dim orderDetail As String = OrdrStatus(OrderNo)
                                        objWalmartSC.WriteLine("Current Order Status: " + Convert.ToString(orderDetail))
                                        If orderDetail.Trim() <> "" Then
                                            Order = orderDetail.Split("^"c)
                                            OrderStatusDetail.orderStatus = Order(0)
                                            OrderStatusDetail.statusDesc = Order(1)
                                            OrderStatusDetail.dueDate = Order(2)
                                            OrderStatusDetail.message = "Success"
                                            objWalmartSC.WriteLine("Order No: " + Convert.ToString(OrderNo) + "Status" + Convert.ToString(OrderStatusDetail.statusDesc))
                                            If OrderStatusDetail.message = "Success" Then
                                                'WAL-622: SC Updates for Canceled Orders And Partial Deliveries 
                                                'Mythili - WAL-824 Need Service Channel API change to map PUR (Ready for Pickup) from In Progress / Parts on Order to new Service Channel Extended Status �In Progress / Parts Ready for Pickup
                                                If OrderStatusDetail.statusDesc = "Delivered" Or OrderStatusDetail.statusDesc = "En Route from Vendor" Or OrderStatusDetail.statusDesc = "Partially Delivered" Or OrderStatusDetail.statusDesc = "Cancelled" Or OrderStatusDetail.statusDesc = "Ready for Pickup" Then
                                                    Dim CheckWOStatus As String = CheckWorkOrderStatus(WorkOrder, THIRDPARTY_COMP_ID)
                                                    objWalmartSC.WriteLine("CheckWOStatus: " + Convert.ToString(CheckWOStatus))
                                                    If CheckWOStatus.ToUpper() <> "COMPLETED" And CheckWOStatus <> "Failed" Then
                                                        Dim WOStatus As String = String.Empty
                                                        If OrderStatusDetail.statusDesc = "Delivered" Then
                                                            WOStatus = "PARTS DELIVERED"
                                                        ElseIf OrderStatusDetail.statusDesc = "En Route from Vendor" Then
                                                            WOStatus = "PARTS SHIPPED"
                                                        ElseIf OrderStatusDetail.statusDesc = "Partially Delivered" Then
                                                            WOStatus = "PARTIAL PARTS DELIVERED"
                                                        ElseIf OrderStatusDetail.statusDesc = "Cancelled" Then
                                                            WOStatus = "INCOMPLETE"
                                                        ElseIf OrderStatusDetail.statusDesc = "Ready for Pickup" Then
                                                            WOStatus = "PARTS READY FOR PICKUP"
                                                        End If
                                                        If CheckWOStatus <> WOStatus Then
                                                            Dim PurchaseNo As String = PurchaseOrderNo(WorkOrder, THIRDPARTY_COMP_ID)
                                                            If PurchaseNo <> "Failed" Then
                                                                If Not String.IsNullOrEmpty(THIRDPARTY_COMP_ID) Then
                                                                    If THIRDPARTY_COMP_ID = ConfigurationManager.AppSettings("CBRECompanyID").ToString() Then
                                                                        UpdateWorkOrderStatus(WorkOrder, "CBRE", WOStatus)
                                                                        UpdateWorkOrderStatus(PurchaseNo, "Walmart", WOStatus)
                                                                    Else
                                                                        UpdateWorkOrderStatus(WorkOrder, "Walmart", WOStatus)
                                                                    End If
                                                                Else
                                                                    UpdateWorkOrderStatus(WorkOrder, "Walmart", WOStatus)
                                                                End If

                                                            End If
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    Catch ex As Exception
                        objWalmartSC.WriteLine("Method:checkAllStatusNew - " + ex.Message & " " & Now())
                    End Try
                Next

            Catch OleDBExp As OleDbException
                Console.WriteLine("")
                Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
                Console.WriteLine("")
                connectOR.Close()
                objWalmartSC.WriteLine("     Error - error reading transaction FROM PS_ISAORDSTATUSLOG A" & " " & Now())
                Return True
            End Try

            If IsDBNull(ds.Tables(0).Rows.Count) Or (ds.Tables(0).Rows.Count) = 0 Then
                Console.WriteLine("Fetched Datas 0")
                objWalmartSC.WriteLine("Fetched Datas 0")
                objWalmartSC.WriteLine("     Warning - no status changes to process at this time for All Statuses" & " " & Now())
                Try
                    connectOR.Close()
                Catch ex As Exception

                End Try
                Return False
            Else
                Console.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()))
                objWalmartSC.WriteLine("Fetched Datas " + Convert.ToString(ds.Tables(0).Rows.Count()) & " " & Now())
            End If
        Catch ex As Exception

        End Try
    End Function

    Private Function GetNotes()  'Mythili - WAL-534, Updating all the supplier portal and expedite comments to service channel
        Try
            Dim ds As New DataSet
            Dim addminutes As Int16 = Convert.ToInt16(ConfigurationSettings.AppSettings("StartDateNotes"))
            Dim StartDate As DateTime = Now().AddMinutes(addminutes)
            Dim EndDate As DateTime = Now()
            Dim sqlstring As String = ""
            'INC0013889 Excluding the problem code WS - Site Wait  from the push to Service Channel Notes
            sqlstring = "select A.ORDER_NO, A.ISA_INTFC_LN, A.ISA_WORK_ORDER_NO,B.PO_ID,A.ISA_LINE_STATUS," & vbCrLf &
                "C.NOTES_1000,A.ISA_EMPLOYEE_ID from PS_ISA_ORD_INTF_LN A,PS_PO_LINE_DISTRIB B,ps_isa_xpd_comment C" & vbCrLf &
                "where A.business_unit_OM = 'I0W01' AND   A.ISA_LINE_STATUS IN ('DSP','ASN')" & vbCrLf &
                "AND A.BUSINESS_UNIT_PO = B.BUSINESS_UNIT" & vbCrLf &
                "AND A.ORDER_NO = B.REQ_ID" & vbCrLf &
                "AND A.ISA_INTFC_LN = B.REQ_LINE_NBR" & vbCrLf &
                "AND B.BUSINESS_UNIT = C.BUSINESS_UNIT" & vbCrLf &
                "AND B.PO_ID = C.PO_ID" & vbCrLf &
                "AND B.LINE_NBR= C.LINE_NBR" & vbCrLf &
                "AND C.ISA_PROBLEM_CODE NOT IN ('AK','WS')" & vbCrLf &
                "AND C.DTTM_STAMP > TO_DATE('" & StartDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                "AND C.DTTM_STAMP <= TO_DATE('" & EndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf

            objWalSCComments.WriteLine("   Supplier comments Query: " & sqlstring & " " & Now())
            objWalSCComments.WriteLine("Start Supplier comment Service Channel " & Now())

            ds = ORDBAccess.GetAdapter(sqlstring, connectOR)

            If ds.Tables(0).Rows.Count > 0 Then
                Dim I As Integer
                For I = 0 To ds.Tables(0).Rows.Count - 1
                    Dim PO_num As String = String.Empty
                    Try
                        Dim Line_Status As String = ds.Tables(0).Rows(I).Item("ISA_LINE_STATUS")
                        PO_num = ds.Tables(0).Rows(I).Item("PO_ID")
                        Dim OrderNum As String = ds.Tables(0).Rows(I).Item("ORDER_NO")
                        Dim WorkOrder As String = ds.Tables(0).Rows(I).Item("ISA_WORK_ORDER_NO")
                        Dim Emp_id As String = ds.Tables(0).Rows(I).Item("ISA_EMPLOYEE_ID")
                        Dim strComments As String = ds.Tables(0).Rows(I).Item("NOTES_1000")
                        Dim Third_party_comp_id As String = ""
                        Try
                            Dim Sqlstring2 As String = "select THIRDPARTY_COMP_ID from SDIX_USERS_TBL where ISA_EMPLOYEE_ID = '" & Emp_id & "'"
                            connectOR.Open()
                            Third_party_comp_id = ORDBAccess.GetScalar(Sqlstring2, connectOR)
                            connectOR.Close()
                        Catch ex As Exception
                            Third_party_comp_id = "0"
                        End Try

                        Dim CredType As String = ""
                        If Third_party_comp_id <> "100" Then 'Mythili -- Status change Email utility -- expedite comment issue
                            CredType = "Walmart"
                        End If
                        UpdateNotes(WorkOrder, CredType, strComments, PO_num, OrderNum)
                    Catch ex As Exception
                        objWalSCComments.WriteLine("Result- Failed in updating notes for the PO " + PO_num & " " & Now())
                    End Try

                Next
                objWalSCComments.WriteLine("/////////////////////////////////////////////////////////////////////////////////////////////")
            Else
                objWalSCComments.WriteLine("No data fetched" & " " & Now())
            End If

        Catch ex As Exception

        End Try
    End Function
    Public Function UpdateNotes(ByVal workOrder As String, credType As String, Note As String, Ponum As String, Ordernum As String) As String  'Mythili - WAL-534, Updating all the supplier portal and expedite comments to service channel
        Try
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                Dim APIresponse = AuthenticateService(credType)
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/notes"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim objNoteParam As New UpdateNotes
                        objNoteParam.Note = Note
                        objNoteParam.MailedTo = ""
                        objNoteParam.ActionRequired = False
                        objNoteParam.ScheduledDate = Now
                        objNoteParam.Visibility = 0
                        objNoteParam.Actor = ""
                        objNoteParam.NotifyFollowers = False
                        objNoteParam.DoNotSendEmail = True

                        Dim serializedparameter = JsonConvert.SerializeObject(objNoteParam)
                        Dim response = httpClient.PostAsync(apiURL, New StringContent(serializedparameter, Encoding.UTF8, "application/json")).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().result
                            objWalSCComments.WriteLine("Result - Success " + Convert.ToString(workorderAPIResponse) + " Work Order-" + workOrder + " PO ID-" + Ponum + " Order No-" + Ordernum + " CredType-" + credType & " " & Now())
                            Return "Success"
                        Else
                            objWalSCComments.WriteLine("Result- Failed in API response Work Order-" + workOrder + " PO ID-" + Ponum + " Order No-" + Ordernum + " CredType-" + credType & " " & Now())
                            Return "Failed"
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Return "Failed"
            objWalSCComments.WriteLine("Method:UpdateNotes - " + ex.Message & " " & Now())
        End Try
    End Function
    Function GetOrderNotes(ByVal OrderNo As String, ByVal BU As String) As String

        Dim strSQLstring As String = ""
        Dim Order_notes As String = ""

        strSQLstring = "SELECT ISA_LINE_NOTES FROM SYSADM8.PS_ISA_ORDLN_NOTE WHERE ORDER_NO ='" & OrderNo & "' AND ISA_INTFC_LN = 0 AND BUSINESS_UNIT_OM = '" & BU & "'"

        Try
            objStreamWriter.WriteLine("  GetOrderNotes: " & strSQLstring)

            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            connectOR.Open()
            Order_notes = ORDBAccess.GetScalar(strSQLstring, connectOR)
            connectOR.Close()
        Catch ex As Exception
            Try
                connectOR.Close()
            Catch ex3 As Exception

            End Try
        End Try
        Return Order_notes
    End Function

    Function GetLineNotes(ByVal OrderNo As String, ByVal BU As String, ByVal strLnNotes As String) As String

        Dim strSQLstring As String = ""
        Dim Order_notes As String = ""
        Dim ds As DataSet

        strSQLstring = "SELECT ISA_LINE_NOTES FROM SYSADM8.PS_ISA_ORDLN_NOTE WHERE ORDER_NO = '" + OrderNo + "' AND business_unit_om = '" + BU + "' AND isa_intfc_ln = " + strLnNotes + ""

        Try
            objStreamWriter.WriteLine("  GetOrderNotes: " & strSQLstring)

            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            connectOR.Open()
            ds = ORDBAccess.GetAdapter(strSQLstring, connectOR)
            If Not ds Is Nothing Then
                If ds.Tables(0).Rows.Count > 0 Then
                    Order_notes = Convert.ToString(ds.Tables(0).Rows(0).Item("ISA_LINE_NOTES"))
                Else

                End If
            Else

            End If
        Catch ex As Exception

        End Try
        Return Order_notes
    End Function

    Private Function GetAscendEmailAddress(ByVal strBu As String, ByVal strOrderNo As String, ByVal connectOR As OleDbConnection) As String
        Dim strAscendEmail As String = ""

        Try
            'GET ASCEND E-MAIL ADDRESS FOR THIS ORDER
            Dim strAscendSql As String = ""
            strAscendSql += " select AB.EMAIL_ADDRESS AS ASCEND_EMAIL_ADDRESS, AB.WORK_ORDER_ID, A.BUSINESS_UNIT_OM," & vbCrLf
            strAscendSql += " A.ORDER_NO FROM ps_isa_ord_intfc_H A," & vbCrLf
            strAscendSql += " sysadm.PS_ISA_INTFC_H_SUP AB" & vbCrLf
            strAscendSql += " where A.BUSINESS_UNIT_OM = AB.BUSINESS_UNIT_OM" & vbCrLf
            strAscendSql += " AND A.ORDER_NO = AB.ORDER_NO" & vbCrLf
            strAscendSql += " AND A.BUSINESS_UNIT_OM='" & strBu & "'" & vbCrLf
            strAscendSql += " AND A.ORDER_NO='" & strOrderNo & "'" & vbCrLf


            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If

            Dim dSet As New DataSet
            dSet = ORDBAccess.GetAdapter(strAscendSql, connectOR)
            If Not dSet Is Nothing Then
                If dSet.Tables.Count > 0 Then
                    If dSet.Tables(0).Rows.Count > 0 Then
                        If Not dSet.Tables(0).Rows(0).Item("ASCEND_EMAIL_ADDRESS") Is Nothing Then
                            If Trim(dSet.Tables(0).Rows(0).Item("ASCEND_EMAIL_ADDRESS")) <> "" Then
                                strAscendEmail = dSet.Tables(0).Rows(0).Item("ASCEND_EMAIL_ADDRESS")
                            End If
                        End If
                    End If
                End If
            End If

            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
        Catch ex As Exception
            strAscendEmail = ""
            Try

                If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                    connectOR.Close()
                Else
                    Try
                        connectOR.Close()
                    Catch ex2 As Exception

                    End Try
                End If
            Catch ex1 As Exception

            End Try
        End Try
        Return strAscendEmail
    End Function
    'Shanmugapriya - INC0037618-UI needs to be revamped for the status change emails from status change email utility
    Public Function bindFooterMail(BU As String) As String
        Dim strFooter As String = ""
        Try
            Dim strQuery As String = "SELECT * FROM SDIX_BU_CONTACT_DETAILS WHERE BUSINESS_UNIT = '" & BU & "' OR (BUSINESS_UNIT = 'SDI' AND NOT " & "EXISTS (SELECT 1 FROM SDIX_BU_CONTACT_DETAILS WHERE BUSINESS_UNIT = '" & BU & "'))"
            Dim footerDS As DataSet = ORDBAccess.GetAdapter(strQuery, connectOR)

            If footerDS IsNot Nothing AndAlso footerDS.Tables.Count > 0 AndAlso footerDS.Tables(0).Rows.Count > 0 Then
                Dim phoneNum As String = footerDS.Tables(0).Rows(0)("PHONE_NUM").ToString().Trim()
                Dim emailID As String = footerDS.Tables(0).Rows(0)("EMAIL_ID").ToString().Trim()
                Dim visibility As String = If(Not IsDBNull(footerDS.Tables(0).Rows(0)("PHONE_EMAIL_VISIBLE")), footerDS.Tables(0).Rows(0)("PHONE_EMAIL_VISIBLE").ToString(), "")

                Dim strPhone As String = If(phoneNum <> "" AndAlso visibility.Contains("P"), "<a href='tel:" & phoneNum & "' style='color: blue; margin: 0; text-decoration: none;'>Call us @ " & phoneNum & "</a> ", "")
                Dim strEmail As String = If(emailID <> "" AndAlso visibility.Contains("E"), "<a href='mailto:" & emailID & "' style ='color: blue; margin: 0; text-decoration: none; margin-right: 12px;'> Contact SDI customer care </a> ", "")

                If strPhone <> "" OrElse strEmail <> "" Then
                    strFooter = "<tfoot><tr><td style='background-color: #F8F8F8; -webkit-print-color-adjust: exact; padding: 10px 5px; font-size: 12px; text-align: center;' colspan='2'>" & strPhone & If(strPhone <> "" AndAlso strEmail <> "", "<span style='border-right:2px solid #C2C2C2; margin:0px 10px;'></span>", "") & strEmail & "</td></tr></tfoot>"
                End If
            End If
        Catch ex As Exception
        End Try
        Return strFooter
    End Function
    'Shanmugapriya - INC0037618-UI needs to be revamped for the status change emails from status change email utility
    Private Sub sendCustEmail1(ByVal dsEmail As DataTable,
                          ByVal strOrderNo As String,
                          ByVal strCustID As String,
                          ByVal strCompanyID As String,
                          ByVal strOrderStatus As String,
                          ByVal strOrderStatDesc As String,
                          ByVal strInvID As String,
                          ByVal strLineNbr As String,
                          ByVal strFirstName As String,
                          ByVal strLastName As String,
                          ByVal strEmail As String, ByVal strorgin As String,
                          ByVal strBU As String, Optional ByVal strEmpID As String = "", Optional ByVal strWOno As String = "", Optional ByVal Store As String = "")

        Dim SDIEmailService As SDiEmailUtilityService.EmailServices = New SDiEmailUtilityService.EmailServices()
        Dim MailAttachmentName As String()
        Dim MailAttachmentbytes As New List(Of Byte())()
        Dim strbodyhead1 As String
        Dim strbodydet1 As String
        Dim txtBody1 As String
        Dim txtHdr1 As String
        Dim txtMsg1 As String
        Dim dataGridHTML1 As String
        Dim SBnstk1 As New StringBuilder
        Dim SWnstk1 As New StringWriter(SBnstk1)
        Dim htmlTWnstk1 As New HtmlTextWriter(SWnstk1)
        Dim bolSelectItem1 As Boolean
        Dim sendEmailAlert As Boolean
        Dim IsProdDB As Boolean = False
        Dim StrQuery As String
        Dim ds As New DataSet
        Dim Op_Description As String = ""
        Dim Non_Stock As String
        Dim Tracking_No As String
        Dim Qty_Order As String
        Dim Line_Notes As String
        Dim firstAssetNumber As String = ""
        Dim assetNumber As String
        Dim Footer As String = ""

        'Email notifications won't be sent for Walmart Orders only Web & Push notifications will be sent for all status changes
        'If strBU = "I0W01" Then
        '    sendEmailAlert = False
        'Else
        '    sendEmailAlert = True
        'End If

        StrQuery = "SELECT CASE WHEN B.isa_machine_no = ' ' THEN '-' ELSE B.isa_machine_no END AS Asset_No, 
                    CASE WHEN TRIM(W.name1) IS NULL OR W.name1 = ' ' THEN '-' ELSE W.name1 END AS OP_Description, 
                    CASE WHEN B.ISA_WORK_ORDER_NO = ' ' THEN '-' ELSE B.ISA_WORK_ORDER_NO END AS Workorder
                    FROM ps_isa_ord_intf_LN B LEFT JOIN PS_ISA_WO_STATUS W ON W.ISA_WORK_ORDER_NO = B.ISA_WORK_ORDER_NO AND W.Business_unit_om = B.BUSINESS_UNIT_OM WHERE B.Order_no = '" & strOrderNo & "'"
        ds = ORDBAccess.GetAdapter(StrQuery, connectOR)
        connectOR.Close()

        Dim Asset As DataTable = New DataTable()
        Asset.Columns.Add("Asset_No", GetType(String))
        Try
            If ds IsNot Nothing Then
                If ds.Tables.Count > 0 Then
                    If ds.Tables(0).Rows.Count > 0 Then
                        Try
                            Dim assetNumbers As String() = ds.Tables(0).AsEnumerable().Select(Function(row) row.Field(Of String)("Asset_No")).ToArray()
                            Dim allAssetNumbersSame As Boolean = False
                            firstAssetNumber = assetNumbers(0)
                            If assetNumbers.Count > 1 Then
                                For i As Integer = 1 To assetNumbers.Count - 1
                                    Try
                                        If assetNumbers(i) <> firstAssetNumber Then
                                            allAssetNumbersSame = True
                                            Exit For
                                        End If
                                    Catch ex As Exception
                                    End Try
                                Next
                            End If

                            Dim finalAssetNumbers As New List(Of String)()

                            If allAssetNumbersSame Then
                                finalAssetNumbers.AddRange(assetNumbers)
                            Else
                                finalAssetNumbers.Add(firstAssetNumber)
                            End If
                            For Each assetNumber In finalAssetNumbers
                                Asset.Rows.Add(assetNumber)
                            Next

                            Op_Description = ds.Tables(0).Rows(0)("OP_Description").ToString()
                            strWOno = ds.Tables(0).Rows(0)("Workorder").ToString()
                        Catch ex As Exception
                        End Try
                    End If
                End If
            End If
        Catch ex As Exception
        End Try

        If Not getDBName() Then
            IsProdDB = False
        Else
            IsProdDB = True
        End If

        'If sendEmailAlert Then
        Dim Mailer1 As MailMessage = New MailMessage
        Dim strccfirst1 As String = "erwin.bautista"  '  "pete.doyle"
        Dim strcclast1 As String = "sdi.com"
        'SP-316 get from email from table - Dhamotharan
        Dim fromMail As String = ""
        'SDI-40628 Changing Mail id as walmartpurchasing@sdi.com from sdiexchange@sdi.com for Walmart BU.
        'If strBU = "I0W01" Then
        '    Mailer1.From = "WalmartPurchasing@sdi.com"
        'Else
        '    Mailer1.From = "SDIExchange@SDI.com"  '  "Insiteonline@SDI.com"
        'End If
        Try
            Dim BU As String = ""
            If strBU = "I0W01" Then
                BU = "WAL00"
                fromMail = getFromMail(BU, connectOR)
            ElseIf strBU = "I0631" Then
                BU = "EMC00"
                fromMail = getFromMail(BU, connectOR)
            ElseIf strBU = "I0643" Then
                BU = "AMC00"
                fromMail = getFromMail(BU, connectOR)
            ElseIf strBU = "I0645" Then
                BU = "BOE00"
                fromMail = getFromMail(BU, connectOR)
            Else
                BU = "ISA00"
                fromMail = getFromMail(BU, connectOR)
            End If
        Catch ex As Exception

        End Try


        Mailer1.From = fromMail
        Mailer1.Cc = ""
        Mailer1.Bcc = strccfirst1 & "@" & strcclast1
        Dim dtgEmail1 As WebControls.DataGrid
        dtgEmail1 = New WebControls.DataGrid

        dsEmail = DuplicateRemoval(dsEmail)

        dtgEmail1.DataSource = dsEmail
        dtgEmail1.DataBind()
        dtgEmail1.BorderColor = Gray
        dtgEmail1.HeaderStyle.BackColor = System.Drawing.Color.LightGray
        dtgEmail1.HeaderStyle.Font.Bold = True
        dtgEmail1.HeaderStyle.ForeColor = Black
        WebControls.Unit.Percentage(90)
        dtgEmail1.CellPadding = 3
        'dtgEmail1.Width.Percentage(90)

        'dtgPO.Columns(9).ItemStyle.HorizontalAlign = HorizontalAlign.Center
        dtgEmail1.RenderControl(htmlTWnstk1)
        dataGridHTML1 = SBnstk1.ToString()

        ''Get Order Notes here
        Dim Ord_notes As String = ""
        Ord_notes = GetOrderNotes(strOrderNo, strBU)
        If Ord_notes Is Nothing OrElse Ord_notes = "(null)" OrElse Ord_notes.Trim() = "" Then
            Ord_notes = "-"
        End If

        'Dim strPurchaserName As String = strCustID
        Dim strPurchaserName As String = strFirstName &
               " " & strLastName
        Try
            Dim txtInfo As TextInfo = New CultureInfo("en-US", False).TextInfo
            strPurchaserName = txtInfo.ToTitleCase(strPurchaserName.ToLower())
        Catch ex As Exception
        End Try
        'Dim ted As String = ";erwin.bautista@sdi.com"
        Dim strPurchaserEmail As String = strEmail

        strbodyhead1 = "<!DOCTYPE html>
                        <html lang='en'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title>Email Template - Table Structure</title>
                        </head>" & vbCrLf

        strbodyhead1 = strbodyhead1 & "<body style='background-color: #ebebeb; font-family: Arial;'>
                                       <style>
                                            @media only screen and (max-width: 840px) {
                                                .table-grid {
                                                    display: grid;
                                                }

                                                .table-grid td {
                                                    width: 100% !important;
                                                }
                                            }
                                        </style>
                                    <table style='background-color:#ffffff; margin:auto; border-collapse:collapse' width='100%' align='center' border='0' cellpadding='0' cellspacing='0'>
                                    <thead> <tr style='padding: 20px 0px; background-color: #151723;'> <th style='text-align:left;' colspan='2'>
                                    <img src=" & ImageUrl & " alt='sdi-logo' style='width: 200px; padding: 14px 24px;'>
                                    </th> </tr> </thead> <tbody> <tr> <td style='padding: 0px 24px;'> <table style='width: 100%; border-collapse: collapse;'> <tbody>" & vbCrLf

        strbodydet1 = "<tr style='font-size: 16px;'> <td colspan='2'> <p style='font-size:18px;margin-bottom:10px;font-weight:bold;margin-top: 0;padding-top: 24px;'>  Hello " & strPurchaserName & "!</p>" & vbCrLf

        strbodydet1 = strbodydet1 & "<p style='color: #000; margin: 24px 0px 18px 0px; line-height: 24px; font-weight:500; font-size:15px;'> We wanted to update you on the status of your recent order with us. </p> </td> </tr>" & vbCrLf

        strbodydet1 = strbodydet1 & "<tr style='font-size: 16px;'> <td style='padding: 18px 12px; background-color: #F9F9F9;' colspan='2'> <p style='margin-top: 0px; margin-bottom: 0px; font-weight:600;'>Order Information</p> </td> </tr>" & vbCrLf

        strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='padding: 0px 12px;' colspan='2'> <table style='width:100%; border-collapse:collapse;'> <tbody> <tr class='table-grid' style='vertical-align: top'> <td style='width:50%'>" & vbCrLf

        strbodydet1 = strbodydet1 & "<p style='font-weight: 600; margin: 10px 0px 12px 0px;'> " & If(strBU = "I0635", "WO # OP : ", "WO # : ") & "<span style='color: #595959; font-weight: 500;'>" & strWOno & "</span></p></td>" & vbCrLf

        strbodydet1 = strbodydet1 & "<td style='width:50%'> <p style='font-weight: 600; margin: 10px 0px 12px 0px;'> Asset # : <span style='color: #595959; font-weight: 500;'>" & If(Asset.Rows.Count > 1, "See line details", firstAssetNumber) & "</span> </p></td></tr>" & vbCrLf

        strbodydet1 = strbodydet1 & "<tr class='table-grid' style='vertical-align: top'> <td style='width:50%'> <p style='font-weight: 600; margin: 10px 0px 12px 0px;'> OP Description : <span style='color: #595959; font-weight: 500;'>" & Op_Description & "</span> </p></td>" & vbCrLf

        strbodydet1 = strbodydet1 & "<td style='width:50%'> <p style='font-weight: 600; margin: 10px 0px 12px 0px;'> Order # : <span style='color: #595959; font-weight: 500;'>" & strOrderNo & "</span> </p></td></tr>" & vbCrLf

        strbodydet1 = strbodydet1 & "<tr class='table-grid' style='vertical-align: top'> <td colspan='2'> <p style='font-weight: 600; margin: 10px 0px 12px 0px;'> Customer Notes : <span style='color: #595959; font-weight: 500;'>" & Ord_notes & "</span> </p></td></tr></tbody></table></td></tr>" & vbCrLf

        strbodydet1 = strbodydet1 & "<tr style='font-size: 16px;'> <td style='padding: 18px 12px; background-color: #F9F9F9;' colspan='2'> 
                                    <p style='margin-top: 0px; margin-bottom: 0px; font-weight:600;'>Item Details</p> </td> </tr>" & vbCrLf

        If dsEmail IsNot Nothing Then
            If dsEmail.Rows.Count > 0 Then
                Dim assetIndex As Integer = 0
                Dim currentRow As Integer = 0
                Dim totalRows As Integer = dsEmail.Rows.Count
                For Each row As DataRow In dsEmail.Rows
                    Try
                        Non_Stock = row("Non-Stock Item Description").ToString()
                        If String.IsNullOrWhiteSpace(Non_Stock) Then
                            Non_Stock = "-"
                        End If
                        Tracking_No = row("Tracking No").ToString()
                        Qty_Order = row("Qty Ordered").ToString()
                        Line_Notes = row("Line Notes").ToString()

                        If Asset.Rows.Count > 1 Then
                            strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='width: 41%; padding: 0px 12px; vertical-align: top; '> <p style='font-weight: 600; white-space : nowrap; margin: 10px 0px;'>Asset # :</p>
                                </td> <td style='padding: 0px 12px; '> <p style='color: #595959; margin: 10px 0px;'>" & Asset.Rows(assetIndex)("Asset_No").ToString() & "</p> </td> </tr>" & vbCrLf
                        End If
                        assetIndex += 1

                        strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='width: 41%; padding: 0px 12px; vertical-align: top; '> <p style='font-weight: 600; white-space : nowrap; margin: 10px 0px;'>Status :</p>
                                </td> <td style='padding: 0px 12px; '> <p style='color: #595959; margin: 10px 0px;'>" & strOrderStatDesc & "</p></td></tr>" & vbCrLf

                        strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='width: 41%; padding: 0px 12px; vertical-align: top;'> <p style='font-weight: 600; white-space : nowrap; margin: 10px 0px;'>Description :</p> </td>
                                    <td style='padding: 0px 12px; '> <p style='color: #595959; margin: 10px 0px'>" & Non_Stock & "</p></td></tr>" & vbCrLf

                        strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='width: 41%; padding: 0px 12px; vertical-align: top; '>
                                    <p style='font-weight: 600;white-space : nowrap; margin: 10px 0px;'>Quantity :</p> </td> <td style='padding: 0px 12px; '> <p style='color: #595959; margin: 10px 0px;'>" & Qty_Order & "</p></td></tr>" & vbCrLf

                        strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='width: 41%; padding: 0px 12px; vertical-align: top; '> <p style='font-weight: 600;white-space : nowrap; margin: 10px 0px;'>Tracking :</p> </td>
                                    <td style='padding: 0px 12px; '> <p style='color: #595959; margin: 10px 0px;'>" & Tracking_No & "</p></td></tr>" & vbCrLf

                        strbodydet1 = strbodydet1 & "<tr style='font-size: 13px;'> <td style='width: 41%; padding: 0px 12px; vertical-align: top; '>
                                    <p style='font-weight: 600; white-space : nowrap; margin: 10px 0px;'>Line notes :</p> </td> <td style='padding: 0px 12px; '> <p style='color: #595959; margin: 10px 0px;'>" & Line_Notes & "</p></td></tr>" & vbCrLf

                        If currentRow < totalRows - 1 Then
                            strbodydet1 = strbodydet1 & "<tr> <td colspan='2' style='border-bottom: #DFDFDF 1px solid;'></td> </tr>" & vbCrLf
                        End If
                        currentRow += 1
                    Catch ex As Exception
                    End Try
                Next
            End If
        End If

        strbodydet1 = strbodydet1 & "<tr style='font-size: 14px;'> <td style='width: 41%; padding: 0px;'> <p style='font-weight: 500; margin: 40px 0px 6px 0px;'>Thanks,</p> </td> </tr> <tr style='font-size: 14px;'>
                                <td style='width: 41%; padding: 0px; '> <p style='font-weight: 500; white-space : nowrap; margin: 4px 0px 40px 0px;'>SDI Customer Care</p> </td> </tr> </tbody> </table> </td> </tr> </tbody>" & vbCrLf
        Try
            Footer = bindFooterMail(strBU)
        Catch ex As Exception
        End Try
        strbodydet1 = strbodydet1 & Footer

        strbodydet1 = strbodydet1 & "</table> </body> </html>"


        Mailer1.Body = strbodyhead1 & strbodydet1
        'SP-316 get to emails and Changed subject for EMCOR  - Dhamotharan
        Try 'Mythili -- INC0023448 Adding CC emails
            Dim toMail As String = ""
            Dim BU As String = ""
            If strBU = "I0W01" Then
                BU = "WAL00"
            ElseIf strBU = "I0631" Then
                BU = "EMC00"
            ElseIf strBU = "I0643" Then
                BU = "AMC00"
            ElseIf strBU = "I0645" Then
                BU = "BOE00"
            Else
                BU = "ISA00"
            End If
            toMail = getToMail(BU, connectOR)
            If Not IsProdDB Then
                Mailer1.To = "webdev@sdi.com"
                If strBU = "I0W01" Then
                    'Mailer1.Cc = toMail
                    Mailer1.Subject = "<<TEST SITE>>Status Update - " + strOrderStatDesc + " - Store #" + Store + " - WO # " & strWOno
                Else
                    Mailer1.Subject = "<<TEST SITE>>SDI Order(" & strOrderNo & ") " & strOrderStatDesc & If(strWOno <> "-", " For " & strWOno, "")
                End If
            Else
                If strBU = "I0W01" Then
                    Mailer1.To = "webdev@sdi.com"
                    Mailer1.Cc = toMail
                    Mailer1.Subject = "Status Update - " + strOrderStatDesc + " - Store #" + Store + " - WO # " & strWOno
                ElseIf (strBU = "I0631" Or strBU = "I0643" Or strBU = "I0645") Then
                    Mailer1.To = "webdev@sdi.com"
                    Mailer1.Cc = toMail
                    Mailer1.Subject = "SDI Order(" & strOrderNo & ") " & strOrderStatDesc & If(strWOno <> "-", " For " & strWOno, "")
                Else
                    Mailer1.To = strPurchaserEmail
                    Mailer1.Cc = ""
                    Mailer1.Subject = "SDI Order(" & strOrderNo & ") " & strOrderStatDesc & If(strWOno <> "-", " For " & strWOno, "")
                End If
            End If
        Catch ex As Exception

        End Try

        Try
            Mailer1.BodyFormat = System.Web.Mail.MailFormat.Html

            SDIEmailService.EmailUtilityServices("MailandStore", Mailer1.From, Mailer1.To, Mailer1.Subject, Mailer1.Cc, "webdev@sdi.com", Mailer1.Body, "StatusChangeEmail1", MailAttachmentName, MailAttachmentbytes.ToArray())
            objStreamWriter.WriteLine("  Generated Email for the order number " & strOrderNo & " " & Now())
        Catch ex As Exception

        End Try


        'End If

        'Push & Web notifications will be sent for all orders of all BUs lively- WAL-632&WW-287 Poornima S
        Dim strPushNoti As String = ""
        'SP-316 changed subject for EMCOR- Dhamotharan
        'INC0043325 - Zeus Ordering app notification Read Section search issue OSC Ticket 240626-005351 (-Suvetha)
        Dim Title As String = ""
        Try
            If Not IsProdDB Then
                If strBU = "I0W01" Then
                    strPushNoti = "<<TEST SITE>>Status Update - " + strOrderStatDesc + " - Store #" + Store + " - WO # " & strWOno
                    Title = "<<TEST SITE>>Order Number: " + strOrderNo + " - Status Modified To " + strOrderStatDesc + ""
                Else
                    strPushNoti = "<<TEST SITE>>SDI Order(" & strOrderNo & ") " & strOrderStatDesc & If(strWOno <> "-", " For " & strWOno, "")
                    Title = strPushNoti
                End If
            Else
                If strBU = "I0W01" Then
                    strPushNoti = "Status Update - " + strOrderStatDesc + " - Store #" + Store + " - WO # " & strWOno
                    Title = "Order Number: " + strOrderNo + " - Status Modified To " + strOrderStatDesc + ""
                Else
                    strPushNoti = "SDI Order(" & strOrderNo & ") " & strOrderStatDesc & If(strWOno <> "-", " For " & strWOno, "")
                    Title = strPushNoti
                End If
            End If
        Catch ex As Exception

        End Try

        If Not strEmpID.Trim = "" Then
            sendNotification(strEmpID, strPushNoti, strOrderNo, strBU)
            sendWebNotification(strEmpID, Title, strBU)
        End If

    End Sub

    Public Sub sendWebNotification(ByVal Session_UserID As String, ByVal subject As String, ByVal BU As String)
        Try
            Dim _notificationResult As New DataSet
            Dim notificationSQLStr = "select max(NOTIFY_ID) As NOTIFY_ID from SDIX_NOTIFY_QUEUE where USER_ID='" + Session_UserID + "'"
            _notificationResult = ORDBAccess.GetAdapter(notificationSQLStr, connectOR)
            Dim NotifyID As Int64 = 1
            If _notificationResult.Tables.Count > 0 Then
                Try
                    NotifyID = _notificationResult.Tables(0).Rows(0).Item("NOTIFY_ID")
                    NotifyID = NotifyID + 1
                Catch ex As Exception
                End Try
            End If
            connectOR.Open()
            Dim strSQLstring As String = "INSERT INTO SDIX_NOTIFY_QUEUE" & vbCrLf &
        " (NOTIFY_ID, NOTIFY_TYPE, USER_ID,DTTMADDED, STATUS,LINK, HTMLMSG, ATTACHMENTS, TITLE, BUSINESS_UNIT) VALUES ('" & NotifyID & "'," & vbCrLf &
        " 'ORD'," & vbCrLf &
        " '" & Session_UserID & "'," & vbCrLf &
        " sysdate," & vbCrLf &
        " 'N'," & vbCrLf &
         " ' ',' ',' '," & vbCrLf &
        " '" & subject & "'," & vbCrLf &
        " '" & BU & "')" & vbCrLf

            Dim command1 As OleDbCommand
            command1 = New OleDbCommand(strSQLstring, connectOR)
            Try
                Dim rowsaffected As Integer
                rowsaffected = command1.ExecuteNonQuery
                If Not rowsaffected = 1 Then

                End If
                command1.Dispose()
            Catch ex As Exception

            End Try
            Try
                connectOR.Close()
            Catch ex As Exception

            End Try

        Catch ex As Exception
        End Try
    End Sub

    Private Function DuplicateRemoval(ByVal dt As DataTable)
        Dim dtbl As DataTable = New DataTable()
        dtbl = dt.Clone()
        dtbl.Rows.Clear()
        Dim list As New List(Of String) From {"01", "02", "03", "04",
                                              "05", "06", "07", "08",
                                              "09", "10", "11", "12",
                                              "13", "14", "15", "16"}
        Try
            Dim dr As DataRow
            For Each dr In dt.Rows
                Dim dts As DataTable = dt.AsEnumerable().Where(Function(x) x.Item("Line Number") = dr.Item("Line Number")).CopyToDataTable()

                If dts.Rows.Count() > 1 Then
                    Dim boolvalue As Boolean = False
                    Dim Nw_dts As DataTable = New DataTable()
                    Try
                        Nw_dts = dtbl.AsEnumerable().Where(Function(x) x.Item("Line Number") = dr.Item("Line Number")).CopyToDataTable()
                    Catch ex As Exception

                    End Try

                    If Nw_dts.Rows.Count() = 1 Then

                    Else
                        For Each drs As DataRow In dts.Rows
                            If list.Contains(drs.Item("Status Code")) Then
                                dtbl.ImportRow(drs)
                                boolvalue = True
                                Exit For
                            Else
                                boolvalue = False
                            End If
                        Next
                        If Not boolvalue Then
                            dtbl.ImportRow(dts.Rows(0))
                        End If
                    End If
                Else
                    dtbl.ImportRow(dr)
                End If
            Next
        Catch ex As Exception

        End Try
        Return dtbl
    End Function

    Private Function updateEnterprise(ByVal strBU As String, ByVal dteEndDate As DateTime) As Boolean
        connectOR.Close()
        Dim strSQLstring As String
        'Dim dteEndDate As DateTime
        Dim ds As New DataSet
        Dim bolerror1 As Boolean
        Dim dteJulian As Integer
        Dim dteStart As Date = "01/01/1900"
        Dim rowsaffected As Integer
        dteJulian = DateDiff(DateInterval.Day, dteStart, Now())

        Dim strXMLPath As String = rootDir & "\XMLOUT\ORDERSTATUS" & Convert.ToString(dteJulian) & Now.Hour.ToString("D2") & Now.Minute.ToString("D2") & Now.Second.ToString("D2") & ".xml"
        Dim objXMLWriter As XmlTextWriter


        ' The enddate coming from PS_ISAORDERSTATUSLOG  is being set back to the original enddate.  The PS_ISA_enterprise table
        ' is then updated with the PS_ISAORDERSTATUSLOG's endddate and the next time in, the date in the PS_ISA_enterprise table is
        ' the startdate.  We increased the enddate a second so we could get all the records from the query.  We were never getting
        ' the last record because of milliseconds were off in the date conversions.  Adding a second we were able to get all
        ' the records in the date range....  If you understand this you have a date to sit with the Dali Lama.. Believe me
        ' it works!!!!!!!!  PFD 4.4.2008
        ' reset the dteEndDate back to original

        dteEndDate.AddSeconds(-1)

        strSQLstring = "UPDATE PS_ISA_ENTERPRISE" & vbCrLf &
                    " SET ISA_LAST_STAT_SEND = TO_DATE('" & dteEndDate & "', 'MM/DD/YYYY HH:MI:SS AM')" & vbCrLf &
                    " WHERE ISA_BUSINESS_UNIT = '" & strBU & "' "

        Try
            Dim Command = New OleDbCommand(strSQLstring, connectOR)
            objStreamWriter.WriteLine("  updateEnterprise (1): " & strSQLstring & " " & Now())
            connectOR.Open()
            rowsaffected = Command.ExecuteNonQuery()
            connectOR.Close()
        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            objStreamWriter.WriteLine("  Error - updating the Enterprise send date " & OleDBExp.ToString & " " & Now())
            bolerror1 = True
            objXMLWriter.WriteEndElement()
            objXMLWriter.Flush()
            objXMLWriter.Close()
            Dim strXMLResult As String
            Dim objSR As StreamReader = File.OpenText(strXMLPath)
            strXMLResult = objSR.ReadToEnd()
            objSR.Close()
            objSR = Nothing
        End Try


        If bolerror1 = True Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function getDBName() As Boolean
        Dim isPRODDB As Boolean = False
        Dim PRODDbList As String = ConfigurationManager.AppSettings("OraPRODDbList").ToString()
        Dim DbUrl As String = ConfigurationManager.AppSettings("OLEDBconString").ToString()
        Try
            DbUrl = DbUrl.Substring(DbUrl.Length - 4).ToUpper()
            isPRODDB = (PRODDbList.IndexOf(DbUrl.Trim.ToUpper) > -1)
        Catch ex As Exception
            isPRODDB = False
        End Try
        Return isPRODDB
    End Function

    Private Function getpo_id(ByVal strorderno As String, ByVal strlineno As String, ByVal strBU As String, ByVal strSiteBU As String) As String
        Dim I As Integer
        Dim strpo_no As String
        Dim strSQLstring As String = "SELECT PO_ID B " & vbCrLf &
                         "from " & vbCrLf &
                           "ps_po_line_distrib   " & vbCrLf &
                             " WHERE req_id= '" & strorderno & "' " & vbCrLf &
                              " AND req_line_nbr = " & CType(strlineno, Integer) & " " & vbCrLf &
                              "and Business_unit= '" & strSiteBU & "' " '& vbCrLf & _
        '" AND BUSINESS_UNIT_IN = '" & strBU & "' "

        Try
            objStreamWriter.WriteLine("  GetPO_ID: " & strSQLstring)

            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            connectOR.Open()
            strpo_no = ORDBAccess.GetScalar(strSQLstring, connectOR)
            connectOR.Close()
        Catch ex As Exception
            Try
                connectOR.Close()
            Catch ex3 As Exception

            End Try
            strpo_no = 0
        End Try
        'Dim ds1 As DataSet = ORDBAccess.GetScalar(strSQLstring, connectOR)
        'strpo_no = ds1.Tables(0).Rows(I).Item("PO_ID")
        Return strpo_no
        '
    End Function
    'INC0042603 - Changed .net maui push notification since the Xamarin push notification is outdated (-Shanmugapriya)
    Public Sub sendNotification(ByVal Session_UserID As String, ByVal subject As String, ByVal orderNo As String, Optional ByVal strbu As String = Nothing, Optional ByVal redirectscreen As String = " ", Optional ByVal CurrentApprover As String = " ")
        Dim Response As String = String.Empty
        Dim URL1 As String = String.Empty
        Try
            Try
                If strbu = "I0W01" Then
                    URL1 = ConfigurationManager.AppSettings("WalmartPushNotification")
                Else
                    URL1 = ConfigurationManager.AppSettings("PushNotification")
                End If
            Catch ex As Exception
            End Try
            Dim client = New HttpClient()
                client.DefaultRequestHeaders.Accept.Clear()
                client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/json"))
                Dim notification As NotificationBO = New NotificationBO()
                notification.subject = subject
                notification.SessionUser = Session_UserID
                notification.orderno = orderNo
                notification.redirectscreen = redirectscreen
                notification.ApproverName = CurrentApprover
                Dim Serializedparameter = JsonConvert.SerializeObject(notification)
                Dim res As HttpResponseMessage = client.PostAsync(URL1, New StringContent(Serializedparameter, Encoding.UTF8, "application/json")).Result
                Try
                    If res.StatusCode = HttpStatusCode.OK Then
                        Response = res.Content.ReadAsStringAsync().Result
                    Else
                        Response = "Error"
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
                Dim d = ex.Message
        End Try
    End Sub

    Public Class NotificationBO

        Public Property SessionUser As String
        Public Property subject As String
        Public Property orderno As String
        Public Property redirectscreen As String
        Public Property ApproverName As String = " "

    End Class

    Public Function PurchaseOrderNo(ByVal workOrder As String, THIRDPARTY_COMP_ID As String) As String
        Try
            Dim APIresponse = String.Empty
            Dim objWorkOrderDetails As New WorkOrderDetails
            'Commented the CBRE Authentication for getting work order details
            If Not String.IsNullOrEmpty(THIRDPARTY_COMP_ID) Then
                If THIRDPARTY_COMP_ID = ConfigurationManager.AppSettings("CBRECompanyID").ToString() Then
                    APIresponse = AuthenticateService("CBRE")
                Else
                    APIresponse = AuthenticateService("Walmart")
                End If
            Else
                APIresponse = AuthenticateService("Walmart")
            End If
            ' APIresponse = Await AuthenticateService(Walmart)
            If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                If (Not APIresponse.Contains("error_description")) Then
                    Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                    Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder
                    Dim httpClient As HttpClient = New HttpClient()
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                    httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                    Dim response = httpClient.GetAsync(apiURL).Result
                    If response.IsSuccessStatusCode Then
                        Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                        If workorderAPIResponse <> "[]" And Not String.IsNullOrEmpty(workorderAPIResponse) And Not String.IsNullOrWhiteSpace(workorderAPIResponse) Then
                            objWorkOrderDetails = JsonConvert.DeserializeObject(Of WorkOrderDetails)(workorderAPIResponse)
                            Return objWorkOrderDetails.PurchaseNumber
                            objWalmartSC.WriteLine("Method: PurchaseOrderNo() Result-" + Convert.ToString(objWorkOrderDetails.PurchaseNumber))
                        Else
                            objWalmartSC.WriteLine("Method: PurchaseOrderNo() Result- Failed in API response")
                            Return "Failed"
                        End If
                    Else
                        Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                        objWalmartSC.WriteLine("Method: PurchaseOrderNo() Result- Failed in API response")
                        Return "Failed"
                    End If
                End If
            End If
        Catch ex As Exception
            Return "Failed"
            objWalmartSC.WriteLine("Method:PurchaseOrderNo - " + ex.Message)
        End Try
    End Function
    Public Function CheckWorkOrderStatus(ByVal workOrder As String, THIRDPARTY_COMP_ID As String) As String
        Try
            Dim APIresponse As String = String.Empty
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                If Not String.IsNullOrEmpty(THIRDPARTY_COMP_ID) Then
                    If THIRDPARTY_COMP_ID = ConfigurationManager.AppSettings("CBRECompanyID").ToString() Then
                        APIresponse = AuthenticateService("CBRE")
                    Else
                        APIresponse = AuthenticateService("Walmart")
                    End If
                End If
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/odata/" + "/workorders(" + workOrder + ")?$select=Status"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim response = httpClient.GetAsync(apiURL).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().result
                            Dim objCheckWo As CheckWo = JsonConvert.DeserializeObject(Of CheckWo)(workorderAPIResponse)
                            Return objCheckWo.Status.Primary
                            objWalmartSC.WriteLine("Method: CheckWorkOrderStatus() Result-" + Convert.ToString(objCheckWo.Status.Extended))
                        Else
                            objWalmartSC.WriteLine("Method: CheckWorkOrderStatus() Result- Failed in API response")
                            Return "Failed"
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Return "Failed"
            objWalmartSC.WriteLine("Method:CheckWorkOrderStatus - " + ex.Message)
        End Try
    End Function

    Public Function UpdateWorkOrderStatus(ByVal workOrder As String, credType As String, status As String) As String
        Try
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                Dim APIresponse = AuthenticateService(credType)
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/status"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        'Dim response = httpClient.GetAsync(apiURL).Result
                        Dim objPartParam As New UpdateWorkOrderBO
                        objPartParam.Note = String.Empty
                        objPartParam.Status = New Status
                        objPartParam.Status.Primary = "In Progress"
                        objPartParam.Status.Extended = status
                        Dim serializedparameter = JsonConvert.SerializeObject(objPartParam)
                        Dim response = httpClient.PutAsync(apiURL, New StringContent(serializedparameter, Encoding.UTF8, "application/json")).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().result
                            objWalmartSC.WriteLine("Result-" + Convert.ToString(workorderAPIResponse) & " " & Now())
                            Return "Success"
                        Else
                            objWalmartSC.WriteLine("Result- Failed in API response" & " " & Now())
                            Return "Failed"
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Return "Failed"
            objWalmartSC.WriteLine("Method:UpdateWorkOrderStatus - " + ex.Message & " " & Now())
        End Try
    End Function

    Public Function AuthenticateService(credType As String) As String
        Try
            Dim httpClient As HttpClient = New HttpClient()
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            Dim username As String = String.Empty
            Dim password As String = String.Empty
            Dim clientKey As String = String.Empty
            If credType = "Walmart" Then
                username = ConfigurationManager.AppSettings("WMUName")
                password = ConfigurationManager.AppSettings("WMPassword")
                clientKey = ConfigurationManager.AppSettings("WMClientKey")
            Else
                username = ConfigurationManager.AppSettings("CBREUName")
                password = ConfigurationManager.AppSettings("CBREPassword")
                clientKey = ConfigurationManager.AppSettings("CBREClientKey")
            End If
            Dim apiurl As String = ConfigurationManager.AppSettings("ServiceChannelLoginEndPoint")
            Dim formContent = New FormUrlEncodedContent({New KeyValuePair(Of String, String)("username", username), New KeyValuePair(Of String, String)("password", password), New KeyValuePair(Of String, String)("grant_type", "password")})
            httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", clientKey) 'Add("Authorization", "Basic " + clientKey)
            Dim response = httpClient.PostAsync(apiurl, formContent).Result
            If response.IsSuccessStatusCode Then
                Dim APIResponse = response.Content.ReadAsStringAsync().Result
                Return APIResponse
            Else
                Dim APIResponse = response.Content.ReadAsStringAsync().Result
                'Dim eobj As ExceptionHelper = New ExceptionHelper()
                'eobj.writeExceptionMessage(APIResponse, "AuthenticateService")
                If APIResponse.Contains("error_description") Then Return APIResponse
                Return "Server Error"
            End If

        Catch ex As Exception
            objWalmartSC.WriteLine("Method:AuthenticateService - " + ex.Message)
        End Try
        Return "Server Error"
    End Function

    Public Function OrdrStatus(orderno As String) As String
        Try
            If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
                connectOR.Close()
            End If
            connectOR.Open()
            Dim orderDetail As String = String.Empty
            Dim qString As String = "select sysadm8.ord_stat_summary('" + orderno + "') from dual"
            orderDetail = ORDBAccess.GetScalar(qString, connectOR)
            Return orderDetail
        Catch ex As Exception
            objWalmartSC.WriteLine("Method: OrdrStatus(): " + Convert.ToString(ex.Message))
        End Try

    End Function

    Public Function GetWorkOrderParts(ByVal workOrder As String) As String
        Try
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                Dim APIresponse = AuthenticateService("Walmart")
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/parts"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim response = httpClient.GetAsync(apiURL).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                            If workorderAPIResponse <> "[]" And Not String.IsNullOrEmpty(workorderAPIResponse) And Not String.IsNullOrWhiteSpace(workorderAPIResponse) Then
                                'Return workorderAPIResponse
                                Dim objWorkOrder As List(Of WorkOrderParts) = JsonConvert.DeserializeObject(Of List(Of WorkOrderParts))(workorderAPIResponse)
                                Dim deletearrayOfID As New List(Of Int32)
                                Dim objWorkOrderParts As WorkOrderParts = objWorkOrder.FirstOrDefault()
                                If Not objWorkOrderParts Is Nothing Then
                                    deletearrayOfID.Add(objWorkOrderParts.id)
                                    DeleteWorkOrder(workOrder, deletearrayOfID.ToArray())
                                End If
                            Else
                                'Dim eobj As ExceptionHelper = New ExceptionHelper()
                                'eobj.writeExceptionMessage(workorderAPIResponse, "GetWorkOrderParts")
                                Return String.Empty
                            End If
                        Else
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                            'Dim eobj As ExceptionHelper = New ExceptionHelper()
                            'eobj.writeExceptionMessage(workorderAPIResponse, "GetWorkOrderParts")
                            Return String.Empty
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Throw ex
        End Try
        Return String.Empty
    End Function

    Public Function DeleteWorkOrder(ByVal workOrder As String, ByVal objPartParam As Integer()) As String
        Try
            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) And objPartParam.Count() > 0 Then
                Dim APIresponse = AuthenticateService("Walmart")
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim querySting As String = String.Empty
                        For Each items As Integer In objPartParam
                            querySting = String.Concat(querySting, "ids=" + items.ToString() + "&")
                        Next
                        querySting = querySting.Substring(0, querySting.LastIndexOf("&"))
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/workorders/" + workOrder + "/parts?" + querySting + ""
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim response = httpClient.DeleteAsync(apiURL).Result
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                        Else
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result
                            'Dim eobj As ExceptionHelper = New ExceptionHelper()
                            'eobj.writeExceptionMessage(workorderAPIResponse, "DeleteWorkOrder")
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            'Dim eobj As ExceptionHelper = New ExceptionHelper()
            'eobj.writeException(ex)
        End Try
    End Function

    Public Sub InsertWorkOrder(workOrder As String)
        Try


            If Not String.IsNullOrEmpty(workOrder) And Not String.IsNullOrWhiteSpace(workOrder) Then
                Dim APIresponse = AuthenticateService("Walmart")
                If (APIresponse <> "Server Error" And APIresponse <> "Internet Error" And APIresponse <> "Error") Then
                    If (Not APIresponse.Contains("error_description")) Then
                        Dim objValidateUserResponseBO As ValidateUserResponseBO = JsonConvert.DeserializeObject(Of ValidateUserResponseBO)(APIresponse)
                        Dim apiURL = ConfigurationManager.AppSettings("ServiceChannelBaseAddress") + "/workorders/inventory/parts/bulkPartUsage"
                        Dim httpClient As HttpClient = New HttpClient()
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", objValidateUserResponseBO.access_token)
                        Dim objInserWorOrdeParts As New InsertWorkOrderPartsBO
                        objInserWorOrdeParts.AddItems = New List(Of AddItem)



                        objInserWorOrdeParts.AddItems.Add(New AddItem() With {
                                .RecId = workOrder,
                             .Description = "SCTest",
                             .Quantity = "1",
                             .UnitCost = "1",
                             .PartNumber = "WM00455570:1"
                            })

                        Dim serializedparameter = JsonConvert.SerializeObject(objInserWorOrdeParts)
                        Dim response = httpClient.PostAsync(apiURL, New StringContent(serializedparameter, Encoding.UTF8, "application/json")).Result()
                        If response.IsSuccessStatusCode Then
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result()
                            'UpdateWorkOrderStatus(workOrder, Walmart)
                        Else
                            Dim workorderAPIResponse As String = response.Content.ReadAsStringAsync().Result()

                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    'SP-316 get from and to mails from the table- Dhamotharan 
    Public Function getFromMail(ByVal strBU As String, ByVal connectOR As OleDb.OleDbConnection)
        Dim sqlStringEmailFrom As String = ""
        Dim fromMail As String = ""
        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If
        Try
            connectOR.Open()
            sqlStringEmailFrom = "Select ISA_PURCH_EML_FROM from PS_ISA_BUS_UNIT_PM where BUSINESS_UNIT_PO = '" & strBU & "'"
            fromMail = ORDBAccess.GetScalar(sqlStringEmailFrom, connectOR)
        Catch ex As Exception
            If (strBU = "I0W01" Or strBU = "WAL00") Then
                fromMail = "WalmartPurchasing@sdi.com"
            ElseIf (strBU = "EMC00" Or strBU = "I0631") Then
                fromMail = "Emcorpurchasing@sdi.com"
            ElseIf (strBU = "I0643" Or strBU = "AMC00") Then
                fromMail = "americoldpurchasing@sdi.com"
            ElseIf (strBU = "I0645" Or strBU = "BOE00") Then
                fromMail = "boeing@sdi.com"
            Else
                fromMail = "SDIExchange@SDI.com"
            End If
        End Try
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try
        Return fromMail
    End Function
    Public Function getToMail(ByVal StrBu As String, ByVal connectOR As OleDb.OleDbConnection)
        Dim sqlStringEmailFrom As String = ""
        Dim toMail As String = ""
        If Not connectOR Is Nothing AndAlso ((connectOR.State And ConnectionState.Open) = ConnectionState.Open) Then
            connectOR.Close()
        End If
        Try
            connectOR.Open() 'Mythili -- INC0023448 Adding CC emails
            sqlStringEmailFrom = "Select ISA_PURCH_EML_TO from PS_ISA_BUS_UNIT_PM where BUSINESS_UNIT_PO = '" & StrBu & "'"
            toMail = ORDBAccess.GetScalar(sqlStringEmailFrom, connectOR)
        Catch ex As Exception
            If (StrBu = "I0W01" Or StrBu = "WAL00") Then
                toMail = "WalmartPurchasing@sdi.com"
            ElseIf (StrBu = "EMC00" Or StrBu = "I0631") Then
                toMail = "Emcorpurchasing@sdi.com"
            ElseIf (StrBu = "I0643" Or StrBu = "AMC00") Then
                toMail = "americoldpurchasing@sdi.com"
            ElseIf (StrBu = "I0645" Or StrBu = "BOE00") Then
                toMail = "boeing@sdi.com"
            End If
        End Try
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try
        Return toMail
    End Function
End Module

Public Class AddItem
    Public Property RecId As String = String.Empty
    Public Property Quantity As Double
    Public Property UnitCost As Double
    Public Property UseDate As String = DateTime.Now.ToString()
    Public Property PartNumber As String = String.Empty
    Public Property Description As String = String.Empty
End Class

Public Class InsertWorkOrderPartsBO
    Public Property AddItems As List(Of AddItem)
    Public Property UpdateItems As List(Of Object) = New List(Of Object)
    Public Property DeleteItems As List(Of Object) = New List(Of Object)
    Public Property IsLocalTime As Boolean = True
End Class

Public Class WorkOrderParts
    Public Property id As Integer
    Public Property Quantity As Double
    Public Property Description As String = String.Empty
    Public Property Price As Double
    Public Property SupplierPartId As String = String.Empty
End Class

Public Class WebRequestFcmData
    Public Property registration_ids As String()
    Public Property notification As New NotificationData
    Public Property data As New dataBO
End Class

Public Class dataBO
    Public Property orderid As String
End Class

Public Class NotificationData
    Public Property body As String
    Public Property title As String = "ZEUS"
    Public Property sound As String
End Class

Public Class OrderStatusDetail
    Public Property message As String
    Public Property orderStatus As String
    Public Property statusDesc As String
    Public Property dueDate As String
End Class

Public Class ValidateUserResponseBO
    Public Property access_token As String
    Public Property refresh_token As String
End Class

Public Class UpdateWorkOrderBO
    Public Property Status As Status
    Public Property Note As String
End Class

Public Class Status
    Public Property Primary As String
    Public Property Extended As String
End Class
Public Class Location
    Public Property StoreId As String = String.Empty
End Class

Public Class Notes
    Public Property Last As Last
End Class

Public Class Last
    Public Property NoteData As String = String.Empty
End Class

Public Class Asset
    Public Property Tag As String = String.Empty
End Class

Public Class WorkOrderDetails
    Public Property Notes As Notes
    Public Property Location As Location
    Public Property Asset As Asset
    Public Property PurchaseNumber As String = String.Empty

End Class

Public Class WOStatus
    Public Property Primary As String
    Public Property Extended As String
    Public Property CanCreateInvoice As String
End Class


Public Class CheckWo
    Public Property OdataContext As String
    Public Property Status As WOStatus
End Class
Public Class UpdateNotes  'Mythili - WAL-534, Updating all the supplier portal and expedite comments to service channel
    Public Property Note As String
    Public Property MailedTo As String
    Public Property ActionRequired As Boolean
    Public Property ScheduledDate As DateTime
    Public Property Visibility As Integer
    Public Property Actor As String
    Public Property NotifyFollowers As Boolean
    Public Property DoNotSendEmail As Boolean
End Class
