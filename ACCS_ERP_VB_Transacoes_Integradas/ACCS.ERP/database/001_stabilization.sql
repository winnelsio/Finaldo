-- ACCS ERP Desktop - estabilização da base
-- Faça backup antes de executar.

ALTER TABLE customers ADD COLUMN IF NOT EXISTS active TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE products ADD COLUMN IF NOT EXISTS active TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE companies ADD COLUMN IF NOT EXISTS active TINYINT(1) NOT NULL DEFAULT 1;

CREATE TABLE IF NOT EXISTS document_sequences (
  company_id INT NOT NULL,
  document_type VARCHAR(20) NOT NULL,
  fiscal_year INT NOT NULL,
  last_number INT NOT NULL DEFAULT 0,
  PRIMARY KEY (company_id, document_type, fiscal_year)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS fiscal_settings (
  company_id INT NOT NULL PRIMARY KEY,
  default_tax_rate DECIMAL(6,3) NOT NULL DEFAULT 0,
  currency_code VARCHAR(8) NOT NULL DEFAULT 'MZN',
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

INSERT IGNORE INTO fiscal_settings(company_id, default_tax_rate, currency_code)
SELECT id, 0, 'MZN' FROM companies;

-- Protecção adicional contra números de documento repetidos.
ALTER TABLE invoices ADD UNIQUE KEY uq_invoice_number (company_id, document_type, document_number);
