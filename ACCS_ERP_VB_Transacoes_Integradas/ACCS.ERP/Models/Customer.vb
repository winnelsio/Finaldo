Namespace Models
    Public Class Customer
        Public Property Id As Integer
        Public Property CompanyId As Integer
        Public Property Name As String = ""
        Public Property Nuit As String = ""
        Public Property Phone As String = ""
        Public Property WhatsApp As String = ""
        Public Property Email As String = ""
        Public Property Address As String = ""
        Public Property Balance As Decimal
        Public Property LoyaltyPoints As Integer
        Public Property VipDiscount As Decimal
    End Class
End Namespace
