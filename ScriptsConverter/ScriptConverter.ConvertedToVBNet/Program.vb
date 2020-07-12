Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO

Class Program
	Friend Shared Sub Main(args As String())
		If args.Length <> 1 Then
			Console.WriteLine("Usage: ScriptsConverter.exe [path_to_dir|path_to_file]")
		Else
			Dim path As String = args(0)
			If File.Exists(path) Then
				ProcessFile(path)
			ElseIf Directory.Exists(path) Then
				ProcessDirectory(path)
			Else
				Console.WriteLine("Invalid file or directory specified." & vbCr & vbLf & vbCr & vbLf & "Usage: ScriptsConverter.exe [path_to_dir|path_to_file]")
			End If
		End If
	End Sub

	Private Shared Sub ProcessDirectory(path As String)
		Dim files As String() = Directory.GetFiles(path, "*.cpp")
		For Each file As String In files
			ProcessFile(file)
		Next
		Dim dirs As String() = Directory.GetDirectories(path)
		For Each dir As String In dirs
			ProcessDirectory(dir)
		Next
	End Sub

	Private Class ScriptData
		Public type As Integer = 0
		Public name As String
		Public methods As New ArrayList()
		Public instanceName As String = Nothing
		Public aiName As String = Nothing
		Public special As String() = New String() {"GetAI_", "GetInstance_", "GetInstanceData_"}

		Public Sub AddMethod(method As String)
			methods.Add(method)
			Dim i As Integer = 0
			For Each s As String In special
				i += 1
				Dim pos As Integer = method.IndexOf(s)
				If pos <> -1 Then
					type = i
					Dim name As String = method.Substring(pos + s.Length)
					If i = 1 Then
						aiName = name & "AI"
					End If
					If i = 2 OrElse i = 3 Then
						instanceName = name
					End If
				End If
			Next
		End Sub

		Public Overrides Function ToString() As String
			Dim sb As New StringBuilder()
			sb.AppendFormat("Script: {0}" & vbLf, name)
			For Each method As String In methods
				sb.Append("    ").Append(method).Append(vbLf)
			Next
			sb.Append(vbLf)
			Return sb.ToString()
		End Function
	End Class

	Private Shared Function GetMethod(method As String, ByRef txt As String, ByRef minPos As Integer) As String
		Dim res As String = Nothing
		Dim r As New Regex(method & "(\s|:|[(])")
		Dim m As Match = r.Match(txt)
		If m.Success Then
			Dim pos As Integer = m.Index
			While System.Math.Max(System.Threading.Interlocked.Decrement(pos),pos + 1) >= 0 AndAlso pos < txt.Length
				If txt(pos) = ControlChars.Lf Then
					Exit While
				End If
			End While
			'pos++;
			Dim lastPos As Integer = txt.IndexOf(vbLf & "}", pos)
			If lastPos <> -1 Then
				lastPos += 2
				While System.Math.Max(System.Threading.Interlocked.Increment(lastPos),lastPos - 1) >= 0 AndAlso lastPos < txt.Length
					If txt(lastPos) = ControlChars.Lf Then
						Exit While
					End If
				End While
				res = txt.Substring(pos, lastPos - pos)
				txt = txt.Remove(pos, lastPos - pos)
			End If
			If pos < minPos Then
				minPos = pos
			End If
		End If
		Return res
	End Function

	Private Shared Sub ProcessFile(filePath As String)
		Console.WriteLine(filePath)

		Dim txt As String = File.ReadAllText(filePath)
		Dim lines As String() = File.ReadAllLines(filePath)
		Array.Reverse(lines)

		Dim scripts As New ArrayList()
		Dim data As ScriptData = Nothing
		Dim scriptStart As Boolean = False
		For Each line As String In lines
            If line.IndexOf("Script*") <> -1 Then
                Exit For
            End If
			If line.IndexOf("->RegisterSelf();") <> -1 Then
				scriptStart = True
				data = New ScriptData()
				Continue For
			End If
			If scriptStart Then
				If line.IndexOf("= new Script") <> -1 Then
					scriptStart = False
					scripts.Add(data)
					data = Nothing
					Continue For
				End If
                Dim r As New Regex("pNewScript->([a-zA-Z]+) *= *&?([""_a-zA-Z0-9]+);")
				Dim m As Match = r.Match(line)
				If m.Success Then
					If m.Groups(1).Value.Equals("Name") Then
						data.name = m.Groups(2).Value.Trim(New Char() {""""C})
					Else
						data.AddMethod(m.Groups(2).Value)
					End If
				End If
				Continue For
			End If
		Next
		If scripts.Count <> 0 Then
			Dim register As String = ""
			For Each sd As ScriptData In scripts
				Dim ss As String = ""
				Console.WriteLine(sd)
				Dim minPos As Integer = txt.Length
				For Each method As String In sd.methods
					Dim s As String = GetMethod(method, txt, minPos)
					ss += s & vbLf
				Next
				If sd.instanceName IsNot Nothing Then
					Dim s As String = GetMethod("struct " & sd.instanceName, txt, minPos)
					ss += s & vbLf
				End If
				If sd.aiName IsNot Nothing Then
					Dim ai As String = GetMethod("struct " & sd.aiName, txt, minPos)
					If ai IsNot Nothing Then
						Dim sm As String = Nothing
						Dim r As New Regex("\S+ " & sd.aiName & "::([^( ]+)")
						While r.IsMatch(txt)
							Dim m As Match = r.Match(txt)
							Dim startPos As Integer = m.Index
							Dim endPos As Integer = txt.IndexOf(vbLf & "}", startPos)
							If endPos <> -1 Then
								endPos += 2
							End If
							While System.Math.Max(System.Threading.Interlocked.Increment(endPos),endPos - 1) >= 0 AndAlso endPos < txt.Length
								If txt(endPos) = ControlChars.Lf Then
									Exit While
								End If
							End While
							sm = txt.Substring(startPos, endPos - startPos)
							txt = txt.Remove(startPos, endPos - startPos)
							If sm IsNot Nothing Then
								sm = sm.Replace(vbLf, vbLf & "    ")
								Dim r1 As New Regex("\S+ " & Convert.ToString(m.Groups(1)) & " *\([^)]*\) *;")
								Dim m1 As Match = r1.Match(ai)
								If m1.Success Then
									ai = r1.Replace(ai, sm)
								End If
							End If
						End While
						ai = ai.Replace(sd.aiName & "::", "")
						ss += ai & vbLf
					End If
				End If
				If ss.Length <> 0 Then
					Dim typeName As String = "UnknownScript"
					Select Case sd.type
						Case 1
							typeName = "CreatureScript"
							Exit Select
						Case 2
							typeName = "InstanceMapScript"
							Exit Select
						Case Else
							If sd.name.IndexOf("npc") = 0 Then
								typeName = "CreatureScript"
							ElseIf sd.name.IndexOf("mob") = 0 Then
								typeName = "CreatureScript"
							ElseIf sd.name.IndexOf("boss_") = 0 Then
								typeName = "CreatureScript"
							ElseIf sd.name.IndexOf("item_") = 0 Then
								typeName = "ItemScript"
							ElseIf sd.name.IndexOf("go_") = 0 Then
								typeName = "GameObjectScript"
							ElseIf sd.name.IndexOf("at_") = 0 Then
								typeName = "AreaTriggerScript"
							ElseIf sd.name.IndexOf("instance_") = 0 Then
								typeName = "InstanceMapScript"
							End If
							Exit Select
					End Select
					If sd.instanceName IsNot Nothing Then
						ss = ss.Replace(sd.instanceName, sd.instanceName & "_InstanceMapScript")
					End If
					ss = ss.Replace(vbLf, vbLf & "    ")
					ss = "class " & sd.name & " : public " & typeName & vbLf & "{" & vbLf & "public:" & vbLf & "    " & sd.name & "() : " & typeName & "(""" & sd.name & """) { }" & vbLf & ss & vbLf & "};"
					ss = ss.Replace("_" & sd.name, "")
					ss = ss.Replace("AIAI", "AI")
					ss = ss.Replace("    " & vbCr & vbLf, vbCr & vbLf)
					ss = ss.Replace("    " & vbLf, vbLf)
					txt = txt.Insert(minPos, ss)
					register = "    new " & sd.name & "();" & vbLf & register
				End If
			Next
			Dim r2 As New Regex("void +AddSC_([_a-zA-Z0-9]+)")
			Dim m2 As Match = r2.Match(txt)
			If m2.Success Then
				txt = txt.Remove(m2.Index)
				txt += "void AddSC_" & m2.Groups(1).Value & "()" & vbLf & "{" & vbLf & register & "}" & vbLf
			End If
			' File.Copy(filePath, filePath + ".bkp");
			txt = txt.Replace(vbCr & vbLf, vbLf)
			File.WriteAllText(filePath, txt)
		End If
	End Sub
End Class
