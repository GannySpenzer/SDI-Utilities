Imports System.IO
Imports System.Data.OleDb
Imports System.Web.Mail
Imports System.Xml
Imports System.Text
Imports System.Web.UI
Imports System.Net
Imports System.Net.Mail
Imports SDI.ApplicationLogger
Imports SDI.UNCC.WorkOrderAdapter
Imports System.Collections.Generic

Module module1

    Private m_logger As appLogger = Nothing
    Private myLoggr1 As appLogger = Nothing

    ' disabled since logging functionality was ported to SDI.ApplicationLogger
    'Dim objStreamWriter As StreamWriter
    Dim rootDir As String = "C:\INTFCXML"
    Dim logpath As String = "C:\INTFCXML\LOGS\UpdINTFCXMLIn" & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim sErrLogPath As String = "C:\INTFCXML\LOGS\MyErredSQLs" & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
    Dim connectOR As New OleDbConnection("Provider=OraOLEDB.Oracle.1;Password=sd1exchange;User ID=sdiexchange;Data Source=RPTG")
    Dim connectSQL As New SqlClient.SqlConnection("server=cplus_prod;uid=einternet;pwd=einternet;initial catalog='contentplus'")
    Dim strOverride As String
    Dim bolWarning As Boolean = False

    'Private m_arrErrXMLs As New ArrayList
    Private m_arrXMLErrFiles As String
    Private m_arrErrorsList As String

    Sub Main()

        Dim rtn As String = "Module1.Main"

        strOverride = "N"

        ' default log level
        Dim logLevel As System.Diagnostics.TraceLevel = TraceLevel.Verbose
        '
        ' read confirugation values ...
        '   values on app.config will always win!
        '   -- 3/19/2014 erwin
        '
        '   (1) connection string / db connection
        Dim cnStringORA As String = ""
        Try
            cnStringORA = My.Settings("oraCNString1").ToString.Trim
        Catch ex As Exception
        End Try
        If (cnStringORA.Length > 0) Then
            ' drop current connection and re-create
            Try
                connectOR.Dispose()
            Catch ex As Exception
            End Try
            connectOR = Nothing
            connectOR = New OleDbConnection(cnStringORA)
        End If
        '   (2) root directory
        Dim rDir As String = ""
        Try
            rDir = My.Settings("rootDir").ToString.Trim
        Catch ex As Exception
        End Try
        If (rDir.Length > 0) Then
            rootDir = rDir
        End If
        '   (3) log path/file
        Dim sLogPath As String = ""
        Try
            sLogPath = My.Settings("logPath").ToString.Trim
        Catch ex As Exception
        End Try
        If (sLogPath.Length > 0) Then
            logpath = sLogPath & "\UpdINTFCXMLIn" & Now.Year & Now.Month & Now.Day & Now.GetHashCode & ".txt"
        End If
        '   (4) connection string / db connection
        Dim cnStringSQL As String = ""
        Try
            cnStringSQL = My.Settings("sqlCNString1").ToString.Trim
        Catch ex As Exception
        End Try
        If (cnStringSQL.Length > 0) Then
            ' drop current connection and re-create
            Try
                connectSQL.Dispose()
            Catch ex As Exception
            End Try
            connectSQL = Nothing
            connectSQL = New SqlClient.SqlConnection(cnStringSQL)
        End If
        '   END : 3/19/2014 erwin

        ' changed this include params for logging level
        '   - erwin 2009.09.06
        'Dim separators As String = " "
        'Dim commands As String = Microsoft.VisualBasic.Command()
        'Dim args() As String = commands.Split(separators.ToCharArray)
        'Console.WriteLine(args(0))
        'If args(0) = "Y" Then
        '    strOverride = "Y"
        'ElseIf System.Diagnostics.Debugger.IsAttached = True Then
        '    strOverride = "Y"
        'End If
        Dim param As String() = Command.Split(" ")
        Dim arr As New ArrayList
        For Each s As String In param
            If s.Trim.Length > 0 Then
                arr.Add(s.Trim.ToUpper)
            End If
        Next
        If (arr.IndexOf("/LOG=VERBOSE") > -1) Then
            logLevel = TraceLevel.Verbose
        ElseIf (arr.IndexOf("/LOG=INFO") > -1) Or (arr.IndexOf("/LOG=INFORMATION") > -1) Then
            logLevel = TraceLevel.Info
        ElseIf (arr.IndexOf("/LOG=WARNING") > -1) Or (arr.IndexOf("/LOG=WARN") > -1) Then
            logLevel = TraceLevel.Warning
        ElseIf (arr.IndexOf("/LOG=ERROR") > -1) Then
            logLevel = TraceLevel.Error
        ElseIf (arr.IndexOf("/LOG=OFF") > -1) Or (arr.IndexOf("/LOG=FALSE") > -1) Then
            logLevel = TraceLevel.Off
        Else
            ' don't change default
        End If
        ' check/enable strOverride (whatever this is)
        If (arr.IndexOf("/OVERRIDE=Y") > -1) Then
            strOverride = "Y"
        ElseIf System.Diagnostics.Debugger.IsAttached Then
            strOverride = "Y"
        End If
        arr = Nothing
        param = Nothing

        ' initialize logs

        myLoggr1 = New SDI.ApplicationLogger.appLogger(sErrLogPath, TraceLevel.Error)
        myLoggr1.WriteErrorLog("Error Log Created")

        m_logger = New appLogger(logpath, logLevel)
        m_logger.WriteInformationLog("" & _
                                     System.Reflection.Assembly.GetExecutingAssembly.GetModules()(0).FullyQualifiedName & _
                                     " v" & System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString & _
                                     "")

        Exec1()

        ' destroy logger object
        Try
            m_logger.Dispose()
        Catch ex As Exception
        Finally
            m_logger = Nothing
        End Try

    End Sub

    Sub Exec1()

        Dim rtn As String = "intfcUpdate.Exec1"

        Console.WriteLine("Start INTFC XML in")
        Console.WriteLine("")

        m_logger.WriteInformationLog(rtn & " :: Update of INTFC IN XML.")

        m_logger.WriteVerboseLog(rtn & " :: (Oracle)connection string : [" & connectOR.ConnectionString & "]")
        'm_logger.WriteVerboseLog(rtn & " :: (SQL Server)connection string : [" & connectSQL.ConnectionString & "]")

        '// ***
        '// This is the moving of files to the XMLIN folder ...
        '// ***

        ' ''Dim dirInfo As DirectoryInfo = New DirectoryInfo("\\contentplus\c$\inetpub\webpartnerplus\xmlprocess\unccxml\")
        ' ''Dim dirInfo As DirectoryInfo = New DirectoryInfo("\\contentplus\unccxml\")
        ''Dim dirInfo As DirectoryInfo = New DirectoryInfo("C:\Inetpub\WebPartnerPlus\XMLProcess\UNCCXML\")
        'Dim dirInfo As DirectoryInfo = New DirectoryInfo("\\dazzle\unccInSiteOnlineOrderIN\")
        Dim dirInfo As DirectoryInfo = New DirectoryInfo("C:\INTFCXML\XmlInSource\")
        Dim s1 As String = ""
        Try
            s1 = CStr(My.Settings("inputDirectory")).Trim
        Catch ex As Exception
        End Try
        If (s1.Length > 0) Then
            dirInfo = New DirectoryInfo(s1)
        End If
        ''debug

        'dirInfo = New DirectoryInfo("c:\INTFCXML\LOGS\")

        'Dim dirInfo As DirectoryInfo = New DirectoryInfo("\\dextest4\c$\inetpub\wwwroot\xmlprocess\unccxml\")
        Dim strFiles As String
        Dim arrOrders As ArrayList
        'Dim strOrderNO As String
        arrOrders = New ArrayList
        'Dim bolInArray As Boolean

        strFiles = "*.XML"
        Dim aFiles As FileInfo() = dirInfo.GetFiles(strFiles)
        Dim I As Integer
        'Dim X As Integer



        Try
            If aFiles.Length > 0 Then
                For I = 0 To aFiles.Length - 1
                    File.Copy(aFiles(I).FullName, "C:\INTFCXML\XMLIN\" & aFiles(I).Name, True)
                    File.Delete(aFiles(I).FullName)
                    m_logger.WriteInformationLog(rtn & " :: " & aFiles(I).FullName & " moved to " & "C:\INTFCXML\XMLIN\" & aFiles(I).Name)
                Next
            Else
                'objStreamWriter.WriteLine("  No files to copy")
                m_logger.WriteInformationLog(rtn & " :: No files to copy from " & dirInfo.FullName)
            End If
        Catch ex As Exception
            'objStreamWriter.WriteLine("  Error copying files")
            'objStreamWriter.WriteLine(ex.ToString)
            'objStreamWriter.WriteLine("  End of INTFC IN XML " & Now())
            'objStreamWriter.Flush()
            'objStreamWriter.Close()
            m_logger.WriteErrorLog(rtn & " :: Error moving file(s) from " & dirInfo.FullName & " to C:\INTFCXML\XMLIN\ " & "...")
            m_logger.WriteErrorLog(rtn & " :: " & ex.ToString)
            m_logger.WriteErrorLog(rtn & " :: End of INTFC IN XML.")
            SendEmail()
            Exit Sub
        End Try

        '// ***
        '// This is the updating of the INTFC tables ...
        '// ***

        Dim bolError As Boolean
        
        Try
            bolError = getINTFCXMLIn()
        Catch ex As Exception
            m_logger.WriteErrorLog(rtn & " :: " & ex.ToString)
            bolError = True
        End Try

        If bolError = True Or _
            bolWarning = True Then
            SendEmail(True)
        End If
        Try
            connectOR.Close()
        Catch ex As Exception

        End Try
        
        m_logger.WriteInformationLog(rtn & " :: End of INTFC IN XML.")

    End Sub

    Private Function getINTFCXMLIn() As Boolean

        Dim rtn As String = "intfcUpdate.getINTFCXMLIn"

        Console.WriteLine("Start Update of INTFC IN XML")
        Console.WriteLine("")

        m_logger.WriteInformationLog(rtn & " :: Start Update of INTFC XML ")

        Dim dirInfo As DirectoryInfo = New DirectoryInfo("C:\INTFCXML\XMLIN\")
        Dim strFiles As String = ""
        Dim sr As System.IO.StreamReader
        Dim XMLContent As String = ""
        Dim strXMLError As String = ""
        Dim bolError As Boolean
        Dim bLineError As Boolean = False

        Dim xmlRequest As New XmlDocument

        Dim I As Integer
        'Dim strItems As String
        'Dim intItems As Integer

        strFiles = "*.XML"
        Dim aFiles As FileInfo() = dirInfo.GetFiles(strFiles)
        Dim root As XmlElement

        Dim sXMLFilename As String = ""
        Dim bAreThereNewOrders As Boolean = False

        Try
            If aFiles.Length > 0 Then
                m_arrXMLErrFiles = ""
                m_arrErrorsList = ""
                For I = 0 To aFiles.Length - 1
                    sXMLFilename = aFiles(I).Name
                    bLineError = False

                    m_logger.WriteInformationLog(rtn & " :: Start File " & aFiles(I).Name)
                    sr = New System.IO.StreamReader(aFiles(I).FullName)
                    XMLContent = sr.ReadToEnd()
                    XMLContent = Replace(XMLContent, "&", "&amp;")
                    'here where we load the xml into memory
                    'XMLContent = Replace(XMLContent, "'", "&#39;")
                    'XMLContent = Replace(XMLContent, """", "&quot;")
                    sr.Close()

                    Try
                        xmlRequest.LoadXml(XMLContent)
                    Catch ex As Exception
                        Console.WriteLine("")
                        Console.WriteLine("***error - " & ex.ToString)
                        Console.WriteLine("")
                        m_logger.WriteErrorLog(rtn & " :: Error " & ex.Message.ToString & " in file " & aFiles(I).Name)
                        strXMLError = ex.ToString
                        bolError = True
                        bLineError = True
                        Try
                            File.Move(aFiles(I).FullName, "C:\INTFCXML\BadXML\" & aFiles(I).Name)
                            File.Delete(aFiles(I).FullName)
                        Catch ex24 As Exception

                            m_logger.WriteErrorLog(rtn & " :: ** ")
                            m_logger.WriteErrorLog(rtn & " :: Error " & ex24.Message & " in file " & aFiles(I).Name)
                            m_logger.WriteErrorLog(rtn & " :: ** ")
                        End Try
                    End Try
                    If Trim(strXMLError) = "" Then
                        root = xmlRequest.DocumentElement

                        If root.FirstChild Is Nothing Then
                            strXMLError = "empty UNCC in file"
                        ElseIf root.LastChild.Name.ToUpper = "PS_ISA_ORD_INTFC_L" Or _
                            root.LastChild.Name.ToUpper = "EMP_ROW" Then
                            strXMLError = ""
                        Else
                            strXMLError = "Missing last INTFC_L Element"
                        End If

                        If Trim(strXMLError) = "" Then
                            If root.LastChild.Name.ToUpper = "PS_ISA_ORD_INTFC_L" Then
                                bAreThereNewOrders = True
                                strXMLError = UpdIntfcTable(aFiles(I).FullName)
                            ElseIf root.LastChild.Name.ToUpper = "EMP_ROW" Then
                                strXMLError = UpdEmpTable(aFiles(I).FullName)
                            End If
                        End If

                        If Trim(strXMLError) = "" Or _
                            Trim(strXMLError) = "Invalid Status" Then
                            Try
                                File.Move(aFiles(I).FullName, "C:\INTFCXML\XMLINProcessed\" & aFiles(I).Name)
                                If Trim(strXMLError) = "Invalid Status" Then
                                    'objStreamWriter.WriteLine(" not status I or C in File " & aFiles(I).Name)
                                    m_logger.WriteInformationLog(rtn & " :: not status I or C in File " & aFiles(I).Name)
                                End If
                                'objStreamWriter.WriteLine(" End - File " & aFiles(I).Name & " has been moved")
                                m_logger.WriteInformationLog(rtn & " :: End - File " & aFiles(I).Name & " has been moved")
                            Catch ex As Exception
                                'objStreamWriter.WriteLine("     Error " & ex.Message.ToString & " in file " & aFiles(I).Name)
                                m_logger.WriteErrorLog(rtn & " :: Error " & ex.Message.ToString & " in file " & aFiles(I).Name)
                            End Try
                            File.Delete(aFiles(I).FullName)
                        Else

                            m_logger.WriteErrorLog(rtn & " :: ** ")
                            m_logger.WriteErrorLog(rtn & " :: Error " & strXMLError & " in file " & aFiles(I).Name)
                            m_logger.WriteErrorLog(rtn & " :: ** ")
                            bolError = True
                            bLineError = True
                            Try
                                File.Move(aFiles(I).FullName, "C:\INTFCXML\BadXML\" & aFiles(I).Name)
                                File.Delete(aFiles(I).FullName)
                            Catch ex As Exception
                                
                                m_logger.WriteErrorLog(rtn & " :: ** ")
                                m_logger.WriteErrorLog(rtn & " :: Error " & ex.Message & " in file " & aFiles(I).Name)
                                m_logger.WriteErrorLog(rtn & " :: ** ")
                            End Try
                        End If

                    End If

                    ' if there's an error, capture the filename of the XML and corresponding error message
                    If (Trim(strXMLError) <> "") Or bLineError Then  '  If (Trim(strXMLError) <> "") Or bolError Then
                        'm_arrErrXMLs.Add(sXMLFilename)
                        If Trim(m_arrXMLErrFiles) = "" Then
                            m_arrXMLErrFiles = sXMLFilename
                        Else
                            m_arrXMLErrFiles &= "," & sXMLFilename
                        End If
                        If Trim(strXMLError) <> "" Then
                            If Len(strXMLError) > 250 Then
                                strXMLError = Microsoft.VisualBasic.Left(strXMLError, 250)
                            End If
                            If Trim(m_arrErrorsList) = "" Then
                                m_arrErrorsList = strXMLError
                            Else
                                m_arrErrorsList &= "," & strXMLError
                            End If
                        Else
                            If Trim(m_arrErrorsList) = "" Then
                                m_arrErrorsList = "No Error Description"
                            Else
                                m_arrErrorsList &= "," & "No Error Description"
                            End If
                        End If
                    End If

                    strXMLError = ""
                Next

            End If  '  If aFiles.Length > 0 Then
            
            If bAreThereNewOrders Then
                SendBuyerEmail()
            End If

        Catch ex As Exception
            'strXMLError = ex.ToString
            'bolError = True
            'm_arrErrXMLs.Add(sXMLFilename)
            If Trim(m_arrXMLErrFiles) = "" Then
                m_arrXMLErrFiles = sXMLFilename
            Else
                m_arrXMLErrFiles &= "," & sXMLFilename
            End If
            If Trim(strXMLError) <> "" Then
                If Len(strXMLError) > 250 Then
                    strXMLError = Microsoft.VisualBasic.Left(strXMLError, 250)
                End If
                If Trim(m_arrErrorsList) = "" Then
                    m_arrErrorsList = strXMLError
                Else
                    m_arrErrorsList &= "," & strXMLError
                End If
            Else
                If Trim(m_arrErrorsList) = "" Then
                    m_arrErrorsList = "No Error Description"
                Else
                    m_arrErrorsList &= "," & "No Error Description"
                End If
            End If
            Try
                File.Move(aFiles(I).FullName, "C:\INTFCXML\BadXML\" & aFiles(I).Name)
                File.Delete(aFiles(I).FullName)
            Catch ex32 As Exception

                m_logger.WriteErrorLog(rtn & " :: ** ")
                m_logger.WriteErrorLog(rtn & " :: Error " & ex32.Message & " in file " & aFiles(I).Name)
                m_logger.WriteErrorLog(rtn & " :: ** ")
            End Try
            Throw New ApplicationException(rtn & "::" & ex.Message, ex)
        End Try

        Return bolError

    End Function

    Private Function UpdEmpTable(ByVal strPath As String) As String

        Dim rtn As String = "intfcUpdate.UpdEmpTable"
        Dim reader As XmlTextReader
        Try
            reader = New XmlTextReader(strPath)
        Catch ex As Exception
            Console.WriteLine("")
            Console.WriteLine("***error - " & ex.ToString)
            Console.WriteLine("")
            Return ex.ToString
        End Try
        reader.Close()

        Dim X As Integer
        Dim H As Integer
        Dim L As Integer
        Dim strEmpError As String

        Dim sr As System.IO.StreamReader
        Dim XMLContent As String
        Dim xmlRequest As New XmlDocument
        sr = New System.IO.StreamReader(strPath)
        XMLContent = sr.ReadToEnd()
        sr.Close()

        xmlRequest.LoadXml(XMLContent)

        Dim jobNode As XmlNode
        Dim strSQLstring As String
        Dim rowsaffected As Integer
        Dim bolErrorOccured As Boolean
        Dim I As Integer
        Dim dteNow As Date
        dteNow = Now().ToString("d")

        'For X = 0 To xmlRequest.ChildNodes.Count - 1
        jobNode = xmlRequest.ChildNodes(1)
        Dim dsRows As New DataSet
        dsRows.ReadXml(New XmlNodeReader(jobNode))
        Dim strbu As String

        connectOR.Open()
        connectSQL.Open()

        For I = 0 To dsRows.Tables(0).Rows.Count - 1
            Dim bolerror As Boolean = False
            Dim strBusinessUnit As String = getColumnValue(dsRows.Tables(0).Rows(I), "BUSINESS_UNIT", 5)
            If Trim(strBusinessUnit) = "" Then
                'objStreamWriter.WriteLine("**")
                'objStreamWriter.WriteLine("     Error - invalid BU for record " & I)
                'objStreamWriter.WriteLine("**")
                m_logger.WriteErrorLog(rtn & " :: Error - invalid BU for record " & I)
                bolerror = True
            End If
            Dim strFirstName As String = getColumnValue(dsRows.Tables(0).Rows(I), "FIRST_NAME_SRCH", 30)
            If Trim(strFirstName) = "" Then
                'objStreamWriter.WriteLine("**")
                'objStreamWriter.WriteLine("     Error - invalid first name for record " & I)
                'objStreamWriter.WriteLine("**")
                m_logger.WriteErrorLog(rtn & " :: Error - invalid first name for record " & I)
                bolerror = True
            End If
            Dim strLastName As String = getColumnValue(dsRows.Tables(0).Rows(I), "LAST_NAME_SRCH", 30)
            If Trim(strLastName) = "" Then
                'objStreamWriter.WriteLine("**")
                'objStreamWriter.WriteLine("     Error - invalid last name for record " & I)
                'objStreamWriter.WriteLine("**")
                m_logger.WriteErrorLog(rtn & " :: Error - invalid last name for record " & I)
                bolerror = True
            End If
            Dim strFullName As String = strLastName & "," & strFirstName
            If strFullName.Length > 40 Then
                strFullName = strFullName.Substring(0, 40)
            End If
            Dim strEmail As String = getColumnValue(dsRows.Tables(0).Rows(I), "ISA_EMPLOYEE_EMAIL", 40)
            If Trim(strEmail) = "" Then
                'objStreamWriter.WriteLine("**")
                'objStreamWriter.WriteLine("     Error - invalid email for record " & I)
                'objStreamWriter.WriteLine("**")
                m_logger.WriteErrorLog(rtn & " :: Error - invalid email for record " & I)
                bolerror = True
            End If
            Dim strPhone As String = getColumnValue(dsRows.Tables(0).Rows(I), "PHONE_NUM", 12)
            If Trim(strPhone) = "" Then
                strPhone = " "
            End If
            Dim strEmpID As String = getColumnValue(dsRows.Tables(0).Rows(I), "ISA_EMPLOYEE_ID", 8)
            If Trim(strEmpID) = "" Then
                'objStreamWriter.WriteLine("**")
                'objStreamWriter.WriteLine("     Error - invalid emp ID for record " & I)
                'objStreamWriter.WriteLine("**")
                m_logger.WriteErrorLog(rtn & " :: Error - invalid emp ID for record " & I)
                bolerror = True
            End If

            Dim objUsersTbl As New clsUserTbl(strEmpID, strBusinessUnit, connectOR)
            If Not bolerror = True Then
                If Not Trim(objUsersTbl.IOLAppEmpID) = "" Then

                    strSQLstring = "UPDATE PS_ISA_USERS_TBL" & vbCrLf & _
                        " SET FIRST_NAME_SRCH = '" & strFirstName & "'," & vbCrLf & _
                        " LAST_NAME_SRCH = '" & strLastName & "'," & vbCrLf & _
                        " ISA_USER_NAME = '" & strFullName & "'," & vbCrLf & _
                        " BUSINESS_UNIT = '" & strBusinessUnit & "'," & vbCrLf & _
                        " ISA_EMPLOYEE_NAME = '" & strFullName & "'," & vbCrLf & _
                        " PHONE_NUM = '" & strPhone & "'," & vbCrLf & _
                        " ISA_EMPLOYEE_EMAIL = '" & strEmail & "'" & vbCrLf & _
                        " WHERE BUSINESS_UNIT = '" & strBusinessUnit & "'" & vbCrLf & _
                        " AND ISA_EMPLOYEE_ID = '" & strEmpID & "'"

                    Dim Command1 As OleDbCommand
                    Command1 = New OleDbCommand(strSQLstring, connectOR)
                    Try
                        rowsaffected = command1.ExecuteNonQuery
                        If rowsaffected = 0 Then
                            'objStreamWriter.WriteLine("**")
                            'objStreamWriter.WriteLine("     Error - 0 rows affected users tbl for record " & I)
                            'objStreamWriter.WriteLine("**")
                            m_logger.WriteErrorLog(rtn & " :: Error - 0 rows affected users tbl for record " & I)
                            bolerror = True
                        End If
                    Catch OleDBExp As OleDbException
                        'objStreamWriter.WriteLine("**")
                        'objStreamWriter.WriteLine("     Error - updating users tbl for record " & I)
                        'objStreamWriter.WriteLine("**")
                        m_logger.WriteErrorLog(rtn & " :: Error - updating users tbl for record " & I)
                        bolerror = True
                    End Try
                    Command1.Dispose()
                Else

                    strSQLstring = "INSERT INTO PS_ISA_USERS_TBL" & vbCrLf & _
                        " (ISA_USER_ID, ISA_USER_NAME," & vbCrLf & _
                        " ISA_PASSWORD_ENCR, FIRST_NAME_SRCH," & vbCrLf & _
                        " LAST_NAME_SRCH, BUSINESS_UNIT," & vbCrLf & _
                        " ISA_EMPLOYEE_ID, ISA_EMPLOYEE_NAME, PHONE_NUM," & vbCrLf & _
                        " ISA_DAILY_ALLOW, ISA_EMPLOYEE_PASSW," & vbCrLf & _
                        " ISA_EMPLOYEE_EMAIL, ISA_EMPLOYEE_ACTYP," & vbCrLf & _
                        " CUST_ID, ISA_SESSION," & vbCrLf & _
                        " ISA_LAST_SYNC_DATE, ISA_SDI_EMPLOYEE, ISA_CUST_SERV_FLG," & vbCrLf & _
                        " LASTUPDOPRID, LASTUPDDTTM, ACTIVE_STATUS)" & vbCrLf & _
                        " VALUES(SEQ_ISA_USER_ID_PK.nextval," & vbCrLf & _
                        " '" & strFullName.ToUpper & "'," & vbCrLf & _
                        " 'nSLnA5YkVzKuLhkvojAo/g=='," & vbCrLf & _
                        " '" & strFirstName.ToUpper & "'," & vbCrLf & _
                        " '" & strLastName.ToUpper & "'," & vbCrLf & _
                        " '" & strBusinessUnit.ToUpper & "'," & vbCrLf & _
                        " '" & strEmpID & "'," & vbCrLf & _
                        " '" & strFullName.ToUpper & "'," & vbCrLf & _
                        " '" & strPhone & "'," & vbCrLf & _
                        " 0, ' ', '" & Trim(strEmail) & "'," & vbCrLf & _
                        " 'USER'," & vbCrLf & _
                        " '0', 0, '', 'C', ' '," & vbCrLf & _
                        " 'INTFCVB'," & vbCrLf & _
                        " TO_DATE('" & Now() & "', 'MM/DD/YYYY HH:MI:SS AM')," & vbCrLf & _
                        " 'A')"

                    Dim Command1 As OleDbCommand
                    Command1 = New OleDbCommand(strSQLstring, connectOR)
                    Try
                        rowsaffected = Command1.ExecuteNonQuery
                        If rowsaffected = 0 Then
                            'objStreamWriter.WriteLine("**")
                            'objStreamWriter.WriteLine("     Error - 0 rows affected users tbl for record " & I)
                            'objStreamWriter.WriteLine("**")
                            m_logger.WriteErrorLog(rtn & " :: Error - 0 rows affected users tbl for record " & I)
                            bolerror = True
                        End If
                    Catch OleDBExp As OleDbException
                        'objStreamWriter.WriteLine("**")
                        'objStreamWriter.WriteLine("     Error - updating users tbl for record " & I)
                        'objStreamWriter.WriteLine("**")
                        m_logger.WriteErrorLog(rtn & " :: Error - updating users tbl for record " & I)
                        bolerror = True
                    End Try
                    Command1.Dispose()
                    strSQLstring = "INSERT INTO PS_ISA_ISOL_PW" & vbCrLf & _
                                " (ISA_USER_ID, ISA_EMPLOYEE_ID," & vbCrLf & _
                                " ISA_ISOL_PW1, ISA_ISOL_PW_DATE1," & vbCrLf & _
                                " ISA_ISOL_PW2, ISA_ISOL_PW_DATE2," & vbCrLf & _
                                " ISA_ISOL_PW3, ISA_ISOL_PW_DATE3)" & vbCrLf & _
                                " VALUES (SEQ_ISA_USER_ID_PK.CURRVAL," & vbCrLf & _
                                " '" & strEmpID & "'," & vbCrLf & _
                                " 'nSLnA5YkVzKuLhkvojAo/g=='," & vbCrLf & _
                                " TO_DATE('" & dteNow & "', 'MM/DD/YYYY')," & vbCrLf & _
                                " ' ', '', ' ','')"
                    Command1 = New OleDbCommand(strSQLstring, connectOR)
                    Try
                        rowsaffected = Command1.ExecuteNonQuery
                        If rowsaffected = 0 Then
                            'objStreamWriter.WriteLine("**")
                            'objStreamWriter.WriteLine("     Error - 0 rows affected PS_ISA_ISOL_PW for record " & I)
                            'objStreamWriter.WriteLine("**")
                            m_logger.WriteErrorLog(rtn & " :: Error - 0 rows affected PS_ISA_ISOL_PW for record " & I)
                            bolerror = True
                        End If
                    Catch OleDBExp As OleDbException
                        'objStreamWriter.WriteLine("**")
                        'objStreamWriter.WriteLine("     Error - updating PS_ISA_ISOL_PW for record " & I)
                        'objStreamWriter.WriteLine("**")
                        m_logger.WriteErrorLog(rtn & " :: Error - updating PS_ISA_ISOL_PW for record " & I)
                        bolerror = True
                    End Try
                    Command1.Dispose()

                    'strSQLstring = "INSERT INTO usernames" & vbCrLf & _
                    '            " (userID, password, lastName, firstName," & vbCrLf & _
                    '            " emailAddress, crUser, crDate)" & vbCrLf & _
                    '            " VALUES ('" & strEmpID & "'," & vbCrLf & _
                    '            " 'temppw'," & vbCrLf & _
                    '            " '" & strLastName & "'," & vbCrLf & _
                    '            " '" & strFirstName & "'," & vbCrLf & _
                    '            " '" & Trim(strEmail) & "'," & vbCrLf & _
                    '            " 'INTFCVB'," & vbCrLf & _
                    '            " '" & Now() & "')"
                    'Dim Command2 As SqlClient.SqlCommand
                    'Command2 = New SqlClient.SqlCommand(strSQLstring, connectSQL)
                    'Try
                    '    rowsaffected = rowsaffected = Command2.ExecuteNonQuery()
                    '    If rowsaffected = 0 Then
                    '        'objStreamWriter.WriteLine("**")
                    '        'objStreamWriter.WriteLine("     Warning - 0 rows affected users tbl for record " & I)
                    '        'objStreamWriter.WriteLine("**")
                    '        m_logger.WriteWarningLog(rtn & " :: Warning - 0 rows affected users tbl for record " & I)
                    '        bolerror = True
                    '    End If
                    'Catch sqlExp As SqlClient.SqlException
                    '    'objStreamWriter.WriteLine("**")
                    '    'objStreamWriter.WriteLine("     Warning - updating users tbl for record " & I)
                    '    'objStreamWriter.WriteLine("**")
                    '    m_logger.WriteErrorLog(rtn & " :: Error - updating users tbl for record " & I)
                    '    bolerror = True
                    'End Try
                    'Command2.Dispose()

                    'Dim objEnterprise As New clsEnterprise(strBusinessUnit, connectOR)
                    'Dim strProdView = objEnterprise.ProdView
                    'Dim intGroupId As Integer = getGroupID(strProdView)
                    'If intGroupId > 0 Then
                    '    strSQLstring = "INSERT INTO GroupUsers" & vbCrLf & _
                    '       " (userID, groupID, crUser, crDate, updUser, updDate)" & vbCrLf & _
                    '       " VALUES ('" & strEmpID & "'," & vbCrLf & _
                    '       " " & intGroupId & "," & vbCrLf & _
                    '       " 'INTFCVB'," & vbCrLf & _
                    '       " '" & Now() & "'," & vbCrLf & _
                    '       " 'INTFCVB'," & vbCrLf & _
                    '       " '" & Now() & "')"
                    '    Command2 = New SqlClient.SqlCommand(strSQLstring, connectSQL)
                    '    Try
                    '        rowsaffected = rowsaffected = Command2.ExecuteNonQuery()
                    '        If rowsaffected = 0 Then
                    '            'objStreamWriter.WriteLine("**")
                    '            'objStreamWriter.WriteLine("     Warning - 0 rows affected group users for record " & I)
                    '            'objStreamWriter.WriteLine("**")
                    '            m_logger.WriteWarningLog(rtn & " :: Warning - 0 rows affected group users for record " & I)
                    '            bolerror = True
                    '        End If
                    '    Catch sqlExp As SqlClient.SqlException
                    '        'objStreamWriter.WriteLine("**")
                    '        'objStreamWriter.WriteLine("     Warning - updating group users for record " & I)
                    '        'objStreamWriter.WriteLine("**")
                    '        m_logger.WriteWarningLog(rtn & " :: Warning - updating group users for record " & I)
                    '        bolerror = True
                    '    End Try
                    '    Command2.Dispose()
                    'End If
                End If
            End If
            If bolerror = True Then
                bolErrorOccured = True
            End If
        Next
        connectOR.Close()
        connectSQL.Close()
        If bolErrorOccured Then
            Return "EmpID Update Error"
        Else
            Return ""
        End If


        'Next

    End Function

    Private Function UpdIntfcTable(ByVal strPath As String) As String

        Dim reader As XmlTextReader
        Try
            reader = New XmlTextReader(strPath)
        Catch ex As Exception
            Console.WriteLine("")
            Console.WriteLine("***error - " & ex.ToString)
            Console.WriteLine("")
            Return ex.ToString
        End Try
        reader.Close()

        'Dim X As Integer
        'Dim H As Integer
        'Dim L As Integer
        'Dim strIntfcError As String
        'Dim strPriority As String

        Dim sr As System.IO.StreamReader
        Dim XMLContent As String
        Dim xmlRequest As New XmlDocument
        sr = New System.IO.StreamReader(strPath)
        XMLContent = sr.ReadToEnd()
        sr.Close()

        'XMLContent = Replace(XMLContent, "&", "&amp;")
        'XMLContent = Replace(XMLContent, "'", "&#39;")

        xmlRequest.LoadXml(XMLContent)

        Dim jobNode As XmlNode

        'For X = 0 To xmlRequest.ChildNodes.Count - 1
        jobNode = xmlRequest.ChildNodes(1)
        Dim dsRows As New DataSet
        dsRows.ReadXml(New XmlNodeReader(jobNode))
        If dsRows.Tables(0).Rows.Count > 1 Then
            Return "XML error - more than one header"
        End If



        '   - erwin
        Dim err As String = ""
        Dim msgs As String = ""
        Dim checkOut As New orderProcessor

        checkOut.oleConnectionString = connectOR.ConnectionString
        checkOut.sqlConnectionString = connectSQL.ConnectionString
        checkOut.Logger = m_logger
        checkOut.MyLogger = myLoggr1

        checkOut.ReadOrderRequest(xmlRequest, connectOR.ConnectionString)

        If checkOut.ValidateOrderRequest Then
            checkOut.SaveOrderRequest()
        End If

        If checkOut.ProcessMessages.Count > 0 Then
            For Each msg As processMsg In checkOut.ProcessMessages
                msgs &= msg.Level.ToString & "::" & msg.Message & vbCrLf
                If msg.Level = TraceLevel.Error Then
                    err &= msg.Level.ToString & "::" & msg.Message & vbCrLf
                End If
            Next
        End If

        checkOut.Dispose()
        checkOut = Nothing

        'If err.Length > 0 Then
        Return err
        'End If

    End Function


    Function getColumnValue(ByVal row As DataRow, ByVal strColumn As String, ByVal intMaxLength As Integer) As String

        Dim rtn As String = "intfcUpdate.getColumnValue"
        Try
            getColumnValue = row.Item(strColumn)
        Catch ex As Exception
            getColumnValue = " "
        End Try
        If getColumnValue = "" Then
            getColumnValue = " "
        End If
        getColumnValue = Replace(getColumnValue, "'", "''")
        getColumnValue = Replace(getColumnValue, """", "&#34;")
        If intMaxLength > 0 Then
            If getColumnValue.Length > intMaxLength Then
                getColumnValue = getColumnValue.Substring(0, intMaxLength)
                Try
                    'objStreamWriter.WriteLine("     Warning - " & strColumn & " was truncated from " & row.Item(strColumn) & " to " & getColumnValue & " for transaction " & " " & row.Item("LINE_NBR"))
                    m_logger.WriteWarningLog(rtn & " :: Warning - " & strColumn & " was truncated from " & row.Item(strColumn) & " to " & getColumnValue & " for transaction " & " " & row.Item("LINE_NBR"))
                Catch ex As Exception
                    'objStreamWriter.WriteLine("     Warning - " & strColumn & " was truncated from " & row.Item(strColumn) & " to " & getColumnValue & " for transaction UNKNOWN LINE")
                    m_logger.WriteWarningLog(rtn & " :: Warning - " & strColumn & " was truncated from " & row.Item(strColumn) & " to " & getColumnValue & " for transaction UNKNOWN LINE")
                End Try

            End If
        End If

    End Function

    Private Function GetSitePrefix1(ByVal sBU As String) As String

        Dim rtn As String = "intfcUpdate.GetSitePrefix1"
        Dim strSitePrefix As String = ""
        Dim strSQLString As String = "SELECT ISA_SITE_CODE" & vbCrLf & _
            " FROM PS_BUS_UNIT_TBL_OM" & vbCrLf & _
            " WHERE BUSINESS_UNIT = '" & sBU & "'" & vbCrLf
        Try
            Dim dtrPrefixReader As OleDbDataReader = ORDBAccess.GetReader(strSQLString, connectOR)
            dtrPrefixReader.Read()
            strSitePrefix = dtrPrefixReader("ISA_SITE_CODE")
            dtrPrefixReader.Close()

            Return strSitePrefix
        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            'objStreamWriter.WriteLine("     Warning - error reading PS_BUS_UNIT_TBL_OM table for " & sBU)
            m_logger.WriteErrorLog(rtn & " :: ERROR - error reading PS_BUS_UNIT_TBL_OM table for " & sBU)
        End Try
    End Function

    Private Sub SendBuyerEmail()

        Dim rtn As String = "intfcUpdate.SendBuyerEmail"

        Console.WriteLine("Start Sending Buyer Email(s)")
        Console.WriteLine("")

        m_logger.WriteInformationLog(rtn & " :: Start Sending Buyer Email(s) ")
        Dim strSQLstring As String = ""
        strSQLstring = "SELECT A.BILL_TO_CUST_ID AS CUST_ID, A.ORDER_NO, B.OPRID_ENTERED_BY," & vbCrLf & _
                    " TO_CHAR(A.ADD_DTTM,'YYYY-MM-DD-HH24.MI.SS') as HdrAddDate," & vbCrLf & _
                    " B.ISA_INTFC_LN AS LINE_NBR," & vbCrLf & _
                    " B.QTY_REQUESTED AS QTY_REQ, B.INV_ITEM_ID, B.UNIT_OF_MEASURE," & vbCrLf & _
                    " B.MFG_ID, B.ISA_MFG_FREEFORM, B.ISA_SELL_PRICE AS NET_UNIT_PRICE," & vbCrLf & _
                    " B.DESCR254, B.MFG_ITM_ID, B.ISA_EMPLOYEE_ID AS EMPLID," & vbCrLf & _
                    " B.ISA_WORK_ORDER_NO," & vbCrLf & _
                    " TO_CHAR(B.ADD_DTTM,'YYYY-MM-DD-HH24.MI.SS') as LnAddDate," & vbCrLf & _
                    " ' ' AS ISA_CUST_NOTES, B.ISA_PRIORITY_FLAG AS PROJECT_ID," & vbCrLf & _
                    " TO_CHAR(B.ISA_REQUIRED_BY_DT,'YYYY-MM-DD') as ReqByDate" & vbCrLf & _
                    " FROM PS_ISA_ORD_INTF_HD A, PS_ISA_ORD_INTF_LN B" & vbCrLf & _
                    " WHERE A.BUSINESS_UNIT_OM = B.BUSINESS_UNIT_OM" & vbCrLf & _
                    " AND A.ORDER_NO = B.ORDER_NO AND A.BUSINESS_UNIT_OM = 'I0256' " & vbCrLf & _
                    " AND B.ISA_LINE_STATUS = 'NEW' " & vbCrLf & _
                                    "   AND NOT EXISTS (" & vbCrLf & _
                                    "                   SELECT 'X'" & vbCrLf & _
                                    "                   FROM PS_ISA_ORDSTAT_EML G" & vbCrLf & _
                                    "                   WHERE A.BUSINESS_UNIT_OM = G.BUSINESS_UNIT_OM" & vbCrLf & _
                                    "                     AND A.ORDER_NO = G.ORDER_NO" & vbCrLf & _
                                    "                     AND G.ISA_LINE_STATUS = 'NEW'" & vbCrLf & _
                                    "                  )" & vbCrLf & _
                    " ORDER BY A.ORDER_NO, B.ISA_INTFC_LN"

        Dim ds As DataSet = ORDBAccess.GetAdapter(strSQLstring, connectOR)

        If Not ds Is Nothing Then
            If ds.Tables(0).Rows.Count = 0 Then

                m_logger.WriteInformationLog(rtn & " :: No Buyer Email(s) info found")
                Exit Sub
            End If
        Else
            m_logger.WriteInformationLog(rtn & " :: No Buyer Email(s)  info found")
            Exit Sub
        End If

        Dim I As Integer
        Dim strPrevOrderNo As String = ds.Tables(0).Rows(0).Item("ORDER_NO")
        Dim dsOrder As DataSet
        dsOrder = ds.Copy
        dsOrder.Tables(0).Clear()
        Dim iSent As Integer = 0
        For I = 0 To ds.Tables(0).Rows.Count - 1
            If ds.Tables(0).Rows(I).Item("ORDER_NO") <> strPrevOrderNo Then
                buildBuyerConfirmation(dsOrder)
                dsOrder.Tables(0).Clear()
                iSent = iSent + 1
            End If
            dsOrder.Tables(0).ImportRow(ds.Tables(0).Rows(I))
            strPrevOrderNo = ds.Tables(0).Rows(I).Item("ORDER_NO")
            If I = ds.Tables(0).Rows.Count - 1 Then
                buildBuyerConfirmation(dsOrder)
                iSent = iSent + 1
            End If
        Next

        m_logger.WriteInformationLog(rtn & " :: " & iSent.ToString() & " Buyer Email(s) were sent")

    End Sub

    Private Sub SendEmail(Optional ByVal bIsSendOut As Boolean = False)

        Dim rtn As String = "intfcUpdate.SendEmail"
        Dim email As New System.Web.Mail.MailMessage
        'Dim myEmail As New System.Net.Mail.MailMessage

        'The email address of the sender
        email.From = "TechSupport@sdi.com"
        If (CStr(My.Settings(propertyName:="onErrorEmail_From")) <> "") Then
            email.From = CStr(My.Settings(propertyName:="onErrorEmail_From")).Trim
        End If
        'myEmail.From = New System.Net.Mail.MailAddress(email.From)

        'The email address of the recipient. 
        email.To = "vitaly.rovensky@sdi.com"
        If bIsSendOut Then
            If (CStr(My.Settings(propertyName:="onErrorEmail_To")) <> "") Then
                email.To = CStr(My.Settings(propertyName:="onErrorEmail_To")).Trim
            End If
        End If

        'myEmail.To.Add(email.To)

        email.Cc = " "
        If (CStr(My.Settings(propertyName:="onErrorEmail_CC")) <> "") Then
            email.Cc = CStr(My.Settings(propertyName:="onErrorEmail_CC")).Trim
        End If

        Try
            If email.Cc Is Nothing Then
                email.Cc = " "
            Else
                If Trim(email.Cc) = "" Then
                    email.Cc = " "
                End If
            End If
        Catch ex As Exception
            email.Cc = " "
        End Try

        'If email.Cc Is Nothing Then
        '    email.Cc = ""
        'Else
        '    myEmail.CC.Add(email.Cc)
        'End If

        If (CStr(My.Settings(propertyName:="onErrorEmail_BCC")) <> "") Then
            email.Bcc = CStr(My.Settings(propertyName:="onErrorEmail_BCC")).Trim
        End If

        'myEmail.Bcc.Add(email.Bcc)

        'The Priority attached and displayed for the email
        email.Priority = System.Web.Mail.MailPriority.High
        'myEmail.Priority = Mail.MailPriority.High

        email.BodyFormat = System.Web.Mail.MailFormat.Html
        'myEmail.IsBodyHtml = True

        email.Body = ""
        email.Body &= "<html><body>"
        email.Body &= "<center><span style='font-family:Arial;font-size:X-Large;width:256px;'>SDI, Inc</span></center><center><span >UNCC XML IN Error</span></center>&nbsp;&nbsp;"

        email.Body &= "<table><tr><td>UNCCXMLIn has completed with "
        If bolWarning = True Then
            email.Body &= "warnings,"
            email.Subject = "UNCC XML IN Warning"
        Else
            email.Body &= "errors;"
            email.Subject = "UNCC XML IN Error"
        End If

        'check is Production server
        If UCase(Right(connectOR.ConnectionString, 4)) = "PROD" Then
            'do nothing
        Else
            email.Subject = " (Test) " & email.Subject
            email.To = "webdev@sdi.com"
        End If

        'VR 12/18/2014 Adding file names and error descriptions in message body
        Dim sInfoErr As String = ""
        Try

            sInfoErr &= " XML file name(s) are below.</td></tr>"
            If Not m_arrXMLErrFiles Is Nothing Then
                If Trim(m_arrXMLErrFiles) <> "" Then
                    Dim arrErrFiles1() As String = Split(m_arrXMLErrFiles, ",")
                    Dim arrErrDescr2() As String = Split(m_arrErrorsList, ",")
                    If arrErrFiles1.Length > 0 Then
                        For i1 As Integer = 0 To arrErrFiles1.Length - 1
                            sInfoErr &= "<tr><td>" & arrErrFiles1(i1) & "</td><td>&nbsp;&nbsp" & arrErrDescr2(i1) & "</td></tr>"
                        Next
                    End If
                End If
            End If
            email.Body &= sInfoErr
        Catch ex As Exception

            email.Body &= " review log.</td></tr>"
        End Try

        email.Body &= "</table>"

        email.Body &= "&nbsp;<br>Sincerely,<br>&nbsp;<br>SDI Customer Care<br>&nbsp;<br></p></div><BR>"
        email.Body &= "<br />"

        Dim sApp As String = "" & _
                             System.Reflection.Assembly.GetExecutingAssembly.GetModules()(0).Name & _
                             " v" & System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString & _
                            ""
        Try
            email.Body &= "" & _
                          "<p style=""text-align:right;font-size:10px"">" & _
                          sApp & _
                          "</p>" & _
                          ""
        Catch ex As Exception
        End Try

        email.Body &= "<br><P><CENTER><SPAN style='FONT-SIZE: 12pt'><SPAN style='FONT-SIZE: 12pt'><FONT color=teal size=2>The information in this communication, including any attachments, is the property of SDI, Inc,&nbsp;</SPAN>is intended only for the addressee and may contain confidential, proprietary, and/or privileged material. Any review, retransmission, dissemination or other use of, or taking of any action in reliance upon, this information by persons or entities other than the intended recipient is prohibited. If you received this in error, please immediately contact the sender by replying to this email and delete the material from all computers.</FONT></SPAN></CENTER></P>"
        email.Body &= "</body></html>"

        'myEmail.Body = email.Body

        '' VR 12/18/2014 Commenting out code below - per Mike Randall: do not send this log file to customer because it is not
        ''               formatted for customer to clearly understand what is the problem with erred XML file
        '' VR 12/29/2014 Attaching Error specific log file
        'Try
        '    email.Attachments.Add(New System.Web.Mail.MailAttachment(filename:=sErrLogPath))
        'Catch ex As Exception
        'End Try

        'Dim int1 As Integer = 0
        'If Not (m_arrErrXMLs Is Nothing) Then
        '    If (m_arrErrXMLs.Count > 0) Then
        '        m_logger.WriteInformationLog(rtn & " :: erronuous xml file count = " & m_arrErrXMLs.Count.ToString)
        '        Try
        '            For int1 = 0 To (m_arrErrXMLs.Count - 1)
        '                If CStr(m_arrErrXMLs(int1)).Trim.Length > 0 Then
        '                    Dim myFileName1 As String = CStr(m_arrErrXMLs(int1))
        '                    email.Attachments.Add(New System.Web.Mail.MailAttachment(myFileName1))
        '                End If
        '            Next
        '        Catch ex As Exception
        '        End Try
        '    End If
        'End If

        'Try
        '    If Not m_arrXMLErrFiles Is Nothing Then
        '        If Trim(m_arrXMLErrFiles) <> "" Then
        '            Dim arrErrFiles() As String = Split(m_arrXMLErrFiles, ",")
        '            If arrErrFiles.Length > 0 Then
        '                m_logger.WriteInformationLog(rtn & " :: erroneous xml file count = " & arrErrFiles.Length.ToString)
        '                For int1 = 0 To arrErrFiles.Length - 1
        '                    Dim myFileName2 As String = "C:\INTFCXML\BadXML\" & arrErrFiles(int1)
        '                    email.Attachments.Add(New System.Web.Mail.MailAttachment(myFileName2))
        '                Next
        '            End If
        '        End If
        '    End If
        'Catch ex As Exception

        'End Try

        Dim bSend As Boolean = False
        Try
            
            SendEmail1(email)
            bSend = True
        Catch ex As Exception

        End Try

        If Not bSend Then
            m_logger.WriteErrorLog(rtn & " :: Error - the email was not sent.")
        End If
    End Sub

    Private Sub SendEmail1(ByVal mailer As System.Web.Mail.MailMessage)
        Dim bSent As Boolean = False
        Try
            
            SendLogger(mailer.Subject, mailer.Body, "INTFCXMLUPDATE", "Mail", mailer.To, mailer.Cc, mailer.Bcc)

            bSent = True
        Catch ex As Exception
            bSent = False
        End Try

    End Sub

    Public Sub SendLogger(ByVal subject As String, ByVal body As String, ByVal messageType As String, ByVal MailType As String, ByVal EmailTo As String, ByVal EmailCc As String, ByVal EmailBcc As String)
        Try
            Dim SDIEmailService As SDiEmailUtilityService.EmailServices = New SDiEmailUtilityService.EmailServices()
            Dim MailAttachmentName As String()
            Dim MailAttachmentbytes As New List(Of Byte())()

            SDIEmailService.EmailUtilityServices(MailType, "SDIExchange@sdi.com", EmailTo, subject, EmailCc, EmailBcc, body, messageType, MailAttachmentName, MailAttachmentbytes.ToArray())

        Catch ex As Exception

        End Try
    End Sub

    Private Function updateIntfcHeader(ByVal strBU As String, ByVal strIntfcreqid As String, ByVal strStatus As String) As String

        'Insert into the INTFC PRH and PRL tables

        Dim strSQLstring As String = ""
        Dim rowsaffected As Integer = 0

        strSQLstring = "UPDATE PS_ISA_ORD_INTF_HD" & vbCrLf & _
            " SET ORDER_STATUS = '" & strStatus & "'" & vbCrLf & _
            " WHERE BUSINESS_UNIT_OM = '" & strBU & "'" & vbCrLf & _
            " AND ORDER_NO = '" & strIntfcreqid & "'"

        Try
            rowsaffected = ORDBAccess.ExecNonQuery(strSQLstring, connectOR)
            If rowsaffected = 0 Then
                Return "Error updating PS_ISA_ORD_INTFC_H status for order " & strIntfcreqid
            End If
        Catch OleDBExp As OleDbException
            Console.WriteLine("")
            Console.WriteLine("***OLEDB error - " & OleDBExp.ToString)
            Console.WriteLine("")
            connectOR.Close()
            Return "Insert error for Status ORDERNO  " & strIntfcreqid & " " & OleDBExp.ToString
        End Try

        Return rowsaffected.ToString()

    End Function

    Private Sub buildBuyerConfirmation(ByVal dsOrder As DataSet)
        Dim strSQLString As String
        Dim strBU As String

        strSQLString = "SELECT ISA_BUSINESS_UNIT" & vbCrLf & _
            " FROM PS_ISA_ENTERPRISE " & vbCrLf & _
            " WHERE CUST_ID = '" & dsOrder.Tables(0).Rows(0).Item("CUST_ID") & "'" & vbCrLf
        Dim dtrEntReader As OleDbDataReader = ORDBAccess.GetReader(strSQLString, connectOR)

        If dtrEntReader.HasRows() = False Then
            dtrEntReader.Close()
            Exit Sub
        Else
            dtrEntReader.Read()
            strBU = dtrEntReader.Item("ISA_BUSINESS_UNIT")
            dtrEntReader.Close()
        End If

        Dim I As Integer
        Dim X As Integer
        Dim strbodyhead As String = ""
        Dim strbodydetl As String = ""
        Dim strItemtype As String = ""
        Dim intGridloop As Integer
        Dim decOrderTot As Decimal
        Dim dr As DataRow
        Dim SBstk As New StringBuilder
        Dim SWstk As New StringWriter(SBstk)
        Dim htmlTWstk As New HtmlTextWriter(SWstk)
        Dim dataGridHTML As String
        Dim SBnstk As New StringBuilder
        Dim SWnstk As New StringWriter(SBnstk)
        Dim htmlTWnstk As New HtmlTextWriter(SWnstk)
        Dim SBall As New StringBuilder
        Dim SWall As New StringWriter(SBall)
        Dim htmlTWall As New HtmlTextWriter(SWall)
        Dim dtgcart As WebControls.DataGrid
        Dim dstcart1 As New DataTable

        Dim strDescr1 As String = ""
        Dim strDescr2 As String = ""
        Dim strDescr3 As String = ""
        Dim strDescr4 As String = ""
        Dim strCustName As String = ""
        Dim strCustEmail As String = ""
        Dim strCustPhone As String = ""
        Dim strrefno As String = " "
        connectOR.Open()
        Dim objUserTbl As clsUserTbl
        objUserTbl = New clsUserTbl(dsOrder.Tables(0).Rows(0).Item("EMPLID"), strBU, connectOR)
        strCustName = objUserTbl.EmployeeName
        strCustEmail = objUserTbl.EmployeeEmail
        strCustPhone = objUserTbl.PhoneNum
        connectOR.Close()
        '' see if there is a order_number (reference number) in the quote table.... if so it is a punchin record
        'connectOR.Open()
        'Dim objRefNo As clsQuote
        'Try
        '    objRefNo = New clsQuote(dsOrder.Tables(0).Rows(0).Item("ORDER_NO"), dsOrder.Tables(0).Rows(0).Item("LINE_NO"), connectOR)
        '    strrefno = objRefNo.ORDER_NO.ToString
        '    If IsDBNull(strrefno) Then
        '        strrefno = " "
        '    End If
        'Catch ex As Exception
        '    strrefno = " "
        '    connectOR.Close()
        'End Try



        Dim Mailer As System.Web.Mail.MailMessage = New System.Web.Mail.MailMessage
        Dim strCustomerName As String
        connectOR.Open()
        Dim objEnterprise As New clsEnterprise(strBU, connectOR)

        strCustomerName = objEnterprise.CustomerName
        connectOR.Close()

        Mailer.From = "SDIExchange@SDI.com"
        Mailer.cc = ""
        Mailer.bcc = ""
        strbodyhead = strbodyhead & "<center><span style='font-family:Arial;font-size:X-Large;width:256px;'>SDI Marketplace</span><center>" & vbCrLf
        'if there is an order_no in the quote table it is a punchin record
        If Not Trim(strrefno) = "" Then
            strbodyhead = strbodyhead + "<br><B><left><span style='font-family:Arial;color: red;font-size:Medium;'> ***NOTE*** THIS IS A PUNCHOUT ORDER!!! </span></B></left>"
        End If
        strbodyhead = strbodyhead & "&nbsp;" & vbCrLf

        strbodydetl = "&nbsp;" & vbCrLf
        strbodydetl = strbodydetl & "<HR width='100%' SIZE='1'>" & vbCrLf
        strbodydetl = strbodydetl & "&nbsp;" & vbCrLf
        strbodydetl = strbodydetl & "<TABLE cellSpacing='1' cellPadding='1' width='100%' border='0'>" & vbCrLf
        strbodydetl = strbodydetl & "<TR>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "<span >Item request from </span>&nbsp;"
        strbodydetl = strbodydetl & strCustomerName & "</td>" & vbCrLf
        strbodydetl = strbodydetl & "<TD style='COLOR: red'>" & vbCrLf
        strbodydetl = strbodydetl & "<span>&nbsp;</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<TD colspan='2'>" & vbCrLf
        strbodydetl = strbodydetl & "&nbsp;</td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        Dim sdiOrderNo As String = dsOrder.Tables(0).Rows(0).Item("ORDER_NO")
        Dim unccOrderNo As String = orderNoMapper.changeToUNCCOrderNo(sdiOrderNo)
        strbodydetl = strbodydetl & "SDI Requisition Number:<span 'width:128px;'>&nbsp;" & unccOrderNo & _
                                    CStr(IIf(unccOrderNo = sdiOrderNo, " *", " (" & sdiOrderNo & ")")) & _
                                    "</span></td>"
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        'strbodydetl = strbodydetl & "Chg. Emp. ID:<span>&nbsp;" & dstcart2.Rows(0).Item(0) & "</span></td></tr>"

        strbodydetl = strbodydetl & "<span>&nbsp;</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "Employee Name:<span>&nbsp;" & strCustName & "</span></td>"
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        'strbodydetl = strbodydetl & "Chg. Code:<span>&nbsp;" & dstcart2.Rows(0).Item(1) & "</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<span>&nbsp;</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "Employee Email:<span>&nbsp;" & strCustEmail & "</span></td>"
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "Work Order #:<span>&nbsp;" & dsOrder.Tables(0).Rows(0).Item("ISA_WORK_ORDER_NO") & "</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "Employee Phone#:<span>&nbsp;" & strCustPhone & "</span></td>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        'strbodydetl = strbodydetl & "Machine Number:<span>&nbsp;" & dstcart2.Rows(0).Item(3) & "</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<span>&nbsp;</span></td></tr>" & vbCrLf
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "Request by Date:<span>&nbsp;" & dsOrder.Tables(0).Rows(0).Item("ReqByDATE") & "</span></td>"
        strbodydetl = strbodydetl & "<TD>" & vbCrLf
        strbodydetl = strbodydetl & "Submit Date:<span>&nbsp;" & Now() & "</span></td></tr>"
        strbodydetl = strbodydetl & "<TD colspan='2'>" & vbCrLf
        'strbodydetl = strbodydetl & "Notes:<br>"
        'strbodydetl = strbodydetl & "<textarea readonly='readonly' style='width:100%;'></textarea></td>" & vbCrLf
        strbodydetl = strbodydetl & "</tr></table>" & vbCrLf
        strbodydetl = strbodydetl & "&nbsp;<br>" & vbCrLf

        Dim strOROItemID As String

        ' for now - no stock email

        Dim dstcartSTK As New DataTable
        dstcartSTK = dsOrder.Tables(0).Copy

        intGridloop = dstcartSTK.Rows.Count
        X = 0
        decOrderTot = 0

        For I = 0 To intGridloop - 1
            strOROItemID = dstcartSTK.Rows(I).Item("INV_ITEM_ID")

            If dstcartSTK.Rows(I).Item("INV_ITEM_ID") = " " Then
                dstcartSTK.Rows(I).Delete()
            Else
                Try
                    Dim objInvItemid As New clsInvItemID(strOROItemID, connectOR)
                    
                    If Trim(objInvItemid.DESCR254) <> "" Then
                        dstcartSTK.Rows(I).Item("DESCR254") = objInvItemid.DESCR254
                    End If

                    If Trim(objInvItemid.MFGID) <> "" Then
                        dstcartSTK.Rows(I).Item("MFG_ID") = objInvItemid.MFGID
                    End If

                    If Trim(objInvItemid.MFGITMID) <> "" Then
                        dstcartSTK.Rows(I).Item("MFG_ITM_ID") = objInvItemid.MFGITMID
                    End If

                    If Trim(objInvItemid.DESCR) <> "" Then
                        dstcartSTK.Rows(I).Item("ISA_MFG_FREEFORM") = objInvItemid.DESCR
                    End If
                    connectOR.Close()
                Catch ex As Exception

                End Try

                If IsDBNull(dstcartSTK.Rows(I).Item("DESCR254")) Then
                    dstcartSTK.Rows(I).Item("DESCR254") = " "
                End If
                If IsDBNull(dstcartSTK.Rows(I).Item("MFG_ID")) Then
                    dstcartSTK.Rows(I).Item("MFG_ID") = " "
                End If
                If IsDBNull(dstcartSTK.Rows(I).Item("MFG_ITM_ID")) Then
                    dstcartSTK.Rows(I).Item("MFG_ITM_ID") = " "
                End If
                If IsDBNull(dstcartSTK.Rows(I).Item("ISA_MFG_FREEFORM")) Then
                    dstcartSTK.Rows(I).Item("ISA_MFG_FREEFORM") = " "
                End If

                strDescr1 = UCase(Left(dstcartSTK.Rows(I).Item("DESCR254"), 60))
                strDescr2 = UCase(Mid(dstcartSTK.Rows(I).Item("DESCR254"), 69, 68))
                strDescr3 = UCase(Mid(dstcartSTK.Rows(I).Item("DESCR254"), 137, 68))
                strDescr4 = UCase(Mid(dstcartSTK.Rows(I).Item("DESCR254"), 205, 58))

                X = X + 1
            End If

        Next

        dstcartSTK.AcceptChanges()
        If dstcartSTK.Rows.Count > 0 Then
            dtgcart = New WebControls.DataGrid

            dstcartSTK.Columns(4).ColumnName = "Line Nbr"
            dstcartSTK.Columns(5).ColumnName = "QTY"
            dstcartSTK.Columns(6).ColumnName = "Item ID"
            dstcartSTK.Columns(7).ColumnName = "UOM"
            dstcartSTK.Columns(8).ColumnName = "MFG ID"
            dstcartSTK.Columns(9).ColumnName = "MFG"
            dstcartSTK.Columns(11).ColumnName = "DESCR"
            dstcartSTK.Columns(12).ColumnName = "MFG Item ID"
            dstcartSTK.Columns(13).ColumnName = "Employee"
            dstcartSTK.Columns(15).ColumnName = "Add Date"
            'dstcartSTK.Columns(16).ColumnName = "Notes"

            dstcartSTK.Columns.Remove("CUST_ID")
            dstcartSTK.Columns.Remove("ORDER_NO")
            dstcartSTK.Columns.Remove("OPRID_ENTERED_BY")
            dstcartSTK.Columns.Remove("HDRADDDATE")
            dstcartSTK.Columns.Remove("NET_UNIT_PRICE")
            dstcartSTK.Columns.Remove("ISA_WORK_ORDER_NO")
            dstcartSTK.Columns.Remove("ISA_CUST_NOTES")

            dstcartSTK.AcceptChanges()
            dtgcart.DataSource = dstcartSTK
            dtgcart.DataBind()
            dtgcart.CellPadding = 3
            'dtgcart.Width.Percentage(90)

            dtgcart.RenderControl(htmlTWstk)

            dataGridHTML = SBstk.ToString()

            strItemtype = "<center><span >SDiExchange - Material Request - Stock</span></center>"
            Mailer.body = strbodyhead & strItemtype & strbodydetl
            Mailer.body = Mailer.body & "<TABLE cellSpacing='1' cellPadding='1' width='100%' border='0'>" & vbCrLf
            Mailer.body = Mailer.body + "<TR><TD Class='DetailRow' width='100%'>" & dataGridHTML & "</TD></TR>"
            Mailer.body = Mailer.body + "<TR><TD Class='DetailRow'>&nbsp;</TD></TR>"
            'Mailer.body = Mailer.body + "<TR><TD Class='DetailRow' align='right'>Order Total&nbsp;&nbsp;&nbsp;" & decOrderTot & "&nbsp;&nbsp;&nbsp;</TD></TR>"
            Mailer.body = Mailer.body & "</TABLE>" & vbCrLf

            Mailer.To = objEnterprise.STKREQEmail
            Mailer.Subject = " IntfcXMLUpdate - Material Request - Stock"

            'check is development databases
            If connectOR.DataSource.ToUpper = "RPTG" Or _
            connectOR.DataSource.ToUpper = "DEVL" Or _
            connectOR.DataSource.ToUpper = "STAR" Or _
            connectOR.DataSource.ToUpper = "PLGR" Then

                Mailer.To = "webdev@sdi.com"
                Mailer.Subject = " <<TEST>> IntfcXMLUpdate - Material Request - Stock"

            End If

            Mailer.BodyFormat = System.Web.Mail.MailFormat.Html

            SendEmail1(Mailer)

            SWstk.Close()
            htmlTWstk.Close()

        End If

        Dim dstCartNSTK As New DataTable
        dstCartNSTK = dsOrder.Tables(0).Copy

        intGridloop = dstCartNSTK.Rows.Count
        X = 0
        decOrderTot = 0
        For I = 0 To intGridloop - 1
            If dstCartNSTK.Rows(I).Item("INV_ITEM_ID") = " " Or _
                dstCartNSTK.Rows(I).Item("INV_ITEM_ID") = "" Then
                strOROItemID = dstCartNSTK.Rows(I).Item("INV_ITEM_ID")
            End If

            If dstCartNSTK.Rows(I).Item("INV_ITEM_ID") = " " Then
                X = X + 1
            Else
                dstCartNSTK.Rows(I).Delete()
            End If

        Next

        dstCartNSTK.AcceptChanges()
        If dstCartNSTK.Rows.Count > 0 Then

            dtgcart = New WebControls.DataGrid

            dstCartNSTK.Columns(4).ColumnName = "Line Nbr"
            dstCartNSTK.Columns(5).ColumnName = "QTY"
            dstCartNSTK.Columns(6).ColumnName = "Item ID"
            dstCartNSTK.Columns(7).ColumnName = "UOM"
            dstCartNSTK.Columns(8).ColumnName = "MFG ID"
            dstCartNSTK.Columns(9).ColumnName = "MFG"
            dstCartNSTK.Columns(11).ColumnName = "DESCR"
            dstCartNSTK.Columns(12).ColumnName = "MFG Item ID"
            dstCartNSTK.Columns(13).ColumnName = "Employee"
            dstCartNSTK.Columns(15).ColumnName = "Add Date"
            dstCartNSTK.Columns(16).ColumnName = "Notes"

            dstCartNSTK.Columns.Remove("CUST_ID")
            dstCartNSTK.Columns.Remove("ORDER_NO")
            dstCartNSTK.Columns.Remove("OPRID_ENTERED_BY")
            dstCartNSTK.Columns.Remove("HDRADDDATE")
            dstCartNSTK.Columns.Remove("NET_UNIT_PRICE")
            dstCartNSTK.Columns.Remove("ISA_WORK_ORDER_NO")

            dstCartNSTK.AcceptChanges()
            dtgcart.DataSource = dstCartNSTK
            dtgcart.DataBind()
            dtgcart.CellPadding = 3
            'dtgcart.Width.Percentage(90)

            dtgcart.RenderControl(htmlTWnstk)

            dataGridHTML = SBnstk.ToString()
            strItemtype = "<center><span >SDiExchange - Material Request - Non-Stock</span></center>"
            Mailer.body = strbodyhead & strItemtype & strbodydetl
            Mailer.body = Mailer.body & "<TABLE cellSpacing='1' cellPadding='1' width='100%' border='0'>" & vbCrLf
            Mailer.body = Mailer.body + "<TR><TD Class='DetailRow' width='100%'>" & dataGridHTML & "</TD></TR>"
            Mailer.body = Mailer.body + "<TR><TD Class='DetailRow'>&nbsp;</TD></TR>"
            'Mailer.body = Mailer.body + "<TR><TD Class='DetailRow' align='right'>Order Total&nbsp;&nbsp;&nbsp;" & decOrderTot & "&nbsp;&nbsp;&nbsp;</TD></TR>"
            Mailer.body = Mailer.body & "</TABLE>" & vbCrLf
            Mailer.To = objEnterprise.NONSKREQEmail

            If Trim(dsOrder.Tables(0).Rows(0).Item("PROJECT_ID")) > "" Then
                If Trim(dsOrder.Tables(0).Rows(0).Item("PROJECT_ID")) = "R" Then
                    Mailer.To = objEnterprise.SiteEmail
                End If
            End If

            Mailer.Subject = " IntfcXMLUpdate - Material Request - Non-Stock "

            'check is development databases
            If connectOR.DataSource.ToUpper = "RPTG" Or _
            connectOR.DataSource.ToUpper = "DEVL" Or _
            connectOR.DataSource.ToUpper = "STAR" Or _
            connectOR.DataSource.ToUpper = "PLGR" Then

                Mailer.To = "webdev@sdi.com"
                Mailer.Subject = " <<TEST>> IntfcXMLUpdate - Material Request - Non-Stock"

            End If

            Mailer.BodyFormat = System.Web.Mail.MailFormat.Html

            SendEmail1(Mailer)

            SWstk.Close()
            htmlTWstk.Close()

        End If
        connectOR.Close()
        Dim strIntfcError As String = updateIntfcHeader(strBU, dsOrder.Tables(0).Rows(0).Item("ORDER_NO"), "P")

        'save record in PS_ISA_ORDSTAT_EML
        flagOrderAsProcessed(dsOrder)

    End Sub

    Private Sub flagOrderAsProcessed(ByVal dsOrder As DataSet)

        Dim I As Integer = 0
        Dim strSQLstring As String = ""
        Dim rowsaffected As Integer = 0

        For I = 0 To dsOrder.Tables(0).Rows.Count - 1

            strSQLstring = "" & _
                                   "INSERT INTO PS_ISA_ORDSTAT_EML " & vbCrLf & _
                                   "(" & vbCrLf & _
                                   " BUSINESS_UNIT_OM" & vbCrLf & _
                                   ",ORDER_NO " & vbCrLf & _
                                   ",LINE_NBR " & vbCrLf & _
                                   ",ORDER_INT_LINE_NO " & vbCrLf & _
                                   ",DEMAND_LINE_NO " & vbCrLf & _
                                   ",RECEIVER_ID " & vbCrLf & _
                                   ",RECV_LN_NBR " & vbCrLf & _
                                   ",EMPLID " & vbCrLf & _
                                   ",ISA_LINE_STATUS " & vbCrLf & _
                                   ",EMAIL_DATETIME " & vbCrLf & _
                                   ")" & vbCrLf & _
                                   "VALUES " & vbCrLf & _
                                   "(" & vbCrLf & _
                                   " 'I0256' " & vbCrLf & _
                                   ",'" & dsOrder.Tables(0).Rows(I).Item("ORDER_NO") & "' " & vbCrLf & _
                                   ",'" & dsOrder.Tables(0).Rows(I).Item("LINE_NBR") & "' " & vbCrLf & _
                                   ",'" & dsOrder.Tables(0).Rows(I).Item("LINE_NBR") & "' " & vbCrLf & _
                                   ",'" & dsOrder.Tables(0).Rows(I).Item("LINE_NBR") & "' " & vbCrLf & _
                                   ",' ' " & vbCrLf & _
                                   ",'0' " & vbCrLf & _
                                   ",'" & dsOrder.Tables(0).Rows(I).Item("EMPLID") & "' " & vbCrLf & _
                                   ",'NEW' " & vbCrLf & _
                                   ",SYSDATE " & vbCrLf & _
                                   ")" & vbCrLf & _
                                   ""
            Try
                rowsaffected = ORDBAccess.ExecNonQuery(strSQLstring, connectOR)
            Catch ex As Exception

            End Try

        Next

    End Sub

    Function getOrderNo(ByVal strDirFileName As String) As String

        Dim sr As System.IO.StreamReader
        Dim XMLContent As String
        Dim strXMLError As String
        Dim bolError As Boolean

        Dim xmlRequest As New XmlDocument

        Dim root As XmlElement

        Try
            sr = New System.IO.StreamReader(strDirFileName)
            XMLContent = sr.ReadToEnd()
            sr.Close()

            Try
                xmlRequest.LoadXml(XMLContent)
            Catch ex As Exception
                Return ""
            End Try

            root = xmlRequest.DocumentElement

            If root.FirstChild Is Nothing Then
                Return ""
            ElseIf Not root.LastChild.Name.ToUpper = "PS_ISA_ORD_INTFC_L" Then
                Return ""
            End If
            Return root.ChildNodes(0).InnerText

        Catch ex As Exception
            Return ""
        End Try

    End Function

    'Private Function getGroupID(ByVal strprodview As String) As Integer

    '    Dim strSQLString As String
    '    strSQLString = "SELECT groupID" & vbCrLf & _
    '            " FROM GroupCatalogs" & vbCrLf & _
    '            " WHERE (productviewID = '" & strprodview & "')"

    '    Dim command1 As SqlClient.SqlCommand
    '    command1 = New SqlClient.SqlCommand(strSQLString, connectSQL)
    '    Dim strGroupid As String = command1.ExecuteScalar
    '    If Trim(strGroupid) = "" Then
    '        getGroupID = 0
    '    Else
    '        getGroupID = Convert.ToInt32(strGroupid)
    '    End If

    'End Function

    Private Function checkPriority(ByVal strEmpID As String) As String
        'pfd
        Dim strXMLDir As String
        If File.Exists("PriorityCust.xml") Then
            strXMLDir = "PriorityCust.xml"
        ElseIf File.Exists("..\PriorityCust.xml") Then
            strXMLDir = "..\PriorityCust.xml"
        Else
            Return " "
        End If

        Dim reader As XmlTextReader = Nothing
        ' Load the XmlTextReader from the stream
        reader = New XmlTextReader(strXMLDir)
        Try
            While reader.Read()
                If reader.NodeType = XmlNodeType.Element Then
                    If reader.Name = "EMPID" Then
                        reader.Read()
                        If reader.Value = strEmpID Then
                            reader.Close()
                            Return "Y"
                        Else
                            Return " "
                        End If
                    End If
                End If
            End While
            reader.Close()
        Catch ex As Exception
            reader.Close()

        End Try
        reader.Close()
        Return " "
    End Function

    'Private Sub checkLineStatus()

    '    Dim strSQLstring As String
    '    Dim strIntfcError As String
    '    Dim I As Integer

    '    strSQLstring = "SELECT A.ORDER_NO" & vbCrLf & _
    '                " FROM PS_ISA_ORD_INTF_HD A, PS_ISA_ORD_INTF_LN B" & vbCrLf & _
    '                " WHERE A.BUSINESS_UNIT_OM = 'I0256'" & vbCrLf & _
    '                " AND A.ORDER_STATUS = 'O'" & vbCrLf & _
    '                " AND A.ORDER_NO = B.ORDER_NO" & vbCrLf & _
    '                " AND B.ISA_LINE_STATUS = 'NEW'"
    '    Dim ds As DataSet = ORDBAccess.GetAdapter(strSQLstring, connectOR)
    '    If Not ds Is Nothing Then
    '        If ds.Tables(0).Rows.Count > 0 Then
    '            For I = 0 To ds.Tables(0).Rows.Count - 1
    '                strIntfcError = updateIntfcHeader("I0256", ds.Tables(0).Rows(I).Item("ORDER_NO"), "P")
    '            Next
    '        End If
    '    End If

    'End Sub

End Module
