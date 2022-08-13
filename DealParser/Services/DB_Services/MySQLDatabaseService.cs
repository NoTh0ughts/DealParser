using System;
using System.Globalization;
using DealParser.Config;
using DealParser.Services.DB_Services.Data_Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace DealParser.Services.DB_Services
{
    /// <summary>
    /// Реализует основную работу с базой данных в приложении
    /// Конфигурация подключения также берется из файла appsettings.json
    /// </summary>
    public class MySqlDatabaseService
    {
        private readonly IOptionsMonitor<AppOptions> _actualOptions;
        private string ConnectionString => _actualOptions.CurrentValue.ConnectionString;
        private readonly MySqlConnection _connection;
        private readonly ILogger<MySqlDatabaseService> _logger;
        
        public MySqlDatabaseService(IOptionsMonitor<AppOptions> actualOptions, ILogger<MySqlDatabaseService> logger)
        {
            _actualOptions = actualOptions;
            _logger = logger;

            _connection = new MySqlConnection(ConnectionString);
        }

        /// <summary>
        /// Открывает соединение с базой данных для взаимодействия
        /// </summary>
        /// <returns> Открыто ли соединение </returns>
        private bool OpenConnection()
        {
            try
            {
                _connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        _logger.LogError("Cant connect to server");
                        break;

                    case 1045:
                        _logger.LogError("Incorrect password or login to mysql");
                        break;
                }
                return false;
            }
        }

        
        /// <summary>
        /// Закрывает соединение с БД
        /// </summary>
        private void CloseConnection()
        {
            try
            {
                _connection.Close();
            }
            catch (MySqlException ex)
            {
                _logger.LogError("Cant close connection");
            }
        }

        /// <summary>
        /// Производит поиск сделок по идентификатору - номеру декларации
        /// </summary>
        /// <param name="number"> Номер декларации искомой сделки </param>
        /// <returns> Найденная информация по сделке или null </returns>
        private Deal FindDeal(string number)
        {
            var query = "select deal_number, volume_seller, volume_buyer, deal_date, seller_id, buyer_id, " + 
                                "s.name as sname, b.name as bname, s.inn as sinn, b.inn as binn " +
                                "from deal join buyer b on b.id = deal.buyer_id join seller s on s.id = deal.seller_id " +
                                $"where deal_number='{number}' limit 1";

            if (OpenConnection() == true)
            {
                var cmd = new MySqlCommand(query, _connection);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    if (!reader.HasRows)
                    {
                        CloseConnection();
                        return null;
                    }
                    
                    var deal = new Deal
                    {
                        Date = DateTime.Parse(reader["deal_date"]+""),
                        Number = reader["deal_number"]+"",
                        WoodVolumeBuyer = decimal.Parse(reader["volume_buyer"]+""),
                        WoodVolumeSeller = decimal.Parse(reader["volume_seller"]+""),
                        Buyer = new Buyer
                        {
                            Id = int.Parse(reader["buyer_id"]+""),
                            Inn = reader["binn"]+"",
                            Name = reader["bname"]+""
                        },
                        Seller = new Seller
                        {
                            Id = int.Parse(reader["seller_id"]+""),
                            Inn = reader["sinn"]+"",
                            Name = reader["sname"]+""
                        }
                    };
                    CloseConnection();
                    return deal;
                }
                
            }
            return null;
        }

        /// <summary>
        /// Производит поиск покупателя по его названию (Для юр лиц)
        /// </summary>
        /// <param name="name"> Имя лица </param>
        /// <returns> Найденная информация или null </returns>
        private Buyer FindBuyer(string name)
        {
            var query = $"select * from buyer where name='{name}'".Replace("'", "\'");
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                var reader = cmd.ExecuteReader();

                reader.Read();
                CloseConnection();
                if (reader.HasRows)
                {
                    return new Buyer
                    {
                        Id = int.Parse(reader["id"]+""),
                        Inn = reader["inn"] + "",
                        Name = reader["name"] + ""
                    };
                }
            }

            return null;
        }
        
        /// <summary>
        /// Производит поиск продавца по его названию (Для юр лиц)
        /// </summary>
        /// <param name="name"> Имя лица </param>
        /// <returns> Найденная информация или null </returns>
        private Seller FindSeller(string name)
        {
            var query = $"select * from seller where name='{name}'";
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                var reader = cmd.ExecuteReader();

                reader.Read();
                CloseConnection();
                if (reader.HasRows)
                {
                    return new Seller
                    {
                        Id = int.Parse(reader["id"]+""),
                        Inn = reader["inn"] + "",
                        Name = reader["name"] + ""
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Производит сравнение записей, на наличие таких-же продавцов
        /// и необходимости обновления данных существуещего, при наличии
        /// </summary>
        /// <param name="seller"> Запись к добавлению </param>
        /// <returns> Идентификатор записи или -1 </returns>
        private long InsertSeller(Seller seller)
        {
            var existing = FindSeller(seller.Name);
            // Существующая запись актуальна
            if (existing == seller) return existing.Id;
            
            // Запись не актуально и необходимо обновить
            if (existing is null == false)
            {
                var updateQuery = $"UPDATE seller set inn='{seller.Inn}' where id = '{seller.Id}';";
                if (OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(updateQuery, _connection);
                    cmd.ExecuteNonQuery();
                    CloseConnection();
                    return existing.Id;
                }
                
                return -1;
            }
            
            // Запись отсутствует в бд, производиться добавление
            var insertQuery = $"INSERT seller(name, inn) values('{seller.Name}','{seller.Inn}')";
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(insertQuery, _connection);
                cmd.ExecuteNonQuery();
                
                CloseConnection();
                return cmd.LastInsertedId;
            }
            
            return -1;
        }

        /// <summary>
        /// Производит сравнение записей, на наличие таких-же покупателей
        /// и необходимости обновления данных существуещего, при наличии
        /// </summary>
        /// <param name="seller"> Запись к добавлению </param>
        /// <returns> Идентификатор записи или -1 </returns>
        private long InsertBuyer(Buyer buyer)
        {
            var existing = FindBuyer(buyer.Name);
            // Существующая запись актуальна
            if (existing == buyer) return existing.Id;
            
            // Запись не актуально и необходимо обновить
            if (existing is null == false)
            {
                var updateQuery = $"UPDATE buyer set inn='{buyer.Inn}' where id = '{buyer.Id}'";
                if (OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(updateQuery, _connection);
                    cmd.ExecuteNonQuery();
                    CloseConnection();
                    return existing.Id;
                }
                
                // не удалось обновить данные
                _logger.LogError("Cant update buyer with id {Buyer.Id}",buyer.Id);
                return -1;
            }
            
            // Запись отсутствует в бд, производиться добавление
            var insertQuery = $"INSERT buyer(name, inn) values('{buyer.Name}','{buyer.Inn}')";
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(insertQuery, _connection);
                cmd.ExecuteNonQuery();
                CloseConnection();
                return cmd.LastInsertedId;
            }

            // не удалось вставить данные
            _logger.LogError("Cant insert buyer with name {Buyer.Name}",buyer.Name);
            return -1;
        }

        public void Insert(Deal deal)
        {
            var existingDeal = FindDeal(deal.Number);
            // Сделка в бд актуальна?
            if (deal.Equals(existingDeal)) return;
            
            // Добавляем / обновляем данные покупателей и продавцов
            var buyerId = InsertBuyer(new Buyer {Inn = deal.Buyer.Inn, Name = deal.Buyer.Name});
            var sellerId = InsertSeller(new Seller {Inn = deal.Seller.Inn, Name = deal.Seller.Name});
    
            
            if (buyerId == -1 || sellerId == -1)
            {
                _logger.LogError("Cant create or update buyer/seller records");
                return;
            }
            
            // Существует такая сделка - необходимо обновить
            if (existingDeal is null == false)
            {
                var updateQuery = $"UPDATE deal set deal_date='{deal.Date.ToShortDateString()}', " + 
                                              $"volume_buyer='{deal.WoodVolumeBuyer.ToString(CultureInfo.GetCultureInfo("en-GB"))}', " + 
                                              $"volume_seller='{deal.WoodVolumeSeller.ToString(CultureInfo.GetCultureInfo("en-GB"))}', " + 
                                              $"seller_id = '{sellerId}', " +
                                              $"buyer_id = '{buyerId}' " +
                                              $"where deal_number = '{deal.Number}'";
                
                if (OpenConnection() == true)
                {
                    MySqlCommand cmd = new MySqlCommand(updateQuery, _connection);
                    cmd.ExecuteNonQuery();
                    CloseConnection();
                }
                return;
            }
            
            // Добавляем новую сделку в бд
            var insertQuery = $"INSERT deal (deal_date, volume_buyer, volume_seller,seller_id,buyer_id, deal_number) " +
                                      $"values(" +
                                          $"'{deal.Date.ToShortDateString()}'," +
                                          $"'{deal.WoodVolumeBuyer.ToString(CultureInfo.GetCultureInfo("en-GB"))}'," +
                                          $"'{deal.WoodVolumeSeller.ToString(CultureInfo.GetCultureInfo("en-GB"))}'," +
                                          $"'{sellerId}'," +
                                          $"'{buyerId}'," +
                                          $"'{deal.Number}');";
            
            if (OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(insertQuery, _connection);
                cmd.ExecuteNonQuery();
                CloseConnection();
            }
        }
    }
}