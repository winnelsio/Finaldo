Namespace Models
    Public Class Product
        Public Property Id As Integer
        Public Property CompanyId As Integer
        Public Property Code As String = ""
        Public Property Name As String = ""
        Public Property Category As String = "Geral"
        Public Property Unit As String = "UN"
        Public Property CostPrice As Decimal
        Public Property SalePrice As Decimal
        Public Property TaxRate As Decimal = 0D ' Taxa configurável
        Public Property CurrentStock As Decimal
        Public Property MinStock As Decimal = 5.0D
        Public Property Active As Boolean = True

        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return CurrentStock <= MinStock
            End Get
        End Property

        Public ReadOnly Property StockStatus As String
            Get
                If CurrentStock <= 0 Then
                    Return "Esgotado"
                ElseIf CurrentStock <= MinStock Then
                    Return "Stock Baixo"
                Else
                    Return "Normal"
                End If
            End Get
        End Property
    End Class
End Namespace
