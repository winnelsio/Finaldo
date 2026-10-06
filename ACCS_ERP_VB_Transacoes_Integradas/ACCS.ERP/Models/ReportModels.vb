Namespace Models
    Public Class VatMapEntry
        Public Property DocumentNumber As String = ""
        Public Property DocumentType As String = ""
        Public Property DateIssued As Date
        Public Property CustomerName As String = ""
        Public Property CustomerNuit As String = ""
        Public Property TaxableBase As Decimal ' Incidência
        Public Property TaxRate As Decimal = 0D
        Public Property TaxAmount As Decimal ' IVA Liquidado
        Public Property RetentionAmount As Decimal
        Public Property TotalAmount As Decimal
    End Class

    Public Class CustomerStatementEntry
        Public Property DateIssued As Date
        Public Property DocumentNumber As String = ""
        Public Property DocumentType As String = ""
        Public Property Description As String = ""
        Public Property Debit As Decimal ' Aumento de dívida (FT)
        Public Property Credit As Decimal ' Pagamento / Recibo (RC)
        Public Property RunningBalance As Decimal
    End Class

    Public Class StockValuationEntry
        Public Property Code As String = ""
        Public Property Name As String = ""
        Public Property Category As String = ""
        Public Property CurrentStock As Decimal
        Public Property CostPrice As Decimal
        Public Property SalePrice As Decimal
        Public Property TotalCostValue As Decimal
        Public Property TotalSaleValue As Decimal
        Public Property ExpectedProfit As Decimal
    End Class

    Public Class SalesSummary
        Public Property TotalInvoiced As Decimal
        Public Property TotalVat As Decimal
        Public Property TotalRetention As Decimal
        Public Property DocumentCount As Integer
        Public Property InvoiceCount As Integer
        Public Property CashSaleCount As Integer
    End Class
End Namespace
