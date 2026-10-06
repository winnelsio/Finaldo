Namespace Models
    Public Class Invoice
        Public Property Id As Integer
        Public Property CompanyId As Integer
        Public Property CustomerId As Integer
        Public Property CustomerName As String = ""
        Public Property CustomerNuit As String = ""
        Public Property CustomerAddress As String = ""
        Public Property DocumentType As String = "FT" ' FT, FR, FP, RC
        Public Property DocumentNumber As String = ""
        Public Property DateIssued As Date = Date.Today
        Public Property DateDue As Date = Date.Today.AddDays(30)
        Public Property RetentionRate As Decimal = 0D
        Public Property Status As String = "Emitida"
        Public Property Notes As String = ""
        Public Property PaymentMethod As String = "Dinheiro"
        Public Property ReferenceInvoiceId As Integer?
        Public Property CreatedBy As Integer?
        Public Property Items As New List(Of InvoiceItem)()
        Public Property StoredSubtotal As Decimal?
        Public Property StoredTaxTotal As Decimal?
        Public Property StoredRetentionAmount As Decimal?
        Public Property StoredTotalAmount As Decimal?

        Public ReadOnly Property Subtotal As Decimal
            Get
                If Items.Count = 0 AndAlso StoredSubtotal.HasValue Then Return StoredSubtotal.Value
                Return Items.Sum(Function(it) it.Subtotal)
            End Get
        End Property
        Public ReadOnly Property TaxTotal As Decimal
            Get
                If Items.Count = 0 AndAlso StoredTaxTotal.HasValue Then Return StoredTaxTotal.Value
                Return Items.Sum(Function(it) it.TaxAmount)
            End Get
        End Property
        Public ReadOnly Property RetentionAmount As Decimal
            Get
                If Items.Count = 0 AndAlso StoredRetentionAmount.HasValue Then Return StoredRetentionAmount.Value
                If RetentionRate <= 0D Then Return 0D
                Return Math.Round(Subtotal * (RetentionRate / 100D), 2)
            End Get
        End Property
        Public ReadOnly Property TotalAmount As Decimal
            Get
                If Items.Count = 0 AndAlso StoredTotalAmount.HasValue Then Return StoredTotalAmount.Value
                Return Math.Round(Subtotal + TaxTotal - RetentionAmount, 2)
            End Get
        End Property
        Public ReadOnly Property DocumentTypeName As String
            Get
                Select Case DocumentType.ToUpperInvariant()
                    Case "FT" : Return "Factura"
                    Case "FR" : Return "Factura/Recibo"
                    Case "RC" : Return "Recibo"
                    Case "FP" : Return "Cotação / Proforma"
                    Case Else : Return DocumentType
                End Select
            End Get
        End Property
    End Class
End Namespace
