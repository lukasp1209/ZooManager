
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ZooManager.Core.Interfaces;
using ZooManager.Core.Models;
using ZooManager.Infrastructure.Persistence.Data;
using ZooManager.Infrastructure.Persistence.Repositories;

namespace ZooManager.Infrastructure.Persistence.Connection
{
    public class SqlitePersistenceService : IPersistenceService
    {
        private readonly DatabaseConnectionManager _connectionManager;
        private readonly IDbSchema _dbSchema;
        private readonly ITestDataProvider _testDataProvider;

        // Repositories
        private readonly UserRepository _userRepository;
        private readonly AnimalRepository _animalRepository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly SpeciesRepository _speciesRepository;
        private readonly EnclosureRepository _enclosureRepository;
        private readonly EventRepository _eventRepository;

        public SqlitePersistenceService(
            string dbFileName = "zoo.db",
            IDbSchema dbSchema = null,
            ITestDataProvider testDataProvider = null)
        {
            if (dbFileName.Contains(";"))
                dbFileName = "zoo.db";

            var connectionString = $"Data Source={dbFileName}";
            _connectionManager = new DatabaseConnectionManager(connectionString);
            _dbSchema = dbSchema ?? new DbSchema();
            _testDataProvider = testDataProvider ?? new TestDataProvider();

            // Initialize repositories
            _userRepository = new UserRepository(_connectionManager);
            _animalRepository = new AnimalRepository(_connectionManager);
            _employeeRepository = new EmployeeRepository(_connectionManager);
            _speciesRepository = new SpeciesRepository(_connectionManager);
            _enclosureRepository = new EnclosureRepository(_connectionManager);
            _eventRepository = new EventRepository(_connectionManager);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            _connectionManager.ExecuteWithConnection(connection =>
            {
                EnableForeignKeys(connection);
                _dbSchema.CreateTables(connection);

                if (IsInitialSetup(connection))
                {
                    InsertTestData(connection);
                }
            });
        }

        private bool IsInitialSetup(SqliteConnection connection)
        {
            var count = new SqlCommandBuilder(connection)
                .WithCommandText("SELECT COUNT(*) FROM Species")
                .ExecuteScalar<long>();
            return count == 0;
        }

        private void InsertTestData(SqliteConnection connection)
        {
            DisableForeignKeys(connection);

            _testDataProvider.InsertSpecies(connection);
            _testDataProvider.InsertEnclosures(connection);
            _testDataProvider.InsertEmployees(connection);
            _testDataProvider.InsertAnimals(connection);
            _testDataProvider.InsertEmployeeQualifications(connection);
            _testDataProvider.InsertAnimalEvents(connection);
            _testDataProvider.InsertZooEvents(connection);
            _testDataProvider.InsertUsers(connection, GeneratePasswordHash);

            EnableForeignKeys(connection);
        }

        private void EnableForeignKeys(SqliteConnection connection)
        {
            new SqlCommandBuilder(connection)
                .WithCommandText("PRAGMA foreign_keys = ON;")
                .ExecuteNonQuery();
        }

        private void DisableForeignKeys(SqliteConnection connection)
        {
            new SqlCommandBuilder(connection)
                .WithCommandText("PRAGMA foreign_keys = OFF;")
                .ExecuteNonQuery();
        }

        private string GeneratePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(password + "ZooManagerSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // User operations
        public User GetUserByUsername(string username) => _userRepository.GetByUsername(username);
        public User GetUserById(int userId) => _userRepository.GetById(userId);
        public bool SaveUser(User user) => _userRepository.Save(user);

        // Animal operations
        public IEnumerable<Animal> LoadAnimals() => _animalRepository.GetAll();
        public IEnumerable<Animal> LoadAnimalsForEmployee(int employeeId) => _animalRepository.GetForEmployee(employeeId);
        public void SaveAnimals(IEnumerable<Animal> animals) => _animalRepository.Save(animals);
        public void DeleteAnimal(int animalId) => _animalRepository.Delete(animalId);
        public void AddAnimalEvent(int animalId, AnimalEvent ev) => _animalRepository.AddEvent(animalId, ev);
        public void DeleteAnimalEvent(int animalId, AnimalEvent ev) => _animalRepository.DeleteEvent(animalId, ev);
        public void UpdateAnimalEvent(int animalId, AnimalEvent oldEvent, AnimalEvent newEvent) => 
            _animalRepository.UpdateEvent(animalId, oldEvent, newEvent);

        // Employee operations
        public IEnumerable<Employee> LoadEmployees() => _employeeRepository.GetAll();
        public void SaveEmployees(IEnumerable<Employee> employees) => _employeeRepository.Save(employees);
        public void DeleteEmployee(int employeeId) => _employeeRepository.Delete(employeeId);
        public void SaveEmployeeQualifications(int employeeId, List<int> speciesIds) => 
            _employeeRepository.SaveQualifications(employeeId, speciesIds);

        // Species operations
        public IEnumerable<Species> LoadSpecies() => _speciesRepository.GetAll();
        public void SaveSpecies(IEnumerable<Species> speciesList) => _speciesRepository.Save(speciesList);
        public void DeleteSpecies(int speciesId) => _speciesRepository.Delete(speciesId);

        // Enclosure operations
        public IEnumerable<Enclosure> LoadEnclosures() => _enclosureRepository.GetAll();
        public void SaveEnclosures(IEnumerable<Enclosure> enclosures) => _enclosureRepository.Save(enclosures);
        public void DeleteEnclosure(int enclosureId) => _enclosureRepository.Delete(enclosureId);

        // Event operations
        public IEnumerable<ZooEvent> LoadEvents() => _eventRepository.GetAll();
        public void SaveEvents(IEnumerable<ZooEvent> events) => _eventRepository.Save(events);
        public void DeleteEvent(string title, DateTime start) => _eventRepository.Delete(title, start);
    }
}