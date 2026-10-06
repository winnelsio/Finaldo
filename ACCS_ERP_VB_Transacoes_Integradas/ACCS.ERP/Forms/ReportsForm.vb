Imports System.Windows.Forms
Imports System.Drawing
Imports ACCS.ERP.Models
Imports ACCS.ERP.Services

Namespace Forms
    Public Class ReportsForm
        Inherits Form

        Private ReadOnly _session As UserSession
        Private ReadOnly _reportService As New ReportService()
        Private ReadOnly _customerService As New CustomerService()

        Private ReadOnly tabControl As New TabControl()
        Private ReadOnly gridVat As New DataGridView()
        Private ReadOnly gridStatement As New DataGridView()
        Private ReadOnly gridStock As New DataGridView()

        ' Filters
        Private ReadOnly dtpVatStart As New DateTimePicker()
        Private ReadOnly dtpVatEnd As New DateTimePicker()
        Private ReadOnly btnFilterVat As New Button()
        Private ReadOnly cboCustomer As New ComboBox()
        Private ReadOnly btnFilterStatement As New Button()
        Private ReadOnly btnRefreshStock As New Button()

        Public Sub New(session As UserSession)
            _session = session
            Text = "ACCS ERP — Relatórios & Fecho Fiscal"
            StartPosition = FormStartPosition.CenterParent
            Size = New Size(1300, 780)
            MinimumSize = New Size(1024, 600)
            BuildUi()
            LoadInitialData()
        End Sub

        Private Sub BuildUi()
            tabControl.Dock = DockStyle.Fill
            Controls.Add(tabControl)

            ' Tab 1: Mapa de IVA
            Dim tabVat = New TabPage("📈  Mapa Periódico de IVA (16%)")
            BuildVatTab(tabVat)
            tabControl.TabPages.Add(tabVat)

            ' Tab 2: Extracto de Conta Corrente
            Dim tabStatement = New TabPage("👥  Extracto de Conta Corrente")
            BuildStatementTab(tabStatement)
            tabControl.TabPages.Add(tabStatement)

            ' Tab 3: Valorização de Inventário
            Dim tabStock = New TabPage("📦  Valorização de Inventário")
            BuildStockTab(tabStock)
            tabControl.TabPages.Add(tabStock)
        End Sub

        Private Sub BuildVatTab(tab As TabPage)
            Dim top = New Panel With {.Dock = DockStyle.Top, .Height = 56, .Padding = New Padding(12), .BackColor = Color.White}
            Dim l1 = New Label With {.Text = "De:", .Location = New Point(15, 18), .AutoSize = True}
            dtpVatStart.Location = New Point(45, 15) : dtpVatStart.Format = DateTimePickerFormat.Short
            dtpVatStart.Value = New Date(Date.Today.Year, Date.Today.Month, 1)

            Dim l2 = New Label With {.Text = "Até:", .Location = New Point(165, 18), .AutoSize = True}
            dtpVatEnd.Location = New Point(195, 15) : dtpVatEnd.Format = DateTimePickerFormat.Short
            dtpVatEnd.Value = Date.Today

            btnFilterVat.Text = "Actualizar Mapa de IVA"
            btnFilterVat.Location = New Point(315, 13)
            btnFilterVat.Width = 170
            btnFilterVat.Height = 30
            btnFilterVat.BackColor = Color.FromArgb(0, 32, 96)
            btnFilterVat.ForeColor = Color.White
            btnFilterVat.FlatStyle = FlatStyle.Flat
            AddHandler btnFilterVat.Click, Sub() LoadVatMap()

            top.Controls.AddRange({l1, dtpVatStart, l2, dtpVatEnd, btnFilterVat})
            tab.Controls.Add(top)

            gridVat.Dock = DockStyle.Fill
            gridVat.ReadOnly = True
            gridVat.AllowUserToAddRows = False
            gridVat.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            gridVat.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            tab.Controls.Add(gridVat)
        End Sub

        Private Sub BuildStatementTab(tab As TabPage)
            Dim top = New Panel With {.Dock = DockStyle.Top, .Height = 56, .Padding = New Padding(12), .BackColor = Color.White}
            Dim l1 = New Label With {.Text = "Cliente:", .Location = New Point(15, 18), .AutoSize = True}
            cboCustomer.Location = New Point(70, 15) : cboCustomer.Width = 320 : cboCustomer.DropDownStyle = ComboBoxStyle.DropDownList

            btnFilterStatement.Text = "Ver Extracto"
            btnFilterStatement.Location = New Point(405, 13)
            btnFilterStatement.Width = 120
            btnFilterStatement.Height = 30
            btnFilterStatement.BackColor = Color.FromArgb(0, 32, 96)
            btnFilterStatement.ForeColor = Color.White
            btnFilterStatement.FlatStyle = FlatStyle.Flat
            AddHandler btnFilterStatement.Click, Sub() LoadStatement()

            top.Controls.AddRange({l1, cboCustomer, btnFilterStatement})
            tab.Controls.Add(top)

            gridStatement.Dock = DockStyle.Fill
            gridStatement.ReadOnly = True
            gridStatement.AllowUserToAddRows = False
            gridStatement.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            gridStatement.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            tab.Controls.Add(gridStatement)
        End Sub

        Private Sub BuildStockTab(tab As TabPage)
            Dim top = New Panel With {.Dock = DockStyle.Top, .Height = 56, .Padding = New Padding(12), .BackColor = Color.White}
            Dim title = New Label With {.Text = "Inventário Físico Valorizado", .Font = New Font("Segoe UI", 12, FontStyle.Bold), .Location = New Point(15, 16), .AutoSize = True}

            btnRefreshStock.Text = "Actualizar Inventário"
            btnRefreshStock.Location = New Point(280, 13)
            btnRefreshStock.Width = 160
            btnRefreshStock.Height = 30
            btnRefreshStock.BackColor = Color.FromArgb(0, 32, 96)
            btnRefreshStock.ForeColor = Color.White
            btnRefreshStock.FlatStyle = FlatStyle.Flat
            AddHandler btnRefreshStock.Click, Sub() LoadStockValuation()

            top.Controls.AddRange({title, btnRefreshStock})
            tab.Controls.Add(top)

            gridStock.Dock = DockStyle.Fill
            gridStock.ReadOnly = True
            gridStock.AllowUserToAddRows = False
            gridStock.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            gridStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            tab.Controls.Add(gridStock)
        End Sub

        Private Sub LoadInitialData()
            Try
                ' Load customers into combo
                Dim custs = _customerService.List(If(_session.CompanyId.HasValue, _session.CompanyId.Value, 0))
                cboCustomer.DataSource = custs
                cboCustomer.DisplayMember = "Name"
                cboCustomer.ValueMember = "Id"

                LoadVatMap()
                LoadStockValuation()
            Catch ex As Exception
                MessageBox.Show("Erro ao carregar dados iniciais: " & ex.Message, "ACCS ERP")
            End Try
        End Sub

        Private Sub LoadVatMap()
            Try
                Dim rows = _reportService.GetVatMap(If(_session.CompanyId.HasValue, _session.CompanyId.Value, 0), dtpVatStart.Value.Date, dtpVatEnd.Value.Date.AddDays(1).AddSeconds(-1))
                gridVat.DataSource = Nothing
                gridVat.DataSource = rows
            Catch ex As Exception
                MessageBox.Show("Erro ao carregar mapa de IVA: " & ex.Message, "ACCS ERP")
            End Try
        End Sub

        Private Sub LoadStatement()
            Try
                If cboCustomer.SelectedValue Is Nothing Then Return
                Dim customerId = Convert.ToInt32(cboCustomer.SelectedValue)
                Dim rows = _reportService.GetCustomerStatement(If(_session.CompanyId.HasValue, _session.CompanyId.Value, 0), customerId)
                gridStatement.DataSource = Nothing
                gridStatement.DataSource = rows
            Catch ex As Exception
                MessageBox.Show("Erro ao carregar extracto: " & ex.Message, "ACCS ERP")
            End Try
        End Sub

        Private Sub LoadStockValuation()
            Try
                Dim rows = _reportService.GetStockValuation(If(_session.CompanyId.HasValue, _session.CompanyId.Value, 0))
                gridStock.DataSource = Nothing
                gridStock.DataSource = rows
            Catch ex As Exception
                MessageBox.Show("Erro ao carregar valorização de inventário: " & ex.Message, "ACCS ERP")
            End Try
        End Sub
    End Class
End Namespace
