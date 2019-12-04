'OregonCore old script convert to new 
'by coolzoom @ 2019 Nov 30


Imports System.IO

Public Class Form1

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'GetDirectoryPath()
        If Not Directory.Exists(RootDirectory) Then
            Exit Sub
        End If

        'get all cpp files
        dictFileLists = New Dictionary(Of String, String)
        GetAllFiles(RootDirectory)
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

                '///now make sure our sc module only has ->Name, ->GetAI, ->RegisterSelf
                Dim oktoprocess As Boolean = (InStr(strSCtemp, "->Name") <> 0 And
                                             InStr(strSCtemp, "->GetInstanceData") <> 0 And
                                             InStr(strSCtemp, "->RegisterSelf") <> 0 And
                                             CountStringExistsNumber(strSCtemp, "->") = 3)
                If Not oktoprocess Then
                    ReportTable.Rows.Add(f, "manual", "file has one more script type inside")
                    Exit Sub
                Else
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
                    Dim structStr As String = "struct " & scnametemp
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
                        strSecond = strSecond.Replace(scnametemp, scnametemp & "AI")
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
    End Class
