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
    Public Function GetScriptNames(strSCtemp As String) As Dictionary(Of String, String)
        'Void AddSC_boss_moroes()
        '{
        'Script* newscript;

        'newscript = New Script;
        'newscript-> Name = "boss_moroes";
        'newscript-> GetAI = & GetAI_boss_moroes;
        'newscript-> RegisterSelf();

        'newscript = New Script;
        'newscript-> Name = "boss_baroness_dorothea_millstipe";
        'newscript-> GetAI = & GetAI_baroness_dorothea_millstipe;
        'newscript-> RegisterSelf();

        'get script name from module
        strSCtemp = strSCtemp.Replace(vbCr, vbCrLf)
        strSCtemp = strSCtemp.Replace(vbLf, vbCrLf)
        strSCtemp = strSCtemp.Replace(" ", "")
        strSCtemp = strSCtemp.Replace(vbTab, "")
        Dim arr
        arr = Split(strSCtemp, vbCrLf)

        Dim dictTemp As Dictionary(Of String, String) = New Dictionary(Of String, String)
        For Each subLine In arr
            'newscript-> Name = "boss_moroes";

            Dim subName As String = SearchMidString(subLine, "->Name=""", """;").Trim
            If subName.Trim = "" Then
                'Exit For
            Else
                If Not dictTemp.ContainsKey(subName) Then
                    dictTemp.Add(subName, "")
                End If

            End If

        Next
        Return dictTemp


    End Function

    ''' <summary>
    ''' get function modules and store in a list, becuase one file may contains several module with that keyword
    ''' </summary>
    Public LstTempModule As List(Of String)
    Public Sub GetModules(ByVal fileContent As String, ByVal keyword As String, ByRef comments As String)
        'this is to get the module function name start from key word and end with }
        '
        'CreatureAI* GetAI_npc_barmaid(Creature* pCreature)
        '{
        '	Return New npc_barmaidAI(pCreature);
        '}
        '////sime funciton may outside of struct
        'void boss_midnightAI: SetMidnight(Creature * pAttumen, UInt64 value)
        '{
        '    CAST_AI(boss_attumenAI, pAttumen -> AI())->Midnight = value;
        '}

        'bool QuestAccept_npc_chicken_cluck(Player* /*pPlayer*/, Creature* pCreature, const Quest* _Quest)
        '{

        '    If (_Quest -> GetQuestId() == QUEST_CLUCK) Then
        '            CAST_AI(npc_chicken_cluckAI, pCreature -> AI())->Reset();

        '    Return True;
        '}


        Dim functionLine As String = ""

        Dim arr
        arr = Split(fileContent, vbLf)
        'here we just search the keyword, please assign correct keyword outside of this function eg functionname( scriptname: etc
        For Each line In arr
            If InStr(line, keyword) <> 0 Then
                functionLine = line
                Exit For
            End If
        Next

        If functionLine = "" Then
            comments &= " could not found " & keyword
            Exit Sub
        End If

        Dim strTemp As String = Mid(fileContent, InStr(fileContent, functionLine))

        'now strTemp is the file content from function start to the file end
        'now we loop every string till the {} equals
        Dim boolStart As Boolean = False ' if we dont find a { means we havent start the condition
        Dim leftcount As Integer = 0 'count of {
        Dim rightcount As Integer = 0 'count of }
        Dim closepostion As Integer = 0 ' the } position, can also use as pointer
        For i = 1 To strTemp.Length
            closepostion = i
            'current char
            Dim currentchar As String = Mid(strTemp, i, 1)
            'get start point
            If currentchar = "{" And leftcount = 0 Then
                boolStart = True
            End If

            'assign count
            If currentchar = "{" Then leftcount += 1
            If currentchar = "}" Then rightcount += 1

            If boolStart And leftcount = rightcount Then
                Exit For
            End If
        Next i

        If closepostion > 1 Then 'means found
            Dim strModule As String = Microsoft.VisualBasic.Left(strTemp, closepostion)
            LstTempModule.Add(strModule)

            'loopback
            Dim strTempNew As String = Mid(strTemp, closepostion)
            GetModules(strTempNew, keyword, comments)
        End If


    End Sub

    ''' <summary>
    ''' only get one module, eg AddSC
    ''' </summary>
    ''' <param name="fileContent"></param>
    ''' <param name="keyword"></param>
    Public Function GetModule(ByVal fileContent As String, ByVal keyword As String)
        'this is to get the module function name start from key word and end with }
        '
        'CreatureAI* GetAI_npc_barmaid(Creature* pCreature)
        '{
        '	Return New npc_barmaidAI(pCreature);
        '}
        '////sime funciton may outside of struct
        'void boss_midnightAI: SetMidnight(Creature * pAttumen, UInt64 value)
        '{
        '    CAST_AI(boss_attumenAI, pAttumen -> AI())->Midnight = value;
        '}

        'bool QuestAccept_npc_chicken_cluck(Player* /*pPlayer*/, Creature* pCreature, const Quest* _Quest)
        '{

        '    If (_Quest -> GetQuestId() == QUEST_CLUCK) Then
        '            CAST_AI(npc_chicken_cluckAI, pCreature -> AI())->Reset();

        '    Return True;
        '}


        Dim functionLine As String = ""

        Dim arr
        arr = Split(fileContent, vbLf)
        'here we just search the keyword, please assign correct keyword outside of this function eg functionname( scriptname: etc
        For Each line In arr
            If InStr(line, keyword) <> 0 Then
                functionLine = line
                Exit For
            End If
        Next

        Dim strTemp As String = Mid(fileContent, InStr(fileContent, functionLine))

        'now strTemp is the file content from function start to the file end
        'now we loop every string till the {} equals
        Dim boolStart As Boolean = False ' if we dont find a { means we havent start the condition
        Dim leftcount As Integer = 0 'count of {
        Dim rightcount As Integer = 0 'count of }
        Dim closepostion As Integer = 0 ' the } position, can also use as pointer
        For i = 1 To strTemp.Length
            closepostion = i
            'current char
            Dim currentchar As String = Mid(strTemp, i, 1)
            'get start point
            If currentchar = "{" And leftcount = 0 Then
                boolStart = True
            End If

            'assign count
            If currentchar = "{" Then leftcount += 1
            If currentchar = "}" Then rightcount += 1

            If boolStart And leftcount = rightcount Then
                Exit For
            End If
        Next i

        If closepostion > 1 Then 'means found
            Dim strModule As String = Microsoft.VisualBasic.Left(strTemp, closepostion)
            LstTempModule.Add(strModule)
            Return strModule
        Else
            Return ""
        End If


    End Function

    Public Function GetNameKeyFunction(fileContent As String, dictNameFunctionKeyHookValue As Dictionary(Of String, Dictionary(Of String, String)), comments As String) As Dictionary(Of String, List(Of String))
        Dim dictTemp As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))
        'void boss_midnightAI: SetMidnight(Creature * pAttumen, UInt64 value)
        '{
        '    CAST_AI(boss_attumenAI, pAttumen -> AI())->Midnight = value;
        '}
        For Each subKey In dictNameFunctionKeyHookValue 'key script name,( GossipHello_custom_example  pGossipHello) pair as value
            If subKey.Key <> "" Then
                LstTempModule = New List(Of String)
                For Each strHookModule In subKey.Value 'key GossipHello_custom_example  value pGossipHello
                    Dim strKeyword As String = strHookModule.Key & "("
                    GetModules(fileContent, strKeyword, comments)
                Next
                If LstTempModule.Count > 0 Then
                    dictTemp.Add(subKey.Key, LstTempModule)
                End If

                'debug
                If subKey.Key = "npc_00x09hl" Then
                    MsgBox("stop")
                End If
            End If
        Next
        Return dictTemp
    End Function

    Public Function GetNameKeyMethod(fileContent As String, dictSCnames As Dictionary(Of String, String), comments As String) As Dictionary(Of String, List(Of String))
        Dim dictTemp As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))
        'void boss_midnightAI: SetMidnight(Creature * pAttumen, UInt64 value)
        '{
        '    CAST_AI(boss_attumenAI, pAttumen -> AI())->Midnight = value;
        '}
        For Each subKey In dictSCnames
            If subKey.Key <> "" Then
                LstTempModule.Clear()
                Dim strKeyword As String = subKey.Key & "AI::"
                GetModules(fileContent, strKeyword, comments)
                If LstTempModule.Count > 0 Then
                    dictTemp.Add(subKey.Key, LstTempModule)
                End If

                'debug
                If subKey.Key = "boss_midnight" Then
                    MsgBox("stop")
                End If

            End If
        Next
        Return dictTemp
    End Function

    Public Function GetCreatureAINames(ByVal fileContent As String, ByVal dictOrg As Dictionary(Of String, String), ByRef comments As String)
        Dim dictTemp As Dictionary(Of String, String) = New Dictionary(Of String, String)
        For Each subKey In dictOrg
            If subKey.Key <> "" Then
                Dim strStart As String = "CreatureAI* GetAI_" & subKey.Key & "("
                Dim strEnd As String = "}"
                If InStr(fileContent, strStart) <> 0 Then
                    Dim strTemp As String = Mid(fileContent, InStr(fileContent, strStart))
                    Dim found As String = Microsoft.VisualBasic.Left(strTemp, InStr(strTemp, strEnd)) ' SearchMidString(fileContent, strStart, strEnd)
                    dictTemp.Add(subKey.Key, found)
                    'MsgBox(found)
                Else
                    comments &= " " & subKey.Key & " not found"
                End If
            End If
        Next
        Return dictTemp
    End Function

    Public Function GetStructNames(ByVal fileContent As String, ByVal dictOrg As Dictionary(Of String, String), ByRef comments As String)
        Dim dictTemp As Dictionary(Of String, String) = New Dictionary(Of String, String)
        For Each subKey In dictOrg
            If subKey.Key <> "" Then
                Dim strStart As String = "struct " & subKey.Key & "AI"
                Dim strEnd As String = "};"
                If InStr(fileContent, strStart) <> 0 Then
                    Dim strTemp As String = Mid(fileContent, InStr(fileContent, strStart))
                    Dim found As String = Microsoft.VisualBasic.Left(strTemp, InStr(strTemp, strEnd) + 1) 'SearchMidString(fileContent, strStart, strEnd)
                    dictTemp.Add(subKey.Key, found)
                    'MsgBox(found)
                Else
                    comments &= " " & subKey.Key & " not found"
                End If
            End If
        Next
        Return dictTemp
    End Function

    Public Function GetFuncionHookRelation(strSCtemp As String) As Dictionary(Of String, String)
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
                        If Not dictTemp.ContainsKey(subFuncName) Then
                            dictTemp.Add(subFuncName, subFunc) 'GossipHello_custom_example  pGossipHello
                        End If
                    End If

                End If


            End If

        Next
        Return dictTemp
    End Function

    Public Function GetNameFuncionHookRelation(strSCtemp As String) As Dictionary(Of String, Dictionary(Of String, String))
        strSCtemp = strSCtemp.Replace(vbCr, vbCrLf)
        strSCtemp = strSCtemp.Replace(vbLf, vbCrLf)
        'start with name and stop with RegisterSelf
        'newscript = New Script;
        'newscript-> Name = "npc_00x09hl";
        'newscript-> GetAI = & GetAI_npc_00x09hl;
        'newscript-> pQuestAccept = & QuestAccept_npc_00x09hl;
        'newscript-> RegisterSelf();

        'newscript = New Script;
        'newscript-> Name = "npc_rinji";
        'newscript-> GetAI = & GetAI_npc_rinji;
        'newscript-> pQuestAccept = & QuestAccept_npc_rinji;
        'newscript-> RegisterSelf();

        strSCtemp = strSCtemp.Replace(vbCr, vbCrLf)
        strSCtemp = strSCtemp.Replace(vbLf, vbCrLf)
        strSCtemp = strSCtemp.Replace(" ", "")
        strSCtemp = strSCtemp.Replace(vbTab, "")


        Dim arr
        arr = Split(strSCtemp, vbCrLf)
        Dim dictTemp As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))

        Dim currentscriptname As String = ""

        For Each subLine In arr
            'first we should check whether if there is a script name included
            'newscript-> Name = "npc_00x09hl";
            Dim subName As String = SearchMidString(subLine, "->Name=""", """;").Trim
            If subName.Trim = "" Then
                'Exit For
            Else
                currentscriptname = subName
                If Not dictTemp.ContainsKey(subName) Then
                    dictTemp.Add(subName, New Dictionary(Of String, String))
                Else
                    'this should never happen because we shouldn't have two same script name in same module
                End If
            End If

            'then we should check if we found a hook
            'newscript->pGossipHello = &GossipHello_custom_example;
            Dim subFunc As String = SearchMidString(subLine, "->", "=").Trim
            If subFunc.Trim = "" Or subFunc.Trim = "GetAI" Then
                'Exit For
            Else 'pGossipHello etc
                Dim subFuncName As String
                If InStr(subLine, "&") <> 0 Then
                    subFuncName = SearchMidString(subLine, "&", ";").Trim.Trim
                    If subFuncName.Trim <> "" And currentscriptname <> "" Then
                        'check if current script already have that mapping dict
                        If Not dictTemp(currentscriptname).ContainsKey(subFuncName) Then
                            dictTemp(currentscriptname).Add(subFuncName, subFunc) 'scriot name as key, GossipHello_custom_example  pGossipHello pair as value
                        End If
                    End If

                End If


            End If

        Next
        Return dictTemp
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


    Public Function AddingIndentAndMakeNewClass(ByVal strStructure As String, ByVal scriptname As String, ByVal typename As String) As String
        strStructure = strStructure.Replace(vbLf, vbCrLf)
        'strStructure = strStructure.Replace(vbCr, vbCrLf)

        Dim strTemp As String = ""

        'and we need it to be included in a new class
        strTemp &= "class " & scriptname & " : public " & typename & vbCrLf ': public CreatureScript
        strTemp &= "{" & vbCrLf
        strTemp &= "public: " & vbCrLf
        strTemp &= "    " & scriptname & "() : " & typename & "(""" & scriptname & """) { }" & vbCrLf
        'add struct
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
