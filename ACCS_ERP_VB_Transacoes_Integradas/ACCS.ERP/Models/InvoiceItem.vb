Namespace Models
    Public Class InvoiceItem
        Public Property Id As Integer
        Public Property InvoiceId As Integer
        Public Property ProductId As Integer?
        Public Property ProductCode As String = ""
        Public Property Description As String = ""
        Public Property Quantity As Decimal = 1D
        Public Property UnitPrice As Decimal
        Public Property DiscountPercent As Decimal = 0D
        Public Property TaxRate As Decimal = 0D
        Public ReadOnly Property Subtotal As Decimal
            Get
                Dim gross = Quantity * UnitPrice
                Return Math.Round(gross - (gross * (DiscountPercent / 100D)), 2)
            End Get
        End Property
        Public ReadOnly Property TaxAmount As Decimal
            Get
                Return Math.Round(Subtotal * (TaxRate / 100D), 2)
            End Get
        End Property
        Public ReadOnly Property Total As Decimal
            Get
                Return Math.Round(Subtotal + TaxAmount, 2)
            End Get
        End Property
    End Class
End Namespace
