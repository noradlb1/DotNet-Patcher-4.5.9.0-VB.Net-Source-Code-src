﻿Imports Mono.Cecil
Imports Helper.CecilHelper
Imports Helper.RandomizeHelper
Imports Implementer.Core.Resources
Imports Implementer.Core.Versions
Imports Implementer.Core.ManifestRequest
Imports Implementer.Core.Packer
Imports Implementer.Core.IconChanger
Imports Implementer.Core.Obfuscation.Anti
Imports Implementer.Core.Obfuscation.Builder.Source
Imports Implementer.Core.Obfuscation.Protection
Imports Implementer.Engine.Context
Imports System.Drawing
Imports System.IO

Namespace Engine.Processing

    ''' <summary>
    ''' INFO : This is the third step of the renamer library. 
    '''        This will process to rename types and members from the selected assembly with settings of your choice.
    ''' </summary>
    Public NotInheritable Class ProcessTask

#Region " Fields "
        Private m_RenamingAccept As RenamerState
#End Region

#Region " Constructor "
        ''' <summary>
        ''' INFO : Initializes a new instance of the Processing.Cls_Processing class from which is started the task of renaming.
        ''' </summary>
        ''' <param name="RenamingAccept"></param>
        Public Sub New(RenamingAccept As RenamerState)
            m_RenamingAccept = RenamingAccept
            RandomizerType.RenameSetting = RenamingAccept.RenamingType
        End Sub
#End Region

