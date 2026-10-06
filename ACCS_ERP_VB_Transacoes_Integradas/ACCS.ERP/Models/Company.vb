Namespace Models
    Public Class Company
        Public Property Id As Integer
        Public Property Name As String = ""
        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class
End Namespace
