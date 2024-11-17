CREATE TABLE "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL,
    "Price" DECIMAL(10, 2) NOT NULL
);

INSERT INTO "Products" ("Id", "Name", "Price") VALUES
(1, 'Laptop', 1200.0),
(2, 'Smartphone', 800.0),
(3, 'Tablet', 400.0);