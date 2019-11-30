Imports System.IO
Imports System.Text.RegularExpressions

Module Module1
    Public dictFileLists As Dictionary(Of String, String)
    Public OCScriptFolder As String
    Public ReportTable As System.Data.DataTable
    Public ScriptFunctionLists As Dictionary(Of String, String)

    Public DictFunctionMapping As Dictionary(Of String, String) = New Dictionary(Of String, String) _
        From {
    {"pGossipHello", "OnGossipHello"},
    {"pGossipSelect", "OnGossipSelect"},
    {"pGossipSelectWithCode", "OnGossipSelectCode"},
    {"pQuestAccept", "OnQuestAccept"},
    {"GoQuestComplete", "OnQuestComplete"},
    {"pAreaTrigger", "OnTrigger"},
    {"pGOHello", "OnGossipHello"},
    {"pChooseReward", "OnQuestReward"},
    {"GetInstanceData", "OnGetInstanceData"},
    {"pItemUse", "OnItemUse"},
    {"pEffectDummyCreature", "OnDummyEffect"},
    {"pQuestComplete", "OnQuestComplete"},
    {"pGOQuestAccept", "OnQuestAccept"},
    {"pGOSelect", "OnGossipSelect"}
    }

    Public Function GetFunctions(ByVal strAddSC As String)
        ''///REGEX METHOD
        'Dim expr As String = strstart & "S*" & strEnd
        'Dim mc As MatchCollection = Regex.Matches(strContent, expr)
        'Dim m As Match
        'For Each m In mc
        '    If Not ScriptFunctionLists.ContainsKey(m.Value) Then
        '        ScriptFunctionLists.Add(m.Value, m.Value)
        '    End If
        '    'Console.WriteLine(m)
        'Next m
        'simple method
        strAddSC = strAddSC.Replace(vbCr, vbCrLf)
        strAddSC = strAddSC.Replace(vbLf, vbCrLf)
        Dim arr
        arr = Split(strAddSC, vbCrLf)
        For Each subLine In arr
            Dim subFunc As String = SearchMidString(subLine, "->", "=")
            If subFunc.Trim = "" Then
                'Exit For
            Else
                If Not ScriptFunctionLists.ContainsKey(subFunc) Then
                    ScriptFunctionLists.Add(subFunc, subFunc)
                End If
            End If

        Next

    End Function

    Public Function GetFunctionsNames(strSCtemp As String) As Dictionary(Of String, String)
        strSCtemp = strSCtemp.Replace(vbCr, vbCrLf)
        strSCtemp = strSCtemp.Replace(vbLf, vbCrLf)
        Dim arr
        arr = Split(strSCtemp, vbCrLf)

        Dim dictTemp As Dictionary(Of String, String) = New Dictionary(Of String, String)
        For Each subLine In arr
            'newscript->pGossipHello = &GossipHello_custom_example;

            Dim subFunc As String = SearchMidString(subLine, "->", "=").Trim
            If subFunc.Trim = "" Then
                'Exit For
            Else
                Dim subFuncName As String
                If InStr(subLine, "&") <> 0 Then
                    subFuncName = SearchMidString(subLine, "&", ";").Trim.Trim
                    If subFunc.Trim <> "" Then
                        If Not dictTemp.ContainsKey(subFunc) Then
                            dictTemp.Add(subFunc, subFuncName)
                        End If
                    End If

                End If


            End If

        Next
        Return dictTemp

    End Function

    Public Function SearchMidString(ByVal s As String, ByVal s1 As String, ByVal s2 As String) As String
        '        Dim strAll As String = "<PName1>654321</PName1><PName2>123456</PName2>"

        '        Dim strResult As String = SearchMidString(strAll, "<PName2>", "</PName2>")

        'result   strResult = "123456"
        Try
            Dim n1 As Integer, n2 As Integer
            n1 = s.IndexOf(s1, 0) + s1.Length
            n2 = s.IndexOf(s2, n1)
            Return s.Substring(n1, n2 - n1)
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Function SearchMidStringByContain(ByVal sAll As String, ByVal sQur As String, ByVal s1 As String, ByVal s2 As String) As String
        '        Dim strAll As String = "12<-34567->8998<-76543->21"

        '        Dim strResult As String = SearchMidStringByContain(strAll, "65", "<-", "->")

        'result     strResult = "76543"

        If sAll.Contains(sQur) = False Then Return ""
        Try
            Dim nQur As Integer = sAll.IndexOf(sQur, 0)
            Dim sLeft As String = Microsoft.VisualBasic.Left(sAll, nQur)
            Dim sRight As String = Microsoft.VisualBasic.Right(sAll, sAll.Length - nQur - sQur.Length)
            Dim n1 As Integer, n2 As Integer
            n1 = sLeft.LastIndexOf(s1) + s1.Length
            n2 = sRight.IndexOf(s2)
            Return sAll.Substring(n1, sLeft.Length + sQur.Length + n2 - n1)
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Sub GetAllFiles(ByVal strDirect As String)  'get all files in a folder
        If Not (strDirect Is Nothing) Then
            Dim mFileInfo As System.IO.FileInfo
            Dim mDir As System.IO.DirectoryInfo
            Dim mDirInfo As New System.IO.DirectoryInfo(strDirect)
            Try

                For Each mFileInfo In mDirInfo.GetFiles("*.cpp")
                    If Not dictFileLists.ContainsKey(mFileInfo.FullName) Then
                        dictFileLists.Add(mFileInfo.FullName, mFileInfo.Name)
                    End If
                Next

                For Each mDir In mDirInfo.GetDirectories
                    'Debug.Print("******folder callback *******")
                    GetAllFiles(mDir.FullName)
                Next
            Catch ex As System.IO.DirectoryNotFoundException
                Debug.Print("directory not found:" + ex.Message)
            End Try
        End If
    End Sub

    Public Sub GetDirectoryPath()
        Dim obj As New FolderBrowserDialog
        obj.RootFolder = Environment.SpecialFolder.Desktop
        With obj
            .ShowDialog()
            OCScriptFolder = .SelectedPath
            If Not Directory.Exists(OCScriptFolder) Then
                MsgBox("Please select a folder")
            End If
        End With
    End Sub

    Public Function CountStringExistsNumber(ByVal original As String, ByVal include As String) As Integer
        Dim arr
        arr = Split(original, include)
        Return UBound(arr) - LBound(arr)
    End Function


    Public Function AddingIndentAndMakeNewClass(ByVal strStructure As String, ByVal scriptname As String) As String
        strStructure = strStructure.Replace(vbLf, vbCrLf)

        Dim strTemp As String = ""

        'and we need it to be included in a new class
        strTemp &= "class " & scriptname & " : public CreatureScript" & vbCrLf
        strTemp &= "{" & vbCrLf
        strTemp &= "public: " & vbCrLf
        strTemp &= "    " & scriptname & "() : CreatureScript(""" & scriptname & """) { }" & vbCrLf

        Dim arr
        arr = Split(strStructure, vbCrLf)
        For Each s In arr
            strTemp &= "    " & s & vbCrLf
        Next

        strTemp &= "};"
        Return strTemp
    End Function

    Public Sub exportDT_to_File(ByVal dt As System.Data.DataTable)

        Try
            Dim strFilePath As String
            Dim obj As New FolderBrowserDialog
            obj.RootFolder = Environment.SpecialFolder.Desktop
            With obj
                .ShowDialog()
                strFilePath = .SelectedPath
                If Not Directory.Exists(strFilePath) Then
                    MsgBox("Please select a folder")
                    Exit Sub
                End If
            End With

            strFilePath = strFilePath & "\ProcessingReport.txt"

            Dim sw As StreamWriter = New StreamWriter(strFilePath, False) 'true is append 
            Dim strCont As String = subTableToString(dt, vbTab, vbCrLf, Form1.ProgressBar1)
            Dim arr
            arr = Split(strCont, vbCrLf)
            For Each strLine In arr
                sw.WriteLine(strLine)
                sw.Flush()
            Next
            sw.Close()
            sw = Nothing

        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    Public Sub exportDict_to_File(ByVal dict As Dictionary(Of String, String))

        Try
            Dim strFilePath As String
            Dim obj As New FolderBrowserDialog
            obj.RootFolder = Environment.SpecialFolder.Desktop
            With obj
                .ShowDialog()
                strFilePath = .SelectedPath
                If Not Directory.Exists(strFilePath) Then
                    MsgBox("Please select a folder")
                    Exit Sub
                End If
            End With

            strFilePath = strFilePath & "\FunctionReport.txt"

            Dim sw As StreamWriter = New StreamWriter(strFilePath, False) 'true is append 

            For Each subKey In dict
                sw.WriteLine(subKey.Key)
                sw.Flush()
            Next
            sw.Close()
            sw = Nothing

        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    Public Function subTableToString(ByVal dtTable As System.Data.DataTable, ByVal strcolDelimeter As String, ByVal strrowDelimeter As String, ByRef ctrlProgress As ProgressBar)
        Dim strNew As String = Nothing


        ' For each row, print the values of each column.
        Dim row As System.Data.DataRow

        Dim intLong As Long = 0
        Dim iCount As Long = dtTable.Rows.Count
        For Each row In dtTable.Rows
            intLong += 1
            ctrlProgress.Value = intLong * ctrlProgress.Maximum / iCount

            Dim column As System.Data.DataColumn
            Dim strLine As String = Nothing

            Dim i As Integer = 1
            For Each column In dtTable.Columns
                'Console.WriteLine(row(column))
                If i = 1 Then 'some row might be empty at first cell
                    strLine = row(column).ToString
                Else
                    strLine = strLine + strcolDelimeter + row(column).ToString
                End If

                i += 1

            Next column
            If strNew = "" Then
                strNew = strLine
            Else
                strNew = strNew + strrowDelimeter + strLine
            End If
        Next row


        Return strNew
    End Function

    Public Sub WriteNewFile(ByVal path As String, ByVal content As String)


        Dim arr
        arr = Split(content, vbCrLf)
        Dim sw As StreamWriter = New StreamWriter(path, False, New System.Text.UTF8Encoding(False)) 'true is append method
        For Each s In arr
            sw.WriteLine(s)
            sw.Flush()
        Next
        sw.Close()
        sw = Nothing
    End Sub

#Region "sample"
    Private Sub subTXTWrite()
        Dim strFilePath As String = "D:\test.txt"
        Dim temp
        Dim sw As StreamWriter = New StreamWriter(strFilePath, True, System.Text.Encoding.UTF8) 'true is append method
        For i = 0 To 10
            temp = i.ToString
            sw.WriteLine(temp)
            sw.Flush()
        Next
        sw.Close()
        sw = Nothing
    End Sub

    Private Sub subTXTRead()
        Dim line As String
        Dim sr As StreamReader = New StreamReader("D:\test.txt", System.Text.Encoding.UTF8)
        Do While sr.Peek() > 0
            line = sr.ReadLine()
        Loop
        sr.Close()
        sr = Nothing
    End Sub
#End Region
End Module
