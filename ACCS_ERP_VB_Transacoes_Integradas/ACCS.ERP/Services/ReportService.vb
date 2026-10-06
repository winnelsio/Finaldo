Imports MySqlConnector
Imports ACCS.ERP.Data
Imports ACCS.ERP.Models

Namespace Services
    Public Class ReportService
        Public Function GetVatMap(companyId As Integer, startDate As Date, endDate As Date) As List(Of VatMapEntry)
            Dim list As New List(Of VatMapEntry)()
            Using cn = Db.OpenConnection()
                Const sql As String = "SELECT document_number, document_type, date_issued, customer_name, customer_nuit, " &
                                    "subtotal, tax_total, retention_amount, total_amount " &
                                    "FROM invoices " &
                                    "WHERE company_id=@companyId AND status <> 'Anulada' " &
                                    "AND date_issued BETWEEN @start AND @end " &
                                    "ORDER BY date_issued ASC, id ASC"

                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    cmd.Parameters.AddWithValue("@start", startDate)
                    cmd.Parameters.AddWithValue("@end", endDate)

                    Using rd = cmd.ExecuteReader()
                        While rd.Read()
                            list.Add(New VatMapEntry With {
                                .DocumentNumber = rd("document_number").ToString(),
                                .DocumentType = rd("document_type").ToString(),
                                .DateIssued = Convert.ToDateTime(rd("date_issued")),
                                .CustomerName = rd("customer_name").ToString(),
                                .CustomerNuit = rd("customer_nuit").ToString(),
                                .TaxableBase = Convert.ToDecimal(rd("subtotal")),
                                .TaxRate = If(Convert.ToDecimal(rd("subtotal")) = 0D, 0D, Math.Round(Convert.ToDecimal(rd("tax_total")) / Convert.ToDecimal(rd("subtotal")) * 100D, 3)),
                                .TaxAmount = Convert.ToDecimal(rd("tax_total")),
                                .RetentionAmount = Convert.ToDecimal(rd("retention_amount")),
                                .TotalAmount = Convert.ToDecimal(rd("total_amount"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function GetCustomerStatement(companyId As Integer, customerId As Integer) As List(Of CustomerStatementEntry)
            Dim list As New List(Of CustomerStatementEntry)()
            Using cn = Db.OpenConnection()
                Const sql As String = "SELECT created_at, document_number, document_type, description, debit, credit, balance_after FROM customer_account_movements WHERE company_id=@companyId AND customer_id=@customerId ORDER BY id ASC"
                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    cmd.Parameters.AddWithValue("@customerId", customerId)
                    Using rd = cmd.ExecuteReader()
                        While rd.Read()
                            list.Add(New CustomerStatementEntry With {
                                .DateIssued = Convert.ToDateTime(rd("created_at")),
                                .DocumentNumber = rd("document_number").ToString(),
                                .DocumentType = rd("document_type").ToString(),
                                .Description = If(rd("description") Is DBNull.Value, "", rd("description").ToString()),
                                .Debit = Convert.ToDecimal(rd("debit")),
                                .Credit = Convert.ToDecimal(rd("credit")),
                                .RunningBalance = Convert.ToDecimal(rd("balance_after"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Function GetStockValuation(companyId As Integer) As List(Of StockValuationEntry)
            Dim list As New List(Of StockValuationEntry)()
            Using cn = Db.OpenConnection()
                Const sql As String = "SELECT code, name, category, stock, cost_price, price " &
                                    "FROM products WHERE company_id=@companyId ORDER BY name ASC"

                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    Using rd = cmd.ExecuteReader()
                        While rd.Read()
                            Dim stock = Convert.ToDecimal(rd("stock"))
                            Dim cost = If(rd("cost_price") Is DBNull.Value, 0D, Convert.ToDecimal(rd("cost_price")))
                            Dim sale = Convert.ToDecimal(rd("price"))
                            Dim totalCost = stock * cost
                            Dim totalSale = stock * sale

                            list.Add(New StockValuationEntry With {
                                .Code = If(rd("code") Is DBNull.Value, "", rd("code").ToString()),
                                .Name = rd("name").ToString(),
                                .Category = If(rd("category") Is DBNull.Value, "Geral", rd("category").ToString()),
                                .CurrentStock = stock,
                                .CostPrice = cost,
                                .SalePrice = sale,
                                .TotalCostValue = totalCost,
                                .TotalSaleValue = totalSale,
                                .ExpectedProfit = totalSale - totalCost
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function
    End Class
End Namespace
