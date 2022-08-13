using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using DealParser.AppConstants;
using DealParser.Config;
using DealParser.Services.DB_Services;
using DealParser.Services.TimedServices;

namespace DealParser
{
    /// <summary>
    /// Программа производит запросы типа GraphQL на получение данных
    /// расписание обновления осуществляется по расписанию заданому в appsettings.json
    /// </summary>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await new HostBuilder()
                //Добавление конфигурационного файла
                .ConfigureAppConfiguration((ctx,builder) =>
                {
                    try
                    {
                        builder.AddJsonFile(AppConstant.APP_SETTINGS_FILENAME, false, true);
                    }
                    catch (FileNotFoundException e)
                    {
                        Environment.Exit(AppConstant.SETTINGS_FILE_NOT_FOUND);
                    }
                }) 
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    // Добавляем логгер, конфиг в DI, и сервисы парсинга и базы данных
                    services.AddLogging();
                    services.AddOptions<AppOptions>()
                        .Bind(hostBuilderContext.Configuration.GetSection(AppOptions.SectionName))
                        .ValidateDataAnnotations(); 
                    services.AddSingleton<MySqlDatabaseService>();
                    services.AddHostedService<TimedParserHostedService>();
                })
                .ConfigureLogging((_, config) =>
                {
                    config.AddConsole();
                    config.AddDebug();
                }).RunConsoleAsync();
        }
    }
}