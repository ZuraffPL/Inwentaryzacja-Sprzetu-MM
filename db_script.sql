CREATE TABLE "Categories" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Code" TEXT NULL,
    "SortOrder" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);


CREATE TABLE "Departments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Departments" PRIMARY KEY AUTOINCREMENT,
    "Code" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedDate" TEXT NOT NULL
);


CREATE TABLE "Locations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Locations" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Code" TEXT NULL,
    "Address" TEXT NULL,
    "City" TEXT NULL,
    "PostalCode" TEXT NULL,
    "Description" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);


CREATE TABLE "Equipment" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Equipment" PRIMARY KEY AUTOINCREMENT,
    "InventoryNumber" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Brand" TEXT NULL,
    "Model" TEXT NULL,
    "SerialNumber" TEXT NULL,
    "IpAddress" TEXT NULL,
    "DepartmentId" INTEGER NULL,
    "LocationDetails" TEXT NULL,
    "PurchasePrice" decimal(18,2) NULL,
    "PurchaseDate" TEXT NULL,
    "WarrantyEndDate" TEXT NULL,
    "Status" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "LastModifiedDate" TEXT NULL,
    "CategoryId" INTEGER NOT NULL,
    "LocationId" INTEGER NOT NULL,
    CONSTRAINT "FK_Equipment_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Equipment_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Equipment_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ChangeLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ChangeLogs" PRIMARY KEY AUTOINCREMENT,
    "EquipmentId" INTEGER NOT NULL,
    "FieldName" TEXT NOT NULL,
    "OldValue" TEXT NULL,
    "NewValue" TEXT NULL,
    "ChangeDate" TEXT NOT NULL,
    "ChangedBy" TEXT NULL,
    "Reason" TEXT NULL,
    CONSTRAINT "FK_ChangeLogs_Equipment_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES "Equipment" ("Id") ON DELETE CASCADE
);


CREATE TABLE "InventoryEvents" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InventoryEvents" PRIMARY KEY AUTOINCREMENT,
    "EventType" INTEGER NOT NULL,
    "Description" TEXT NOT NULL,
    "EventDate" TEXT NOT NULL,
    "PerformedBy" TEXT NULL,
    "Notes" TEXT NULL,
    "CreatedDate" TEXT NOT NULL,
    "EquipmentId" INTEGER NULL,
    "LocationId" INTEGER NULL,
    CONSTRAINT "FK_InventoryEvents_Equipment_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES "Equipment" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_InventoryEvents_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE SET NULL
);


CREATE UNIQUE INDEX "IX_Categories_Code" ON "Categories" ("Code");


CREATE INDEX "IX_ChangeLogs_EquipmentId" ON "ChangeLogs" ("EquipmentId");


CREATE UNIQUE INDEX "IX_Departments_Code" ON "Departments" ("Code");


CREATE INDEX "IX_Equipment_CategoryId" ON "Equipment" ("CategoryId");


CREATE INDEX "IX_Equipment_DepartmentId" ON "Equipment" ("DepartmentId");


CREATE UNIQUE INDEX "IX_Equipment_InventoryNumber" ON "Equipment" ("InventoryNumber");


CREATE INDEX "IX_Equipment_LocationId" ON "Equipment" ("LocationId");


CREATE INDEX "IX_InventoryEvents_EquipmentId" ON "InventoryEvents" ("EquipmentId");


CREATE INDEX "IX_InventoryEvents_LocationId" ON "InventoryEvents" ("LocationId");


CREATE UNIQUE INDEX "IX_Locations_Code" ON "Locations" ("Code");


