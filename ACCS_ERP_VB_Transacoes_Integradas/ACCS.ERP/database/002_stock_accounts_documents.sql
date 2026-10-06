-- ACCS ERP Desktop - movimentos de stock, conta corrente e documentos transaccionais
-- Execute DEPOIS de 001_stabilization.sql e faça backup antes.

ALTER TABLE invoices ADD COLUMN IF NOT EXISTS reference_invoice_id INT NULL;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS payment_method VARCHAR(30) NULL;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS created_by INT NULL;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;

CREATE TABLE IF NOT EXISTS stock_movements (
  id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,
  product_id INT NOT NULL,
  invoice_id INT NULL,
  document_type VARCHAR(20) NULL,
  document_number VARCHAR(60) NULL,
  movement_type VARCHAR(30) NOT NULL,
  quantity DECIMAL(18,3) NOT NULL,
  stock_before DECIMAL(18,3) NOT NULL,
  stock_after DECIMAL(18,3) NOT NULL,
  reason VARCHAR(255) NULL,
  created_by INT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX ix_stock_company_product (company_id, product_id),
  INDEX ix_stock_invoice (invoice_id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS customer_account_movements (
  id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,
  customer_id INT NOT NULL,
  invoice_id INT NULL,
  reference_invoice_id INT NULL,
  document_type VARCHAR(20) NULL,
  document_number VARCHAR(60) NULL,
  movement_type VARCHAR(30) NOT NULL,
  debit DECIMAL(18,2) NOT NULL DEFAULT 0,
  credit DECIMAL(18,2) NOT NULL DEFAULT 0,
  balance_after DECIMAL(18,2) NOT NULL DEFAULT 0,
  description VARCHAR(255) NULL,
  created_by INT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX ix_account_customer (company_id, customer_id),
  INDEX ix_account_invoice (invoice_id)
) ENGINE=InnoDB;
