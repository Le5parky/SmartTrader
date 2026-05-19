using Autofac;
using CryptoTrader.Application.Interfaces;
using CryptoTrader.Application.Services;
using CryptoTrader.Application.Signals;
using CryptoTrader.Domain.Interfaces;
using CryptoTrader.Domain.Parsers;
using CryptoTrader.Infrastructure.ByBit;
using CryptoTrader.Infrastructure.Persistence.Repositories;
using CryptoTrader.Infrastructure.Services;
using CryptoTrader.Telegram.Services;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using CryptoTrader.Events;
using CryptoTrader.JobController.Services;

namespace CryptoTrader.DiExtensions.Autofac;

public class MainModule : Module
{
    private readonly IConfiguration _configuration;

    public MainModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Events
        builder.RegisterType<EventAggregator>().As<IEventAggregator>().SingleInstance();

        // Domain
        builder.RegisterType<CatMassageParser>().As<IMessageParser>().SingleInstance();
        builder.RegisterType<NewSignalParser>().As<IMessageParser>().SingleInstance();
        builder.RegisterType<MessageParserProcessor>().AsSelf().SingleInstance();

        // Application
        builder.RegisterType<CryptoTraderService>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<OrderService>().As<IOrderService>().SingleInstance();
        builder.RegisterType<MonitoringQueue>().AsSelf().AsImplementedInterfaces().SingleInstance();

        // Infrastructure
        builder.RegisterType<TradingService>().As<ITradingService>().InstancePerLifetimeScope();
        builder.RegisterType<DealMonitoringService>().As<IDealMonitoringService>().AsSelf().SingleInstance();
        builder.RegisterType<PositionStateRepository>().As<IPositionStateRepository>().SingleInstance();
        builder.RegisterType<PositionStateRecoveryHostedService>().AsImplementedInterfaces().SingleInstance();

        // Telegram
        builder.RegisterType<UpdateHandler>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<TelegramAlertService>().As<ITelegramAlertService>().SingleInstance();
        builder.RegisterType<TelegramNotificationHostedService>().AsImplementedInterfaces().SingleInstance();
        builder.RegisterType<TelegramBotHostedService>().AsImplementedInterfaces().SingleInstance();
        builder.RegisterType<JobBackgroundService>().AsImplementedInterfaces().SingleInstance();

        var botToken = _configuration["Telegram:botKey"];
        builder.RegisterInstance(new TelegramBotClient(botToken ?? "")).As<ITelegramBotClient>().SingleInstance();
    }
}
