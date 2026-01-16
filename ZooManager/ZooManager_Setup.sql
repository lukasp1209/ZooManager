CREATE DATABASE IF NOT EXISTS ZooManagerDB;
USE ZooManagerDB;

CREATE TABLE IF NOT EXISTS Species (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    RequiredClimate VARCHAR(50),
    NeedsWater BOOLEAN DEFAULT FALSE,
    MinSpacePerAnimal DOUBLE DEFAULT 0
);

CREATE TABLE IF NOT EXISTS SpeciesFieldDefinitions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SpeciesId INT,
    FieldName VARCHAR(100) NOT NULL,
    DataType VARCHAR(50) NOT NULL,
    IsRequired BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (SpeciesId) REFERENCES Species(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Enclosures (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    ClimateType VARCHAR(50),
    HasWaterAccess BOOLEAN DEFAULT FALSE,
    TotalArea DOUBLE NOT NULL,
    MaxCapacity INT NOT NULL
);

CREATE TABLE IF NOT EXISTS Animals (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    SpeciesId INT,
    EnclosureId INT,
    NextFeedingTime DATETIME,
    FOREIGN KEY (SpeciesId) REFERENCES Species(Id),
    FOREIGN KEY (EnclosureId) REFERENCES Enclosures(Id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS AnimalAttributes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    AnimalId INT,
    FieldDefinitionId INT,
    ValueText TEXT,
    FOREIGN KEY (AnimalId) REFERENCES Animals(Id) ON DELETE CASCADE,
    FOREIGN KEY (FieldDefinitionId) REFERENCES SpeciesFieldDefinitions(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AnimalEvents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    AnimalId INT,
    EventDate DATETIME NOT NULL,
    EventType VARCHAR(100),
    Description TEXT,
    FOREIGN KEY (AnimalId) REFERENCES Animals(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ZooEvents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    Start DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS Employees (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FirstName VARCHAR(100),
    LastName VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS EmployeeQualifications (
    EmployeeId INT,
    SpeciesId INT,
    PRIMARY KEY (EmployeeId, SpeciesId),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE,
    FOREIGN KEY (SpeciesId) REFERENCES Species(Id) ON DELETE CASCADE
);

INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES ('Löwe', 'Trocken', FALSE, 50.0);
INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES ('Pinguin', 'Polar', TRUE, 10.0);

INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES ('Savanne A1', 'Trocken', TRUE, 500.0, 5);
INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES ('Eishalle', 'Polar', TRUE, 200.0, 20);

INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES ('Simba', 1, 1, NOW());
INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES ('Pingu', 2, 2, NOW());

INSERT INTO Employees (FirstName, LastName) VALUES ('Max', 'Mustermann');
INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES (1, 1);