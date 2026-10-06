Imports MySqlConnector
Imports ACCS.ERP.Data
Imports ACCS.ERP.Models

Namespace Services
    Public Class ProductService
        Private Shared Function HasColumn(cn As MySqlConnection, columnName As String) As Boolean
            Const sql As String = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='products' AND COLUMN_NAME=@column"
            Using cmd As New MySqlCommand(sql, cn)
                cmd.Parameters.AddWithValue("@column", columnName)
                Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
            End Using
        End Function

        Public Function List(companyId As Integer, Optional search As String = "", Optional category As String = "", Optional onlyLowStock As Boolean = False) As List(Of Product)
            Dim result As New List(Of Product)()
            Using cn = Db.OpenConnection()
                Dim hasTax = HasColumn(cn, "tax_rate")
                Dim hasCost = HasColumn(cn, "cost_price")
                Dim hasUnit = HasColumn(cn, "unit")
                Dim hasCategory = HasColumn(cn, "category")
                Dim hasMinStock = HasColumn(cn, "min_stock")
                Dim hasActive = HasColumn(cn, "active")

                Dim sql As String = "SELECT id, company_id, code, name, price, stock" &
                    If(hasTax, ", tax_rate", ", 0.0 AS tax_rate") &
                    If(hasCost, ", cost_price", ", 0 AS cost_price") &
                    If(hasUnit, ", unit", ", 'UN' AS unit") &
                    If(hasCategory, ", category", ", 'Geral' AS category") &
                    If(hasMinStock, ", min_stock", ", 5.0 AS min_stock") &
                    If(hasActive, ", active", ", 1 AS active") &
                    " FROM products WHERE company_id=@companyId"

                If hasActive Then sql &= " AND active=1"

                If Not String.IsNullOrWhiteSpace(search) Then
                    sql &= " AND (name LIKE @search OR code LIKE @search)"
                End If

                If Not String.IsNullOrWhiteSpace(category) AndAlso category <> "Todas" Then
                    sql &= " AND category = @category"
                End If

                If onlyLowStock Then
                    sql &= " AND stock <= " & If(hasMinStock, "min_stock", "5.0")
                End If

                sql &= " ORDER BY id DESC"

                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    If Not String.IsNullOrWhiteSpace(search) Then cmd.Parameters.AddWithValue("@search", "%" & search.Trim() & "%")
                    If Not String.IsNullOrWhiteSpace(category) AndAlso category <> "Todas" Then cmd.Parameters.AddWithValue("@category", category.Trim())

                    Using rd = cmd.ExecuteReader()
                        While rd.Read()
                            result.Add(New Product With {
                                .Id = Convert.ToInt32(rd("id")),
                                .CompanyId = Convert.ToInt32(rd("company_id")),
                                .Code = If(rd("code") Is DBNull.Value, "", rd("code").ToString()),
                                .Name = rd("name").ToString(),
                                .SalePrice = Convert.ToDecimal(rd("price")),
                                .CurrentStock = Convert.ToDecimal(rd("stock")),
                                .TaxRate = Convert.ToDecimal(rd("tax_rate")),
                                .CostPrice = Convert.ToDecimal(rd("cost_price")),
                                .Unit = rd("unit").ToString(),
                                .Category = rd("category").ToString(),
                                .MinStock = Convert.ToDecimal(rd("min_stock")),
                                .Active = Convert.ToBoolean(rd("active"))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return result
        End Function

        Public Sub Save(product As Product, companyId As Integer)
            If String.IsNullOrWhiteSpace(product.Name) Then Throw New ArgumentException("Informe a designação / nome do produto.")
            If product.SalePrice < 0 Then Throw New ArgumentException("O preço de venda não pode ser negativo.")

            Using cn = Db.OpenConnection()
                Dim hasTax = HasColumn(cn, "tax_rate")
                Dim hasCost = HasColumn(cn, "cost_price")
                Dim hasUnit = HasColumn(cn, "unit")
                Dim hasCategory = HasColumn(cn, "category")
                Dim hasMinStock = HasColumn(cn, "min_stock")

                If product.Id = 0 Then
                    Dim cols = "company_id,code,name,price,stock"
                    Dim vals = "@companyId,@code,@name,@price,@stock"
                    If hasTax Then cols &= ",tax_rate" : vals &= ",@tax_rate"
                    If hasCost Then cols &= ",cost_price" : vals &= ",@cost_price"
                    If hasUnit Then cols &= ",unit" : vals &= ",@unit"
                    If hasCategory Then cols &= ",category" : vals &= ",@category"
                    If hasMinStock Then cols &= ",min_stock" : vals &= ",@min_stock"

                    Using cmd As New MySqlCommand($"INSERT INTO products ({cols}) VALUES ({vals})", cn)
                        AddParams(cmd, product, companyId, hasTax, hasCost, hasUnit, hasCategory, hasMinStock)
                        cmd.ExecuteNonQuery()
                    End Using
                Else
                    Dim sets = "code=@code,name=@name,price=@price"
                    If hasTax Then sets &= ",tax_rate=@tax_rate"
                    If hasCost Then sets &= ",cost_price=@cost_price"
                    If hasUnit Then sets &= ",unit=@unit"
                    If hasCategory Then sets &= ",category=@category"
                    If hasMinStock Then sets &= ",min_stock=@min_stock"

                    Using cmd As New MySqlCommand($"UPDATE products SET {sets} WHERE id=@id AND company_id=@companyId", cn)
                        cmd.Parameters.AddWithValue("@id", product.Id)
                        AddParams(cmd, product, companyId, hasTax, hasCost, hasUnit, hasCategory, hasMinStock)
                        cmd.ExecuteNonQuery()
                    End Using
                End If
            End Using
        End Sub

        Public Sub AdjustStock(productId As Integer, companyId As Integer, quantityDelta As Decimal, reason As String)
            Using cn = Db.OpenConnection()
                Const sql As String = "UPDATE products SET stock = stock + @delta WHERE id=@id AND company_id=@companyId"
                Using cmd As New MySqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@delta", quantityDelta)
                    cmd.Parameters.AddWithValue("@id", productId)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub Delete(id As Integer, companyId As Integer)
            Using cn = Db.OpenConnection()
                Dim hasActive = HasColumn(cn, "active")
                If Not hasActive Then Throw New InvalidOperationException("A base precisa da coluna products.active. Execute database\001_stabilization.sql antes de desactivar produtos.")
                Using cmd As New MySqlCommand("UPDATE products SET active=0 WHERE id=@id AND company_id=@companyId", cn)
                    cmd.Parameters.AddWithValue("@id", id)
                    cmd.Parameters.AddWithValue("@companyId", companyId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Shared Sub AddParams(cmd As MySqlCommand, p As Product, companyId As Integer, hasTax As Boolean, hasCost As Boolean, hasUnit As Boolean, hasCat As Boolean, hasMin As Boolean)
            cmd.Parameters.AddWithValue("@companyId", companyId)
            cmd.Parameters.AddWithValue("@code", p.Code.Trim())
            cmd.Parameters.AddWithValue("@name", p.Name.Trim())
            cmd.Parameters.AddWithValue("@price", p.SalePrice)
            cmd.Parameters.AddWithValue("@stock", p.CurrentStock)
            If hasTax Then cmd.Parameters.AddWithValue("@tax_rate", p.TaxRate)
            If hasCost Then cmd.Parameters.AddWithValue("@cost_price", p.CostPrice)
            If hasUnit Then cmd.Parameters.AddWithValue("@unit", p.Unit.Trim())
            If hasCat Then cmd.Parameters.AddWithValue("@category", p.Category.Trim())
            If hasMin Then cmd.Parameters.AddWithValue("@min_stock", p.MinStock)
        End Sub
    End Class
End Namespace
