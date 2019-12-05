'OregonCore old script convert to new 
'by coolzoom @ 2019 Nov 30


Imports System.IO

Public Class Form1

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OCScriptFolder = "H:\WOWServer\Source\OregonCore\src\scripts"
        'GetDirectoryPath()
        If Not Directory.Exists(OCScriptFolder) Then
            Exit Sub
        End If
        'initial function dict
        ScriptFunctionLists = New Dictionary(Of String, String)

        'get all cpp files

        dictFileLists = New Dictionary(Of String, String)


        GetAllFiles(OCScriptFolder)
        'list number
        MsgBox(dictFileLists.Count)
        'initial report table
        ReportTable = New System.Data.DataTable
        ReportTable.TableName = "Processing Report"
        ReportTable.Columns.Add("FilePath")
        ReportTable.Columns.Add("Result")
        ReportTable.Columns.Add("Reason")
        'loop all files
        For Each cppFile In dictFileLists
            Dim f As String = cppFile.Key
            If File.Exists(f) Then
                '



                ProcessingSimpleBossScript(f)

            End If
        Next
        'indicate report
        DataGridView1.DataSource = ReportTable
    End Sub

    Private Sub ProcessingSimpleBossScript(ByVal f As String)
        Dim strSCKey As String = "void AddSC"

        Dim sr As StreamReader = New StreamReader(f, System.Text.Encoding.UTF8)
        Dim FileContent As String = sr.ReadToEnd
        sr.Close()
        sr = Nothing
        '///first we make sure we only do the one which has only one void AddSC, at least for now
        Dim SCCount As Integer = CountStringExistsNumber(FileContent, strSCKey)

        Select Case SCCount
            Case 0
                ReportTable.Rows.Add(f, "ignore", "no script module")
            Case 1
                '///get AddSC module
                'void AddSC *
                '{
                '       * -> Name = "script_name";
                '}
                Dim SCStartPosition As Integer = InStr(FileContent, strSCKey)
                Dim strSCtemp As String = Mid(FileContent, SCStartPosition) 'we now get the last portion of void AddSC to the end
                Dim SCEndPosition As Integer = InStr(strSCtemp, "}") ' the first } is where sc end
                strSCtemp = Microsoft.VisualBasic.Left(strSCtemp, SCEndPosition)
                'MsgBox(strSCtemp)

                '///now check if the module already converted by look at the SC part see if there is Script* included
                Dim converted As Boolean = (InStr(strSCtemp, "Script*") = 0)
                If converted Then
                    ReportTable.Rows.Add(f, "done", "file already converted")
                    Exit Sub
                End If

                '///now make sure our sc module only has ->Name, ->GetAI, ->RegisterSelf
                Dim oktoprocess As Boolean = (InStr(strSCtemp, "->Name") <> 0) 'And
                'InStr(strSCtemp, "->GetAI") <> 0 And
                'InStr(strSCtemp, "->RegisterSelf") <> 0 And
                'CountStringExistsNumber(strSCtemp, "->") = 3)
                If Not oktoprocess Then

                    ReportTable.Rows.Add(f, "manual", "file has no script name")
                    Exit Sub
                Else
                    'get function lists this is for prepare purpose
                    'GetFunctions(strSCtemp) ' get function name between eg: ->pGossipHello =  &

                    'get script count
                    Dim intScCount As Integer = CountStringExistsNumber(strSCtemp, "->Name")
                    If Not intScCount = 1 Then
                        'ReportTable.Rows.Add(f, "complex", "file has multiple more script name")
                        'Exit Sub
                        Dim comments As String = ""
                        'get script name
                        Dim dictSCnames As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        '///struct
                        Dim dictNameKeyStructValue As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        '///CreatureAI
                        Dim dictNameKeyCreatureAIValue As Dictionary(Of String, String) = New Dictionary(Of String, String)

                        'there're some modules that are outside of struct as well,eg: void boss_midnightAI:: SetMidnight(Creature * pAttumen, UInt64 value)
                        Dim dictNameKeyMethodValue As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))
                        'get the function modules and this also need to include in new class eg:GossipHello_custom_example
                        Dim dictNameKeyFunctionValue As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))

                        dictSCnames = GetScriptNames(strSCtemp)

                        'get hook mapping 
                        'Dim dictFunctionKeyHookValue As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        'dictFunctionKeyHookValue = GetFuncionHookRelation(strSCtemp) ' key GossipHello_custom_example  value pGossipHello

                        'get hook mapping  script name as key, GossipHello_custom_example pGossipHello pair as value                       
                        Dim dictNameFunctionKeyHookValue As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))
                        dictNameFunctionKeyHookValue = GetNameFuncionHookRelation(strSCtemp)

                        'right now i dont have a good way to include the hooks, so ignore for now
                        'For Each subKey In dictNameFunctionKeyHookValue
                        '    If subKey.Value.Count > 0 Then
                        '        comments &= " hooks found, ignore for now " & subKey.Key
                        '    End If
                        'Next

                        'get struct, start with struct script_nameAI : Public ScriptedAI and end with  first }; found
                        dictNameKeyStructValue = GetStructNames(FileContent, dictSCnames, comments)
                        'struct script_nameAI : Public ScriptedAI
                        '{
                        '	   script_nameAI(Creature * c) : ScriptedAI(c) {}
                        '};

                        'get CreatureAI* GetAI_script_name, end with first } found
                        dictNameKeyCreatureAIValue = GetCreatureAINames(FileContent, dictSCnames, comments)
                        'CreatureAI* GetAI_script_name(Creature * pCreature)
                        '{
                        '	Return New script_nameAI(pCreature);
                        '}

                        '///get function hook module GossipHello_custom_example
                        dictNameKeyFunctionValue = GetNameKeyFunction(FileContent, dictNameFunctionKeyHookValue, comments) 'script_name as key, (GossipHello_custom_example ,whole method) as value


                        ''///get other method
                        'dictNameKeyMethodValue = GetNameKeyMethod(FileContent, dictSCnames, comments) 'script_name as key, (SetMidnight ,whole method) as value
                        ''void boss_midnightAI:: SetMidnight(Creature * pAttumen, UInt64 value)
                        ''{
                        ''    CAST_AI(boss_attumenAI, pAttumen -> AI())->Midnight = value;
                        ''}


                        'remove old Struct
                        For Each subkey In dictNameKeyStructValue
                            FileContent = FileContent.Replace(subkey.Value, "")
                        Next
                        'remove old  Creature AI
                        For Each subkey In dictNameKeyCreatureAIValue
                            FileContent = FileContent.Replace(subkey.Value, "")
                        Next
                        ''remove old method
                        'For Each subkey In dictNameKeyMethodValue
                        '    For Each item In subkey.Value
                        '        FileContent = FileContent.Replace(item, "")
                        '    Next
                        'Next
                        'remove old function hook
                        For Each subkey In dictNameKeyFunctionValue
                            For Each item In subkey.Value
                                FileContent = FileContent.Replace(item, "")
                            Next
                        Next
                        'remove old AddSC
                        FileContent = FileContent.Replace(strSCtemp, "")

                        '


                        'replace hooks within struct one module by one module
                        Dim dictNameKeyFunctionTemp As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))
                        For Each subModule In dictNameKeyFunctionValue
                            'check if the mapping list have this script
                            If dictNameFunctionKeyHookValue.ContainsKey(subModule.Key) Then
                                Dim lstTemp As List(Of String) = New List(Of String)
                                For i = 0 To subModule.Value.Count - 1 'get the whole function
                                    'loop the table to get the keyword and replace
                                    For Each subHook In dictNameFunctionKeyHookValue(subModule.Key) ' key GossipHello_custom_example  value pGossipHello
                                        If DictFunctionMapping.ContainsKey(subHook.Value) Then
                                            'replace module
                                            Dim strTemp As String = subModule.Value(i).Replace(subHook.Key & "(", DictFunctionMapping(subHook.Value) & "(")
                                            'adding override at behind
                                            Dim newStr As String = ""
                                            Dim arr = Split(strTemp, vbLf)
                                            For Each line In arr
                                                If InStr(line, DictFunctionMapping(subHook.Value)) Then
                                                    newStr &= line & " override" & vbLf
                                                Else
                                                    newStr &= line & vbLf
                                                End If
                                            Next


                                            lstTemp.Add(newStr)
                                        End If
                                    Next
                                Next
                                If lstTemp.Count > 0 Then
                                    dictNameKeyFunctionTemp.Add(subModule.Key, lstTemp)
                                End If

                            End If

                        Next
                        dictNameKeyFunctionValue = dictNameKeyFunctionTemp
                        'Dim dictTemp As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        'For Each subSturct In dictNameKeyStructValue
                        '    For Each subkey In dictFunctionKeyHookValue
                        '        'make sure we have the mapping in that table otherwise stop and ignore
                        '        If subkey.Value <> "GetAI" Then
                        '            If DictFunctionMapping.ContainsKey(subkey.Value) Then
                        '                If dictTemp.ContainsKey(subSturct.Key) Then
                        '                    dictTemp(subSturct.Key) = dictNameKeyStructValue(subSturct.Key).Replace(subkey.Key & "(", DictFunctionMapping(subkey.Value) & "(")
                        '                Else
                        '                    dictTemp.Add(subSturct.Key, dictNameKeyStructValue(subSturct.Key).Replace(subkey.Key & "(", DictFunctionMapping(subkey.Value) & "("))
                        '                End If
                        '            Else
                        '                comments &= " " & subkey.Value & " has no mapping in mapping table"
                        '            End If
                        '        End If

                        '    Next
                        'Next
                        'dictNameKeyStructValue = dictTemp



                        '///making new class
                        Dim strNewStuct As String = ""
                        'loop each script name and making new class
                        For Each subName In dictSCnames
                            'CreatureAI
                            Dim strCreatureAI As String = ""
                            If (dictNameKeyCreatureAIValue.ContainsKey(subName.Key.Trim)) Then
                                strCreatureAI = dictNameKeyCreatureAIValue(subName.Key.Trim)
                            Else
                                comments &= " " & subName.Key.Trim & " no CreatureAI"
                            End If
                            ''Methods if any
                            'Dim strMethods As String = ""
                            'If (dictNameKeyMethodValue.ContainsKey(subStruct.Key.Trim)) Then
                            '    For Each item In dictNameKeyMethodValue(subStruct.Key.Trim)
                            '        strMethods &= item & vbCrLf
                            '    Next
                            'End If
                            'FunctionHooks
                            Dim FunctionHooks As String = ""
                            If (dictNameKeyFunctionValue.ContainsKey(subName.Key.Trim)) Then
                                For Each item In dictNameKeyFunctionValue(subName.Key.Trim)
                                    FunctionHooks &= item & vbCrLf
                                Next
                            End If

                            'get struct
                            Dim newContent As String = ""
                            Dim newStruct As String = ""
                            If dictNameKeyStructValue.ContainsKey(subName.Key) Then 'found structure
                                newStruct = dictNameKeyStructValue(subName.Key) & vbCrLf
                            End If

                            'ai
                            newContent &= newStruct & vbCrLf

                            'ai
                            newContent &= strCreatureAI & vbCrLf
                            ''method
                            'newContent &= strMethods & vbCrLf
                            'function hooks
                            newContent &= FunctionHooks & vbCrLf
                            If newStruct.Trim = "" Then
                                strNewStuct &= AddingIndentAndMakeNewClass(newContent, subName.Key, "UnknownScript") & vbCrLf & vbCrLf
                            Else
                                strNewStuct &= AddingIndentAndMakeNewClass(newContent, subName.Key, "CreatureScript") & vbCrLf & vbCrLf
                            End If


                        Next

                        '///now it is safe we create a new void AddSC module
                        Dim scname As String = ""
                        For Each subkey In dictSCnames
                            If subkey.Key.Trim <> "" Then
                                scname &= "    new " & subkey.Key.Trim & "();" & vbCrLf
                            End If
                        Next
                        '
                        Dim scNewAddSC As String = Microsoft.VisualBasic.Left(strSCtemp, InStr(strSCtemp, "{") - 1) 'now we should have void AddSC*
                        scNewAddSC = scNewAddSC.Trim().Replace(vbCrLf, "") & vbCrLf &
                            "{" & vbCrLf &
                            scname & vbCrLf &
                            "}"

                        TextBox1.Text = FileContent
                        TextBox2.Text = strNewStuct
                        TextBox3.Text = scNewAddSC


                        Dim strFinal As String = FileContent & vbCrLf
                        strFinal &= strNewStuct & vbCrLf
                        strFinal &= scNewAddSC & vbCrLf

                        '///now we should able to write this file
                        'WriteNewFile("D:\test.txt", strFinalContent)
                        If comments.Trim = "" Then
                            WriteNewFile(f, strFinal)
                            ReportTable.Rows.Add(f, "done", "converted")

                        Else
                            WriteNewFile(f, strFinal)
                            ReportTable.Rows.Add(f, "partical", comments)
                        End If

                        'this module is for complex scripting, so exit directly
                        Exit Sub

                    Else


                    End If

                    'get function names for replacement purpose
                    Dim dictOldFunctionAskeyOldFunctionNameAsValue As Dictionary(Of String, String) = New Dictionary(Of String, String)
                    dictOldFunctionAskeyOldFunctionNameAsValue = GetFunctionsNames(strSCtemp)


                    '///we need to get the module name from ->Name
                    Dim scnametemp As String = Mid(strSCtemp, InStr(strSCtemp, "->Name"))
                    scnametemp = Microsoft.VisualBasic.Left(scnametemp, InStr(scnametemp, ";")) 'now we have ->Name = "script_name";
                    scnametemp = scnametemp.Replace(" ", "") 'now we have ->Name="script_name";
                    scnametemp = scnametemp.Replace("->Name=""", "") 'now we have script_name";
                    scnametemp = scnametemp.Replace(""";", "") 'now we have script_name
                    'MsgBox(scnametemp)

                    '///now it is safe we create a new void AddSC module
                    Dim scNew As String = Microsoft.VisualBasic.Left(strSCtemp, InStr(strSCtemp, "{") - 1) 'now we should have void AddSC*
                    scNew = scNew.Trim().Replace(vbCrLf, "") & vbCrLf &
                        "{" & vbCrLf &
                        "    new " & scnametemp & "();" & vbCrLf &
                        "}"
                    'MsgBox(scNew)


                    'now we should get the position of struct script_nameAI 
                    'get struct script_nameAI position
                    Dim structStr As String = "struct " & scnametemp & "AI"
                    Dim structPos As Integer = InStr(FileContent, structStr)


                    If structPos = 0 Then
                        ReportTable.Rows.Add(f, "manual", "could not found " & structStr)
                        Exit Sub
                    Else
                        '///////string that Is before struct script_nameAI
                        Dim strFirst As String = Microsoft.VisualBasic.Left(FileContent, structPos - 1)

                        'get first } after find CreatureAI, actually SCStartPosition is the end
                        '/// so we have the second part
                        Dim strSecond As String = Mid(FileContent, structPos, SCStartPosition - structPos)

                        'replace old function with new
                        If dictOldFunctionAskeyOldFunctionNameAsValue.Count > 0 Then
                            For Each subKey In dictOldFunctionAskeyOldFunctionNameAsValue
                                'make sure we have the mapping in that table otherwise stop and ignore
                                If subKey.Key <> "GetAI" Then
                                    If DictFunctionMapping.ContainsKey(subKey.Key) Then
                                        strSecond = strSecond.Replace(subKey.Value & "(", DictFunctionMapping(subKey.Key) & "(")
                                    Else
                                        ReportTable.Rows.Add(f, "fail", "function " & subKey.Value & " has no mapping in mapping table")
                                        Exit Sub
                                    End If
                                End If

                            Next
                        End If

                        TextBox1.Text = strFirst
                        TextBox2.Text = strSecond
                        TextBox3.Text = scNew

                        '///our new structure needs to have 4 more indent
                        strSecond = AddingIndentAndMakeNewClass(strSecond, scnametemp, "CreatureScript")

                        Dim strFinalContent As String = strFirst & vbCrLf
                        strFinalContent &= strSecond & vbCrLf
                        strFinalContent &= scNew & vbCrLf

                        '///now we should able to write this file
                        'WriteNewFile("D:\test.txt", strFinalContent)
                        WriteNewFile(f, strFinalContent)

                        ReportTable.Rows.Add(f, "done", "converted")
                    End If










                End If

                'get -> Count

            Case Else
                ReportTable.Rows.Add(f, "manual", "file has one more script inside")
        End Select






        For Each line In FileContent.Split(vbCrLf)

        Next

    End Sub



    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        exportDT_to_File(ReportTable)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        exportDict_to_File(ScriptFunctionLists)
    End Sub

    Private Sub test()


        Dim str As String = "    void Test1: ReceiveEmote(Player* player, uint32 emote)
    {
        switch (emote)
        {
            case TEXT_EMOTE_CHICKEN:
                if (player->GetQuestStatus(QUEST_CLUCK) == QUEST_STATUS_NONE && rand32() % 30 == 1)
                {
                    me->SetFlag(UNIT_NPC_FLAGS, UNIT_NPC_FLAG_GOSSIP | UNIT_NPC_FLAG_QUESTGIVER);
                    me->SetFaction(FACTION_FRIENDLY);
                    DoScriptText(EMOTE_HELLO, me);
                }
                break;
            case TEXT_EMOTE_CHEER:
                if (player->GetQuestStatus(QUEST_CLUCK) == QUEST_STATUS_COMPLETE)
                {
                    me->SetFlag(UNIT_NPC_FLAGS, UNIT_NPC_FLAG_GOSSIP | UNIT_NPC_FLAG_QUESTGIVER);
                    me->SetFaction(FACTION_FRIENDLY);
                    DoScriptText(EMOTE_CLUCK_TEXT, me);
                }
                break;
        }
    }
};

void Test2: ReceiveEmote(Player* player, uint32 emote)
    {
        switch (emote)
        {
            case TEXT_EMOTE_CHICKEN:
                if (player->GetQuestStatus(QUEST_CLUCK) == QUEST_STATUS_NONE && rand32() % 30 == 1)
                {
                    me->SetFlag(UNIT_NPC_FLAGS, UNIT_NPC_FLAG_GOSSIP | UNIT_NPC_FLAG_QUESTGIVER);
                    me->SetFaction(FACTION_FRIENDLY);
                    DoScriptText(EMOTE_HELLO, me);
                }
                break;
            case TEXT_EMOTE_CHEER:
                if (player->GetQuestStatus(QUEST_CLUCK) == QUEST_STATUS_COMPLETE)
                {
                    me->SetFlag(UNIT_NPC_FLAGS, UNIT_NPC_FLAG_GOSSIP | UNIT_NPC_FLAG_QUESTGIVER);
                    me->SetFaction(FACTION_FRIENDLY);
                    DoScriptText(EMOTE_CLUCK_TEXT, me);
                }
                break;
        }
    }
};

CreatureAI* GetAI_npc_chicken_cluck(Creature* pCreature)
{
    return new npc_chicken_cluckAI(pCreature);
}"
        LstTempModule.Clear()

        GetModules(str, "ReceiveEmote(", "")
        MsgBox(LstTempModule.Count)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        test()
    End Sub
End Class
