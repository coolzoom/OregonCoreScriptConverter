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
                        Dim dictNameKeyStructValue As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        Dim dictNameKeyCreatureAIValue As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        dictSCnames = GetScriptNames(strSCtemp)

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

                        'remove Struct and Creature AI
                        For Each subkey In dictNameKeyStructValue
                            FileContent = FileContent.Replace(subkey.Value, "")
                        Next
                        For Each subkey In dictNameKeyCreatureAIValue
                            FileContent = FileContent.Replace(subkey.Value, "")
                        Next

                        'get hook mapping 
                        Dim dictFunctionKeyHookValue As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        dictFunctionKeyHookValue = GetFuncionHookRelation(strSCtemp) ' key GossipHello_custom_example  value pGossipHello
                        'replace hooks within struct one module by one module
                        Dim dictTemp As Dictionary(Of String, String) = New Dictionary(Of String, String)
                        For Each subSturct In dictNameKeyStructValue
                            For Each subkey In dictFunctionKeyHookValue
                                'make sure we have the mapping in that table otherwise stop and ignore
                                If subkey.Value <> "GetAI" Then
                                    If DictFunctionMapping.ContainsKey(subkey.Value) Then
                                        If dictTemp.ContainsKey(subSturct.Key) Then
                                            dictTemp(subSturct.Key) = dictNameKeyStructValue(subSturct.Key).Replace(subkey.Key & "(", DictFunctionMapping(subkey.Value) & "(")
                                        Else
                                            dictTemp.Add(subSturct.Key, dictNameKeyStructValue(subSturct.Key).Replace(subkey.Key & "(", DictFunctionMapping(subkey.Value) & "("))
                                        End If
                                    Else
                                        comments &= " " & subkey.Value & " has no mapping in mapping table"
                                    End If
                                End If

                            Next
                        Next
                        dictNameKeyStructValue = dictTemp
                        'make new AddSC portion
                        Dim scname As String
                        For Each subkey In dictSCnames
                            If subkey.Key.Trim <> "" Then
                                scname &= "    new " & subkey.Key.Trim & "();" & vbCrLf
                            End If
                        Next
                        '///now it is safe we create a new void AddSC module
                        Dim scNewt As String = Microsoft.VisualBasic.Left(strSCtemp, InStr(strSCtemp, "{") - 1) 'now we should have void AddSC*
                        scNewt = scNewt.Trim().Replace(vbCrLf, "") & vbCrLf &
                            "{" & vbCrLf &
                            scname & vbCrLf &
                            "}"

                        '///also making new class
                        Dim strNewStuct As String = ""
                        For Each subkey In dictNameKeyStructValue
                            If subkey.Key.Trim <> "" Then
                                strNewStuct &= AddingIndentAndMakeNewClass(subkey.Value, subkey.Key) & vbCrLf & vbCrLf
                            End If
                        Next

                        TextBox1.Text = FileContent
                        TextBox2.Text = strNewStuct
                        TextBox3.Text = scNewt


                        Dim strFinal As String = FileContent & vbCrLf
                        strFinal &= strNewStuct & vbCrLf
                        strFinal &= scNewt & vbCrLf

                        '///now we should able to write this file
                        'WriteNewFile("D:\test.txt", strFinalContent)
                        If comments.Trim = "" Then
                            WriteNewFile(f, strFinal)
                            ReportTable.Rows.Add(f, "done", "converted")
                        Else
                            ReportTable.Rows.Add(f, "partical", comments)
                        End If




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
                        strSecond = AddingIndentAndMakeNewClass(strSecond, scnametemp)

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
End Class
