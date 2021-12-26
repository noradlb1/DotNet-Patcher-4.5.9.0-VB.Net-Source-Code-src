﻿Imports System.IO

Namespace AssemblyHelper
    Public Class DataFull
        Inherits Data

#Region " Properties "
        Public Property ManifestResourceNames As IEnumerable(Of String)
        Public Property ManifestResourceStreams As List(Of Stream)
        Public Property TypesClass As IEnumerable(Of Type)
        Public Property HasSerializableAttribute As Boolean
#End Region

    End Class
End Namespace

