using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using System.Text.Json.Nodes;
using DealParser.AppConstants;
using DealParser.Config;
using DealParser.Services.DB_Services;
using DealParser.Services.DB_Services.Data_Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DealParser.Services.TimedServices
{
    /// <summary>
    /// Производит запуск периодического вытягивания данных из указанного сервиса
    /// </summary>
    public class TimedParserHostedService : BackgroundService
    {
        /// <summary> Актуальный конфиг программы </summary>
        private AppOptions ActualOptions => _optionsMonitor.CurrentValue;
        
        /// <summary> Сервис логгирования </summary>
        private readonly ILogger<TimedParserHostedService> _logger;
        
        /// <summary> Сервис отслеживает и обновляет конфиг  </summary>
        private readonly IOptionsMonitor<AppOptions> _optionsMonitor;
        
        /// <summary> Сервис базы данных </summary>
        private readonly MySqlDatabaseService _service;

        private Task _currentProcess;
        private Timer _timer;
        private int _iterationNumber = 1;
        
        
        public TimedParserHostedService(ILogger<TimedParserHostedService> logger,
            IOptionsMonitor<AppOptions> optionsMonitor,
            MySqlDatabaseService service)
        {
            _logger = logger;
            _optionsMonitor = optionsMonitor;
            _service = service;
        }

        /// <summary>
        /// Выполняется при запуске программы, парсит строку с расписанием в крон выражение
        /// и запускает процесс в соответствии с ним 
        /// </summary>
        /// <param name="stoppingToken"> Токен остановки работы </param>
        /// <returns> Процесс выполнения </returns>
        /// <exception cref="OptionsValidationException"> Некорректный формат настроек </exception>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Started BackupService");

            try
            {
                var cronString = ActualOptions.Schedule;
                var cronExpression = CronExpression.Parse(cronString);
                var nowDate = DateTime.UtcNow;

                var periodicity = cronExpression.GetNextOccurrence(nowDate) - nowDate;

                if (periodicity.HasValue)
                    _timer = new Timer(DoWork, cronExpression, TimeSpan.Zero, periodicity.Value);
                else
                {
                    throw new OptionsValidationException("Periodicity",
                        typeof(string),
                        new[]
                        {
                            "Can`t parse periodicity to cron expression"
                        });
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical("Critical error: {e.Message}", e.Message);
                Environment.Exit(AppConstant.IVALID_SETTINGS_FORMAT);
            }
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Выполняет основную работу, в случае, если предидущая задача завершила свое исполнения
        /// </summary>
        /// <param name="state"></param>
        private void DoWork(object state)
        {
            if (_currentProcess is null || _currentProcess.IsCompleted)
                _currentProcess = Task.Run(WorkProcess);
            else
                _logger.LogWarning("The previous copying process was not completed");
        }

        /// <summary>
        /// Производит запрос данных через <see cref="ClientHub"/>
        /// Далее результат JSON преобразуется в объект сделки и добавляется в бд
        /// </summary>
        /// <returns></returns>
        private Task WorkProcess()
        {
            _logger.LogInformation("Iteration number: {IterationNumber}",_iterationNumber++);
            try
            {
                // Запрашиваем общее количество записей
                var dealsCount = ClientHub.GetCountOfDeals();
                var chunkSize = ActualOptions.ChunkSize;
                
                // Согласно размеру чанка выполняем добавление в БД
                // В данном случае задержка между запросами "искусственная"
                // Поскольку она получается из-за задержки добавления новых записей в БД и парсинга
                for (var i = 0; i <= dealsCount; i += chunkSize)
                {
                    var str = ClientHub.GetNextPage(chunkSize, i / chunkSize);
                    var json = JsonNode.Parse(str);

                    var array = json.AsObject()["data"]["searchReportWoodDeal"]["content"].AsArray();
                
                    foreach (var record in array)
                    {
                        _service.Insert(new Deal(record));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.CompletedTask;
        }
        
    }
}