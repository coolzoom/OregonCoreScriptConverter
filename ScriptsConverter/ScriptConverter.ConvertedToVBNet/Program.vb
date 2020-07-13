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
    ''' ��ű���صĺ���functionsƥ������ں������ĺ������� ��pNewScript->pProcessEventId = &ProcessEventTransports;
    ''' </summary>
    Public Shared aifuncmapping As New Dictionary(Of String, String)
    ''' <summary>
    ''' ��ű���صĺ���functionsƥ������ں������ĺ������� ��pNewScript->pProcessEventId = &ProcessEventTransports; pNewScript->pGossipHello = &GossipHello_npc_spirit_guide;
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
        ''' �ű�����ǰ׺script type  '0 "GetAI_", 1 "GetInstance_", 2 "GetInstanceData_"
        ''' </summary>
        Public type As Integer = 0
        ''' <summary>
        ''' �ű�����ǰ׺scriptname for this creature/go/item etc  mob_ npc_
        ''' </summary>
        Public targetname As String
        ''' <summary>
        ''' ��ű���صĺ���functions for this creature/go/item etc
        ''' </summary>
        Public aifunctions As New ArrayList()
        ''' <summary>
        ''' ��������instance name if it is instancemap
        ''' </summary>
        Public instanceName As String = Nothing
        ''' <summary>
        ''' GetAI�����AI���� ainame in the GetAI module
        ''' </summary>
        Public aiName As String = Nothing
        ''' <summary>
        ''' �ű�����ǰ׺���� prefix for different ai
        ''' </summary>
        Public scAIprefix As String() = New String() {"GetAI_", "GetInstance_", "GetInstanceData_"}

        ''' <summary>
        ''' �� "GetAI_", "GetInstance_", "GetInstanceData_" ���ַ����л�ȡ����ɽű�ai������ boss_xxxAI
        ''' </summary>
        ''' <param name="aifunc"></param>
        Public Sub AddFunction(aifunc As String)
            aifunctions.Add(aifunc)
            Dim i As Integer = 0
            For Each s As String In scAIprefix
                i += 1
                'check whether if the string include this aiprefix "GetAI_", "GetInstance_", "GetInstanceData_"
                '����ַ����Ƿ�����ű�����ǰ׺���ж������ֽű�
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
                        'һЩ�Ѿ�����AI�������AI
                        aiName = aiName.Replace("AIAI", "AI")
                    End If
                    If i = 2 OrElse i = 3 Then
                        instanceName = name
                    End If
                End If
            Next
        End Sub
        ''' <summary>
        ''' ��ӡ�ö�������к�������
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
    ''' ���ַ����л�ȡ���㺯�����ĺ�������
    ''' </summary>
    ''' <param name="method">������</param>
    ''' <param name="txtContent">cpp�ĵ����ݻ��ַ���</param>
    ''' <param name="minPos">cpp�ļ����ȣ������һ��λ��</param>
    ''' <returns></returns>
    Private Shared Function GetFunction(method As String, ByRef txtContent As String, ByRef minPos As Integer) As String
        Dim res As String = Nothing
        Dim r As New Regex(method & "(\s|:|[(])") 'ƥ�亯��������ո���߸�ð�Ż��߸����ŵ��ֶ�
        Dim m As Match = r.Match(txtContent)
        If m.Success Then
            Dim pos As Integer = m.Index
            'pos--,�Ӵ˴�����һ���س����м�Ϊ������ʼ
            While System.Math.Max(System.Threading.Interlocked.Decrement(pos), pos + 1) >= 0 AndAlso pos < txtContent.Length
                If txtContent(pos) = ControlChars.Lf Then
                    Exit While
                End If
            End While
            'pos++;�Ӵ˴�ѭ������һ��}λ�ü�Ϊ��������
            Dim lastPos As Integer = txtContent.IndexOf(vbLf & "}", pos) 'TODO �����Ƿ�Ӧ����};
            If lastPos <> -1 Then
                lastPos += 2
                While System.Math.Max(System.Threading.Interlocked.Increment(lastPos), lastPos - 1) >= 0 AndAlso lastPos < txtContent.Length
                    If txtContent(lastPos) = ControlChars.Lf Then
                        Exit While
                    End If
                End While
                '���ö������ݴ�
                res = txtContent.Substring(pos, lastPos - pos)
                '�Ƴ��Ѿ��������ַ���
                txtContent = txtContent.Remove(pos, lastPos - pos)
            End If
            '���ֶ��Ѿ���ȡ��ԭλ��Ӧ�ü���
            If pos < minPos Then
                minPos = pos
            End If

            '���ĺ������� ��pNewScript->pProcessEventId = &ProcessEventTransports; pNewScript->pGossipHello = &GossipHello_npc_spirit_guide;������override
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
    ''' cpp�ļ�����
    ''' </summary>
    ''' <param name="filePath"></param>
    Private Shared Sub ProcessFile(filePath As String)

        Console.WriteLine(filePath)

        Dim cppContent As String = File.ReadAllText(filePath)
        Dim lines As String() = File.ReadAllLines(filePath)
        Array.Reverse(lines)
        '��cpp�����Ľű��б�
        Dim scripts As New ArrayList()
        '�����ű����������Ϣ�ݴ�
        Dim data As ScriptData = Nothing
        '��AddSC�����ȡ�ű����͹��̣��˺���Ϊ��ʱֵ�ж��Ƿ�Ϊ�ű���ʼ
        Dim scriptStart As Boolean = False
        For Each line As String In lines
            '��Ϊ�ǵ�������RegisterSelfʱ����һ���ű��Ŀ�ʼ
            If line.IndexOf("->RegisterSelf();") <> -1 Then
                scriptStart = True
                data = New ScriptData()
                Continue For
            End If
            '�����ǰ���ڽ����ű��Ĺ���
            If scriptStart Then
                'ÿ���ű����һ��Ϊnew Script��öνű��Ѿ�������Ͽ�����ӵ��ű��б���
                If line.IndexOf("= new Script") <> -1 Then
                    scriptStart = False
                    scripts.Add(data)
                    data = Nothing
                    Continue For
                End If
                '�ӵ�ǰ��pNewScript->�����жϷ����͹���
                'һЩ���̷Ǳ�׼���̣���Ҫת���ɱ�׼AI ����' pNewScript->GetAI = &GetNewAIInstance<npc_spirit_guideAI>;
                If line.Contains("GetNewAIInstance<") And line.Contains("GetAI") Then
                    line = line.Replace("GetNewAIInstance<", "GetAI_")
                    line = line.Replace(">;", ";")
                End If
                '��׼AIӦ������
                'pNewScript->GetAI = &GetAI_npc_professor_phizzlethorpe;
                Dim r As New Regex("pNewScript->([a-zA-Z]+) *= *&?([""_a-zA-Z0-9]+);")


                Dim m As Match = r.Match(line)
                If m.Success Then

                    If m.Groups(1).Value.Equals("Name") Then
                        '�����pNewScript->Name����ӽű�����pNewScript-> Name = "npc_spawned_oronok_tornheart";
                        data.targetname = m.Groups(2).Value.Trim(New Char() {""""c})
                    Else
                        '���������������Ϊ������غ�������ֵ���������� "GetAI_", "GetInstance_", "GetInstanceData_" ���ַ����л�ȡ����ɽű�ai������ boss_xxxAI
                        '�������������������
                        'pNewScript-> GetAI = & GetAI_npc_spawned_oronok_tornheart;
                        'pNewScript-> pGossipHello =  & GossipHello_npc_spawned_oronok_tornheart;
                        'pNewScript-> pGossipSelect = & GossipSelect_npc_spawned_oronok_tornheart;
                        data.AddFunction(m.Groups(2).Value)
                        AddFuncMapping(m.Groups(2).Value, m.Groups(1).Value)
                    End If
                End If
                Continue For
            End If
            '��ǰ�ļ���AddSC��β����Ϊ�ǵ���
            If line.IndexOf("Script*") <> -1 Then
                Exit For
            End If
        Next
        Dim strErrorMsg As String = ""
        '�����������Ϊ��
        If scripts.Count <> 0 Then
            Dim register As String = ""
            'ѭ���ű��б�
            For Each scInfo As ScriptData In scripts
                '�����ݴ�Ľű����й����ַ���
                Dim scsring As String = ""
                '����������͹�����Ϣ
                Console.WriteLine(scInfo)
                '�����ĵ����һ���ַ�λ�����ڻ�ȡ����
                Dim minPos As Integer = cppContent.Length
                'ѭ��ÿ���������ֱ������ĵ��л�ȡ���̶�
                For Each aifunction As String In scInfo.aifunctions
                    Dim s As String = GetFunction(aifunction, cppContent, minPos)
                    If s = "" Then
                        strErrorMsg += String.Format("Error GetFunction {0}", aifunction) & vbLf
                    End If
                    scsring += s & vbLf
                Next
                '����Ǹ���
                If scInfo.instanceName IsNot Nothing Then
                    '��structure����
                    Dim s As String = GetFunction("struct " & scInfo.instanceName, cppContent, minPos)
                    If s = "" Then
                        strErrorMsg += String.Format("Error GetFunction {0}", "struct " & scInfo.instanceName) & vbLf
                    End If
                    scsring += s & vbLf
                End If
                '���������ai
                If scInfo.aiName IsNot Nothing Then
                    '��structure����
                    Dim ai As String = GetFunction("struct " & scInfo.aiName, cppContent, minPos)
                    If ai = "" Then
                        strErrorMsg += String.Format("Error GetFunction {0}", "struct " & scInfo.instanceName) & vbLf
                    End If
                    '�����structure����
                    If ai IsNot Nothing Then
                        Dim sm As String = Nothing
                        '�����ai��ص��Ӻ���
                        Dim r As New Regex("\S+ " & scInfo.aiName & "::([^( ]+)") '�ǿո�(��ͷ)���ai����˫���ŵ��ֶ�
                        While r.IsMatch(cppContent)
                            Dim m As Match = r.Match(cppContent)
                            Dim startPos As Integer = m.Index
                            Dim endPos As Integer = cppContent.IndexOf(vbLf & "}", startPos) 'TODO �����Ƿ�Ӧ����};
                            If endPos <> -1 Then
                                endPos += 2
                            End If
                            While System.Math.Max(System.Threading.Interlocked.Increment(endPos), endPos - 1) >= 0 AndAlso endPos < cppContent.Length
                                If cppContent(endPos) = ControlChars.Lf Then
                                    Exit While
                                End If
                            End While
                            '�ӹ�������
                            sm = cppContent.Substring(startPos, endPos - startPos)
                            '�Ƴ��Ѿ�ƥ���������
                            cppContent = cppContent.Remove(startPos, endPos - startPos)
                            If sm IsNot Nothing Then
                                sm = sm.Replace(vbLf, vbLf & "    ")
                                Dim r1 As New Regex("\S+ " & Convert.ToString(m.Groups(1)) & " *\([^)]*\) *;")
                                Dim m1 As Match = r1.Match(ai)
                                If m1.Success Then
                                    '��ԭ���Ĺ����滻Ϊ�µĹ���
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

                    scsring = scsring.Replace("_" & scInfo.targetname, "") 'ɾ����������ai��ص������罫GossipHello_npc_spirit_guide��ΪGossipHello

                    scsring = scsring.Replace("AIAI", "AI")
                    scsring = scsring.Replace("    " & vbCr & vbLf, vbCr & vbLf)
                    scsring = scsring.Replace("    " & vbLf, vbLf)
                    cppContent = cppContent.Insert(minPos, scsring)
                    register = "    new " & scInfo.targetname & "();" & vbLf & register
                End If
            Next
            '��ȡRegisterSpellScript<��RegisterAuraScript<
            Dim strOther As String = ""
            For Each strl As String In lines
                If strl.Contains("RegisterSpellScript<") Or strl.Contains("RegisterAuraScript<") Then
                    strOther &= strl & vbLf
                End If
            Next
            '��ȡAddSC����Ϣ
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