#Region " Methods "

        ''' <summary>
        ''' INFO : This is the beginning of the renamer method ! Namespaces, Types and Resources renaming.
        ''' </summary>
        ''' <param name="type"></param>
        Public Sub ProcessType(type As TypeDefinition)

            If m_RenamingAccept.CustomAttributes Then
                Utils.RemoveCustomAttributeByName(type, "ObsoleteAttribute")
                Utils.RemoveCustomAttributeByName(type, "DescriptionAttribute")
                Utils.RemoveCustomAttributeByName(type, "CategoryAttribute")
            End If

            Dim NamespaceOriginal As String = type.Namespace
            Dim NamespaceObfuscated As String = type.Namespace

            If Not type.Name = "<Module>" Then
                If m_RenamingAccept.Namespaces Then
                    NamespaceObfuscated = If(m_RenamingAccept.ReplaceNamespacesSetting And RenamerState.ReplaceNamespaces.Empty, String.Empty, Randomizer.GenerateNew())
                    type.Namespace = Mapping.RenameTypeDef(type, NamespaceObfuscated, True)
                End If
            End If

            If NameChecker.IsRenamable(type) Then
                Dim TypeOriginal As String = type.Name
                Dim TypeObfuscated As String = type.Name

                If m_RenamingAccept.Types Then
                    type.Name = Mapping.RenameTypeDef(type, Randomizer.GenerateNew())
                    TypeObfuscated = type.Name
                    Renamer.RenameResources(type, NamespaceOriginal, NamespaceObfuscated, TypeOriginal, TypeObfuscated)
                End If

                If m_RenamingAccept.Namespaces Then
                    type.Namespace = Mapping.RenameTypeDef(type, NamespaceObfuscated, True)
                    Renamer.RenameResources(type, NamespaceOriginal, NamespaceObfuscated, TypeOriginal, TypeObfuscated)
                End If

                If m_RenamingAccept.Properties Then Renamer.RenameResourceManager(type)

                If m_RenamingAccept.Types OrElse m_RenamingAccept.Namespaces Then
                    Renamer.RenameInitializeComponentsValues(type, TypeOriginal, TypeObfuscated, False)
                End If
            End If
        End Sub

        Public Sub ProcessMildCalls(AssDef As AssemblyDefinition, frmwk$, pack As Boolean)
            Mild.DoJob(AssDef, frmwk, m_RenamingAccept.ExclusionRule, pack)
        End Sub

        Public Sub ProcessHidePinvokeCalls(AssDef As AssemblyDefinition, frmwk$, pack As Boolean)
            Pinvoke.DoJob(AssDef, frmwk, m_RenamingAccept.ExclusionRule, pack)
        End Sub

        Public Sub ProcessControlFlow(AssDef As AssemblyDefinition, frmwk$, pack As Boolean)
            ControlFlow.DoJob(AssDef, m_RenamingAccept.ExclusionRule, frmwk, pack)
        End Sub

        Public Sub ProcessConstants(AssDef As AssemblyDefinition)
            Constants.DoJob(AssDef, m_RenamingAccept.ExclusionRule)
        End Sub

        Public Sub ProcessEncryptString(AssDef As AssemblyDefinition, frmwk$, EncryptToResources As EncryptType, pack As Boolean)
            Str.DoJob(AssDef, frmwk, If(pack = True, EncryptType.ByDefault, EncryptToResources), m_RenamingAccept.ExclusionRule, pack)
        End Sub

        Public Sub ProcessEncryptBoolean(AssDef As AssemblyDefinition, frmwk$, EncryptToResources As EncryptType, pack As Boolean)
            Bool.DoJob(AssDef, frmwk, If(pack = True, EncryptType.ByDefault, EncryptToResources), m_RenamingAccept.ExclusionRule, pack)
        End Sub

        Public Sub ProcessEncryptNumeric(AssDef As AssemblyDefinition, frmwk$, EncryptToResources As EncryptType, pack As Boolean)
            Numeric.DoJob(AssDef, frmwk, If(pack = True, EncryptType.ByDefault, EncryptToResources), m_RenamingAccept.ExclusionRule, pack)
        End Sub

        Public Sub ProcessRenameResourcesContent(AssDef As AssemblyDefinition)
            Content.Rename(AssDef)
        End Sub

        Public Sub ProcessAntiDebug(AssDef As AssemblyDefinition, Frmwk$, pack As Boolean)
            AntiDebug.InjectAntiDebug(AssDef, Frmwk, pack)
        End Sub

        Public Sub ProcessAntiDumper(AssDef As AssemblyDefinition, pack As Boolean)
            AntiDumper.CreateAntiDumperClass(AssDef, pack)
        End Sub

        Public Sub ProcessPreAntiTamper(AssDef As AssemblyDefinition, Frmwk$, Pack As Boolean)
            AntiTamper.CreateAntiTamperClass(AssDef, Frmwk, Pack)
        End Sub

        Public Sub ProcessPostAntiTamper(FilePath$)
            AntiTamper.InjectMD5(FilePath)
        End Sub

        Public Sub ProcessAntiIlDasm(AssDef As AssemblyDefinition)
            AntiIlDasm.Inject(AssDef)
        End Sub

        Public Sub ProcessVersionInfos(FilePath$, Frmwk$, vInfos As Infos)
            Injector.InjectAssemblyVersionInfos(FilePath, vInfos)
        End Sub

        Public Sub ProcessManifest(FilePath$, CurrentRequested As String)
            ManifestWriter.ApplyManifest(FilePath, CurrentRequested)
        End Sub

        Public Sub ProcessPacker(FilePath$, SevenZipResName$, Frmwk$, pInfos As PackInfos, vInfos As Infos)
            Dim pack As New Pack(FilePath)
            With pack
                .CreateStub(Frmwk, SevenZipResName)
                .ReplaceIcon(pInfos.NewIcon)
                .InjectAssemblyVersionInfos(vInfos)
                .InjectManifest(pInfos.RequestedLevel)
            End With
        End Sub

        Public Sub ProcessIconChanger(FilePath$, Frmwk$, NewIconPath As Icon, vInfos As Infos)
            Replacer.ReplaceFromIcon(FilePath, NewIconPath)
            Injector.InjectAssemblyVersionInfos(FilePath, vInfos)
        End Sub

        Public Sub ProcessInjectWatermark(AssDef As AssemblyDefinition, ByVal Pack As Boolean)
            Attribut.DoInjection(AssDef, Pack)
        End Sub

        Public Sub ProcessInvalidMetadata(AssDef As AssemblyDefinition, psr As MetadataProcessor)
            InvalidMetadata.DoJob(AssDef, psr)
        End Sub

        ''' <summary>
        ''' INFO : Methods, Parameters and Variables renamer routine.
        ''' </summary>
        ''' <param name="type"></param>
        Public Sub ProcessMethods(type As TypeDefinition)
            If m_RenamingAccept.Methods OrElse m_RenamingAccept.Parameters Then
                For Each method As MethodDefinition In type.Methods
                    If m_RenamingAccept.CustomAttributes Then
                        Utils.RemoveCustomAttributeByName(method, "ObsoleteAttribute")
                        Utils.RemoveCustomAttributeByName(method, "DescriptionAttribute")
                        Utils.RemoveCustomAttributeByName(method, "CategoryAttribute")
                    End If
                    If NameChecker.IsRenamable(method) Then
                        'If method.IsNewSlot AndAlso method.IsVirtual Then
                        '    MsgBox(method.FullName)
                        'End If
                        'If method.Name = "GetTask" Then
                        '    MsgBox("HasOverrides : " & method.HasOverrides.ToString & vbNewLine)
                        'End If
                        If Not Finder.AccessorMethods(type).Contains(method) Then ProcessMethod(method, "Methods")
                    Else
                        If m_RenamingAccept.Parameters Then
                            ProcessParameters(method)
                        End If
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' INFO : Properties, CustomAttributes (Only "AccessedThroughPropertyAttribute" attribute) renamer routine. 
        ''' </summary>
        ''' <param name="type"></param>
        Public Sub ProcessProperties(type As TypeDefinition)
            If m_RenamingAccept.Properties OrElse m_RenamingAccept.CustomAttributes OrElse m_RenamingAccept.Fields Then
                For Each propDef As PropertyDefinition In type.Properties
                    If m_RenamingAccept.CustomAttributes Then
                        Utils.RemoveCustomAttributeByName(propDef, "ObsoleteAttribute")
                        Utils.RemoveCustomAttributeByName(propDef, "DescriptionAttribute")
                        Utils.RemoveCustomAttributeByName(propDef, "CategoryAttribute")
                    End If
                    If NameChecker.IsRenamable(propDef) Then

                        Dim originalN = propDef.Name
                        Dim obfuscatedN = propDef.Name

                        If m_RenamingAccept.Properties Then
                            obfuscatedN = Randomizer.GenerateNew()
                        End If

                        Renamer.RenameProperty(propDef, obfuscatedN)
                        Renamer.RenameInitializeComponentsValues(propDef.DeclaringType, originalN, obfuscatedN, True)
                        Renamer.RenameSettings(propDef.GetMethod, originalN, obfuscatedN)
                        Renamer.RenameSettings(propDef.SetMethod, originalN, obfuscatedN)

                        If m_RenamingAccept.Fields Then
                            Renamer.RenameFields(type, propDef, originalN, obfuscatedN)
                        End If

                        If m_RenamingAccept.Methods Then
                            Dim flag = "Property"
                            If Not propDef.GetMethod Is Nothing Then ProcessMethod(propDef.GetMethod, flag)
                            If Not propDef.SetMethod Is Nothing Then ProcessMethod(propDef.SetMethod, flag)
                            For Each def In propDef.OtherMethods
                                ProcessMethod(def, flag)
                            Next
                        End If
                        'Else
                        '    If propDef.DeclaringType.IsSerializable Then

                        '    End If
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' INFO : Fields renamer routine. 
        ''' </summary>
        ''' <param name="type"></param>
        Public Sub ProcessFields(type As TypeDefinition)
            If m_RenamingAccept.Fields Then
                For Each field As FieldDefinition In type.Fields
                    If field.HasConstant AndAlso field.IsStatic Then
                        field.HasConstant = False
                    End If
                    If Not Finder.HasCustomAttributeByName(field, "AccessedThroughPropertyAttribute") Then
                        If NameChecker.IsRenamable(field) Then Renamer.RenameField(field, Randomizer.GenerateNew())
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' INFO : Events renamer routine. 
        ''' </summary>
        ''' <param name="type"></param>
        Public Sub ProcessEvents(type As TypeDefinition)
            If m_RenamingAccept.Events Then
                For Each events As EventDefinition In type.Events

                    If m_RenamingAccept.CustomAttributes Then
                        Utils.RemoveCustomAttributeByName(events, "ObsoleteAttribute")
                        Utils.RemoveCustomAttributeByName(events, "DescriptionAttribute")
                        Utils.RemoveCustomAttributeByName(events, "CategoryAttribute")
                    End If

                    'MsgBox("IsDefinition : " & events.IsDefinition.ToString & vbNewLine &
                    '       "IsRuntimeSpecialName : " & events.IsRuntimeSpecialName.ToString & vbNewLine &
                    '       "IsSpecialName : " & events.IsSpecialName.ToString & vbNewLine)

                    If NameChecker.IsRenamable(events) Then
                        Dim obfName As String = Randomizer.GenerateNew()
                        Renamer.RenameEvent(events, obfName)

                        Dim flag = "Event"
                        If Not events.AddMethod Is Nothing Then ProcessMethod(events.AddMethod, flag)
                        If Not events.RemoveMethod Is Nothing Then ProcessMethod(events.RemoveMethod, flag)
                        For Each def In events.OtherMethods
                            ProcessMethod(def, flag)
                        Next
                    End If
                Next
            End If
        End Sub

        Private Sub ProcessMethod(mDef As MethodDefinition, DestNodeName$)
            Dim meth As MethodDefinition = mDef
            If m_RenamingAccept.Methods Then
                If DestNodeName = "Event" Then
                    meth = Renamer.RenameMethod(meth)
                Else
                    If NameChecker.IsRenamable(meth) Then
                        meth = Renamer.RenameMethod(meth)
                    End If
                End If
            End If
            ProcessParameters(meth)
        End Sub

        Private Sub ProcessParameters(Meth As MethodDefinition)
            If m_RenamingAccept.Parameters Then
                Renamer.RenameParameters(Meth)
            End If
            If m_RenamingAccept.Variables Then Renamer.RenameVariables(Meth)
        End Sub
#End Region

    End Class
End Namespace