# ACCS ERP Desktop — Documentos, Stock e Conta Corrente

## O que foi implementado
- Formulário real de emissão: FT, FR, FP (Cotação/Proforma) e RC.
- Numeração transaccional por empresa/tipo/ano.
- Emissão atómica com COMMIT/ROLLBACK.
- Bloqueio de stock e saldo de cliente com `SELECT ... FOR UPDATE`.
- Histórico em `stock_movements`.
- Histórico em `customer_account_movements`.
- Validação de stock insuficiente antes da confirmação.
- IVA/taxa fiscal deixou de estar fixo em 16% no núcleo; produtos/documentos usam taxa configurada.
- Extracto de cliente passa a ler os movimentos reais da conta corrente.

## Regras implementadas
- **FP — Cotação/Proforma:** grava documento e itens; não baixa stock; não altera conta corrente.
- **FT — Factura:** grava documento e itens; baixa stock; lança débito na conta corrente.
- **FR — Factura/Recibo:** grava documento e itens; baixa stock; regista débito + liquidação imediata; saldo líquido do cliente não aumenta.
- **RC — Recibo:** não baixa stock; lança crédito na conta corrente; impede recebimento acima do saldo em dívida.

## Antes de testar
1. Faça backup do MySQL.
2. Execute `database/001_stabilization.sql` (se ainda não executou).
3. Execute `database/002_stock_accounts_documents.sql`.
4. Configure `fiscal_settings.default_tax_rate` para a taxa aplicável à empresa, se necessário.
5. Confirme a connection string em `appsettings.json`.

## Teste recomendado
1. Escolha um produto com stock conhecido (ex.: 10).
2. Emita FP de 2 unidades: stock deve continuar 10 e saldo do cliente não muda.
3. Emita FT de 2 unidades: stock deve ir para 8 e saldo aumenta pelo total.
4. Emita RC para parte da dívida: stock continua 8 e saldo reduz pelo valor recebido.
5. Emita FR de 1 unidade: stock vai para 7 e o saldo anterior permanece igual (venda paga no acto).
6. Tente FT com quantidade superior ao stock: toda a operação deve falhar e nenhum cabeçalho/item/movimento deve permanecer gravado.

## Nota
O ambiente de geração não dispõe do SDK `dotnet`; por isso foi feita validação estrutural/estática, mas a compilação final deve ser executada no Visual Studio/.NET 8 no computador de desenvolvimento.
