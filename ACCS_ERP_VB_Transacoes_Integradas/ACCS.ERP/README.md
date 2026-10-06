# ACCS ERP Desktop — VB.NET (.NET 8 Windows Forms)

Migração completa do ACCS ERP PHP/MySQL para aplicação desktop nativa.

## Módulos Concluídos
- **Fase 1**: Autenticação e Segurança (Login com hash BCrypt compatível com PHP `password_hash`, leitura de `users`, sessões com multi-tenant `company_id`).
- **Fase 2**: Gestão de Clientes (`CustomersForm` / `CustomerService`), validação estrita de NUIT com 9 dígitos, colunas dinâmicas `loyalty_points`, `vip_discount`, `whatsapp` e conta corrente.
- **Fase 3**: Produtos & Gestão de Stock (`ProductsForm` / `ProductService` / `Product`):
  - Catálogo de artigos com SKU/Código de barras, Preço de Custo, Preço de Venda e IVA 16%.
  - Alerta de stock mínimo e ajuste rápido de inventário.
- **Fase 4**: Facturação e Emissão de Documentos (`InvoicesForm` / `InvoiceService` / `Invoice` / `InvoiceItem`):
  - Factura Comercial (FT), Venda a Dinheiro (FR), Factura Pró-forma (FP) e Recibo (RC).
  - Cálculo de IVA a 16%, retenção na fonte (IRPS/IRPC), abatimento de stock e conta corrente.
- **Fase 5**: Relatórios e Fecho Fiscal (`ReportsForm` / `ReportService` / `ReportModels`):
  - Mapa Periódico de IVA (16%) com discriminação da base de incidência e valor de imposto apurado para declaração M/20.
  - Extracto de Conta Corrente de Clientes com débitos, créditos e saldo progressivo.
  - Valorização de Inventário Físico (capital empatado a preço de custo vs retorno potencial a preço de venda e margem de lucro).

## Requisitos de Execução
- Windows 10 ou Windows 11
- .NET 8 Desktop Runtime ou SDK
- Base de dados MySQL / MariaDB (`accs_escritorio`)
- Ficheiro `appsettings.json` com a connection string configurada
