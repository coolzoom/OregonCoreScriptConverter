Imports System.IO

Module Module1
    Public dictFileLists As Dictionary(Of String, String)
    Public RootDirectory As String = "H:\WOWServer\Source\OregonCore\src\scripts"
    Public ReportTable As System.Data.DataTable
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
            RootDirectory = .SelectedPath
            If Not Directory.Exists(RootDirectory) Then
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
        strTemp &= "class " & scriptname & " : public InstanceMapScript" & vbCrLf
        strTemp &= "{" & vbCrLf
        strTemp &= "public: " & vbCrLf
        strTemp &= "    " & scriptname & "() : InstanceMapScript(""" & scriptname & """) { }" & vbCrLf

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
