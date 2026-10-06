Imports MySqlConnector
Imports ACCS.ERP.Data
Imports ACCS.ERP.Models

Namespace Services
    Public Class InvoiceService
        Public Function List(companyId As Integer, Optional docType As String = "", Optional search As String = "") As List(Of Invoice)
            Dim result As New List(Of Invoice)()
            Using cn = Db.OpenConnection()
                Dim sql = "SELECT id,company_id,customer_id,customer_name,customer_nuit,customer_address,document_type,document_number,date_issued,date_due,subtotal,tax_total,retention_rate,retention_amount,total_amount,status,notes FROM invoices WHERE company_id=@companyId"
                If Not String.IsNullOrWhiteSpace(docType) AndAlso docType <> "TODOS" Then sql &= " AND document_type=@docType"
                If Not String.IsNullOrWhiteSpace(search) Then sql &= " AND (document_number LIKE @search OR customer_name LIKE @search OR customer_nuit LIKE @search)"
                sql &= " ORDER BY id DESC"
                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    If Not String.IsNullOrWhiteSpace(docType) AndAlso docType <> "TODOS" Then cmd.Parameters.AddWithValue("@docType", docType.Trim())
                    If Not String.IsNullOrWhiteSpace(search) Then cmd.Parameters.AddWithValue("@search", "%" & search.Trim() & "%")
                    Using rd = cmd.ExecuteReader()
                        While rd.Read()
                            result.Add(New Invoice With {
                                .Id = Convert.ToInt32(rd("id")), .CompanyId = Convert.ToInt32(rd("company_id")),
                                .CustomerId = If(rd("customer_id") Is DBNull.Value, 0, Convert.ToInt32(rd("customer_id"))),
                                .CustomerName = rd("customer_name").ToString(), .CustomerNuit = rd("customer_nuit").ToString(),
                                .CustomerAddress = If(rd("customer_address") Is DBNull.Value, "", rd("customer_address").ToString()),
                                .DocumentType = rd("document_type").ToString(), .DocumentNumber = rd("document_number").ToString(),
                                .DateIssued = Convert.ToDateTime(rd("date_issued")), .DateDue = Convert.ToDateTime(rd("date_due")),
                                .RetentionRate = Convert.ToDecimal(rd("retention_rate")), .Status = rd("status").ToString(),
                                .Notes = If(rd("notes") Is DBNull.Value, "", rd("notes").ToString())
                            })
                            result(result.Count - 1).StoredSubtotal = Convert.ToDecimal(rd("subtotal"))
                            result(result.Count - 1).StoredTaxTotal = Convert.ToDecimal(rd("tax_total"))
                            result(result.Count - 1).StoredRetentionAmount = Convert.ToDecimal(rd("retention_amount"))
                            result(result.Count - 1).StoredTotalAmount = Convert.ToDecimal(rd("total_amount"))
                        End While
                    End Using
                End Using
            End Using
            Return result
        End Function

        Public Function GetDefaultTaxRate(companyId As Integer) As Decimal
            Using cn = Db.OpenConnection()
                Using cmd As New MySqlCommand("SELECT default_tax_rate FROM fiscal_settings WHERE company_id=@companyId", cn)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    Dim value = cmd.ExecuteScalar()
                    If value Is Nothing OrElse value Is DBNull.Value Then Return 0D
                    Return Convert.ToDecimal(value)
                End Using
            End Using
        End Function

        Private Function ReserveNextNumber(cn As MySqlConnection, tr As MySqlTransaction, companyId As Integer, docType As String, fiscalYear As Integer) As String
            Const sql = "INSERT INTO document_sequences(company_id,document_type,fiscal_year,last_number) VALUES(@companyId,@docType,@year,1) ON DUPLICATE KEY UPDATE last_number=last_number+1"
            Using cmd As New MySqlCommand(sql, cn, tr)
                cmd.Parameters.AddWithValue("@companyId", companyId) : cmd.Parameters.AddWithValue("@docType", docType) : cmd.Parameters.AddWithValue("@year", fiscalYear)
                cmd.ExecuteNonQuery()
            End Using
            Using cmd As New MySqlCommand("SELECT last_number FROM document_sequences WHERE company_id=@companyId AND document_type=@docType AND fiscal_year=@year FOR UPDATE", cn, tr)
                cmd.Parameters.AddWithValue("@companyId", companyId) : cmd.Parameters.AddWithValue("@docType", docType) : cmd.Parameters.AddWithValue("@year", fiscalYear)
                Dim number = Convert.ToInt32(cmd.ExecuteScalar())
                Return $"{docType} {fiscalYear}/{number:D4}"
            End Using
        End Function

        Public Sub Save(invoice As Invoice, companyId As Integer)
            ValidateDocument(invoice)
            Using cn = Db.OpenConnection()
                Using tr = cn.BeginTransaction()
                    Try
                        invoice.CompanyId = companyId
                        invoice.DocumentNumber = ReserveNextNumber(cn, tr, companyId, invoice.DocumentType.ToUpperInvariant(), invoice.DateIssued.Year)
                        Dim invoiceId = InsertHeader(cn, tr, invoice, companyId)
                        For Each item In invoice.Items
                            InsertItem(cn, tr, invoiceId, item)
                            If AffectsStock(invoice.DocumentType) AndAlso item.ProductId.HasValue Then
                                ApplyStockMovement(cn, tr, companyId, invoiceId, invoice, item)
                            End If
                        Next
                        ApplyCustomerAccount(cn, tr, companyId, invoiceId, invoice)
                        tr.Commit()
                        invoice.Id = invoiceId
                    Catch
                        Try : tr.Rollback() : Catch : End Try
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Shared Sub ValidateDocument(invoice As Invoice)
            Dim t = invoice.DocumentType.ToUpperInvariant()
            If t <> "FT" AndAlso t <> "FR" AndAlso t <> "FP" AndAlso t <> "RC" Then Throw New ArgumentException("Tipo de documento inválido.")
            If invoice.CustomerId <= 0 OrElse String.IsNullOrWhiteSpace(invoice.CustomerName) Then Throw New ArgumentException("Seleccione um cliente cadastrado.")
            If invoice.Items.Count = 0 Then Throw New ArgumentException("O documento deve ter pelo menos uma linha.")
            For Each item In invoice.Items
                If item.Quantity <= 0D Then Throw New ArgumentException("A quantidade deve ser superior a zero.")
                If item.UnitPrice < 0D Then Throw New ArgumentException("O preço não pode ser negativo.")
                If item.DiscountPercent < 0D OrElse item.DiscountPercent > 100D Then Throw New ArgumentException("Desconto inválido.")
                If item.TaxRate < 0D Then Throw New ArgumentException("Taxa de imposto inválida.")
            Next
            If (t = "FT" OrElse t = "FR") AndAlso invoice.Items.Any(Function(x) Not x.ProductId.HasValue) Then
                Throw New ArgumentException("Factura e Factura/Recibo exigem artigos cadastrados para garantir o controlo de stock.")
            End If
        End Sub

        Private Shared Function AffectsStock(docType As String) As Boolean
            Return docType.Equals("FT", StringComparison.OrdinalIgnoreCase) OrElse docType.Equals("FR", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function InsertHeader(cn As MySqlConnection, tr As MySqlTransaction, invoice As Invoice, companyId As Integer) As Integer
            Const sql = "INSERT INTO invoices(company_id,customer_id,customer_name,customer_nuit,customer_address,document_type,document_number,date_issued,date_due,subtotal,tax_total,retention_rate,retention_amount,total_amount,status,notes,reference_invoice_id,payment_method,created_by) VALUES(@companyId,@customerId,@customerName,@customerNuit,@customerAddress,@docType,@docNumber,@dateIssued,@dateDue,@subtotal,@taxTotal,@retRate,@retAmount,@totalAmount,@status,@notes,@referenceId,@paymentMethod,@createdBy); SELECT LAST_INSERT_ID();"
            Using cmd As New MySqlCommand(sql, cn, tr)
                cmd.Parameters.AddWithValue("@companyId", companyId) : cmd.Parameters.AddWithValue("@customerId", invoice.CustomerId)
                cmd.Parameters.AddWithValue("@customerName", invoice.CustomerName.Trim()) : cmd.Parameters.AddWithValue("@customerNuit", invoice.CustomerNuit.Trim())
                cmd.Parameters.AddWithValue("@customerAddress", invoice.CustomerAddress.Trim()) : cmd.Parameters.AddWithValue("@docType", invoice.DocumentType.ToUpperInvariant())
                cmd.Parameters.AddWithValue("@docNumber", invoice.DocumentNumber) : cmd.Parameters.AddWithValue("@dateIssued", invoice.DateIssued)
                cmd.Parameters.AddWithValue("@dateDue", invoice.DateDue) : cmd.Parameters.AddWithValue("@subtotal", invoice.Subtotal)
                cmd.Parameters.AddWithValue("@taxTotal", invoice.TaxTotal) : cmd.Parameters.AddWithValue("@retRate", invoice.RetentionRate)
                cmd.Parameters.AddWithValue("@retAmount", invoice.RetentionAmount) : cmd.Parameters.AddWithValue("@totalAmount", invoice.TotalAmount)
                cmd.Parameters.AddWithValue("@status", invoice.Status) : cmd.Parameters.AddWithValue("@notes", invoice.Notes.Trim())
                cmd.Parameters.AddWithValue("@referenceId", If(invoice.ReferenceInvoiceId.HasValue, CType(invoice.ReferenceInvoiceId.Value, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@paymentMethod", invoice.PaymentMethod.Trim())
                cmd.Parameters.AddWithValue("@createdBy", If(invoice.CreatedBy.HasValue, CType(invoice.CreatedBy.Value, Object), DBNull.Value))
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Function

        Private Shared Sub InsertItem(cn As MySqlConnection, tr As MySqlTransaction, invoiceId As Integer, item As InvoiceItem)
            Const sql = "INSERT INTO invoice_items(invoice_id,product_id,product_code,description,quantity,unit_price,discount_percent,tax_rate,subtotal,tax_amount,total) VALUES(@invoiceId,@productId,@code,@desc,@qty,@price,@discount,@tax,@subtotal,@taxAmount,@total)"
            Using cmd As New MySqlCommand(sql, cn, tr)
                cmd.Parameters.AddWithValue("@invoiceId", invoiceId)
                cmd.Parameters.AddWithValue("@productId", If(item.ProductId.HasValue, CType(item.ProductId.Value, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@code", item.ProductCode.Trim()) : cmd.Parameters.AddWithValue("@desc", item.Description.Trim())
                cmd.Parameters.AddWithValue("@qty", item.Quantity) : cmd.Parameters.AddWithValue("@price", item.UnitPrice)
                cmd.Parameters.AddWithValue("@discount", item.DiscountPercent) : cmd.Parameters.AddWithValue("@tax", item.TaxRate)
                cmd.Parameters.AddWithValue("@subtotal", item.Subtotal) : cmd.Parameters.AddWithValue("@taxAmount", item.TaxAmount) : cmd.Parameters.AddWithValue("@total", item.Total)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub ApplyStockMovement(cn As MySqlConnection, tr As MySqlTransaction, companyId As Integer, invoiceId As Integer, invoice As Invoice, item As InvoiceItem)
            Dim beforeStock As Decimal
            Using cmd As New MySqlCommand("SELECT stock FROM products WHERE id=@id AND company_id=@companyId AND active=1 FOR UPDATE", cn, tr)
                cmd.Parameters.AddWithValue("@id", item.ProductId.Value) : cmd.Parameters.AddWithValue("@companyId", companyId)
                Dim value = cmd.ExecuteScalar()
                If value Is Nothing Then Throw New InvalidOperationException($"Produto {item.ProductCode} não encontrado ou inactivo.")
                beforeStock = Convert.ToDecimal(value)
            End Using
            If beforeStock < item.Quantity Then Throw New InvalidOperationException($"Stock insuficiente para {item.Description}. Disponível: {beforeStock:N3}; solicitado: {item.Quantity:N3}.")
            Dim afterStock = beforeStock - item.Quantity
            Using cmd As New MySqlCommand("UPDATE products SET stock=@stock WHERE id=@id AND company_id=@companyId", cn, tr)
                cmd.Parameters.AddWithValue("@stock", afterStock) : cmd.Parameters.AddWithValue("@id", item.ProductId.Value) : cmd.Parameters.AddWithValue("@companyId", companyId)
                If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Não foi possível actualizar o stock.")
            End Using
            Const sqlMove = "INSERT INTO stock_movements(company_id,product_id,invoice_id,document_type,document_number,movement_type,quantity,stock_before,stock_after,reason,created_by) VALUES(@companyId,@productId,@invoiceId,@docType,@docNumber,'SAIDA_VENDA',@qty,@before,@after,@reason,@createdBy)"
            Using cmd As New MySqlCommand(sqlMove, cn, tr)
                cmd.Parameters.AddWithValue("@companyId", companyId) : cmd.Parameters.AddWithValue("@productId", item.ProductId.Value) : cmd.Parameters.AddWithValue("@invoiceId", invoiceId)
                cmd.Parameters.AddWithValue("@docType", invoice.DocumentType) : cmd.Parameters.AddWithValue("@docNumber", invoice.DocumentNumber) : cmd.Parameters.AddWithValue("@qty", item.Quantity)
                cmd.Parameters.AddWithValue("@before", beforeStock) : cmd.Parameters.AddWithValue("@after", afterStock) : cmd.Parameters.AddWithValue("@reason", "Emissão " & invoice.DocumentNumber)
                cmd.Parameters.AddWithValue("@createdBy", If(invoice.CreatedBy.HasValue, CType(invoice.CreatedBy.Value, Object), DBNull.Value))
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub ApplyCustomerAccount(cn As MySqlConnection, tr As MySqlTransaction, companyId As Integer, invoiceId As Integer, invoice As Invoice)
            If invoice.DocumentType.Equals("FP", StringComparison.OrdinalIgnoreCase) Then Return
            Dim currentBalance As Decimal
            Using cmd As New MySqlCommand("SELECT balance FROM customers WHERE id=@id AND company_id=@companyId AND active=1 FOR UPDATE", cn, tr)
                cmd.Parameters.AddWithValue("@id", invoice.CustomerId) : cmd.Parameters.AddWithValue("@companyId", companyId)
                Dim value = cmd.ExecuteScalar()
                If value Is Nothing Then Throw New InvalidOperationException("Cliente não encontrado ou inactivo.")
                currentBalance = Convert.ToDecimal(value)
            End Using

            Dim debit As Decimal = 0D, credit As Decimal = 0D, movementType As String = "", description As String = ""
            Select Case invoice.DocumentType.ToUpperInvariant()
                Case "FT"
                    debit = invoice.TotalAmount : movementType = "FACTURA" : description = "Débito pela " & invoice.DocumentNumber
                    WriteAccountMovement(cn, tr, companyId, invoice.CustomerId, invoiceId, invoice, debit, 0D, currentBalance + debit, movementType, description)
                    UpdateCustomerBalance(cn, tr, companyId, invoice.CustomerId, currentBalance + debit)
                Case "FR"
                    debit = invoice.TotalAmount : movementType = "VENDA_LIQUIDADA"
                    WriteAccountMovement(cn, tr, companyId, invoice.CustomerId, invoiceId, invoice, debit, 0D, currentBalance + debit, movementType, "Venda " & invoice.DocumentNumber)
                    WriteAccountMovement(cn, tr, companyId, invoice.CustomerId, invoiceId, invoice, 0D, debit, currentBalance, "PAGAMENTO", "Liquidação imediata " & invoice.DocumentNumber)
                    ' saldo líquido permanece inalterado
                Case "RC"
                    credit = invoice.TotalAmount
                    If credit <= 0D Then Throw New InvalidOperationException("O valor do recibo deve ser superior a zero.")
                    If credit > currentBalance Then Throw New InvalidOperationException($"O recibo ({credit:N2}) não pode exceder o saldo em dívida ({currentBalance:N2}).")
                    Dim newBalance = currentBalance - credit
                    WriteAccountMovement(cn, tr, companyId, invoice.CustomerId, invoiceId, invoice, 0D, credit, newBalance, "RECIBO", "Recebimento " & invoice.DocumentNumber)
                    UpdateCustomerBalance(cn, tr, companyId, invoice.CustomerId, newBalance)
            End Select
        End Sub

        Private Shared Sub WriteAccountMovement(cn As MySqlConnection, tr As MySqlTransaction, companyId As Integer, customerId As Integer, invoiceId As Integer, invoice As Invoice, debit As Decimal, credit As Decimal, balanceAfter As Decimal, movementType As String, description As String)
            Const sql = "INSERT INTO customer_account_movements(company_id,customer_id,invoice_id,reference_invoice_id,document_type,document_number,movement_type,debit,credit,balance_after,description,created_by) VALUES(@companyId,@customerId,@invoiceId,@referenceId,@docType,@docNumber,@movementType,@debit,@credit,@balance,@description,@createdBy)"
            Using cmd As New MySqlCommand(sql, cn, tr)
                cmd.Parameters.AddWithValue("@companyId", companyId) : cmd.Parameters.AddWithValue("@customerId", customerId) : cmd.Parameters.AddWithValue("@invoiceId", invoiceId)
                cmd.Parameters.AddWithValue("@referenceId", If(invoice.ReferenceInvoiceId.HasValue, CType(invoice.ReferenceInvoiceId.Value, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@docType", invoice.DocumentType) : cmd.Parameters.AddWithValue("@docNumber", invoice.DocumentNumber) : cmd.Parameters.AddWithValue("@movementType", movementType)
                cmd.Parameters.AddWithValue("@debit", debit) : cmd.Parameters.AddWithValue("@credit", credit) : cmd.Parameters.AddWithValue("@balance", balanceAfter)
                cmd.Parameters.AddWithValue("@description", description) : cmd.Parameters.AddWithValue("@createdBy", If(invoice.CreatedBy.HasValue, CType(invoice.CreatedBy.Value, Object), DBNull.Value))
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub UpdateCustomerBalance(cn As MySqlConnection, tr As MySqlTransaction, companyId As Integer, customerId As Integer, balance As Decimal)
            Using cmd As New MySqlCommand("UPDATE customers SET balance=@balance WHERE id=@id AND company_id=@companyId", cn, tr)
                cmd.Parameters.AddWithValue("@balance", balance) : cmd.Parameters.AddWithValue("@id", customerId) : cmd.Parameters.AddWithValue("@companyId", companyId)
                If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Não foi possível actualizar a conta corrente do cliente.")
            End Using
        End Sub
    End Class
End Namespace
