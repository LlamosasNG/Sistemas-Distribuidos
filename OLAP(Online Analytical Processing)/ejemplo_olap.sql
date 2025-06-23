-- (c) Carlos Pineda Guerrero. 2024

create table sales (
  sales decimal(10,2),
  order_date date,
  product varchar(256),
  customer varchar(256),
  country varchar(256),
  employee varchar(256),
  category varchar(256)
);

-- Tablas de dimensiones:

CREATE TABLE country (
  id_country int NOT NULL AUTO_INCREMENT,
  country varchar(256) DEFAULT NULL,
  PRIMARY KEY (id_country)
);

CREATE TABLE customer (
  id_customer int NOT NULL AUTO_INCREMENT,
  customer varchar(256) DEFAULT NULL,
  PRIMARY KEY (id_customer)
);

CREATE TABLE employee (
  id_employee int NOT NULL AUTO_INCREMENT,
  employee varchar(256) DEFAULT NULL,
  PRIMARY KEY (id_employee)
);

CREATE TABLE order_date (
  id_order_date int NOT NULL AUTO_INCREMENT,
  order_date date DEFAULT NULL,
  PRIMARY KEY (id_order_date)
);

CREATE TABLE category (
  id_category int NOT NULL AUTO_INCREMENT,
  category varchar(256) DEFAULT NULL,
  PRIMARY KEY (id_category)
);

CREATE TABLE product (
  id_product int NOT NULL AUTO_INCREMENT,
  product varchar(256) DEFAULT NULL,
  PRIMARY KEY (id_product)
);

-- Fact table:

CREATE TABLE fact_table (
  sales decimal(10,2) DEFAULT NULL,
  id_order_date int DEFAULT NULL,
  id_product int DEFAULT NULL,
  id_customer int DEFAULT NULL,
  id_country int DEFAULT NULL,
  id_employee int DEFAULT NULL,
  id_category int DEFAULT NULL,
  KEY id_order_date (id_order_date),
  KEY id_product (id_product),
  KEY id_customer (id_customer),
  KEY id_country (id_country),
  KEY id_employee (id_employee),
  KEY id_category (id_category),
  FOREIGN KEY (id_order_date) REFERENCES order_date (id_order_date),
  FOREIGN KEY (id_product) REFERENCES product (id_product),
  FOREIGN KEY (id_customer) REFERENCES customer (id_customer),
  FOREIGN KEY (id_country) REFERENCES country (id_country),
  FOREIGN KEY (id_employee) REFERENCES employee (id_employee),
  FOREIGN KEY (id_category) REFERENCES category (id_category)
);
