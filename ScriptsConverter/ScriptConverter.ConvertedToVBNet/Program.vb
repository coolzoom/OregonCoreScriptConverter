Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO

Class Program
    Public Shared DictFunctionMapping As Dictionary(Of String, String) = New Dictionary(Of String, String) _
        From {
    {"pGossipHello", "OnGossipHello"},
    {"pGossipHelloGO", "OnGossipHello"},
    {"pGossipSelect", "OnGossipSelect"},
    {"pGossipSelectGO", "OnGossipSelect"},
    {"pGossipSelectWithCode", "OnGossipSelectCode"},
    {"pGossipSelectGOWithCode", "OnGossipSelectCode"},
    {"pDialogStatusNPC", "GetDialogStatus"},
    {"pDialogStatusGO", "GetDialogStatus"},
    {"pQuestAcceptNPC", "OnQuestAccept"},
    {"pQuestAcceptGO", "OnQuestAccept"},
    {"pQuestAcceptItem", "OnQuestAccept"},
    {"pQuestRewardedNPC", "OnQuestRewarded"},
    {"pQuestRewardedGO", "OnQuestRewarded"},
    {"pGOUse", "OnGameObjectUse"},
    {"pItemUse", "OnItemUse"},
    {"pItemLoot", "OnItemLoot"},
    {"pAreaTrigger", "OnAreaTrigger"},
    {"pProcessEventId", "OnProcessEvent"},
    {"pEffectDummyNPC", "OnEffectDummy"},
    {"pEffectDummyGO", "OnEffectDummy"},
    {"pEffectDummyItem", "OnEffectDummy"},
    {"pEffectScriptEffectNPC", "OnEffectScriptEffect"},
    {"pEffectAuraDummy", "OnAuraDummy"},
    {"pTrapSearching", "OnTrapSearch"}
    }
    Friend Shared Sub Main(args As String())
        Dim path As String = ""
        If args.Length <> 1 Then
            Console.WriteLine("Usage: ScriptsConverter.exe [path_to_dir|path_to_file], now using default path")
            path = "F:\WOWServer\Source\mangos-tbc\src\game\AI"
        Else
            path = args(0)

        End If

        If File.Exists(path) Then
            ProcessFile(path)
        ElseIf Directory.Exists(path) Then
            ProcessDirectory(path)
        Else
            Console.WriteLine("Invalid file or directory specified." & vbCr & vbLf & vbCr & vbLf & "Usage: ScriptsConverter.exe [path_to_dir|path_to_file]")
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

    ''' <summary>
    ''' 与脚本相关的函数functions匹配表，用于后续更改函数名称 如pNewScript->pProcessEventId = &ProcessEventTransports;
    ''' </summary>
    Public Shared aifuncmapping As New Dictionary(Of String, String)
    ''' <summary>
    ''' 与脚本相关的函数functions匹配表，用于后续更改函数名称 如pNewScript->pProcessEventId = &ProcessEventTransports; pNewScript->pGossipHello = &GossipHello_npc_spirit_guide;
    ''' </summary>
    ''' <param name="func">GossipSelect_npc_spawned_oronok_tornheart</param>
    ''' <param name="funcname">pGossipHello</param>
    Public Shared Sub AddFuncMapping(func As String, funcname As String)
        If Not aifuncmapping.ContainsKey(func) Then
            aifuncmapping.Add(func, funcname)
        End If

    End Sub
    Private Class ScriptData
        ''' <summary>
        ''' 脚本类型前缀script type  '0 "GetAI_", 1 "GetInstance_", 2 "GetInstanceData_"
        ''' </summary>
        Public type As Integer = 0
        ''' <summary>
        ''' 脚本名称前缀scriptname for this creature/go/item etc  mob_ npc_
        ''' </summary>
        Public targetname As String
        ''' <summary>
        ''' 与脚本相关的函数functions for this creature/go/item etc
        ''' </summary>
        Public aifunctions As New ArrayList()
        ''' <summary>
        ''' 副本名称instance name if it is instancemap
        ''' </summary>
        Public instanceName As String = Nothing
        ''' <summary>
        ''' GetAI里面的AI名称 ainame in the GetAI module
        ''' </summary>
        Public aiName As String = Nothing
        ''' <summary>
        ''' 脚本类型前缀数组 prefix for different ai
        ''' </summary>
        Public scAIprefix As String() = New String() {"GetAI_", "GetInstance_", "GetInstanceData_"}

        ''' <summary>
        ''' 从 "GetAI_", "GetInstance_", "GetInstanceData_" 等字符串中获取并组成脚本ai名，如 boss_xxxAI
        ''' </summary>
        ''' <param name="aifunc"></param>
        Public Sub AddFunction(aifunc As String)
            aifunctions.Add(aifunc)
            Dim i As Integer = 0
            For Each s As String In scAIprefix
                i += 1
                'check whether if the string include this aiprefix "GetAI_", "GetInstance_", "GetInstanceData_"
                '检查字符串是否包含脚本类型前缀以判断是哪种脚本
                Dim pos As Integer = aifunc.IndexOf(s)
                If pos <> -1 Then
                    type = i
                    'get the ainame
                    Dim name As String = aifunc.Substring(pos + s.Length)
                    If i = 1 Then
                        'debug
                        If name.Contains("npc_spirit_guide") Then
                            MsgBox("")
                        End If
                        'TODO, some AI dont have AI behind, so may not able to get the struct data
                        aiName = name & "AI"
                        '一些已经包含AI则不再添加AI
                        aiName = aiName.Replace("AIAI", "AI")
                    End If
                    If i = 2 OrElse i = 3 Then
                        instanceName = name
                    End If
                End If
            Next
        End Sub
        ''' <summary>
        ''' 打印该对象的所有函数方法
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            Dim sb As New StringBuilder()
            sb.AppendFormat("Script: {0}" & vbLf, targetname)
            For Each method As String In aifunctions
                sb.Append("    ").Append(method).Append(vbLf)
            Next
            sb.Append(vbLf)
            Return sb.ToString()
        End Function
    End Class
    ''' <summary>
    ''' 从字符段中获取满足函数名的函数过程
    ''' </summary>
    ''' <param name="method">函数名</param>
    ''' <param name="txtContent">cpp文档内容或字符串</param>
    ''' <param name="minPos">cpp文件长度，即最后一个位置</param>
    ''' <returns></returns>
    Private Shared Function GetFunction(method As String, ByRef txtContent As String, ByRef minPos As Integer) As String
        Dim res As String = Nothing
        Dim r As New Regex(method & "(\s|:|[(])") '匹配函数名后跟空格或者跟冒号或者跟括号的字段
        Dim m As Match = r.Match(txtContent)
        If m.Success Then
            Dim pos As Integer = m.Index
            'pos--,从此处找上一个回车换行即为函数开始
            While System.Math.Max(System.Threading.Interlocked.Decrement(pos), pos + 1) >= 0 AndAlso pos < txtContent.Length
                If txtContent(pos) = ControlChars.Lf Then
                    Exit While
                End If
            End While
            'pos++;从此处循环找下一个}位置即为函数结束
            Dim lastPos As Integer = txtContent.IndexOf(vbLf & "}", pos) 'TODO 这里是否应该是};
            If lastPos <> -1 Then
                lastPos += 2
                While System.Math.Max(System.Threading.Interlocked.Increment(lastPos), lastPos - 1) >= 0 AndAlso lastPos < txtContent.Length
                    If txtContent(lastPos) = ControlChars.Lf Then
                        Exit While
                    End If
                End While
                '将该段内容暂存
                res = txtContent.Substring(pos, lastPos - pos)
                '移除已经解析的字符段
                txtContent = txtContent.Remove(pos, lastPos - pos)
            End If
            '因字段已经截取，原位置应该减少
            If pos < minPos Then
                minPos = pos
            End If

            '更改函数名称 如pNewScript->pProcessEventId = &ProcessEventTransports; pNewScript->pGossipHello = &GossipHello_npc_spirit_guide;并增加override
            For Each skeypair As KeyValuePair(Of String, String) In aifuncmapping
                If res.Contains(skeypair.Key) Then
                    If DictFunctionMapping.ContainsKey(skeypair.Value) Then
                        res = res.Replace(skeypair.Key, DictFunctionMapping(skeypair.Value))
                        res = res.Replace(")" & vbCr, ") override" & vbCr)
                        res = res.Replace(")" & vbLf, ") override" & vbLf)
                        res = res.Replace(")" & vbCrLf, ") override" & vbCrLf)
                    End If

                End If
            Next
        End If



        Return res
    End Function
    ''' <summary>
    ''' cpp文件处理
    ''' </summary>
    ''' <param name="filePath"></param>
    Private Shared Sub ProcessFile(filePath As String)

        Console.WriteLine(filePath)

        Dim cppContent As String = File.ReadAllText(filePath)
        Dim lines As String() = File.ReadAllLines(filePath)
        Array.Reverse(lines)
        '该cpp包含的脚本列表
        Dim scripts As New ArrayList()
        '单个脚本所有相关信息暂存
        Dim data As ScriptData = Nothing
        '从AddSC里面获取脚本名和过程，此函数为暂时值判断是否为脚本开始
        Dim scriptStart As Boolean = False
        For Each line As String In lines
            '因为是倒序，所以RegisterSelf时便是一个脚本的开始
            If line.IndexOf("->RegisterSelf();") <> -1 Then
                scriptStart = True
                data = New ScriptData()
                Continue For
            End If
            '如果当前处于解析脚本的过程
            If scriptStart Then
                '每个脚本最后一个为new Script则该段脚本已经解析完毕可以添加到脚本列表中
                If line.IndexOf("= new Script") <> -1 Then
                    scriptStart = False
                    scripts.Add(data)
                    data = Nothing
                    Continue For
                End If
                '从当前行pNewScript->后面判断方法和过程
                '一些过程非标准过程，需要转换成标准AI 例如' pNewScript->GetAI = &GetNewAIInstance<npc_spirit_guideAI>;
                If line.Contains("GetNewAIInstance<") And line.Contains("GetAI") Then
                    line = line.Replace("GetNewAIInstance<", "GetAI_")
                    line = line.Replace(">;", ";")
                End If
                '标准AI应该如下
                'pNewScript->GetAI = &GetAI_npc_professor_phizzlethorpe;
                Dim r As New Regex("pNewScript->([a-zA-Z]+) *= *&?([""_a-zA-Z0-9]+);")


                Dim m As Match = r.Match(line)
                If m.Success Then

                    If m.Groups(1).Value.Equals("Name") Then
                        '如果是pNewScript->Name则添加脚本名‘pNewScript-> Name = "npc_spawned_oronok_tornheart";
                        data.targetname = m.Groups(2).Value.Trim(New Char() {""""c})
                    Else
                        '如果是其他名字则为其他相关函数，则赋值函数名，从 "GetAI_", "GetInstance_", "GetInstanceData_" 等字符串中获取并组成脚本ai名，如 boss_xxxAI
                        '如果不在这个表里则不添加
                        'pNewScript-> GetAI = & GetAI_npc_spawned_oronok_tornheart;
                        'pNewScript-> pGossipHello =  & GossipHello_npc_spawned_oronok_tornheart;
                        'pNewScript-> pGossipSelect = & GossipSelect_npc_spawned_oronok_tornheart;
                        data.AddFunction(m.Groups(2).Value)
                        AddFuncMapping(m.Groups(2).Value, m.Groups(1).Value)
                    End If
                End If
                Continue For
            End If
            '当前文件的AddSC结尾，因为是倒序
            If line.IndexOf("Script*") <> -1 Then
                Exit For
            End If
        Next
        Dim strErrorMsg As String = ""
        '如果函数名不为空
        If scripts.Count <> 0 Then
            Dim register As String = ""
            '循环脚本列表
            For Each scInfo As ScriptData In scripts
                '定义暂存的脚本所有过程字符串
                Dim scsring As String = ""
                '输出函数名和过程信息
                Console.WriteLine(scInfo)
                '定义文档最后一个字符位置用于获取内容
                Dim minPos As Integer = cppContent.Length
                '循环每个过程名字表，并从文档中获取过程段
                For Each aifunction As String In scInfo.aifunctions
                    Dim s As String = GetFunction(aifunction, cppContent, minPos)
                    If s = "" Then
                        strErrorMsg += String.Format("Error GetFunction {0}", aifunction) & vbLf
                    End If
                    scsring += s & vbLf
                Next
                '如果是副本
                If scInfo.instanceName IsNot Nothing Then
                    '找structure函数
                    Dim s As String = GetFunction("struct " & scInfo.instanceName, cppContent, minPos)
                    If s = "" Then
                        strErrorMsg += String.Format("Error GetFunction {0}", "struct " & scInfo.instanceName) & vbLf
                    End If
                    scsring += s & vbLf
                End If
                '如果是生物ai
                If scInfo.aiName IsNot Nothing Then
                    '找structure函数
                    Dim ai As String = GetFunction("struct " & scInfo.aiName, cppContent, minPos)
                    If ai = "" Then
                        strErrorMsg += String.Format("Error GetFunction {0}", "struct " & scInfo.instanceName) & vbLf
                    End If
                    '如果找structure函数
                    If ai IsNot Nothing Then
                        Dim sm As String = Nothing
                        '找与该ai相关的子函数
                        Dim r As New Regex("\S+ " & scInfo.aiName & "::([^( ]+)") '非空格(开头)后带ai名带双引号的字段
                        While r.IsMatch(cppContent)
                            Dim m As Match = r.Match(cppContent)
                            Dim startPos As Integer = m.Index
                            Dim endPos As Integer = cppContent.IndexOf(vbLf & "}", startPos) 'TODO 这里是否应该是};
                            If endPos <> -1 Then
                                endPos += 2
                            End If
                            While System.Math.Max(System.Threading.Interlocked.Increment(endPos), endPos - 1) >= 0 AndAlso endPos < cppContent.Length
                                If cppContent(endPos) = ControlChars.Lf Then
                                    Exit While
                                End If
                            End While
                            '子过程内容
                            sm = cppContent.Substring(startPos, endPos - startPos)
                            '移除已经匹配过的内容
                            cppContent = cppContent.Remove(startPos, endPos - startPos)
                            If sm IsNot Nothing Then
                                sm = sm.Replace(vbLf, vbLf & "    ")
                                Dim r1 As New Regex("\S+ " & Convert.ToString(m.Groups(1)) & " *\([^)]*\) *;")
                                Dim m1 As Match = r1.Match(ai)
                                If m1.Success Then
                                    '将原来的过程替换为新的过程
                                    ai = r1.Replace(ai, sm)
                                End If
                            End If
                        End While
                        ai = ai.Replace(scInfo.aiName & "::", "")
                        scsring += ai & vbLf
                    End If
                End If
                If scsring.Length <> 0 Then
                    Dim typeName As String = "UnknownScript"
                    Select Case scInfo.type
                        Case 1
                            typeName = "CreatureScript"
                            Exit Select
                        Case 2
                            typeName = "InstanceMapScript"
                            Exit Select
                        Case Else
                            If scInfo.targetname.IndexOf("npc") = 0 Then
                                typeName = "CreatureScript"
                            ElseIf scInfo.targetname.IndexOf("mob") = 0 Then
                                typeName = "CreatureScript"
                            ElseIf scInfo.targetname.IndexOf("boss_") = 0 Then
                                typeName = "CreatureScript"
                            ElseIf scInfo.targetname.IndexOf("item_") = 0 Then
                                typeName = "ItemScript"
                            ElseIf scInfo.targetname.IndexOf("go_") = 0 Then
                                typeName = "GameObjectScript"
                            ElseIf scInfo.targetname.IndexOf("at_") = 0 Then
                                typeName = "AreaTriggerScript"
                            ElseIf scInfo.targetname.IndexOf("instance_") = 0 Then
                                typeName = "InstanceMapScript"
                            End If
                            Exit Select
                    End Select
                    If scInfo.instanceName IsNot Nothing Then
                        scsring = scsring.Replace(scInfo.instanceName, scInfo.instanceName & "_InstanceMapScript")
                    End If
                    scsring = scsring.Replace(vbLf, vbLf & "    ")
                    scsring = "class " & scInfo.targetname & " : public " & typeName & vbLf & "{" & vbLf & "public:" & vbLf & "    " & scInfo.targetname & "() : " & typeName & "(""" & scInfo.targetname & """) { }" & vbLf & scsring & vbLf & "};"

                    scsring = scsring.Replace("_" & scInfo.targetname, "") '删除过程中与ai相关的名字如将GossipHello_npc_spirit_guide改为GossipHello

                    scsring = scsring.Replace("AIAI", "AI")
                    scsring = scsring.Replace("    " & vbCr & vbLf, vbCr & vbLf)
                    scsring = scsring.Replace("    " & vbLf, vbLf)
                    cppContent = cppContent.Insert(minPos, scsring)
                    register = "    new " & scInfo.targetname & "();" & vbLf & register
                End If
            Next
            '获取RegisterSpellScript<与RegisterAuraScript<
            Dim strOther As String = ""
            For Each strl As String In lines
                If strl.Contains("RegisterSpellScript<") Or strl.Contains("RegisterAuraScript<") Then
                    strOther &= strl & vbLf
                End If
            Next
            '获取AddSC段信息
            Dim r2 As New Regex("void +AddSC_([_a-zA-Z0-9]+)")
            Dim m2 As Match = r2.Match(cppContent)
            If m2.Success Then
                cppContent = cppContent.Remove(m2.Index)
                cppContent += "void AddSC_" & m2.Groups(1).Value & "()" & vbLf & "{" & vbLf & register & vbLf & strOther & "}" & vbLf
            End If
            ' File.Copy(filePath, filePath + ".bkp");
            cppContent = cppContent.Replace(vbCr & vbLf, vbLf)
            File.WriteAllText(filePath, cppContent)

            'error message
            If strErrorMsg <> "" Then
                File.WriteAllText(filePath & ".err", strErrorMsg)
            End If
        End If
    End Sub
End Class
